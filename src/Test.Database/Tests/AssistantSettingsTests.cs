namespace Test.Database.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class AssistantSettingsTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- AssistantSettings Tests ---");

            string tenantId = TenantTests.TestTenantId;
            UserMaster user = await driver.User.CreateAsync(new UserMaster { TenantId = tenantId, Email = "aset-owner@example.com" }, token);
            Assistant asst = await driver.Assistant.CreateAsync(new Assistant { TenantId = tenantId, UserId = user.Id, Name = "Settings Test Asst" }, token);
            string assistantId = asst.Id;
            string createdId = null;

            await runner.RunTestAsync("AssistantSettings.Create", async ct =>
            {
                AssistantSettings settings = new AssistantSettings
                {
                    AssistantId = assistantId,
                    Temperature = 0.9,
                    TopP = 0.8,
                    SystemPrompt = "You are a test bot.",
                    MaxTokens = 2048,
                    ContextWindow = 4096,
                    Model = "llama3:8b",
                    EnableRag = true,
                    EnableRetrievalGate = true,
                    CollectionId = "col_test_123",
                    RetrievalTopK = 5,
                    RetrievalScoreThreshold = 0.5,
                    SearchMode = "Hybrid",
                    TextWeight = 0.4,
                    FullTextSearchType = "TsRankCd",
                    FullTextLanguage = "spanish",
                    FullTextNormalization = 16,
                    FullTextMinimumScore = 0.1,
                    InferenceEndpointId = "ep_inf_123",
                    EmbeddingEndpointId = "ep_emb_456",
                    Title = "Test Chat",
                    LogoUrl = "https://example.com/logo.png",
                    FaviconUrl = "https://example.com/favicon.ico",
                    Streaming = false
                };

                AssistantSettings created = await driver.AssistantSettings.CreateAsync(settings, ct);
                AssertHelper.IsNotNull(created, "created settings");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "aset_", "Id prefix");
                AssertHelper.AreEqual(assistantId, created.AssistantId, "AssistantId");
                AssertHelper.AreEqual(0.9, created.Temperature, "Temperature");
                AssertHelper.AreEqual(0.8, created.TopP, "TopP");
                AssertHelper.AreEqual("You are a test bot.", created.SystemPrompt, "SystemPrompt");
                AssertHelper.AreEqual(2048, created.MaxTokens, "MaxTokens");
                AssertHelper.AreEqual(4096, created.ContextWindow, "ContextWindow");
                AssertHelper.AreEqual("llama3:8b", created.Model, "Model");
                AssertHelper.AreEqual(true, created.EnableRag, "EnableRag");
                AssertHelper.AreEqual(true, created.EnableRetrievalGate, "EnableRetrievalGate");
                AssertHelper.AreEqual("col_test_123", created.CollectionId, "CollectionId");
                AssertHelper.AreEqual(5, created.RetrievalTopK, "RetrievalTopK");
                AssertHelper.AreEqual(0.5, created.RetrievalScoreThreshold, "RetrievalScoreThreshold");
                AssertHelper.AreEqual("Hybrid", created.SearchMode, "SearchMode");
                AssertHelper.AreEqual(0.4, created.TextWeight, "TextWeight");
                AssertHelper.AreEqual("TsRankCd", created.FullTextSearchType, "FullTextSearchType");
                AssertHelper.AreEqual("spanish", created.FullTextLanguage, "FullTextLanguage");
                AssertHelper.AreEqual(16, created.FullTextNormalization, "FullTextNormalization");
                AssertHelper.AreEqual(0.1, created.FullTextMinimumScore.Value, "FullTextMinimumScore");
                AssertHelper.AreEqual("ep_inf_123", created.InferenceEndpointId, "InferenceEndpointId");
                AssertHelper.AreEqual("ep_emb_456", created.EmbeddingEndpointId, "EmbeddingEndpointId");
                AssertHelper.AreEqual("Test Chat", created.Title, "Title");
                AssertHelper.AreEqual("https://example.com/logo.png", created.LogoUrl, "LogoUrl");
                AssertHelper.AreEqual("https://example.com/favicon.ico", created.FaviconUrl, "FaviconUrl");
                AssertHelper.AreEqual(false, created.Streaming, "Streaming");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("AssistantSettings.Create_Defaults", async ct =>
            {
                Assistant asst2 = await driver.Assistant.CreateAsync(new Assistant { UserId = user.Id, Name = "Defaults Asst" }, ct);
                AssistantSettings settings = new AssistantSettings { AssistantId = asst2.Id };

                AssistantSettings created = await driver.AssistantSettings.CreateAsync(settings, ct);
                AssertHelper.AreEqual(0.7, created.Temperature, "default Temperature");
                AssertHelper.AreEqual(1.0, created.TopP, "default TopP");
                AssertHelper.AreEqual(4096, created.MaxTokens, "default MaxTokens");
                AssertHelper.AreEqual(8192, created.ContextWindow, "default ContextWindow");
                AssertHelper.AreEqual("gemma3:4b", created.Model, "default Model");
                AssertHelper.AreEqual(false, created.EnableRag, "default EnableRag");
                AssertHelper.AreEqual(false, created.EnableRetrievalGate, "default EnableRetrievalGate");
                AssertHelper.IsNull(created.CollectionId, "default CollectionId");
                AssertHelper.AreEqual(10, created.RetrievalTopK, "default RetrievalTopK");
                AssertHelper.AreEqual(0.3, created.RetrievalScoreThreshold, "default RetrievalScoreThreshold");
                AssertHelper.AreEqual("Vector", created.SearchMode, "default SearchMode");
                AssertHelper.AreEqual(0.3, created.TextWeight, "default TextWeight");
                AssertHelper.AreEqual("TsRank", created.FullTextSearchType, "default FullTextSearchType");
                AssertHelper.AreEqual("english", created.FullTextLanguage, "default FullTextLanguage");
                AssertHelper.AreEqual(32, created.FullTextNormalization, "default FullTextNormalization");
                AssertHelper.IsNull(created.FullTextMinimumScore, "default FullTextMinimumScore");
                AssertHelper.IsNull(created.InferenceEndpointId, "default InferenceEndpointId");
                AssertHelper.IsNull(created.EmbeddingEndpointId, "default EmbeddingEndpointId");
                AssertHelper.IsNull(created.Title, "default Title");
                AssertHelper.IsNull(created.LogoUrl, "default LogoUrl");
                AssertHelper.IsNull(created.FaviconUrl, "default FaviconUrl");
                AssertHelper.AreEqual(true, created.Streaming, "default Streaming");
            }, token);

            await runner.RunTestAsync("AssistantSettings.Read", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read settings");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(assistantId, read.AssistantId, "AssistantId");
                AssertHelper.AreEqual(0.9, read.Temperature, "Temperature");
                AssertHelper.AreEqual(0.8, read.TopP, "TopP");
                AssertHelper.AreEqual("You are a test bot.", read.SystemPrompt, "SystemPrompt");
                AssertHelper.AreEqual(2048, read.MaxTokens, "MaxTokens");
                AssertHelper.AreEqual(4096, read.ContextWindow, "ContextWindow");
                AssertHelper.AreEqual("llama3:8b", read.Model, "Model");
                AssertHelper.AreEqual(true, read.EnableRag, "EnableRag");
                AssertHelper.AreEqual(true, read.EnableRetrievalGate, "EnableRetrievalGate");
                AssertHelper.AreEqual("col_test_123", read.CollectionId, "CollectionId");
                AssertHelper.AreEqual(5, read.RetrievalTopK, "RetrievalTopK");
                AssertHelper.AreEqual(0.5, read.RetrievalScoreThreshold, "RetrievalScoreThreshold");
                AssertHelper.AreEqual("Hybrid", read.SearchMode, "SearchMode");
                AssertHelper.AreEqual(0.4, read.TextWeight, "TextWeight");
                AssertHelper.AreEqual("TsRankCd", read.FullTextSearchType, "FullTextSearchType");
                AssertHelper.AreEqual("spanish", read.FullTextLanguage, "FullTextLanguage");
                AssertHelper.AreEqual(16, read.FullTextNormalization, "FullTextNormalization");
                AssertHelper.AreEqual(0.1, read.FullTextMinimumScore.Value, "FullTextMinimumScore");
                AssertHelper.AreEqual("ep_inf_123", read.InferenceEndpointId, "InferenceEndpointId");
                AssertHelper.AreEqual("ep_emb_456", read.EmbeddingEndpointId, "EmbeddingEndpointId");
                AssertHelper.AreEqual("Test Chat", read.Title, "Title");
                AssertHelper.AreEqual("https://example.com/logo.png", read.LogoUrl, "LogoUrl");
                AssertHelper.AreEqual("https://example.com/favicon.ico", read.FaviconUrl, "FaviconUrl");
                AssertHelper.AreEqual(false, read.Streaming, "Streaming");
            }, token);

            await runner.RunTestAsync("AssistantSettings.Read_NotFound", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadAsync("aset_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent settings");
            }, token);

            await runner.RunTestAsync("AssistantSettings.ReadByAssistantId", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadByAssistantIdAsync(assistantId, ct);
                AssertHelper.IsNotNull(read, "read by assistant id");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(assistantId, read.AssistantId, "AssistantId");
            }, token);

            await runner.RunTestAsync("AssistantSettings.ReadByAssistantId_NotFound", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadByAssistantIdAsync("asst_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent assistant settings");
            }, token);

            await runner.RunTestAsync("AssistantSettings.Update", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadAsync(createdId, ct);
                read.Temperature = 1.5;
                read.TopP = 0.5;
                read.SystemPrompt = "Updated system prompt.";
                read.MaxTokens = 8192;
                read.ContextWindow = 16384;
                read.Model = "gpt-4o";
                read.EnableRag = false;
                read.EnableRetrievalGate = false;
                read.CollectionId = null;
                read.RetrievalTopK = 20;
                read.RetrievalScoreThreshold = 0.7;
                read.SearchMode = "FullText";
                read.TextWeight = 0.8;
                read.FullTextSearchType = "TsRank";
                read.FullTextLanguage = "french";
                read.FullTextNormalization = 0;
                read.FullTextMinimumScore = null;
                read.InferenceEndpointId = null;
                read.EmbeddingEndpointId = null;
                read.Title = "Updated Title";
                read.LogoUrl = null;
                read.FaviconUrl = null;
                read.Streaming = true;

                AssistantSettings updated = await driver.AssistantSettings.UpdateAsync(read, ct);
                AssertHelper.AreEqual(1.5, updated.Temperature, "updated Temperature");
                AssertHelper.AreEqual(0.5, updated.TopP, "updated TopP");
                AssertHelper.AreEqual("Updated system prompt.", updated.SystemPrompt, "updated SystemPrompt");
                AssertHelper.AreEqual(8192, updated.MaxTokens, "updated MaxTokens");
                AssertHelper.AreEqual(16384, updated.ContextWindow, "updated ContextWindow");
                AssertHelper.AreEqual("gpt-4o", updated.Model, "updated Model");
                AssertHelper.AreEqual(false, updated.EnableRag, "updated EnableRag");
                AssertHelper.AreEqual(false, updated.EnableRetrievalGate, "updated EnableRetrievalGate");
                AssertHelper.IsNull(updated.CollectionId, "updated CollectionId");
                AssertHelper.AreEqual(20, updated.RetrievalTopK, "updated RetrievalTopK");
                AssertHelper.AreEqual(0.7, updated.RetrievalScoreThreshold, "updated RetrievalScoreThreshold");
                AssertHelper.AreEqual("FullText", updated.SearchMode, "updated SearchMode");
                AssertHelper.AreEqual(0.8, updated.TextWeight, "updated TextWeight");
                AssertHelper.AreEqual("TsRank", updated.FullTextSearchType, "updated FullTextSearchType");
                AssertHelper.AreEqual("french", updated.FullTextLanguage, "updated FullTextLanguage");
                AssertHelper.AreEqual(0, updated.FullTextNormalization, "updated FullTextNormalization");
                AssertHelper.IsNull(updated.FullTextMinimumScore, "updated FullTextMinimumScore");
                AssertHelper.IsNull(updated.InferenceEndpointId, "updated InferenceEndpointId");
                AssertHelper.IsNull(updated.EmbeddingEndpointId, "updated EmbeddingEndpointId");
                AssertHelper.AreEqual("Updated Title", updated.Title, "updated Title");
                AssertHelper.IsNull(updated.LogoUrl, "updated LogoUrl");
                AssertHelper.IsNull(updated.FaviconUrl, "updated FaviconUrl");
                AssertHelper.AreEqual(true, updated.Streaming, "updated Streaming");
            }, token);

            await runner.RunTestAsync("AssistantSettings.Update_VerifyPersistence", async ct =>
            {
                AssistantSettings read = await driver.AssistantSettings.ReadAsync(createdId, ct);
                AssertHelper.AreEqual(1.5, read.Temperature, "Temperature after re-read");
                AssertHelper.AreEqual("gpt-4o", read.Model, "Model after re-read");
                AssertHelper.AreEqual("FullText", read.SearchMode, "SearchMode after re-read");
                AssertHelper.AreEqual(true, read.Streaming, "Streaming after re-read");
            }, token);

            await runner.RunTestAsync("AssistantSettings.Delete", async ct =>
            {
                Assistant asstDel = await driver.Assistant.CreateAsync(new Assistant { UserId = user.Id, Name = "Del Settings" }, ct);
                AssistantSettings settings = await driver.AssistantSettings.CreateAsync(new AssistantSettings { AssistantId = asstDel.Id }, ct);

                await driver.AssistantSettings.DeleteAsync(settings.Id, ct);

                AssistantSettings read = await driver.AssistantSettings.ReadAsync(settings.Id, ct);
                AssertHelper.IsNull(read, "deleted settings");
            }, token);

            await runner.RunTestAsync("AssistantSettings.DeleteByAssistantId", async ct =>
            {
                Assistant asstDel2 = await driver.Assistant.CreateAsync(new Assistant { UserId = user.Id, Name = "Del By AsstId" }, ct);
                AssistantSettings settings = await driver.AssistantSettings.CreateAsync(new AssistantSettings { AssistantId = asstDel2.Id }, ct);

                await driver.AssistantSettings.DeleteByAssistantIdAsync(asstDel2.Id, ct);

                AssistantSettings read = await driver.AssistantSettings.ReadByAssistantIdAsync(asstDel2.Id, ct);
                AssertHelper.IsNull(read, "deleted settings by assistant id");
            }, token);
        }
    }
}
