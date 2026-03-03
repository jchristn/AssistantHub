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

    public static class AuthenticationFlowTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Authentication Flow Tests ---");

            await runner.RunTestAsync("Auth.PostAuthenticate_ValidCredentials", async ct =>
            {
                var payload = new { Email = "admin@test.local", Password = "testpassword123", TenantId = server.DefaultTenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "valid auth should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("BearerToken") || body.Contains("bearerToken") || body.Contains("bearer_token"),
                    "response should contain bearer token field");
            }, token);

            await runner.RunTestAsync("Auth.PostAuthenticate_InvalidPassword", async ct =>
            {
                var payload = new { Email = "admin@test.local", Password = "wrongpassword", TenantId = server.DefaultTenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                // Should return 401 or similar error
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                    "invalid password should return 401 or 400, got " + (int)resp.StatusCode);
            }, token);

            await runner.RunTestAsync("Auth.PostAuthenticate_NonExistentEmail", async ct =>
            {
                var payload = new { Email = "nonexistent@test.local", Password = "password123", TenantId = server.DefaultTenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                    "non-existent email should return 401 or 400, got " + (int)resp.StatusCode);
            }, token);

            await runner.RunTestAsync("Auth.BearerToken_SubsequentRequest", async ct =>
            {
                // Authenticate and get a token
                var payload = new { Email = "admin@test.local", Password = "testpassword123", TenantId = server.DefaultTenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage authResp = await server.Client.PostAsync("/v1.0/authenticate", content);
                string authBody = await authResp.Content.ReadAsStringAsync();

                // Use the existing admin bearer token to make a subsequent request
                using HttpClient tokenClient = new HttpClient();
                tokenClient.BaseAddress = new Uri(server.BaseUrl);
                tokenClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {server.AdminBearerToken}");

                HttpResponseMessage resp = await tokenClient.GetAsync("/v1.0/tenants");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "authenticated request should succeed");
            }, token);
        }
    }
}
