namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class IngestionRuleTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- IngestionRule Tests ---");

            string tenantId = TenantTests.TestTenantId;
            string createdId = null;

            await runner.RunTestAsync("IngestionRule.Create", async ct =>
            {
                IngestionRule rule = new IngestionRule
                {
                    TenantId = tenantId,
                    Name = "Test Ingestion Rule",
                    Description = "A rule for testing ingestion",
                    Bucket = "ingest-bucket",
                    CollectionName = "test-collection",
                    CollectionId = "col_ingest_123",
                    Labels = new List<string> { "docs", "test" },
                    Tags = new Dictionary<string, string> { { "env", "test" }, { "tier", "free" } },
                    Summarization = new IngestionSummarizationConfig
                    {
                        CompletionEndpointId = "ep_summ_1",
                        Order = SummarizationOrderEnum.BottomUp,
                        SummarizationPrompt = "Summarize: {content}",
                        MaxSummaryTokens = 512,
                        MinCellLength = 256,
                        MaxParallelTasks = 2,
                        MaxRetriesPerSummary = 5,
                        MaxRetries = 15,
                        TimeoutMs = 60000
                    },
                    Chunking = new IngestionChunkingConfig
                    {
                        Strategy = "SentenceBased",
                        FixedTokenCount = 512,
                        OverlapCount = 50,
                        OverlapPercentage = 0.1,
                        OverlapStrategy = "SlidingWindow",
                        RowGroupSize = 10,
                        ContextPrefix = "Document context: ",
                        RegexPattern = null
                    },
                    Embedding = new IngestionEmbeddingConfig
                    {
                        EmbeddingEndpointId = "ep_embed_1",
                        L2Normalization = true
                    }
                };

                IngestionRule created = await driver.IngestionRule.CreateAsync(rule, ct);
                AssertHelper.IsNotNull(created, "created rule");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "irule_", "Id prefix");
                AssertHelper.AreEqual("Test Ingestion Rule", created.Name, "Name");
                AssertHelper.AreEqual("A rule for testing ingestion", created.Description, "Description");
                AssertHelper.AreEqual("ingest-bucket", created.Bucket, "Bucket");
                AssertHelper.AreEqual("test-collection", created.CollectionName, "CollectionName");
                AssertHelper.AreEqual("col_ingest_123", created.CollectionId, "CollectionId");
                AssertHelper.IsNotNull(created.Labels, "Labels");
                AssertHelper.AreEqual(2, created.Labels.Count, "Labels count");
                AssertHelper.IsNotNull(created.Tags, "Tags");
                AssertHelper.AreEqual(2, created.Tags.Count, "Tags count");

                AssertHelper.IsNotNull(created.Summarization, "Summarization");
                AssertHelper.AreEqual("ep_summ_1", created.Summarization.CompletionEndpointId, "Summarization.CompletionEndpointId");
                AssertHelper.AreEqual(SummarizationOrderEnum.BottomUp, created.Summarization.Order, "Summarization.Order");
                AssertHelper.AreEqual("Summarize: {content}", created.Summarization.SummarizationPrompt, "Summarization.SummarizationPrompt");
                AssertHelper.AreEqual(512, created.Summarization.MaxSummaryTokens, "Summarization.MaxSummaryTokens");
                AssertHelper.AreEqual(256, created.Summarization.MinCellLength, "Summarization.MinCellLength");
                AssertHelper.AreEqual(2, created.Summarization.MaxParallelTasks, "Summarization.MaxParallelTasks");
                AssertHelper.AreEqual(5, created.Summarization.MaxRetriesPerSummary, "Summarization.MaxRetriesPerSummary");
                AssertHelper.AreEqual(15, created.Summarization.MaxRetries, "Summarization.MaxRetries");
                AssertHelper.AreEqual(60000, created.Summarization.TimeoutMs, "Summarization.TimeoutMs");

                AssertHelper.IsNotNull(created.Chunking, "Chunking");
                AssertHelper.AreEqual("SentenceBased", created.Chunking.Strategy, "Chunking.Strategy");
                AssertHelper.AreEqual(512, created.Chunking.FixedTokenCount, "Chunking.FixedTokenCount");
                AssertHelper.AreEqual(50, created.Chunking.OverlapCount, "Chunking.OverlapCount");
                AssertHelper.AreEqual(0.1, created.Chunking.OverlapPercentage.Value, "Chunking.OverlapPercentage");
                AssertHelper.AreEqual("SlidingWindow", created.Chunking.OverlapStrategy, "Chunking.OverlapStrategy");
                AssertHelper.AreEqual(10, created.Chunking.RowGroupSize, "Chunking.RowGroupSize");
                AssertHelper.AreEqual("Document context: ", created.Chunking.ContextPrefix, "Chunking.ContextPrefix");

                AssertHelper.IsNotNull(created.Embedding, "Embedding");
                AssertHelper.AreEqual("ep_embed_1", created.Embedding.EmbeddingEndpointId, "Embedding.EmbeddingEndpointId");
                AssertHelper.AreEqual(true, created.Embedding.L2Normalization, "Embedding.L2Normalization");

                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("IngestionRule.Create_Minimal", async ct =>
            {
                IngestionRule rule = new IngestionRule
                {
                    TenantId = tenantId,
                    Name = "Minimal Rule",
                    Bucket = "min-bucket",
                    CollectionName = "min-collection"
                };

                IngestionRule created = await driver.IngestionRule.CreateAsync(rule, ct);
                AssertHelper.AreEqual("Minimal Rule", created.Name, "Name");
                AssertHelper.AreEqual("min-bucket", created.Bucket, "Bucket");
                AssertHelper.AreEqual("min-collection", created.CollectionName, "CollectionName");
                AssertHelper.IsNull(created.Description, "Description");
                AssertHelper.IsNull(created.CollectionId, "CollectionId");
            }, token);

            await runner.RunTestAsync("IngestionRule.Read", async ct =>
            {
                IngestionRule read = await driver.IngestionRule.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read rule");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("Test Ingestion Rule", read.Name, "Name");
                AssertHelper.AreEqual("A rule for testing ingestion", read.Description, "Description");
                AssertHelper.AreEqual("ingest-bucket", read.Bucket, "Bucket");
                AssertHelper.AreEqual("test-collection", read.CollectionName, "CollectionName");
                AssertHelper.AreEqual("col_ingest_123", read.CollectionId, "CollectionId");

                AssertHelper.IsNotNull(read.Summarization, "Summarization after read");
                AssertHelper.AreEqual("ep_summ_1", read.Summarization.CompletionEndpointId, "Summarization.CompletionEndpointId");
                AssertHelper.IsNotNull(read.Chunking, "Chunking after read");
                AssertHelper.AreEqual("SentenceBased", read.Chunking.Strategy, "Chunking.Strategy");
                AssertHelper.IsNotNull(read.Embedding, "Embedding after read");
                AssertHelper.AreEqual(true, read.Embedding.L2Normalization, "Embedding.L2Normalization");
            }, token);

            await runner.RunTestAsync("IngestionRule.Read_NotFound", async ct =>
            {
                IngestionRule read = await driver.IngestionRule.ReadAsync("irule_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent rule");
            }, token);

            await runner.RunTestAsync("IngestionRule.Exists_True", async ct =>
            {
                bool exists = await driver.IngestionRule.ExistsAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "rule should exist");
            }, token);

            await runner.RunTestAsync("IngestionRule.Exists_False", async ct =>
            {
                bool exists = await driver.IngestionRule.ExistsAsync("irule_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent rule");
            }, token);

            await runner.RunTestAsync("IngestionRule.Update", async ct =>
            {
                IngestionRule read = await driver.IngestionRule.ReadAsync(createdId, ct);
                read.Name = "Updated Rule";
                read.Description = "Updated description";
                read.Bucket = "updated-bucket";
                read.CollectionName = "updated-collection";
                read.CollectionId = "col_updated_456";
                read.Labels = new List<string> { "updated" };
                read.Tags = new Dictionary<string, string> { { "version", "2" } };
                read.Summarization = new IngestionSummarizationConfig
                {
                    CompletionEndpointId = "ep_summ_2",
                    Order = SummarizationOrderEnum.TopDown,
                    MaxSummaryTokens = 2048
                };
                read.Chunking = new IngestionChunkingConfig
                {
                    Strategy = "ParagraphBased",
                    FixedTokenCount = 1024
                };
                read.Embedding = new IngestionEmbeddingConfig
                {
                    EmbeddingEndpointId = "ep_embed_2",
                    L2Normalization = false
                };

                IngestionRule updated = await driver.IngestionRule.UpdateAsync(read, ct);
                AssertHelper.AreEqual("Updated Rule", updated.Name, "Name");
                AssertHelper.AreEqual("Updated description", updated.Description, "Description");
                AssertHelper.AreEqual("updated-bucket", updated.Bucket, "Bucket");
                AssertHelper.AreEqual("updated-collection", updated.CollectionName, "CollectionName");
                AssertHelper.AreEqual("col_updated_456", updated.CollectionId, "CollectionId");
                AssertHelper.AreEqual(1, updated.Labels.Count, "Labels count");
                AssertHelper.AreEqual(1, updated.Tags.Count, "Tags count");
                AssertHelper.AreEqual(SummarizationOrderEnum.TopDown, updated.Summarization.Order, "Summarization.Order");
                AssertHelper.AreEqual("ParagraphBased", updated.Chunking.Strategy, "Chunking.Strategy");
                AssertHelper.AreEqual(false, updated.Embedding.L2Normalization, "Embedding.L2Normalization");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("IngestionRule.Update_VerifyPersistence", async ct =>
            {
                IngestionRule read = await driver.IngestionRule.ReadAsync(createdId, ct);
                AssertHelper.AreEqual("Updated Rule", read.Name, "Name after re-read");
                AssertHelper.AreEqual("updated-bucket", read.Bucket, "Bucket after re-read");
            }, token);

            await runner.RunTestAsync("IngestionRule.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<IngestionRule> result = await driver.IngestionRule.EnumerateAsync(tenantId, query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "objects count");
            }, token);

            await runner.RunTestAsync("IngestionRule.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<IngestionRule> r1 = await driver.IngestionRule.EnumerateAsync(tenantId, q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<IngestionRule> r2 = await driver.IngestionRule.EnumerateAsync(tenantId, q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
            }, token);

            await runner.RunTestAsync("IngestionRule.Delete", async ct =>
            {
                IngestionRule rule = await driver.IngestionRule.CreateAsync(new IngestionRule
                {
                    TenantId = tenantId,
                    Name = "Delete Me Rule",
                    Bucket = "del-bucket",
                    CollectionName = "del-collection"
                }, ct);

                await driver.IngestionRule.DeleteAsync(rule.Id, ct);

                IngestionRule read = await driver.IngestionRule.ReadAsync(rule.Id, ct);
                AssertHelper.IsNull(read, "deleted rule");
                bool exists = await driver.IngestionRule.ExistsAsync(rule.Id, ct);
                AssertHelper.IsFalse(exists, "deleted rule should not exist");
            }, token);
        }
    }
}
