namespace Test.Integration.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public static class CrudLifecycleTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- CRUD Lifecycle Tests ---");

            string tenantId = server.DefaultTenantId;
            string createdAssistantId = null;

            // --- Assistant CRUD lifecycle ---

            await runner.RunTestAsync("CRUD.Assistant.Create", async ct =>
            {
                var payload = new { Name = "Integration Test Assistant", TenantId = tenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                    "create assistant should return 201, got " + (int)resp.StatusCode);

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("asst_"), "response should contain assistant ID with asst_ prefix");

                // Extract the ID from the response
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                    createdAssistantId = idElem.GetString();
                else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                    createdAssistantId = idElem2.GetString();

                AssertHelper.IsNotNull(createdAssistantId, "should extract assistant ID from response");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Read", async ct =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read assistant should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("Integration Test Assistant"), "response should contain assistant name");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Read_NotFound", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_nonexistent");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent assistant should return 404");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate assistants should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Length > 2, "response should contain data");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Update", async ct =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                // Read the current state
                HttpResponseMessage readResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                string readBody = await readResp.Content.ReadAsStringAsync();

                // Update via PUT
                using JsonDocument doc = JsonDocument.Parse(readBody);
                string updatedJson = readBody.Replace("Integration Test Assistant", "Updated Assistant Name");
                HttpContent content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/assistants/{createdAssistantId}", content);
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode,
                    "update assistant should return 200, got " + (int)resp.StatusCode);

                // Verify the update
                HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                string verifyBody = await verifyResp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(verifyBody.Contains("Updated Assistant Name"), "updated name should be persisted");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Head_Exists", async ct =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/assistants/{createdAssistantId}");
                HttpResponseMessage resp = await server.Client.SendAsync(req);
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing assistant should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Head_NotFound", async ct =>
            {
                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, "/v1.0/assistants/asst_nonexistent");
                HttpResponseMessage resp = await server.Client.SendAsync(req);
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "HEAD non-existent should return 404");
            }, token);

            await runner.RunTestAsync("CRUD.Assistant.Delete", async ct =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/assistants/{createdAssistantId}");
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                    "delete should return 200 or 204, got " + (int)resp.StatusCode);

                // Verify deletion
                HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted assistant should not be found");
            }, token);

            // --- User CRUD lifecycle ---

            string createdUserId = null;

            await runner.RunTestAsync("CRUD.User.Create", async ct =>
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
            }, token);

            await runner.RunTestAsync("CRUD.User.Read", async ct =>
            {
                AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read user should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("testuser@integration.local"), "response should contain user email");
                // Password should NOT be in response
                AssertHelper.IsTrue(!body.Contains("password123"), "password should not be in response body");
            }, token);

            await runner.RunTestAsync("CRUD.User.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate users should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.User.Delete", async ct =>
            {
                AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                    "delete user should return 200 or 204, got " + (int)resp.StatusCode);
            }, token);

            // --- Ingestion Rule CRUD ---

            string createdRuleId = null;

            await runner.RunTestAsync("CRUD.IngestionRule.Create", async ct =>
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
            }, token);

            await runner.RunTestAsync("CRUD.IngestionRule.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/ingestion-rules");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate rules should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.IngestionRule.Delete", async ct =>
            {
                if (createdRuleId != null)
                {
                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/ingestion-rules/{createdRuleId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete rule should return 200 or 204, got " + (int)resp.StatusCode);
                }
            }, token);

            // --- Tenant CRUD lifecycle ---

            string createdTenantId = null;

            await runner.RunTestAsync("CRUD.Tenant.Create", async ct =>
            {
                var payload = new { Name = "Integration CRUD Tenant", Active = true };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/tenants", content);
                AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                    "create tenant should return 201, got " + (int)resp.StatusCode);

                string body = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);
                // Response is { Tenant: {...}, Provisioning: {...} }
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
            }, token);

            await runner.RunTestAsync("CRUD.Tenant.Read", async ct =>
            {
                AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read tenant should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("Integration CRUD Tenant"), "response should contain tenant name");
            }, token);

            await runner.RunTestAsync("CRUD.Tenant.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/tenants");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate tenants should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Length > 2, "response should contain data");
            }, token);

            await runner.RunTestAsync("CRUD.Tenant.Head_Exists", async ct =>
            {
                AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/tenants/{createdTenantId}");
                HttpResponseMessage resp = await server.Client.SendAsync(req);
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing tenant should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.Tenant.Delete", async ct =>
            {
                AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{createdTenantId}");
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                    "delete tenant should return 200 or 204, got " + (int)resp.StatusCode);

                // Verify deletion
                HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted tenant should return 404");
            }, token);

            // --- Credential CRUD lifecycle ---

            string createdCredentialId = null;

            await runner.RunTestAsync("CRUD.Credential.Create", async ct =>
            {
                // Create a user for the credential
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
            }, token);

            await runner.RunTestAsync("CRUD.Credential.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/credentials");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate credentials should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.Credential.Delete", async ct =>
            {
                if (createdCredentialId != null)
                {
                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/credentials/{createdCredentialId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete credential should return 200 or 204, got " + (int)resp.StatusCode);
                }
            }, token);

            // --- Assistant Settings lifecycle ---

            string settingsAssistantId = null;

            await runner.RunTestAsync("CRUD.Settings.CreateAssistant", async ct =>
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
            }, token);

            await runner.RunTestAsync("CRUD.Settings.Put", async ct =>
            {
                AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                var payload = new
                {
                    AssistantId = settingsAssistantId,
                    Temperature = 0.8,
                    Model = "llama3:8b",
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
            }, token);

            await runner.RunTestAsync("CRUD.Settings.Get", async ct =>
            {
                AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{settingsAssistantId}/settings");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "get settings should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("llama3:8b") || body.Contains("llama3"), "response should contain model name");
            }, token);

            // Cleanup settings assistant
            await runner.RunTestAsync("CRUD.Settings.Cleanup", async ct =>
            {
                if (settingsAssistantId != null)
                {
                    await server.Client.DeleteAsync($"/v1.0/assistants/{settingsAssistantId}");
                }
            }, token);

            // --- Feedback lifecycle (read-only via HTTP — created internally during chat) ---

            await runner.RunTestAsync("CRUD.Feedback.Enumerate_Empty", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate feedback should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.Feedback.Read_NotFound", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback/afb_nonexistent");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent feedback should return 404");
            }, token);

            // --- History lifecycle (read-only via HTTP — created internally during chat) ---

            await runner.RunTestAsync("CRUD.History.Enumerate_Empty", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate history should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.History.Read_NotFound", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history/ch_nonexistent");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent history should return 404");
            }, token);

            // --- CrawlPlan CRUD lifecycle ---

            string createdCrawlPlanId = null;

            await runner.RunTestAsync("CRUD.CrawlPlan.Create", async ct =>
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
            }, token);

            await runner.RunTestAsync("CRUD.CrawlPlan.Read", async ct =>
            {
                AssertHelper.IsNotNull(createdCrawlPlanId, "crawl plan ID should exist");

                HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read crawl plan should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("Integration Test Crawl Plan"), "response should contain crawl plan name");
            }, token);

            await runner.RunTestAsync("CRUD.CrawlPlan.Enumerate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/crawlplans");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate crawl plans should return 200");
            }, token);

            await runner.RunTestAsync("CRUD.CrawlPlan.Delete", async ct =>
            {
                if (createdCrawlPlanId != null)
                {
                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete crawl plan should return 200 or 204, got " + (int)resp.StatusCode);
                }
            }, token);
        }
    }
}
