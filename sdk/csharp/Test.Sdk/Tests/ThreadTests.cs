namespace Test.Sdk.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Common;

    /// <summary>
    /// Tests for thread creation, retrieval, and deletion lifecycle.
    /// </summary>
    public static class ThreadTests
    {
        /// <summary>
        /// Runs all thread management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdAssistantId = null;
            string createdThreadId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Thread: Create assistant for thread tests", async (CancellationToken ct) =>
            {
                Assistant assistant = new Assistant
                {
                    Name = "test-thread-assistant-" + uniqueSuffix,
                    Description = "Assistant created for thread tests"
                };

                Assistant created = await client.CreateAssistantAsync(assistant, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateAssistant result");
                AssertHelper.IsNotNull(created.Id, "Created assistant ID");
                AssertHelper.StartsWith(created.Id, "asst_", "Assistant ID prefix");
                createdAssistantId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Thread: Create thread for assistant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                string threadId = await client.CreateThreadAsync(createdAssistantId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(threadId, "CreateThread result");
                createdThreadId = threadId;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Thread: List threads includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                AssertHelper.IsNotNull(createdThreadId, "createdThreadId from previous test");
                EnumerationResult<string> result = await client.ListThreadsAsync(createdAssistantId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListThreads result");
                AssertHelper.IsNotNull(result.Objects, "ListThreads result.Objects");
                bool found = result.Objects.Any(t => t == createdThreadId);
                AssertHelper.IsTrue(found, "Created thread should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Thread: Get thread history", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                AssertHelper.IsNotNull(createdThreadId, "createdThreadId from previous test");
                List<ChatCompletionMessage> messages = await client.GetThreadAsync(createdAssistantId, createdThreadId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(messages, "GetThread result");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Thread: Delete thread", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdThreadId, "createdThreadId from previous test");
                await client.DeleteThreadAsync(createdThreadId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Thread: Clean up assistant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                await client.DeleteAssistantAsync(createdAssistantId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }
    }
}
