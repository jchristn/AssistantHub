namespace Test.Integration.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public static class ServerLifecycleTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Server Lifecycle Tests ---");

            await runner.RunTestAsync("Server.RootEndpoint_Returns200", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "GET / should return 200");
            }, token);

            await runner.RunTestAsync("Server.RootEndpoint_ReturnsJson", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/");
                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsNotNull(body, "response body should not be null");
                AssertHelper.IsTrue(body.Length > 0, "response body should not be empty");
            }, token);

            await runner.RunTestAsync("Server.NonExistentEndpoint_Returns404", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/nonexistent");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent endpoint should return 404");
            }, token);

            await runner.RunTestAsync("Server.UnauthenticatedRequest_Returns401", async ct =>
            {
                // Create a new client without auth header
                using HttpClient noAuthClient = new HttpClient();
                noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                HttpResponseMessage resp = await noAuthClient.GetAsync("/v1.0/tenants");
                AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "unauthenticated request should return 401");
            }, token);

            await runner.RunTestAsync("Server.InvalidToken_Returns401", async ct =>
            {
                using HttpClient badClient = new HttpClient();
                badClient.BaseAddress = new Uri(server.BaseUrl);
                badClient.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token-xyz");

                HttpResponseMessage resp = await badClient.GetAsync("/v1.0/tenants");
                AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "invalid token should return 401");
            }, token);

            await runner.RunTestAsync("Server.WhoAmI_ReturnsAuthenticatedUser", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/whoami");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "whoami should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                AssertHelper.IsTrue(body.Contains("admin@test.local"), "whoami should contain admin email");
            }, token);
        }
    }
}
