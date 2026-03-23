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
    /// Tests for embedding and completion endpoint CRUD lifecycle.
    /// </summary>
    public static class EndpointTests
    {
        /// <summary>
        /// Runs all endpoint management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdEmbeddingId = null;
            string createdCompletionId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            // --- Embedding Endpoint Tests ---

            await runner.RunTestAsync("Endpoint: Create embedding endpoint", async (CancellationToken ct) =>
            {
                EmbeddingEndpoint endpoint = new EmbeddingEndpoint
                {
                    Name = "test-embedding-" + uniqueSuffix,
                    Model = "test-model",
                    Endpoint = "http://localhost:8321",
                    ApiFormat = "OpenAI",
                    Active = true
                };

                EmbeddingEndpoint created = await client.CreateEmbeddingEndpointAsync(endpoint, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateEmbeddingEndpoint result");
                AssertHelper.IsNotNull(created.Id, "Created embedding endpoint ID");
                AssertHelper.AreEqual("test-embedding-" + uniqueSuffix, created.Name, "Created embedding endpoint name");
                createdEmbeddingId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: List embedding endpoints includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdEmbeddingId, "createdEmbeddingId from previous test");
                EnumerationResult<EmbeddingEndpoint> result = await client.ListEmbeddingEndpointsAsync(null, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListEmbeddingEndpoints result");
                AssertHelper.IsNotNull(result.Objects, "ListEmbeddingEndpoints result.Objects");
                bool found = result.Objects.Any(e => e.Id == createdEmbeddingId);
                AssertHelper.IsTrue(found, "Created embedding endpoint should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Get embedding endpoint by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdEmbeddingId, "createdEmbeddingId from previous test");
                EmbeddingEndpoint endpoint = await client.GetEmbeddingEndpointAsync(createdEmbeddingId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(endpoint, "GetEmbeddingEndpoint result");
                AssertHelper.AreEqual(createdEmbeddingId, endpoint.Id, "Embedding endpoint ID");
                AssertHelper.AreEqual("test-embedding-" + uniqueSuffix, endpoint.Name, "Embedding endpoint name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Update embedding endpoint", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdEmbeddingId, "createdEmbeddingId from previous test");
                EmbeddingEndpoint endpoint = new EmbeddingEndpoint
                {
                    Name = "test-embedding-updated-" + uniqueSuffix,
                    Model = "test-model-updated",
                    Endpoint = "http://localhost:8321",
                    ApiFormat = "OpenAI",
                    Active = true
                };

                EmbeddingEndpoint updated = await client.UpdateEmbeddingEndpointAsync(createdEmbeddingId, endpoint, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateEmbeddingEndpoint result");
                AssertHelper.AreEqual("test-embedding-updated-" + uniqueSuffix, updated.Name, "Updated embedding endpoint name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Check embedding health", async (CancellationToken ct) =>
            {
                List<EndpointHealthStatus> healthStatuses = await client.CheckEmbeddingHealthAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(healthStatuses, "CheckEmbeddingHealth result");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Delete embedding endpoint", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdEmbeddingId, "createdEmbeddingId from previous test");
                await client.DeleteEmbeddingEndpointAsync(createdEmbeddingId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            // --- Completion Endpoint Tests ---

            await runner.RunTestAsync("Endpoint: Create completion endpoint", async (CancellationToken ct) =>
            {
                CompletionEndpoint endpoint = new CompletionEndpoint
                {
                    Name = "test-completion-" + uniqueSuffix,
                    Model = "test-model",
                    Endpoint = "http://localhost:8321",
                    ApiFormat = "OpenAI",
                    Active = true
                };

                CompletionEndpoint created = await client.CreateCompletionEndpointAsync(endpoint, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateCompletionEndpoint result");
                AssertHelper.IsNotNull(created.Id, "Created completion endpoint ID");
                AssertHelper.AreEqual("test-completion-" + uniqueSuffix, created.Name, "Created completion endpoint name");
                createdCompletionId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: List completion endpoints includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCompletionId, "createdCompletionId from previous test");
                EnumerationResult<CompletionEndpoint> result = await client.ListCompletionEndpointsAsync(null, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListCompletionEndpoints result");
                AssertHelper.IsNotNull(result.Objects, "ListCompletionEndpoints result.Objects");
                bool found = result.Objects.Any(e => e.Id == createdCompletionId);
                AssertHelper.IsTrue(found, "Created completion endpoint should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Get completion endpoint by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCompletionId, "createdCompletionId from previous test");
                CompletionEndpoint endpoint = await client.GetCompletionEndpointAsync(createdCompletionId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(endpoint, "GetCompletionEndpoint result");
                AssertHelper.AreEqual(createdCompletionId, endpoint.Id, "Completion endpoint ID");
                AssertHelper.AreEqual("test-completion-" + uniqueSuffix, endpoint.Name, "Completion endpoint name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Update completion endpoint", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCompletionId, "createdCompletionId from previous test");
                CompletionEndpoint endpoint = new CompletionEndpoint
                {
                    Name = "test-completion-updated-" + uniqueSuffix,
                    Model = "test-model-updated",
                    Endpoint = "http://localhost:8321",
                    ApiFormat = "OpenAI",
                    Active = true
                };

                CompletionEndpoint updated = await client.UpdateCompletionEndpointAsync(createdCompletionId, endpoint, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateCompletionEndpoint result");
                AssertHelper.AreEqual("test-completion-updated-" + uniqueSuffix, updated.Name, "Updated completion endpoint name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Check completion health", async (CancellationToken ct) =>
            {
                List<EndpointHealthStatus> healthStatuses = await client.CheckCompletionHealthAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(healthStatuses, "CheckCompletionHealth result");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Endpoint: Delete completion endpoint", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCompletionId, "createdCompletionId from previous test");
                await client.DeleteCompletionEndpointAsync(createdCompletionId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }
    }
}
