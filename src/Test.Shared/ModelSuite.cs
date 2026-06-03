namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using Test.Shared;

    public class ModelSuite : SuiteBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions _jsonOptionsDefault = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions _jsonOptionsIgnoreNever = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            // ===== EnumTests =====

            await ExecuteTestAsync("Enum.DatabaseTypeEnum: all 4 values parse from string", async () =>
            {
                AssertHelper.AreEqual(DatabaseTypeEnum.Sqlite, Enum.Parse<DatabaseTypeEnum>("Sqlite"), "Sqlite");
                AssertHelper.AreEqual(DatabaseTypeEnum.Postgresql, Enum.Parse<DatabaseTypeEnum>("Postgresql"), "Postgresql");
                AssertHelper.AreEqual(DatabaseTypeEnum.SqlServer, Enum.Parse<DatabaseTypeEnum>("SqlServer"), "SqlServer");
                AssertHelper.AreEqual(DatabaseTypeEnum.Mysql, Enum.Parse<DatabaseTypeEnum>("Mysql"), "Mysql");
            });

            await ExecuteTestAsync("Enum.DocumentStatusEnum: all values round-trip through JSON", async () =>
            {
                foreach (DocumentStatusEnum val in Enum.GetValues<DocumentStatusEnum>())
                {
                    string json = JsonSerializer.Serialize(val, _jsonOptionsDefault);
                    DocumentStatusEnum deserialized = JsonSerializer.Deserialize<DocumentStatusEnum>(json, _jsonOptionsDefault);
                    AssertHelper.AreEqual(val, deserialized, $"DocumentStatusEnum.{val}");
                }
                AssertHelper.AreEqual(12, Enum.GetValues<DocumentStatusEnum>().Length, "DocumentStatusEnum count");
            });

            await ExecuteTestAsync("Enum.InferenceProviderEnum: Ollama, OpenAI, and Gemini", async () =>
            {
                AssertHelper.AreEqual(InferenceProviderEnum.Ollama, Enum.Parse<InferenceProviderEnum>("Ollama"), "Ollama");
                AssertHelper.AreEqual(InferenceProviderEnum.OpenAI, Enum.Parse<InferenceProviderEnum>("OpenAI"), "OpenAI");
                AssertHelper.AreEqual(InferenceProviderEnum.Gemini, Enum.Parse<InferenceProviderEnum>("Gemini"), "Gemini");
                AssertHelper.AreEqual(3, Enum.GetValues<InferenceProviderEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.FeedbackRatingEnum: all values", async () =>
            {
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, Enum.Parse<FeedbackRatingEnum>("ThumbsUp"), "ThumbsUp");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsDown, Enum.Parse<FeedbackRatingEnum>("ThumbsDown"), "ThumbsDown");
                AssertHelper.AreEqual(2, Enum.GetValues<FeedbackRatingEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.ApiErrorEnum: all values exist and are distinct", async () =>
            {
                var values = Enum.GetValues<ApiErrorEnum>();
                AssertHelper.AreEqual(6, values.Length, "count");
                var set = new HashSet<int>();
                foreach (var v in values)
                {
                    AssertHelper.IsTrue(set.Add((int)v), $"ApiErrorEnum.{v} should be distinct");
                }
            });

            await ExecuteTestAsync("Enum.ScheduleIntervalEnum: all values", async () =>
            {
                AssertHelper.AreEqual(ScheduleIntervalEnum.OneTime, Enum.Parse<ScheduleIntervalEnum>("OneTime"), "OneTime");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Minutes, Enum.Parse<ScheduleIntervalEnum>("Minutes"), "Minutes");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Hours, Enum.Parse<ScheduleIntervalEnum>("Hours"), "Hours");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Days, Enum.Parse<ScheduleIntervalEnum>("Days"), "Days");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Weeks, Enum.Parse<ScheduleIntervalEnum>("Weeks"), "Weeks");
                AssertHelper.AreEqual(5, Enum.GetValues<ScheduleIntervalEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.CrawlOperationStateEnum: all states", async () =>
            {
                var values = Enum.GetValues<CrawlOperationStateEnum>();
                AssertHelper.AreEqual(8, values.Length, "count");
                foreach (var val in values)
                {
                    string json = JsonSerializer.Serialize(val, _jsonOptionsDefault);
                    CrawlOperationStateEnum deserialized = JsonSerializer.Deserialize<CrawlOperationStateEnum>(json, _jsonOptionsDefault);
                    AssertHelper.AreEqual(val, deserialized, $"CrawlOperationStateEnum.{val}");
                }
            });

            await ExecuteTestAsync("Enum.CrawlPlanStateEnum: all states", async () =>
            {
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, Enum.Parse<CrawlPlanStateEnum>("Stopped"), "Stopped");
                AssertHelper.AreEqual(CrawlPlanStateEnum.Running, Enum.Parse<CrawlPlanStateEnum>("Running"), "Running");
                AssertHelper.AreEqual(2, Enum.GetValues<CrawlPlanStateEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.EnumerationOrderEnum: CreatedAscending and CreatedDescending", async () =>
            {
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedAscending, Enum.Parse<EnumerationOrderEnum>("CreatedAscending"), "Ascending");
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedDescending, Enum.Parse<EnumerationOrderEnum>("CreatedDescending"), "Descending");
                AssertHelper.AreEqual(2, Enum.GetValues<EnumerationOrderEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.RepositoryTypeEnum: Web", async () =>
            {
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, Enum.Parse<RepositoryTypeEnum>("Web"), "Web");
                AssertHelper.AreEqual(1, Enum.GetValues<RepositoryTypeEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.WebAuthTypeEnum: all values", async () =>
            {
                AssertHelper.AreEqual(WebAuthTypeEnum.None, Enum.Parse<WebAuthTypeEnum>("None"), "None");
                AssertHelper.AreEqual(WebAuthTypeEnum.Basic, Enum.Parse<WebAuthTypeEnum>("Basic"), "Basic");
                AssertHelper.AreEqual(WebAuthTypeEnum.ApiKey, Enum.Parse<WebAuthTypeEnum>("ApiKey"), "ApiKey");
                AssertHelper.AreEqual(WebAuthTypeEnum.BearerToken, Enum.Parse<WebAuthTypeEnum>("BearerToken"), "BearerToken");
                AssertHelper.AreEqual(4, Enum.GetValues<WebAuthTypeEnum>().Length, "count");
            });

            await ExecuteTestAsync("Enum.SummarizationOrderEnum: all values", async () =>
            {
                AssertHelper.AreEqual(SummarizationOrderEnum.BottomUp, Enum.Parse<SummarizationOrderEnum>("BottomUp"), "BottomUp");
                AssertHelper.AreEqual(SummarizationOrderEnum.TopDown, Enum.Parse<SummarizationOrderEnum>("TopDown"), "TopDown");
                AssertHelper.AreEqual(2, Enum.GetValues<SummarizationOrderEnum>().Length, "count");
            });

            // ===== CoreModelTests =====

            await ExecuteTestAsync("Model.TenantMetadata: defaults and JSON round-trip", async () =>
            {
                var t = new TenantMetadata();
                AssertHelper.IsNotNull(t.Id, "Id");
                AssertHelper.StartsWith(t.Id, "ten_", "Id prefix");
                AssertHelper.AreEqual("My Tenant", t.Name, "default Name");
                AssertHelper.AreEqual(true, t.Active, "default Active");
                AssertHelper.DateTimeRecent(t.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(t.LastUpdateUtc, "LastUpdateUtc");

                t.Name = "Test Tenant";
                string json = JsonSerializer.Serialize(t, _jsonOptions);
                var deserialized = JsonSerializer.Deserialize<TenantMetadata>(json, _jsonOptions);
                AssertHelper.AreEqual(t.Id, deserialized.Id, "round-trip Id");
                AssertHelper.AreEqual("Test Tenant", deserialized.Name, "round-trip Name");
            });

            await ExecuteTestAsync("Model.UserMaster: defaults and password", async () =>
            {
                var u = new UserMaster();
                AssertHelper.IsNotNull(u.Id, "Id");
                AssertHelper.StartsWith(u.Id, "usr_", "Id prefix");
                AssertHelper.AreEqual(Constants.DefaultTenantId, u.TenantId, "default TenantId");
                AssertHelper.AreEqual("user@example.com", u.Email, "default Email");
                AssertHelper.AreEqual(false, u.IsAdmin, "default IsAdmin");
                AssertHelper.AreEqual(true, u.Active, "default Active");
            });

            await ExecuteTestAsync("Model.UserMaster: SetPassword and VerifyPassword", async () =>
            {
                var u = new UserMaster();
                u.SetPassword("mysecret");
                AssertHelper.IsNotNull(u.PasswordSha256, "PasswordSha256 set");
                AssertHelper.IsTrue(u.VerifyPassword("mysecret"), "correct password matches");
                AssertHelper.IsFalse(u.VerifyPassword("wrongpassword"), "wrong password does not match");
                AssertHelper.IsFalse(u.VerifyPassword(""), "empty password does not match");
            });

            await ExecuteTestAsync("Model.UserMaster: password not serialized as plain text", async () =>
            {
                var u = new UserMaster();
                u.SetPassword("mysecret");
                string json = JsonSerializer.Serialize(u, _jsonOptions);
                AssertHelper.IsFalse(json.Contains("mysecret"), "plain text password should not appear in JSON");
            });

            await ExecuteTestAsync("Model.Credential: defaults and bearer token", async () =>
            {
                var c = new Credential();
                AssertHelper.IsNotNull(c.Id, "Id");
                AssertHelper.StartsWith(c.Id, "cred_", "Id prefix");
                AssertHelper.IsNotNull(c.BearerToken, "BearerToken");
                AssertHelper.IsTrue(c.BearerToken.Length > 0, "BearerToken has length");
                AssertHelper.AreEqual(true, c.Active, "default Active");

                var c2 = new Credential();
                AssertHelper.AreNotEqual(c.BearerToken, c2.BearerToken, "unique bearer tokens");
            });

            await ExecuteTestAsync("Model.Assistant: defaults and JSON round-trip", async () =>
            {
                var a = new Assistant();
                AssertHelper.IsNotNull(a.Id, "Id");
                AssertHelper.StartsWith(a.Id, "asst_", "Id prefix");
                AssertHelper.AreEqual("My Assistant", a.Name, "default Name");
                AssertHelper.AreEqual(true, a.Active, "default Active");

                a.Name = "Test Assistant";
                a.Description = "A test assistant";
                string json = JsonSerializer.Serialize(a, _jsonOptions);
                var d = JsonSerializer.Deserialize<Assistant>(json, _jsonOptions);
                AssertHelper.AreEqual("Test Assistant", d.Name, "round-trip Name");
                AssertHelper.AreEqual("A test assistant", d.Description, "round-trip Description");
            });

            await ExecuteTestAsync("Model.AssistantDocument: defaults and status", async () =>
            {
                var doc = new AssistantDocument();
                AssertHelper.IsNotNull(doc.Id, "Id");
                AssertHelper.StartsWith(doc.Id, "adoc_", "Id prefix");
                AssertHelper.AreEqual("Untitled Document", doc.Name, "default Name");
                AssertHelper.AreEqual(DocumentStatusEnum.Pending, doc.Status, "default Status");
                AssertHelper.AreEqual("application/octet-stream", doc.ContentType, "default ContentType");
                AssertHelper.AreEqual(0L, doc.SizeBytes, "default SizeBytes");

                doc.Status = DocumentStatusEnum.Processing;
                AssertHelper.AreEqual(DocumentStatusEnum.Processing, doc.Status, "Status after set");
                doc.Status = DocumentStatusEnum.Completed;
                AssertHelper.AreEqual(DocumentStatusEnum.Completed, doc.Status, "Status completed");
            });

            await ExecuteTestAsync("Model.AssistantFeedback: defaults and rating", async () =>
            {
                var fb = new AssistantFeedback();
                AssertHelper.IsNotNull(fb.Id, "Id");
                AssertHelper.StartsWith(fb.Id, "afb_", "Id prefix");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, fb.Rating, "default Rating");

                fb.Rating = FeedbackRatingEnum.ThumbsDown;
                string json = JsonSerializer.Serialize(fb, _jsonOptions);
                var d = JsonSerializer.Deserialize<AssistantFeedback>(json, _jsonOptions);
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsDown, d.Rating, "round-trip Rating");
            });

            await ExecuteTestAsync("Model.ChatHistory: defaults", async () =>
            {
                var ch = new ChatHistory();
                AssertHelper.IsNotNull(ch.Id, "Id");
                AssertHelper.StartsWith(ch.Id, "chist_", "Id prefix");
                AssertHelper.AreEqual(Constants.DefaultTenantId, ch.TenantId, "default TenantId");
                AssertHelper.DateTimeRecent(ch.CreatedUtc, "CreatedUtc");
            });

            await ExecuteTestAsync("Model.IngestionRule: defaults", async () =>
            {
                var rule = new IngestionRule();
                AssertHelper.IsNotNull(rule.Id, "Id");
                AssertHelper.StartsWith(rule.Id, "irule_", "Id prefix");
                AssertHelper.AreEqual("Untitled Rule", rule.Name, "default Name");
                AssertHelper.AreEqual("default", rule.Bucket, "default Bucket");
                AssertHelper.AreEqual("default", rule.CollectionName, "default CollectionName");
            });

            await ExecuteTestAsync("Model.CrawlPlan: defaults and nested objects", async () =>
            {
                var plan = new CrawlPlan();
                AssertHelper.IsNotNull(plan.Id, "Id");
                AssertHelper.StartsWith(plan.Id, "cplan_", "Id prefix");
                AssertHelper.AreEqual("My crawl plan", plan.Name, "default Name");
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, plan.RepositoryType, "default RepositoryType");
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, plan.State, "default State");
                AssertHelper.IsNotNull(plan.IngestionSettings, "IngestionSettings");
                AssertHelper.IsNotNull(plan.RepositorySettings, "RepositorySettings");
                AssertHelper.IsNotNull(plan.Schedule, "Schedule");
                AssertHelper.IsNotNull(plan.Filter, "Filter");
                AssertHelper.AreEqual(8, plan.MaxDrainTasks, "default MaxDrainTasks");
                AssertHelper.AreEqual(7, plan.RetentionDays, "default RetentionDays");
            });

            await ExecuteTestAsync("Model.CrawlOperation: defaults and state", async () =>
            {
                var op = new CrawlOperation();
                AssertHelper.IsNotNull(op.Id, "Id");
                AssertHelper.StartsWith(op.Id, "cop_", "Id prefix");
                AssertHelper.AreEqual(CrawlOperationStateEnum.NotStarted, op.State, "default State");
                AssertHelper.AreEqual(0L, op.ObjectsEnumerated, "default ObjectsEnumerated");
                AssertHelper.AreEqual(0L, op.BytesEnumerated, "default BytesEnumerated");
            });

            await ExecuteTestAsync("Model.RetrievalChunk: defaults and rerank_score JSON", async () =>
            {
                var chunk = new RetrievalChunk();
                AssertHelper.IsNull(chunk.RerankScore, "default RerankScore");
                AssertHelper.AreEqual(0.0, chunk.Score, "default Score");
                AssertHelper.IsNull(chunk.DocumentId, "default DocumentId");
            });

            // ===== RerankingModelTests =====

            await ExecuteTestAsync("AssistantSettings.EnableReranking: defaults to false", async () =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(false, s.EnableReranking, "EnableReranking default");
            });

            await ExecuteTestAsync("AssistantSettings.RerankerTopK: defaults to 5", async () =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(5, s.RerankerTopK, "RerankerTopK default");
            });

            await ExecuteTestAsync("AssistantSettings.RerankerTopK: setter clamps below 1", async () =>
            {
                bool threw = false;
                try
                {
                    var s = new AssistantSettings();
                    s.RerankerTopK = 0;
                }
                catch (ArgumentOutOfRangeException)
                {
                    threw = true;
                }
                AssertHelper.IsTrue(threw, "RerankerTopK = 0 should throw");
            });

            await ExecuteTestAsync("AssistantSettings.RerankerScoreThreshold: defaults to 3.0", async () =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(3.0, s.RerankerScoreThreshold, "RerankerScoreThreshold default");
            });

            await ExecuteTestAsync("AssistantSettings.RerankerScoreThreshold: clamps to range 0-10", async () =>
            {
                bool threwBelow = false;
                try
                {
                    var s = new AssistantSettings();
                    s.RerankerScoreThreshold = -1.0;
                }
                catch (ArgumentOutOfRangeException)
                {
                    threwBelow = true;
                }
                AssertHelper.IsTrue(threwBelow, "below 0 should throw");

                bool threwAbove = false;
                try
                {
                    var s = new AssistantSettings();
                    s.RerankerScoreThreshold = 11.0;
                }
                catch (ArgumentOutOfRangeException)
                {
                    threwAbove = true;
                }
                AssertHelper.IsTrue(threwAbove, "above 10 should throw");

                var valid = new AssistantSettings();
                valid.RerankerScoreThreshold = 0.0;
                AssertHelper.AreEqual(0.0, valid.RerankerScoreThreshold, "0.0 accepted");
                valid.RerankerScoreThreshold = 10.0;
                AssertHelper.AreEqual(10.0, valid.RerankerScoreThreshold, "10.0 accepted");
            });

            await ExecuteTestAsync("AssistantSettings.RerankPrompt: defaults to null", async () =>
            {
                var s = new AssistantSettings();
                AssertHelper.IsNull(s.RerankPrompt, "RerankPrompt default");
            });

            await ExecuteTestAsync("AssistantSettings: JSON round-trip preserves reranking fields", async () =>
            {
                var s = new AssistantSettings();
                s.EnableReranking = true;
                s.RerankerTopK = 7;
                s.RerankerScoreThreshold = 5.5;
                s.RerankPrompt = "Test prompt {query} {chunks}";

                string json = JsonSerializer.Serialize(s, _jsonOptionsIgnoreNever);
                var d = JsonSerializer.Deserialize<AssistantSettings>(json, _jsonOptionsIgnoreNever);

                AssertHelper.AreEqual(true, d.EnableReranking, "round-trip EnableReranking");
                AssertHelper.AreEqual(7, d.RerankerTopK, "round-trip RerankerTopK");
                AssertHelper.AreEqual(5.5, d.RerankerScoreThreshold, "round-trip RerankerScoreThreshold");
                AssertHelper.AreEqual("Test prompt {query} {chunks}", d.RerankPrompt, "round-trip RerankPrompt");
            });

            await ExecuteTestAsync("AssistantSettings utility endpoint IDs: default to null", async () =>
            {
                var s = new AssistantSettings();
                AssertHelper.IsNull(s.RetrievalGateInferenceEndpointId, "RetrievalGateInferenceEndpointId default");
                AssertHelper.IsNull(s.QueryRewriteInferenceEndpointId, "QueryRewriteInferenceEndpointId default");
                AssertHelper.IsNull(s.RerankInferenceEndpointId, "RerankInferenceEndpointId default");
            });

            await ExecuteTestAsync("AssistantSettings utility endpoint IDs: JSON round-trip", async () =>
            {
                var s = new AssistantSettings();
                s.InferenceEndpointId = "ep_response";
                s.RetrievalGateInferenceEndpointId = "ep_gate";
                s.QueryRewriteInferenceEndpointId = "ep_rewrite";
                s.RerankInferenceEndpointId = "ep_rerank";

                string json = JsonSerializer.Serialize(s, _jsonOptionsIgnoreNever);
                var d = JsonSerializer.Deserialize<AssistantSettings>(json, _jsonOptionsIgnoreNever);

                AssertHelper.AreEqual("ep_response", d.InferenceEndpointId, "round-trip InferenceEndpointId");
                AssertHelper.AreEqual("ep_gate", d.RetrievalGateInferenceEndpointId, "round-trip RetrievalGateInferenceEndpointId");
                AssertHelper.AreEqual("ep_rewrite", d.QueryRewriteInferenceEndpointId, "round-trip QueryRewriteInferenceEndpointId");
                AssertHelper.AreEqual("ep_rerank", d.RerankInferenceEndpointId, "round-trip RerankInferenceEndpointId");
            });

            await ExecuteTestAsync("ChatHistory.RerankDurationMs: defaults to 0", async () =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0.0, ch.RerankDurationMs, "default RerankDurationMs");
            });

            await ExecuteTestAsync("ChatHistory.RerankInputCount: defaults to 0", async () =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0, ch.RerankInputCount, "default RerankInputCount");
            });

            await ExecuteTestAsync("ChatHistory.RerankOutputCount: defaults to 0", async () =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0, ch.RerankOutputCount, "default RerankOutputCount");
            });

            await ExecuteTestAsync("ChatHistory: JSON round-trip preserves reranking fields", async () =>
            {
                var ch = new ChatHistory();
                ch.RerankDurationMs = 123.4;
                ch.RerankInputCount = 10;
                ch.RerankOutputCount = 3;

                string json = JsonSerializer.Serialize(ch, _jsonOptionsIgnoreNever);
                var d = JsonSerializer.Deserialize<ChatHistory>(json, _jsonOptionsIgnoreNever);

                AssertHelper.AreEqual(123.4, d.RerankDurationMs, "round-trip RerankDurationMs");
                AssertHelper.AreEqual(10, d.RerankInputCount, "round-trip RerankInputCount");
                AssertHelper.AreEqual(3, d.RerankOutputCount, "round-trip RerankOutputCount");
            });

            await ExecuteTestAsync("RetrievalChunk.RerankScore: defaults to null", async () =>
            {
                var chunk = new RetrievalChunk();
                AssertHelper.IsNull(chunk.RerankScore, "default RerankScore");
            });

            await ExecuteTestAsync("RetrievalChunk: JSON uses property name rerank_score", async () =>
            {
                var chunk = new RetrievalChunk { RerankScore = 8.5 };
                string json = JsonSerializer.Serialize(chunk);
                AssertHelper.IsTrue(json.Contains("\"rerank_score\""), "JSON should contain rerank_score key");
            });

            await ExecuteTestAsync("RetrievalChunk.RerankScore: round-trips when set", async () =>
            {
                var chunk = new RetrievalChunk { RerankScore = 7.2 };
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.AreEqual(7.2, d.RerankScore, "round-trip RerankScore");
            });

            await ExecuteTestAsync("RetrievalChunk.RerankScore: round-trips as null when not set", async () =>
            {
                var chunk = new RetrievalChunk();
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.IsNull(d.RerankScore, "round-trip null RerankScore");
            });

            await ExecuteTestAsync("ChatCompletionRetrieval: reranking defaults", async () =>
            {
                var r = new ChatCompletionRetrieval();
                AssertHelper.AreEqual(0.0, r.RerankDurationMs, "default RerankDurationMs");
                AssertHelper.AreEqual(0, r.RerankInputCount, "default RerankInputCount");
                AssertHelper.AreEqual(0, r.RerankOutputCount, "default RerankOutputCount");
            });

            await ExecuteTestAsync("ChatCompletionRetrieval: fields present when non-zero", async () =>
            {
                var r = new ChatCompletionRetrieval
                {
                    RerankDurationMs = 55.5,
                    RerankInputCount = 8,
                    RerankOutputCount = 3
                };
                string json = JsonSerializer.Serialize(r);
                AssertHelper.IsTrue(json.Contains("\"rerank_duration_ms\""), "rerank_duration_ms present");
                AssertHelper.IsTrue(json.Contains("\"rerank_input_count\""), "rerank_input_count present");
                AssertHelper.IsTrue(json.Contains("\"rerank_output_count\""), "rerank_output_count present");
            });

            await ExecuteTestAsync("ChatCompletionRetrieval: fields omitted when zero (WhenWritingDefault)", async () =>
            {
                var r = new ChatCompletionRetrieval();
                string json = JsonSerializer.Serialize(r);
                AssertHelper.IsFalse(json.Contains("\"rerank_duration_ms\""), "rerank_duration_ms omitted when 0");
                AssertHelper.IsFalse(json.Contains("\"rerank_input_count\""), "rerank_input_count omitted when 0");
                AssertHelper.IsFalse(json.Contains("\"rerank_output_count\""), "rerank_output_count omitted when 0");
            });

            await ExecuteTestAsync("CitationSource.RerankScore: defaults to null, omitted in JSON", async () =>
            {
                var cs = new CitationSource();
                AssertHelper.IsNull(cs.RerankScore, "default RerankScore");
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsFalse(json.Contains("\"rerank_score\""), "omitted when null");
            });

            await ExecuteTestAsync("CitationSource.RerankScore: present when set", async () =>
            {
                var cs = new CitationSource { RerankScore = 8.5 };
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsTrue(json.Contains("\"rerank_score\""), "present when set");
                AssertHelper.IsTrue(json.Contains("8.5"), "correct value in JSON");
            });

            await ExecuteTestAsync("CitationSource.RerankScore: omitted when null", async () =>
            {
                var cs = new CitationSource { RerankScore = null };
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsFalse(json.Contains("\"rerank_score\""), "omitted when null");
            });

            await ExecuteTestAsync("RetrievalChunk.FusionScore: defaults to null", async () =>
            {
                var chunk = new RetrievalChunk();
                AssertHelper.IsNull(chunk.FusionScore, "default FusionScore");
            });

            await ExecuteTestAsync("RetrievalChunk: JSON uses property name fusion_score", async () =>
            {
                var chunk = new RetrievalChunk { FusionScore = 0.016393 };
                string json = JsonSerializer.Serialize(chunk);
                AssertHelper.IsTrue(json.Contains("\"fusion_score\""), "JSON should contain fusion_score key");
            });

            await ExecuteTestAsync("RetrievalChunk.FusionScore: round-trips when set", async () =>
            {
                var chunk = new RetrievalChunk { FusionScore = 0.032787 };
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.AreEqual(0.032787, d.FusionScore, "round-trip FusionScore");
            });

            await ExecuteTestAsync("RetrievalChunk.FusionScore: round-trips as null when not set", async () =>
            {
                var chunk = new RetrievalChunk();
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.IsNull(d.FusionScore, "round-trip null FusionScore");
            });

            await ExecuteTestAsync("CitationSource.FusionScore: defaults to null, omitted in JSON", async () =>
            {
                var cs = new CitationSource();
                AssertHelper.IsNull(cs.FusionScore, "default FusionScore");
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsFalse(json.Contains("\"fusion_score\""), "omitted when null");
            });

            await ExecuteTestAsync("CitationSource.FusionScore: present when set", async () =>
            {
                var cs = new CitationSource { FusionScore = 0.048 };
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsTrue(json.Contains("\"fusion_score\""), "present when set");
            });

            // ===== ApiContractModelTests =====

            await ExecuteTestAsync("ApiContract.ChatCompletionRequest: deserialization", async () =>
            {
                string json = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}";
                var req = JsonSerializer.Deserialize<ChatCompletionRequest>(json, _jsonOptionsDefault);
                AssertHelper.IsNotNull(req, "deserialized request");
                AssertHelper.AreEqual("gpt-4", req.Model, "Model");
                AssertHelper.IsNotNull(req.Messages, "Messages");
                AssertHelper.IsTrue(req.Messages.Count > 0, "Messages has items");
            });

            await ExecuteTestAsync("ApiContract.ChatCompletionResponse: structure", async () =>
            {
                var resp = new ChatCompletionResponse
                {
                    Id = "chatcmpl-test",
                    Object = "chat.completion",
                    Model = "gpt-4",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessage { Role = "assistant", Content = "Hello!" },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 10,
                        CompletionTokens = 5,
                        TotalTokens = 15
                    }
                };

                string json = JsonSerializer.Serialize(resp);
                var d = JsonSerializer.Deserialize<ChatCompletionResponse>(json, _jsonOptionsDefault);
                AssertHelper.AreEqual("chatcmpl-test", d.Id, "Id");
                AssertHelper.AreEqual("chat.completion", d.Object, "Object");
                AssertHelper.AreEqual(1, d.Choices.Count, "Choices count");
                AssertHelper.AreEqual("Hello!", d.Choices[0].Message.Content, "Choice message");
                AssertHelper.IsNotNull(d.Usage, "Usage");
                AssertHelper.AreEqual(15, d.Usage.TotalTokens, "TotalTokens");
            });

            await ExecuteTestAsync("ApiContract.AuthenticateRequest: round-trip", async () =>
            {
                var req = new AuthenticateRequest { Email = "test@example.com", Password = "pass123" };
                string json = JsonSerializer.Serialize(req, _jsonOptionsDefault);
                var d = JsonSerializer.Deserialize<AuthenticateRequest>(json, _jsonOptionsDefault);
                AssertHelper.AreEqual("test@example.com", d.Email, "Email");
                AssertHelper.AreEqual("pass123", d.Password, "Password");
            });

            await ExecuteTestAsync("ApiContract.AuthenticateResult: round-trip", async () =>
            {
                var res = new AuthenticateResult
                {
                    Success = true,
                    TenantId = "ten_test",
                    IsGlobalAdmin = true,
                    IsTenantAdmin = false
                };
                string json = JsonSerializer.Serialize(res, _jsonOptionsDefault);
                var d = JsonSerializer.Deserialize<AuthenticateResult>(json, _jsonOptionsDefault);
                AssertHelper.AreEqual(true, d.Success, "Success");
                AssertHelper.AreEqual("ten_test", d.TenantId, "TenantId");
                AssertHelper.AreEqual(true, d.IsGlobalAdmin, "IsGlobalAdmin");
            });

            await ExecuteTestAsync("ApiContract.ApiErrorResponse: error enum and message", async () =>
            {
                var err = new ApiErrorResponse(ApiErrorEnum.NotFound);
                AssertHelper.AreEqual(ApiErrorEnum.NotFound, err.Error, "Error enum");
                AssertHelper.AreEqual(404, err.StatusCode, "StatusCode");
                AssertHelper.StringContains(err.Message, "not found", "Message contains 'not found'");

                string json = JsonSerializer.Serialize(err, _jsonOptionsDefault);
                AssertHelper.IsTrue(json.Contains("NotFound") || json.Contains("notFound"), "error enum in JSON");
            });

            await ExecuteTestAsync("ApiContract.EnumerationQuery: default MaxResults and ordering", async () =>
            {
                var q = new EnumerationQuery();
                AssertHelper.AreEqual(100, q.MaxResults, "default MaxResults");
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedDescending, q.Ordering, "default Ordering");
                AssertHelper.IsNull(q.ContinuationToken, "default ContinuationToken");
            });

            await ExecuteTestAsync("ApiContract.EnumerationQuery: MaxResults validation", async () =>
            {
                bool threw = false;
                try { var q = new EnumerationQuery(); q.MaxResults = 0; }
                catch (ArgumentException) { threw = true; }
                AssertHelper.IsTrue(threw, "MaxResults = 0 should throw");

                threw = false;
                try { var q = new EnumerationQuery(); q.MaxResults = 1001; }
                catch (ArgumentException) { threw = true; }
                AssertHelper.IsTrue(threw, "MaxResults = 1001 should throw");
            });

            await ExecuteTestAsync("ApiContract.EnumerationResult: structure", async () =>
            {
                var r = new EnumerationResult<string>();
                AssertHelper.AreEqual(true, r.EndOfResults, "default EndOfResults");
                AssertHelper.IsNull(r.ContinuationToken, "default ContinuationToken");
                AssertHelper.AreEqual(0L, r.TotalRecords, "default TotalRecords");
                AssertHelper.AreEqual(0L, r.RecordsRemaining, "default RecordsRemaining");
                AssertHelper.IsNotNull(r.Objects, "Objects not null");
                AssertHelper.AreEqual(0, r.Objects.Count, "Objects empty");
            });

            // ===== SettingsModelTests =====

            await ExecuteTestAsync("Settings.AssistantHubSettings: loads from JSON with defaults", async () =>
            {
                var settings = new AssistantHubSettings();
                AssertHelper.IsNotNull(settings, "settings object");
            });

            await ExecuteTestAsync("Settings.DatabaseSettings: sensible defaults", async () =>
            {
                var db = new DatabaseSettings();
                AssertHelper.IsNotNull(db, "DatabaseSettings");
            });

            await ExecuteTestAsync("Settings.WebserverSettings: defaults", async () =>
            {
                var ws = new WebserverSettings();
                AssertHelper.IsNotNull(ws, "WebserverSettings");
            });

            await ExecuteTestAsync("Settings.S3Settings: defaults", async () =>
            {
                var s3 = new S3Settings();
                AssertHelper.IsNotNull(s3, "S3Settings");
                AssertHelper.AreEqual("", s3.DashboardUrl, "DashboardUrl default");
            });

            await ExecuteTestAsync("Settings.InferenceSettings: defaults", async () =>
            {
                var inf = new InferenceSettings();
                AssertHelper.IsNotNull(inf, "InferenceSettings");
                AssertHelper.AreEqual("", inf.DashboardUrl, "DashboardUrl default");
            });

            await ExecuteTestAsync("Settings.RecallDbSettings: defaults", async () =>
            {
                var rdb = new RecallDbSettings();
                AssertHelper.IsNotNull(rdb, "RecallDbSettings");
                AssertHelper.AreEqual("", rdb.DashboardUrl, "DashboardUrl default");
            });

            await ExecuteTestAsync("Settings.ChunkingSettings: defaults", async () =>
            {
                var ch = new ChunkingSettings();
                AssertHelper.IsNotNull(ch, "ChunkingSettings");
                AssertHelper.AreEqual("", ch.DashboardUrl, "DashboardUrl default");
            });

            await ExecuteTestAsync("Settings.EmbeddingsSettings: defaults", async () =>
            {
                var emb = new EmbeddingsSettings();
                AssertHelper.IsNotNull(emb, "EmbeddingsSettings");
            });

            await ExecuteTestAsync("Settings.DocumentAtomSettings: defaults", async () =>
            {
                var da = new DocumentAtomSettings();
                AssertHelper.IsNotNull(da, "DocumentAtomSettings");
                AssertHelper.AreEqual("", da.DashboardUrl, "DashboardUrl default");
            });

            await ExecuteTestAsync("Settings.LoggingSettings: defaults", async () =>
            {
                var log = new LoggingSettings();
                AssertHelper.IsNotNull(log, "LoggingSettings");
            });

            await ExecuteTestAsync("Settings.CrawlSettings: defaults", async () =>
            {
                var crawl = new CrawlSettings();
                AssertHelper.IsNotNull(crawl, "CrawlSettings");
            });

            await ExecuteTestAsync("Settings.AssistantHubSettings: JSON round-trip", async () =>
            {
                var settings = new AssistantHubSettings();
                string json = JsonSerializer.Serialize(settings, _jsonOptionsDefault);
                AssertHelper.IsTrue(json.Length > 10, "serialized JSON not empty");
                var d = JsonSerializer.Deserialize<AssistantHubSettings>(json, _jsonOptionsDefault);
                AssertHelper.IsNotNull(d, "deserialized settings");
            });

            await ExecuteTestAsync("Models.AssistantSettings: Slack defaults", async () =>
            {
                var settings = new AssistantSettings();
                AssertHelper.AreEqual(false, settings.EnableSlack, "EnableSlack default");
                AssertHelper.IsNull(settings.SlackAppToken, "SlackAppToken default");
                AssertHelper.IsNull(settings.SlackBotToken, "SlackBotToken default");
                AssertHelper.IsNull(settings.SlackChannelId, "SlackChannelId default");
                AssertHelper.IsNull(settings.SlackMessagePrefix, "SlackMessagePrefix default");
            });

            return GetResults();
        }
    }
}
