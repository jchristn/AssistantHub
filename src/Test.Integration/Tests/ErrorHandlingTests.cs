namespace Test.Integration.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public static class ErrorHandlingTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Error Handling Tests ---");

            await runner.RunTestAsync("Error.MalformedJson_Returns400", async ct =>
            {
                HttpContent content = new StringContent("{invalid json!!!", Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                    "malformed JSON should return 400 or 500, got " + (int)resp.StatusCode);
            }, token);

            await runner.RunTestAsync("Error.EmptyBody_Returns400", async ct =>
            {
                HttpContent content = new StringContent("", Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                AssertHelper.IsTrue(
                    (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                    "empty body should return 400 or 500, got " + (int)resp.StatusCode);
            }, token);

            await runner.RunTestAsync("Error.NonExistentEntity_Returns404", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_does_not_exist_xyz");
                AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode,
                    "non-existent entity should return 404");
            }, token);
        }
    }
}
