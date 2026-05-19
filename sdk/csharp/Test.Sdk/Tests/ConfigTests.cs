namespace Test.Sdk.Tests
{
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using Test.Shared;

    /// <summary>
    /// Tests for configuration endpoint.
    /// </summary>
    public static class ConfigTests
    {
        /// <summary>
        /// Runs all configuration tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            await runner.RunTestAsync("Config: Get config returns valid response", async (CancellationToken ct) =>
            {
                JsonElement config = await client.GetConfigAsync(ct).ConfigureAwait(false);
                AssertHelper.IsTrue(config.ValueKind == JsonValueKind.Object, "Config should be a JSON object");
            }, token).ConfigureAwait(false);
        }
    }
}
