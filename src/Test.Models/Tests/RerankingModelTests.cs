namespace Test.Models.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using Test.Common;

    public static class RerankingModelTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("RerankingModelTests");

            // 2.2.1 AssistantSettings — reranking fields
            await runner.RunTestAsync("AssistantSettings.EnableReranking: defaults to false", async ct =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(false, s.EnableReranking, "EnableReranking default");
            }, token);

            await runner.RunTestAsync("AssistantSettings.RerankerTopK: defaults to 5", async ct =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(5, s.RerankerTopK, "RerankerTopK default");
            }, token);

            await runner.RunTestAsync("AssistantSettings.RerankerTopK: setter clamps below 1", async ct =>
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
            }, token);

            await runner.RunTestAsync("AssistantSettings.RerankerScoreThreshold: defaults to 3.0", async ct =>
            {
                var s = new AssistantSettings();
                AssertHelper.AreEqual(3.0, s.RerankerScoreThreshold, "RerankerScoreThreshold default");
            }, token);

            await runner.RunTestAsync("AssistantSettings.RerankerScoreThreshold: clamps to range 0-10", async ct =>
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

                // Valid values work
                var valid = new AssistantSettings();
                valid.RerankerScoreThreshold = 0.0;
                AssertHelper.AreEqual(0.0, valid.RerankerScoreThreshold, "0.0 accepted");
                valid.RerankerScoreThreshold = 10.0;
                AssertHelper.AreEqual(10.0, valid.RerankerScoreThreshold, "10.0 accepted");
            }, token);

            await runner.RunTestAsync("AssistantSettings.RerankPrompt: defaults to null", async ct =>
            {
                var s = new AssistantSettings();
                AssertHelper.IsNull(s.RerankPrompt, "RerankPrompt default");
            }, token);

            await runner.RunTestAsync("AssistantSettings: JSON round-trip preserves reranking fields", async ct =>
            {
                var s = new AssistantSettings();
                s.EnableReranking = true;
                s.RerankerTopK = 7;
                s.RerankerScoreThreshold = 5.5;
                s.RerankPrompt = "Test prompt {query} {chunks}";

                string json = JsonSerializer.Serialize(s, _jsonOptions);
                var d = JsonSerializer.Deserialize<AssistantSettings>(json, _jsonOptions);

                AssertHelper.AreEqual(true, d.EnableReranking, "round-trip EnableReranking");
                AssertHelper.AreEqual(7, d.RerankerTopK, "round-trip RerankerTopK");
                AssertHelper.AreEqual(5.5, d.RerankerScoreThreshold, "round-trip RerankerScoreThreshold");
                AssertHelper.AreEqual("Test prompt {query} {chunks}", d.RerankPrompt, "round-trip RerankPrompt");
            }, token);

            // 2.2.2 ChatHistory — reranking telemetry fields
            await runner.RunTestAsync("ChatHistory.RerankDurationMs: defaults to 0", async ct =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0.0, ch.RerankDurationMs, "default RerankDurationMs");
            }, token);

            await runner.RunTestAsync("ChatHistory.RerankInputCount: defaults to 0", async ct =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0, ch.RerankInputCount, "default RerankInputCount");
            }, token);

            await runner.RunTestAsync("ChatHistory.RerankOutputCount: defaults to 0", async ct =>
            {
                var ch = new ChatHistory();
                AssertHelper.AreEqual(0, ch.RerankOutputCount, "default RerankOutputCount");
            }, token);

            await runner.RunTestAsync("ChatHistory: JSON round-trip preserves reranking fields", async ct =>
            {
                var ch = new ChatHistory();
                ch.RerankDurationMs = 123.4;
                ch.RerankInputCount = 10;
                ch.RerankOutputCount = 3;

                string json = JsonSerializer.Serialize(ch, _jsonOptions);
                var d = JsonSerializer.Deserialize<ChatHistory>(json, _jsonOptions);

                AssertHelper.AreEqual(123.4, d.RerankDurationMs, "round-trip RerankDurationMs");
                AssertHelper.AreEqual(10, d.RerankInputCount, "round-trip RerankInputCount");
                AssertHelper.AreEqual(3, d.RerankOutputCount, "round-trip RerankOutputCount");
            }, token);

            // 2.2.3 RetrievalChunk — rerank score
            await runner.RunTestAsync("RetrievalChunk.RerankScore: defaults to null", async ct =>
            {
                var chunk = new RetrievalChunk();
                AssertHelper.IsNull(chunk.RerankScore, "default RerankScore");
            }, token);

            await runner.RunTestAsync("RetrievalChunk: JSON uses property name rerank_score", async ct =>
            {
                var chunk = new RetrievalChunk { RerankScore = 8.5 };
                string json = JsonSerializer.Serialize(chunk);
                AssertHelper.IsTrue(json.Contains("\"rerank_score\""), "JSON should contain rerank_score key");
            }, token);

            await runner.RunTestAsync("RetrievalChunk.RerankScore: round-trips when set", async ct =>
            {
                var chunk = new RetrievalChunk { RerankScore = 7.2 };
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.AreEqual(7.2, d.RerankScore, "round-trip RerankScore");
            }, token);

            await runner.RunTestAsync("RetrievalChunk.RerankScore: round-trips as null when not set", async ct =>
            {
                var chunk = new RetrievalChunk();
                string json = JsonSerializer.Serialize(chunk);
                var d = JsonSerializer.Deserialize<RetrievalChunk>(json);
                AssertHelper.IsNull(d.RerankScore, "round-trip null RerankScore");
            }, token);

            // 2.3.1 ChatCompletionRetrieval — reranking telemetry
            await runner.RunTestAsync("ChatCompletionRetrieval: reranking defaults", async ct =>
            {
                var r = new ChatCompletionRetrieval();
                AssertHelper.AreEqual(0.0, r.RerankDurationMs, "default RerankDurationMs");
                AssertHelper.AreEqual(0, r.RerankInputCount, "default RerankInputCount");
                AssertHelper.AreEqual(0, r.RerankOutputCount, "default RerankOutputCount");
            }, token);

            await runner.RunTestAsync("ChatCompletionRetrieval: fields present when non-zero", async ct =>
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
            }, token);

            await runner.RunTestAsync("ChatCompletionRetrieval: fields omitted when zero (WhenWritingDefault)", async ct =>
            {
                var r = new ChatCompletionRetrieval();
                string json = JsonSerializer.Serialize(r);
                AssertHelper.IsFalse(json.Contains("\"rerank_duration_ms\""), "rerank_duration_ms omitted when 0");
                AssertHelper.IsFalse(json.Contains("\"rerank_input_count\""), "rerank_input_count omitted when 0");
                AssertHelper.IsFalse(json.Contains("\"rerank_output_count\""), "rerank_output_count omitted when 0");
            }, token);

            // 2.3.2 CitationSource — rerank score
            await runner.RunTestAsync("CitationSource.RerankScore: defaults to null, omitted in JSON", async ct =>
            {
                var cs = new CitationSource();
                AssertHelper.IsNull(cs.RerankScore, "default RerankScore");
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsFalse(json.Contains("\"rerank_score\""), "omitted when null");
            }, token);

            await runner.RunTestAsync("CitationSource.RerankScore: present when set", async ct =>
            {
                var cs = new CitationSource { RerankScore = 8.5 };
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsTrue(json.Contains("\"rerank_score\""), "present when set");
                AssertHelper.IsTrue(json.Contains("8.5"), "correct value in JSON");
            }, token);

            await runner.RunTestAsync("CitationSource.RerankScore: omitted when null", async ct =>
            {
                var cs = new CitationSource { RerankScore = null };
                string json = JsonSerializer.Serialize(cs);
                AssertHelper.IsFalse(json.Contains("\"rerank_score\""), "omitted when null");
            }, token);
        }
    }
}
