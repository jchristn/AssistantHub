namespace Test.Sdk.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Common;

    /// <summary>
    /// Tests for inference model listing.
    /// </summary>
    public static class InferenceTests
    {
        /// <summary>
        /// Runs all inference tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            await runner.RunTestAsync("Inference: List models returns results", async (CancellationToken ct) =>
            {
                List<InferenceModel> models = await client.ListModelsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(models, "ListModels result");
                // Models list may be empty if no inference provider is configured -- that is acceptable.
            }, token).ConfigureAwait(false);
        }
    }
}
