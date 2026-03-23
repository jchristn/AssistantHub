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
    /// Tests for assistant CRUD lifecycle.
    /// </summary>
    public static class AssistantTests
    {
        /// <summary>
        /// Runs all assistant management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdAssistantId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Assistant: Create assistant with name and description", async (CancellationToken ct) =>
            {
                Assistant assistant = new Assistant
                {
                    Name = "test-assistant-" + uniqueSuffix,
                    Description = "Test assistant created by SDK tests"
                };

                Assistant created = await client.CreateAssistantAsync(assistant, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateAssistant result");
                AssertHelper.IsNotNull(created.Id, "Created assistant ID");
                AssertHelper.StartsWith(created.Id, "asst_", "Created assistant ID prefix");
                AssertHelper.AreEqual("test-assistant-" + uniqueSuffix, created.Name, "Created assistant name");
                createdAssistantId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Assistant: List assistants includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                EnumerationResult<Assistant> result = await client.ListAssistantsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListAssistants result");
                AssertHelper.IsNotNull(result.Objects, "ListAssistants result.Objects");
                bool found = result.Objects.Any(a => a.Id == createdAssistantId);
                AssertHelper.IsTrue(found, "Created assistant should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Assistant: Get assistant by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                Assistant assistant = await client.GetAssistantAsync(createdAssistantId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(assistant, "GetAssistant result");
                AssertHelper.AreEqual(createdAssistantId, assistant.Id, "Assistant ID");
                AssertHelper.AreEqual("test-assistant-" + uniqueSuffix, assistant.Name, "Assistant name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Assistant: Update assistant name", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                Assistant assistant = new Assistant
                {
                    Name = "test-assistant-updated-" + uniqueSuffix,
                    Description = "Updated description"
                };

                Assistant updated = await client.UpdateAssistantAsync(createdAssistantId, assistant, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateAssistant result");
                AssertHelper.AreEqual("test-assistant-updated-" + uniqueSuffix, updated.Name, "Updated assistant name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Assistant: Delete assistant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                await client.DeleteAssistantAsync(createdAssistantId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Assistant: Verify assistant no longer in list", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                EnumerationResult<Assistant> result = await client.ListAssistantsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListAssistants result");
                AssertHelper.IsNotNull(result.Objects, "ListAssistants result.Objects");
                bool found = result.Objects.Any(a => a.Id == createdAssistantId);
                AssertHelper.IsFalse(found, "Deleted assistant should not appear in list");
            }, token).ConfigureAwait(false);
        }
    }
}
