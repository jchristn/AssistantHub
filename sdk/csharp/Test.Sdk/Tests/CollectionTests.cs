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
    /// Tests for collection CRUD lifecycle.
    /// </summary>
    public static class CollectionTests
    {
        /// <summary>
        /// Runs all collection management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdCollectionId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Collection: Create collection with name", async (CancellationToken ct) =>
            {
                Collection collection = new Collection
                {
                    Name = "test-collection-" + uniqueSuffix
                };

                Collection created = await client.CreateCollectionAsync(collection, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateCollection result");
                AssertHelper.IsNotNull(created.Id, "Created collection ID");
                AssertHelper.AreEqual("test-collection-" + uniqueSuffix, created.Name, "Created collection name");
                createdCollectionId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Collection: List collections includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                EnumerationResult<Collection> result = await client.ListCollectionsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListCollections result");
                AssertHelper.IsNotNull(result.Objects, "ListCollections result.Objects");
                bool found = result.Objects.Any(c => c.Id == createdCollectionId);
                AssertHelper.IsTrue(found, "Created collection should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Collection: Get collection by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                Collection collection = await client.GetCollectionAsync(createdCollectionId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(collection, "GetCollection result");
                AssertHelper.AreEqual(createdCollectionId, collection.Id, "Collection ID");
                AssertHelper.AreEqual("test-collection-" + uniqueSuffix, collection.Name, "Collection name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Collection: Update collection name", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                Collection collection = new Collection
                {
                    Name = "test-collection-updated-" + uniqueSuffix
                };

                Collection updated = await client.UpdateCollectionAsync(createdCollectionId, collection, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateCollection result");
                AssertHelper.AreEqual("test-collection-updated-" + uniqueSuffix, updated.Name, "Updated collection name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Collection: Delete collection", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                await client.DeleteCollectionAsync(createdCollectionId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Collection: Verify collection no longer in list", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                EnumerationResult<Collection> result = await client.ListCollectionsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListCollections result");
                AssertHelper.IsNotNull(result.Objects, "ListCollections result.Objects");
                bool found = result.Objects.Any(c => c.Id == createdCollectionId);
                AssertHelper.IsFalse(found, "Deleted collection should not appear in list");
            }, token).ConfigureAwait(false);
        }
    }
}
