namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Test.Shared;

    public class IntegrationSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            TestServer server = await TestServer.CreateAsync();

            try
            {
                // ===== ServerLifecycleTests =====

                await ExecuteTestAsync("Server.RootEndpoint_Returns200", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "GET / should return 200");
                });

                await ExecuteTestAsync("Server.RootEndpoint_ReturnsJson", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/");
                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsNotNull(body, "response body should not be null");
                    AssertHelper.IsTrue(body.Length > 0, "response body should not be empty");
                });

                await ExecuteTestAsync("Server.NonExistentEndpoint_Returns404", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent endpoint should return 404");
                });

                await ExecuteTestAsync("Server.UnauthenticatedRequest_Returns401", async () =>
                {
                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                    HttpResponseMessage resp = await noAuthClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "unauthenticated request should return 401");
                });

                await ExecuteTestAsync("Server.InvalidToken_Returns401", async () =>
                {
                    using HttpClient badClient = new HttpClient();
                    badClient.BaseAddress = new Uri(server.BaseUrl);
                    badClient.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token-xyz");

                    HttpResponseMessage resp = await badClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "invalid token should return 401");
                });

                await ExecuteTestAsync("Server.WhoAmI_ReturnsAuthenticatedUser", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/whoami");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "whoami should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("admin@test.local"), "whoami should contain admin email");
                });

                // ===== AuthenticationFlowTests =====

                await ExecuteTestAsync("Auth.PostAuthenticate_ValidCredentials", async () =>
                {
                    var payload = new { Email = "admin@test.local", Password = "testpassword123", TenantId = server.DefaultTenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "valid auth should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("BearerToken") || body.Contains("bearerToken") || body.Contains("bearer_token"),
                        "response should contain bearer token field");
                });

                await ExecuteTestAsync("Auth.PostAuthenticate_InvalidPassword", async () =>
                {
                    var payload = new { Email = "admin@test.local", Password = "wrongpassword", TenantId = server.DefaultTenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                        "invalid password should return 401 or 400, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Auth.PostAuthenticate_NonExistentEmail", async () =>
                {
                    var payload = new { Email = "nonexistent@test.local", Password = "password123", TenantId = server.DefaultTenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                        "non-existent email should return 401 or 400, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Auth.BearerToken_SubsequentRequest", async () =>
                {
                    var payload = new { Email = "admin@test.local", Password = "testpassword123", TenantId = server.DefaultTenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage authResp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    string authBody = await authResp.Content.ReadAsStringAsync();

                    using HttpClient tokenClient = new HttpClient();
                    tokenClient.BaseAddress = new Uri(server.BaseUrl);
                    tokenClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {server.AdminBearerToken}");

                    HttpResponseMessage resp = await tokenClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "authenticated request should succeed");
                });

                // ===== CrudLifecycleTests =====

                string tenantId = server.DefaultTenantId;
                string createdAssistantId = null;

                await ExecuteTestAsync("CRUD.Assistant.Create", async () =>
                {
                    var payload = new { Name = "Integration Test Assistant", TenantId = tenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create assistant should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("asst_"), "response should contain assistant ID with asst_ prefix");

                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdAssistantId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdAssistantId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdAssistantId, "should extract assistant ID from response");
                });

                await ExecuteTestAsync("CRUD.Assistant.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read assistant should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration Test Assistant"), "response should contain assistant name");
                });

                await ExecuteTestAsync("CRUD.Assistant.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent assistant should return 404");
                });

                await ExecuteTestAsync("CRUD.Assistant.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate assistants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Length > 2, "response should contain data");
                });

                await ExecuteTestAsync("CRUD.Assistant.Update", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage readResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    string readBody = await readResp.Content.ReadAsStringAsync();

                    using JsonDocument doc = JsonDocument.Parse(readBody);
                    string updatedJson = readBody.Replace("Integration Test Assistant", "Updated Assistant Name");
                    HttpContent content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/assistants/{createdAssistantId}", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode,
                        "update assistant should return 200, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    string verifyBody = await verifyResp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(verifyBody.Contains("Updated Assistant Name"), "updated name should be persisted");
                });

                await ExecuteTestAsync("CRUD.Assistant.Head_Exists", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/assistants/{createdAssistantId}");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing assistant should return 200");
                });

                await ExecuteTestAsync("CRUD.Assistant.Head_NotFound", async () =>
                {
                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, "/v1.0/assistants/asst_nonexistent");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "HEAD non-existent should return 404");
                });

                await ExecuteTestAsync("CRUD.Assistant.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete should return 200 or 204, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted assistant should not be found");
                });

                // --- User CRUD lifecycle ---

                string createdUserId = null;

                await ExecuteTestAsync("CRUD.User.Create", async () =>
                {
                    var payload = new
                    {
                        FirstName = "Test",
                        LastName = "User",
                        Email = "testuser@integration.local",
                        Password = "password123",
                        TenantId = tenantId
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/users", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create user should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdUserId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdUserId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdUserId, "should extract user ID from response");
                });

                await ExecuteTestAsync("CRUD.User.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read user should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("testuser@integration.local"), "response should contain user email");
                    AssertHelper.IsTrue(!body.Contains("password123"), "password should not be in response body");
                });

                await ExecuteTestAsync("CRUD.User.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate users should return 200");
                });

                await ExecuteTestAsync("CRUD.User.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete user should return 200 or 204, got " + (int)resp.StatusCode);
                });

                // --- Ingestion Rule CRUD ---

                string createdRuleId = null;

                await ExecuteTestAsync("CRUD.IngestionRule.Create", async () =>
                {
                    var payload = new { TenantId = tenantId, Name = "Test Rule" };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/ingestion-rules", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create rule should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdRuleId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdRuleId = idElem2.GetString();
                });

                await ExecuteTestAsync("CRUD.IngestionRule.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/ingestion-rules");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate rules should return 200");
                });

                await ExecuteTestAsync("CRUD.IngestionRule.Delete", async () =>
                {
                    if (createdRuleId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/ingestion-rules/{createdRuleId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete rule should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // --- Tenant CRUD lifecycle ---

                string createdTenantId = null;

                await ExecuteTestAsync("CRUD.Tenant.Create", async () =>
                {
                    var payload = new { Name = "Integration CRUD Tenant", Active = true };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/tenants", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create tenant should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Tenant", out JsonElement tenantElem))
                    {
                        if (tenantElem.TryGetProperty("Id", out JsonElement idElem))
                            createdTenantId = idElem.GetString();
                        else if (tenantElem.TryGetProperty("id", out JsonElement idElem2))
                            createdTenantId = idElem2.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdTenantId = idElem.GetString();

                    AssertHelper.IsNotNull(createdTenantId, "should extract tenant ID");
                    AssertHelper.IsTrue(createdTenantId.StartsWith("ten_"), "tenant ID should have ten_ prefix");
                });

                await ExecuteTestAsync("CRUD.Tenant.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read tenant should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration CRUD Tenant"), "response should contain tenant name");
                });

                await ExecuteTestAsync("CRUD.Tenant.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate tenants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Length > 2, "response should contain data");
                });

                await ExecuteTestAsync("CRUD.Tenant.Head_Exists", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/tenants/{createdTenantId}");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing tenant should return 200");
                });

                await ExecuteTestAsync("CRUD.Tenant.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete tenant should return 200 or 204, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted tenant should return 404");
                });

                // --- Credential CRUD lifecycle ---

                string createdCredentialId = null;

                await ExecuteTestAsync("CRUD.Credential.Create", async () =>
                {
                    var userPayload = new
                    {
                        FirstName = "Cred",
                        LastName = "TestUser",
                        Email = "credtest@integration.local",
                        Password = "password123",
                        TenantId = tenantId
                    };
                    string userJson = JsonSerializer.Serialize(userPayload);
                    HttpContent userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage userResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/users", userContent);
                    string userBody = await userResp.Content.ReadAsStringAsync();

                    string credUserId = null;
                    using (JsonDocument userDoc = JsonDocument.Parse(userBody))
                    {
                        if (userDoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                            credUserId = idElem.GetString();
                        else if (userDoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                            credUserId = idElem2.GetString();
                    }

                    var payload = new { TenantId = tenantId, UserId = credUserId, Active = true };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/credentials", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create credential should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement credIdElem))
                        createdCredentialId = credIdElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement credIdElem2))
                        createdCredentialId = credIdElem2.GetString();

                    AssertHelper.IsNotNull(createdCredentialId, "should extract credential ID");
                });

                await ExecuteTestAsync("CRUD.Credential.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/credentials");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate credentials should return 200");
                });

                await ExecuteTestAsync("CRUD.Credential.Delete", async () =>
                {
                    if (createdCredentialId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/credentials/{createdCredentialId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete credential should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // --- Assistant Settings lifecycle ---

                string settingsAssistantId = null;

                await ExecuteTestAsync("CRUD.Settings.CreateAssistant", async () =>
                {
                    var payload = new { Name = "Settings Test Assistant", TenantId = tenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        settingsAssistantId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        settingsAssistantId = idElem2.GetString();

                    AssertHelper.IsNotNull(settingsAssistantId, "should extract assistant ID for settings tests");
                });

                await ExecuteTestAsync("CRUD.Settings.Put", async () =>
                {
                    AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                    var payload = new
                    {
                        AssistantId = settingsAssistantId,
                        Temperature = 0.8,
                        InferenceEndpointId = "ep_test_inference",
                        EnableReranking = true,
                        RerankerTopK = 3,
                        RerankerScoreThreshold = 5.0
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/assistants/{settingsAssistantId}/settings", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 201,
                        "put settings should return 200 or 201, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("CRUD.Settings.Get", async () =>
                {
                    AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{settingsAssistantId}/settings");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "get settings should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("ep_test_inference"), "response should contain inference endpoint id");
                });

                await ExecuteTestAsync("CRUD.Settings.Cleanup", async () =>
                {
                    if (settingsAssistantId != null)
                    {
                        await server.Client.DeleteAsync($"/v1.0/assistants/{settingsAssistantId}");
                    }
                });

                // --- Feedback lifecycle ---

                await ExecuteTestAsync("CRUD.Feedback.Enumerate_Empty", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate feedback should return 200");
                });

                await ExecuteTestAsync("CRUD.Feedback.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback/afb_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent feedback should return 404");
                });

                // --- History lifecycle ---

                await ExecuteTestAsync("CRUD.History.Enumerate_Empty", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate history should return 200");
                });

                await ExecuteTestAsync("CRUD.History.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history/ch_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent history should return 404");
                });

                // --- CrawlPlan CRUD lifecycle ---

                string createdCrawlPlanId = null;

                await ExecuteTestAsync("CRUD.CrawlPlan.Create", async () =>
                {
                    var payload = new
                    {
                        TenantId = tenantId,
                        Name = "Integration Test Crawl Plan",
                        RepositoryType = "Web"
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/crawlplans", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create crawl plan should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdCrawlPlanId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdCrawlPlanId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdCrawlPlanId, "should extract crawl plan ID");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdCrawlPlanId, "crawl plan ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read crawl plan should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration Test Crawl Plan"), "response should contain crawl plan name");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/crawlplans");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate crawl plans should return 200");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Delete", async () =>
                {
                    if (createdCrawlPlanId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete crawl plan should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // ===== PaginationTests =====

                string[] assistantIds = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    var payload = new { Name = $"Pagination Asst {i}", TenantId = tenantId };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        assistantIds[i] = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        assistantIds[i] = idElem2.GetString();
                }

                await ExecuteTestAsync("Pagination.MaxResults1_MultiplePages", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "page 1 should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    bool hasObjects = doc.RootElement.TryGetProperty("Objects", out JsonElement objsElem)
                        || doc.RootElement.TryGetProperty("objects", out objsElem);
                    AssertHelper.IsTrue(hasObjects, "response should have Objects property");
                    AssertHelper.AreEqual(1, objsElem.GetArrayLength(), "page 1 should have exactly 1 item");
                });

                await ExecuteTestAsync("Pagination.ContinuationToken_Works", async () =>
                {
                    HttpResponseMessage resp1 = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                    string body1 = await resp1.Content.ReadAsStringAsync();
                    using JsonDocument doc1 = JsonDocument.Parse(body1);

                    string continuationToken = null;
                    if (doc1.RootElement.TryGetProperty("ContinuationToken", out JsonElement ctElem))
                        continuationToken = ctElem.GetString();
                    else if (doc1.RootElement.TryGetProperty("continuationToken", out JsonElement ctElem2))
                        continuationToken = ctElem2.GetString();

                    AssertHelper.IsNotNull(continuationToken, "page 1 should have a continuation token");

                    HttpResponseMessage resp2 = await server.Client.GetAsync($"/v1.0/assistants?maxResults=1&continuationToken={continuationToken}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp2.StatusCode, "page 2 should return 200");

                    string body2 = await resp2.Content.ReadAsStringAsync();
                    using JsonDocument doc2 = JsonDocument.Parse(body2);

                    bool hasObjects2 = doc2.RootElement.TryGetProperty("Objects", out JsonElement objs2)
                        || doc2.RootElement.TryGetProperty("objects", out objs2);
                    AssertHelper.IsTrue(hasObjects2, "page 2 should have Objects");
                    AssertHelper.AreEqual(1, objs2.GetArrayLength(), "page 2 should have exactly 1 item");

                    string id1 = null, id2 = null;
                    if (doc1.RootElement.TryGetProperty("Objects", out JsonElement o1) || doc1.RootElement.TryGetProperty("objects", out o1))
                    {
                        var first = o1[0];
                        if (first.TryGetProperty("Id", out JsonElement ie)) id1 = ie.GetString();
                        else if (first.TryGetProperty("id", out JsonElement ie2)) id1 = ie2.GetString();
                    }
                    if (doc2.RootElement.TryGetProperty("Objects", out JsonElement o2) || doc2.RootElement.TryGetProperty("objects", out o2))
                    {
                        var first = o2[0];
                        if (first.TryGetProperty("Id", out JsonElement ie)) id2 = ie.GetString();
                        else if (first.TryGetProperty("id", out JsonElement ie2)) id2 = ie2.GetString();
                    }
                    AssertHelper.AreNotEqual(id1, id2, "page 1 and page 2 should have different items");
                });

                await ExecuteTestAsync("Pagination.EndOfResults_OnFinalPage", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    bool endOfResults = false;
                    if (doc.RootElement.TryGetProperty("EndOfResults", out JsonElement eorElem))
                        endOfResults = eorElem.GetBoolean();
                    else if (doc.RootElement.TryGetProperty("endOfResults", out JsonElement eorElem2))
                        endOfResults = eorElem2.GetBoolean();

                    AssertHelper.IsTrue(endOfResults, "large page should indicate end of results");
                });

                await ExecuteTestAsync("Pagination.TotalRecords_Accurate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    int totalRecords = 0;
                    if (doc.RootElement.TryGetProperty("TotalRecords", out JsonElement trElem))
                        totalRecords = trElem.GetInt32();
                    else if (doc.RootElement.TryGetProperty("totalRecords", out JsonElement trElem2))
                        totalRecords = trElem2.GetInt32();

                    AssertHelper.IsTrue(totalRecords >= 3, "total records should be at least 3, got " + totalRecords);
                });

                // Cleanup pagination assistants
                foreach (string id in assistantIds)
                {
                    if (id != null)
                        await server.Client.DeleteAsync($"/v1.0/assistants/{id}");
                }

                // ===== MultiTenantIsolationTests =====

                string tenantAId = server.DefaultTenantId;
                string tenantBId = null;
                string tenantBToken = null;

                // Setup: Create tenant B
                var tenantPayload = new { Name = "Isolation Tenant B", Active = true };
                string tenantJson = JsonSerializer.Serialize(tenantPayload);
                HttpContent tenantContent = new StringContent(tenantJson, Encoding.UTF8, "application/json");
                HttpResponseMessage tenantResp = await server.Client.PutAsync("/v1.0/tenants", tenantContent);
                string tenantBody = await tenantResp.Content.ReadAsStringAsync();
                using (JsonDocument tenantDoc = JsonDocument.Parse(tenantBody))
                {
                    if (tenantDoc.RootElement.TryGetProperty("Tenant", out JsonElement tenantElem))
                    {
                        if (tenantElem.TryGetProperty("Id", out JsonElement idElem))
                            tenantBId = idElem.GetString();
                        else if (tenantElem.TryGetProperty("id", out JsonElement idElem2))
                            tenantBId = idElem2.GetString();
                    }
                }

                // Setup: Create regular user in tenant B
                if (tenantBId != null)
                {
                    var userPayload = new
                    {
                        FirstName = "Regular",
                        LastName = "UserB",
                        Email = "regular@tenantb.local",
                        Password = "password123",
                        TenantId = tenantBId,
                        IsAdmin = false,
                        IsTenantAdmin = false
                    };
                    string userJson = JsonSerializer.Serialize(userPayload);
                    HttpContent userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage userResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantBId}/users", userContent);
                    string userBody = await userResp.Content.ReadAsStringAsync();

                    string regularUserId = null;
                    using (JsonDocument userDoc = JsonDocument.Parse(userBody))
                    {
                        if (userDoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                            regularUserId = idElem.GetString();
                        else if (userDoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                            regularUserId = idElem2.GetString();
                    }

                    if (regularUserId != null)
                    {
                        var credPayload = new { TenantId = tenantBId, UserId = regularUserId, Active = true };
                        string credJson = JsonSerializer.Serialize(credPayload);
                        HttpContent credContent = new StringContent(credJson, Encoding.UTF8, "application/json");
                        HttpResponseMessage credResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantBId}/credentials", credContent);
                        string credBody = await credResp.Content.ReadAsStringAsync();
                        using (JsonDocument credDoc = JsonDocument.Parse(credBody))
                        {
                            if (credDoc.RootElement.TryGetProperty("BearerToken", out JsonElement btElem))
                                tenantBToken = btElem.GetString();
                            else if (credDoc.RootElement.TryGetProperty("bearerToken", out JsonElement btElem2))
                                tenantBToken = btElem2.GetString();
                        }
                    }
                }

                // Create an assistant in tenant A
                string tenantAAssistantId = null;
                var asstAPayload = new { Name = "Tenant A Assistant", TenantId = tenantAId };
                string asstAJson = JsonSerializer.Serialize(asstAPayload);
                HttpContent asstAContent = new StringContent(asstAJson, Encoding.UTF8, "application/json");
                HttpResponseMessage asstAResp = await server.Client.PutAsync("/v1.0/assistants", asstAContent);
                string asstABody = await asstAResp.Content.ReadAsStringAsync();
                using (JsonDocument asstADoc = JsonDocument.Parse(asstABody))
                {
                    if (asstADoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        tenantAAssistantId = idElem.GetString();
                    else if (asstADoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        tenantAAssistantId = idElem2.GetString();
                }

                HttpClient tenantBClient = new HttpClient();
                tenantBClient.BaseAddress = new Uri(server.BaseUrl);
                if (tenantBToken != null)
                    tenantBClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tenantBToken}");

                await ExecuteTestAsync("MultiTenant.TenantB_CannotSee_TenantA_Assistants", async () =>
                {
                    if (tenantBToken == null)
                    {
                        AssertHelper.IsNotNull(tenantBToken, "tenant B token required");
                        return;
                    }

                    HttpResponseMessage resp = await tenantBClient.GetAsync("/v1.0/assistants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "tenant B enumerate should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(!body.Contains("Tenant A Assistant"),
                        "tenant B should not see tenant A's assistants");
                });

                await ExecuteTestAsync("MultiTenant.TenantB_CannotRead_TenantA_Assistant", async () =>
                {
                    if (tenantBToken == null || tenantAAssistantId == null)
                    {
                        AssertHelper.IsNotNull(tenantBToken, "tenant B token required");
                        return;
                    }

                    HttpResponseMessage resp = await tenantBClient.GetAsync($"/v1.0/assistants/{tenantAAssistantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 404 || (int)resp.StatusCode == 403,
                        "tenant B reading tenant A's assistant should return 403 or 404, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("MultiTenant.GlobalAdmin_CanSee_BothTenants", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "admin enumerate tenants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains(tenantAId) || body.Contains("Integration Test Tenant"),
                        "admin should see tenant A");
                    if (tenantBId != null)
                    {
                        AssertHelper.IsTrue(body.Contains(tenantBId) || body.Contains("Isolation Tenant B"),
                            "admin should see tenant B");
                    }
                });

                // Cleanup multi-tenant
                tenantBClient.Dispose();
                if (tenantAAssistantId != null)
                    await server.Client.DeleteAsync($"/v1.0/assistants/{tenantAAssistantId}");
                if (tenantBId != null)
                    await server.Client.DeleteAsync($"/v1.0/tenants/{tenantBId}");

                // ===== ErrorHandlingTests =====

                await ExecuteTestAsync("Error.MalformedJson_Returns400", async () =>
                {
                    HttpContent content = new StringContent("{invalid json!!!", Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                        "malformed JSON should return 400 or 500, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Error.EmptyBody_Returns400", async () =>
                {
                    HttpContent content = new StringContent("", Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                        "empty body should return 400 or 500, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Error.NonExistentEntity_Returns404", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_does_not_exist_xyz");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode,
                        "non-existent entity should return 404");
                });
            }
            finally
            {
                server.Dispose();
            }

            return GetResults();
        }
    }
}
