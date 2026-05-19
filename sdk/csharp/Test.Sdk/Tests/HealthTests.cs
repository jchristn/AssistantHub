namespace Test.Sdk.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using Test.Shared;

    /// <summary>
    /// Tests for health check and authentication endpoints.
    /// </summary>
    public static class HealthTests
    {
        /// <summary>
        /// Runs all health and authentication tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            await runner.RunTestAsync("Health: HealthCheck returns true", async (CancellationToken ct) =>
            {
                bool healthy = await client.HealthCheckAsync(ct).ConfigureAwait(false);
                AssertHelper.IsTrue(healthy, "HealthCheck should return true");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Health: WhoAmI returns authenticated identity", async (CancellationToken ct) =>
            {
                JsonElement identity = await client.WhoAmIAsync(ct).ConfigureAwait(false);
                AssertHelper.IsTrue(identity.ValueKind != JsonValueKind.Null && identity.ValueKind != JsonValueKind.Undefined, "WhoAmI should return a valid JSON object");
            }, token).ConfigureAwait(false);
        }
    }
}
