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

    public static class MultiTenantIsolationTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Multi-Tenant Isolation Tests ---");

            string tenantAId = server.DefaultTenantId;
            string tenantBId = null;
            string tenantBToken = null;

            // Step 1: Create tenant B using global admin
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

            // Step 2: Create a regular (non-admin) user in tenant B
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

                // Step 3: Create credential for the regular user
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

            // Create tenant B client with the regular user's credential
            HttpClient tenantBClient = new HttpClient();
            tenantBClient.BaseAddress = new Uri(server.BaseUrl);
            if (tenantBToken != null)
                tenantBClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tenantBToken}");

            await runner.RunTestAsync("MultiTenant.TenantB_CannotSee_TenantA_Assistants", async ct =>
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
            }, token);

            await runner.RunTestAsync("MultiTenant.TenantB_CannotRead_TenantA_Assistant", async ct =>
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
            }, token);

            await runner.RunTestAsync("MultiTenant.GlobalAdmin_CanSee_BothTenants", async ct =>
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
            }, token);

            // Cleanup
            tenantBClient.Dispose();
            if (tenantAAssistantId != null)
                await server.Client.DeleteAsync($"/v1.0/assistants/{tenantAAssistantId}");
            if (tenantBId != null)
                await server.Client.DeleteAsync($"/v1.0/tenants/{tenantBId}");
        }
    }
}
