namespace Test.Sdk.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Common;

    /// <summary>
    /// Tests for document upload, retrieval, and deletion lifecycle.
    /// </summary>
    public static class DocumentTests
    {
        /// <summary>
        /// Runs all document management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdCollectionId = null;
            string ingestionRuleId = null;
            string uploadedDocumentId = null;
            string secondDocumentId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Document: Create collection for document tests", async (CancellationToken ct) =>
            {
                Collection collection = new Collection
                {
                    Name = "test-doc-collection-" + uniqueSuffix
                };

                Collection created = await client.CreateCollectionAsync(collection, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateCollection result");
                AssertHelper.IsNotNull(created.Id, "Created collection ID");
                createdCollectionId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Get ingestion rule for collection", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                EnumerationResult<IngestionRule> rules = await client.ListIngestionRulesAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(rules, "ListIngestionRules result");
                AssertHelper.IsNotNull(rules.Objects, "ListIngestionRules result.Objects");

                IngestionRule rule = rules.Objects.FirstOrDefault(r => r.CollectionId == createdCollectionId);
                AssertHelper.IsNotNull(rule, "Ingestion rule for created collection");
                AssertHelper.StartsWith(rule.Id, "irule_", "Ingestion rule ID prefix");
                ingestionRuleId = rule.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Upload text document", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(ingestionRuleId, "ingestionRuleId from previous test");
                byte[] content = Encoding.UTF8.GetBytes("This is a test document for SDK testing. It contains sample text content.");

                AssistantDocument document = await client.UploadDocumentAsync(
                    ingestionRuleId,
                    content,
                    "test-document-" + uniqueSuffix,
                    "test-document-" + uniqueSuffix + ".txt",
                    "text/plain",
                    ct).ConfigureAwait(false);

                AssertHelper.IsNotNull(document, "UploadDocument result");
                AssertHelper.IsNotNull(document.Id, "Uploaded document ID");
                AssertHelper.StartsWith(document.Id, "adoc_", "Document ID prefix");
                uploadedDocumentId = document.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: List documents includes uploaded one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                EnumerationResult<AssistantDocument> result = await client.ListDocumentsAsync(null, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListDocuments result");
                AssertHelper.IsNotNull(result.Objects, "ListDocuments result.Objects");
                bool found = result.Objects.Any(d => d.Id == uploadedDocumentId);
                AssertHelper.IsTrue(found, "Uploaded document should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Get document by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                AssistantDocument document = await client.GetDocumentAsync(uploadedDocumentId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(document, "GetDocument result");
                AssertHelper.AreEqual(uploadedDocumentId, document.Id, "Document ID");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Delete document", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                await client.DeleteDocumentAsync(uploadedDocumentId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Verify document no longer in list", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                EnumerationResult<AssistantDocument> result = await client.ListDocumentsAsync(null, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListDocuments result");
                AssertHelper.IsNotNull(result.Objects, "ListDocuments result.Objects");
                bool found = result.Objects.Any(d => d.Id == uploadedDocumentId);
                AssertHelper.IsFalse(found, "Deleted document should not appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Upload two documents for bulk delete", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(ingestionRuleId, "ingestionRuleId from previous test");
                byte[] content1 = Encoding.UTF8.GetBytes("Bulk delete test document one.");
                byte[] content2 = Encoding.UTF8.GetBytes("Bulk delete test document two.");

                AssistantDocument doc1 = await client.UploadDocumentAsync(
                    ingestionRuleId,
                    content1,
                    "bulk-doc-1-" + uniqueSuffix,
                    "bulk-doc-1-" + uniqueSuffix + ".txt",
                    "text/plain",
                    ct).ConfigureAwait(false);

                AssistantDocument doc2 = await client.UploadDocumentAsync(
                    ingestionRuleId,
                    content2,
                    "bulk-doc-2-" + uniqueSuffix,
                    "bulk-doc-2-" + uniqueSuffix + ".txt",
                    "text/plain",
                    ct).ConfigureAwait(false);

                AssertHelper.IsNotNull(doc1, "First bulk document");
                AssertHelper.IsNotNull(doc1.Id, "First bulk document ID");
                AssertHelper.IsNotNull(doc2, "Second bulk document");
                AssertHelper.IsNotNull(doc2.Id, "Second bulk document ID");
                uploadedDocumentId = doc1.Id;
                secondDocumentId = doc2.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Bulk delete documents", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                AssertHelper.IsNotNull(secondDocumentId, "secondDocumentId from previous test");
                List<string> idsToDelete = new List<string> { uploadedDocumentId, secondDocumentId };
                await client.BulkDeleteDocumentsAsync(idsToDelete, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Verify bulk deleted documents no longer in list", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
                AssertHelper.IsNotNull(secondDocumentId, "secondDocumentId from previous test");
                EnumerationResult<AssistantDocument> result = await client.ListDocumentsAsync(null, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListDocuments result");
                AssertHelper.IsNotNull(result.Objects, "ListDocuments result.Objects");
                bool foundFirst = result.Objects.Any(d => d.Id == uploadedDocumentId);
                bool foundSecond = result.Objects.Any(d => d.Id == secondDocumentId);
                AssertHelper.IsFalse(foundFirst, "First bulk deleted document should not appear in list");
                AssertHelper.IsFalse(foundSecond, "Second bulk deleted document should not appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Document: Clean up test collection", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdCollectionId, "createdCollectionId from previous test");
                await client.DeleteCollectionAsync(createdCollectionId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }
    }
}
