namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class AssistantDocumentTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- AssistantDocument Tests ---");

            string createdId = null;

            await runner.RunTestAsync("AssistantDocument.Create", async ct =>
            {
                AssistantDocument doc = new AssistantDocument
                {
                    Name = "Test Document",
                    OriginalFilename = "test-file.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 102400,
                    S3Key = "uploads/test-file.pdf",
                    Status = DocumentStatusEnum.Pending,
                    StatusMessage = "Awaiting processing",
                    IngestionRuleId = "irule_placeholder",
                    BucketName = "test-bucket",
                    CollectionId = "col_abc_123",
                    Labels = "[\"label1\",\"label2\"]",
                    Tags = "{\"env\":\"test\",\"priority\":\"high\"}",
                    ChunkRecordIds = "[\"rec_1\",\"rec_2\",\"rec_3\"]"
                };

                AssistantDocument created = await driver.AssistantDocument.CreateAsync(doc, ct);
                AssertHelper.IsNotNull(created, "created document");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "adoc_", "Id prefix");
                AssertHelper.AreEqual("Test Document", created.Name, "Name");
                AssertHelper.AreEqual("test-file.pdf", created.OriginalFilename, "OriginalFilename");
                AssertHelper.AreEqual("application/pdf", created.ContentType, "ContentType");
                AssertHelper.AreEqual(102400L, created.SizeBytes, "SizeBytes");
                AssertHelper.AreEqual("uploads/test-file.pdf", created.S3Key, "S3Key");
                AssertHelper.AreEqual(DocumentStatusEnum.Pending, created.Status, "Status");
                AssertHelper.AreEqual("Awaiting processing", created.StatusMessage, "StatusMessage");
                AssertHelper.AreEqual("irule_placeholder", created.IngestionRuleId, "IngestionRuleId");
                AssertHelper.AreEqual("test-bucket", created.BucketName, "BucketName");
                AssertHelper.AreEqual("col_abc_123", created.CollectionId, "CollectionId");
                AssertHelper.AreEqual("[\"label1\",\"label2\"]", created.Labels, "Labels");
                AssertHelper.AreEqual("{\"env\":\"test\",\"priority\":\"high\"}", created.Tags, "Tags");
                AssertHelper.AreEqual("[\"rec_1\",\"rec_2\",\"rec_3\"]", created.ChunkRecordIds, "ChunkRecordIds");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("AssistantDocument.Create_Minimal", async ct =>
            {
                AssistantDocument doc = new AssistantDocument { Name = "Minimal Doc" };

                AssistantDocument created = await driver.AssistantDocument.CreateAsync(doc, ct);
                AssertHelper.IsNotNull(created, "created minimal doc");
                AssertHelper.AreEqual("Minimal Doc", created.Name, "Name");
                AssertHelper.IsNull(created.OriginalFilename, "OriginalFilename");
                AssertHelper.AreEqual("application/octet-stream", created.ContentType, "default ContentType");
                AssertHelper.AreEqual(0L, created.SizeBytes, "default SizeBytes");
                AssertHelper.IsNull(created.S3Key, "S3Key");
                AssertHelper.AreEqual(DocumentStatusEnum.Pending, created.Status, "default Status");
                AssertHelper.IsNull(created.StatusMessage, "StatusMessage");
                AssertHelper.IsNull(created.IngestionRuleId, "IngestionRuleId");
                AssertHelper.IsNull(created.BucketName, "BucketName");
                AssertHelper.IsNull(created.CollectionId, "CollectionId");
                AssertHelper.IsNull(created.Labels, "Labels");
                AssertHelper.IsNull(created.Tags, "Tags");
                AssertHelper.IsNull(created.ChunkRecordIds, "ChunkRecordIds");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Read", async ct =>
            {
                AssistantDocument read = await driver.AssistantDocument.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read document");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("Test Document", read.Name, "Name");
                AssertHelper.AreEqual("test-file.pdf", read.OriginalFilename, "OriginalFilename");
                AssertHelper.AreEqual("application/pdf", read.ContentType, "ContentType");
                AssertHelper.AreEqual(102400L, read.SizeBytes, "SizeBytes");
                AssertHelper.AreEqual("uploads/test-file.pdf", read.S3Key, "S3Key");
                AssertHelper.AreEqual(DocumentStatusEnum.Pending, read.Status, "Status");
                AssertHelper.AreEqual("Awaiting processing", read.StatusMessage, "StatusMessage");
                AssertHelper.AreEqual("irule_placeholder", read.IngestionRuleId, "IngestionRuleId");
                AssertHelper.AreEqual("test-bucket", read.BucketName, "BucketName");
                AssertHelper.AreEqual("col_abc_123", read.CollectionId, "CollectionId");
                AssertHelper.AreEqual("[\"label1\",\"label2\"]", read.Labels, "Labels");
                AssertHelper.AreEqual("{\"env\":\"test\",\"priority\":\"high\"}", read.Tags, "Tags");
                AssertHelper.AreEqual("[\"rec_1\",\"rec_2\",\"rec_3\"]", read.ChunkRecordIds, "ChunkRecordIds");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Read_NotFound", async ct =>
            {
                AssistantDocument read = await driver.AssistantDocument.ReadAsync("adoc_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent document");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Exists_True", async ct =>
            {
                bool exists = await driver.AssistantDocument.ExistsAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "document should exist");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Exists_False", async ct =>
            {
                bool exists = await driver.AssistantDocument.ExistsAsync("adoc_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent document");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Update", async ct =>
            {
                AssistantDocument read = await driver.AssistantDocument.ReadAsync(createdId, ct);
                read.Name = "Updated Document";
                read.OriginalFilename = "updated-file.docx";
                read.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                read.SizeBytes = 204800;
                read.S3Key = "uploads/updated-file.docx";
                read.Status = DocumentStatusEnum.Completed;
                read.StatusMessage = "Processing complete";
                read.Labels = "[\"updated\"]";
                read.Tags = "{\"env\":\"prod\"}";

                AssistantDocument updated = await driver.AssistantDocument.UpdateAsync(read, ct);
                AssertHelper.AreEqual("Updated Document", updated.Name, "Name");
                AssertHelper.AreEqual("updated-file.docx", updated.OriginalFilename, "OriginalFilename");
                AssertHelper.AreEqual("application/vnd.openxmlformats-officedocument.wordprocessingml.document", updated.ContentType, "ContentType");
                AssertHelper.AreEqual(204800L, updated.SizeBytes, "SizeBytes");
                AssertHelper.AreEqual("uploads/updated-file.docx", updated.S3Key, "S3Key");
                AssertHelper.AreEqual(DocumentStatusEnum.Completed, updated.Status, "Status");
                AssertHelper.AreEqual("Processing complete", updated.StatusMessage, "StatusMessage");
                AssertHelper.AreEqual("[\"updated\"]", updated.Labels, "Labels");
                AssertHelper.AreEqual("{\"env\":\"prod\"}", updated.Tags, "Tags");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Update_VerifyPersistence", async ct =>
            {
                AssistantDocument read = await driver.AssistantDocument.ReadAsync(createdId, ct);
                AssertHelper.AreEqual("Updated Document", read.Name, "Name after re-read");
                AssertHelper.AreEqual(DocumentStatusEnum.Completed, read.Status, "Status after re-read");
                AssertHelper.AreEqual(204800L, read.SizeBytes, "SizeBytes after re-read");
            }, token);

            await runner.RunTestAsync("AssistantDocument.UpdateStatus", async ct =>
            {
                await driver.AssistantDocument.UpdateStatusAsync(createdId, DocumentStatusEnum.Failed, "Ingestion error occurred", ct);
                AssistantDocument read = await driver.AssistantDocument.ReadAsync(createdId, ct);
                AssertHelper.AreEqual(DocumentStatusEnum.Failed, read.Status, "Status after UpdateStatus");
                AssertHelper.AreEqual("Ingestion error occurred", read.StatusMessage, "StatusMessage after UpdateStatus");
            }, token);

            await runner.RunTestAsync("AssistantDocument.UpdateStatus_AllStatuses", async ct =>
            {
                AssistantDocument doc = await driver.AssistantDocument.CreateAsync(new AssistantDocument { Name = "Status Cycle Doc" }, ct);
                DocumentStatusEnum[] statuses = new[]
                {
                    DocumentStatusEnum.Uploading,
                    DocumentStatusEnum.Uploaded,
                    DocumentStatusEnum.TypeDetecting,
                    DocumentStatusEnum.TypeDetectionSuccess,
                    DocumentStatusEnum.Processing,
                    DocumentStatusEnum.ProcessingChunks,
                    DocumentStatusEnum.Summarizing,
                    DocumentStatusEnum.StoringEmbeddings,
                    DocumentStatusEnum.Completed,
                    DocumentStatusEnum.Failed,
                    DocumentStatusEnum.TypeDetectionFailed
                };

                foreach (DocumentStatusEnum status in statuses)
                {
                    await driver.AssistantDocument.UpdateStatusAsync(doc.Id, status, $"Testing {status}", ct);
                    AssistantDocument read = await driver.AssistantDocument.ReadAsync(doc.Id, ct);
                    AssertHelper.AreEqual(status, read.Status, $"Status={status}");
                    AssertHelper.AreEqual($"Testing {status}", read.StatusMessage, $"StatusMessage={status}");
                }
            }, token);

            await runner.RunTestAsync("AssistantDocument.UpdateChunkRecordIds", async ct =>
            {
                string newChunks = "[\"chunk_a\",\"chunk_b\",\"chunk_c\",\"chunk_d\"]";
                await driver.AssistantDocument.UpdateChunkRecordIdsAsync(createdId, newChunks, ct);
                AssistantDocument read = await driver.AssistantDocument.ReadAsync(createdId, ct);
                AssertHelper.AreEqual(newChunks, read.ChunkRecordIds, "ChunkRecordIds after update");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<AssistantDocument> result = await driver.AssistantDocument.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "objects count");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<AssistantDocument> r1 = await driver.AssistantDocument.EnumerateAsync(q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<AssistantDocument> r2 = await driver.AssistantDocument.EnumerateAsync(q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different docs on different pages");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Enumerate_BucketFilter", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    BucketNameFilter = "test-bucket"
                };
                EnumerationResult<AssistantDocument> result = await driver.AssistantDocument.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "filtered result");
                foreach (AssistantDocument doc in result.Objects)
                    AssertHelper.AreEqual("test-bucket", doc.BucketName, "BucketName filter");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Enumerate_CollectionFilter", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    CollectionIdFilter = "col_abc_123"
                };
                EnumerationResult<AssistantDocument> result = await driver.AssistantDocument.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "filtered result");
                foreach (AssistantDocument doc in result.Objects)
                    AssertHelper.AreEqual("col_abc_123", doc.CollectionId, "CollectionId filter");
            }, token);

            await runner.RunTestAsync("AssistantDocument.Delete", async ct =>
            {
                AssistantDocument doc = await driver.AssistantDocument.CreateAsync(new AssistantDocument { Name = "Delete Me Doc" }, ct);
                await driver.AssistantDocument.DeleteAsync(doc.Id, ct);

                AssistantDocument read = await driver.AssistantDocument.ReadAsync(doc.Id, ct);
                AssertHelper.IsNull(read, "deleted document");
                bool exists = await driver.AssistantDocument.ExistsAsync(doc.Id, ct);
                AssertHelper.IsFalse(exists, "deleted document should not exist");
            }, token);
        }
    }
}
