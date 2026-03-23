namespace Test.Sdk.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Common;

    /// <summary>
    /// Tests for eval fact CRUD lifecycle.
    /// </summary>
    public static class EvalTests
    {
        /// <summary>
        /// Runs all eval fact tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdFactId = null;
            string assistantId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Eval: Create assistant for eval tests", async (CancellationToken ct) =>
            {
                Assistant assistant = new Assistant
                {
                    Name = "test-eval-assistant-" + uniqueSuffix,
                    Description = "Temporary assistant for eval tests"
                };

                Assistant created = await client.CreateAssistantAsync(assistant, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateAssistant result");
                AssertHelper.IsNotNull(created.Id, "Created assistant ID");
                assistantId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Eval: Create eval fact", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(assistantId, "assistantId from previous test");
                EvalFact fact = new EvalFact
                {
                    AssistantId = assistantId,
                    Category = "test-category-" + uniqueSuffix,
                    Question = "What is the test question?",
                    ExpectedFacts = "[\"fact1\", \"fact2\"]"
                };

                EvalFact created = await client.CreateEvalFactAsync(fact, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateEvalFact result");
                AssertHelper.IsNotNull(created.Id, "Created eval fact ID");
                AssertHelper.StartsWith(created.Id, "ef_", "Created eval fact ID prefix");
                AssertHelper.AreEqual(assistantId, created.AssistantId, "Eval fact assistant ID");
                createdFactId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Eval: List eval facts includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdFactId, "createdFactId from previous test");
                EnumerationResult<EvalFact> result = await client.ListEvalFactsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListEvalFacts result");
                AssertHelper.IsNotNull(result.Objects, "ListEvalFacts result.Objects");
                bool found = result.Objects.Any(f => f.Id == createdFactId);
                AssertHelper.IsTrue(found, "Created eval fact should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Eval: Delete eval fact", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdFactId, "createdFactId from previous test");
                await client.DeleteEvalFactAsync(createdFactId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Eval: Cleanup assistant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(assistantId, "assistantId from previous test");
                await client.DeleteAssistantAsync(assistantId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }
    }
}
