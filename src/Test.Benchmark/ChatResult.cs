namespace Test.Benchmark
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;

    /// <summary>
    /// The parts of a non-streaming chat response the harness scores.
    /// </summary>
    public class ChatResult
    {
        #region Public-Members

        /// <summary>Answer text.</summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>Chunks injected into the prompt.</summary>
        public List<RetrievedChunk> Chunks { get; set; } = new List<RetrievedChunk>();

        /// <summary>AssistantHub document ids the answer cited.</summary>
        public List<string> CitedDocumentIds { get; set; } = new List<string>();

        /// <summary>Whether the response carried a citations block.</summary>
        public bool CitationsPresent { get; set; } = false;

        /// <summary>Answerability decision.</summary>
        public string? AnswerabilityDecision { get; set; } = null;

        /// <summary>Server retrieval duration.</summary>
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>Prompt tokens.</summary>
        public int PromptTokens { get; set; } = 0;

        /// <summary>Completion tokens.</summary>
        public int CompletionTokens { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Parse a chat completion response body.
        /// </summary>
        /// <param name="body">JSON body.</param>
        /// <returns>Parsed result.</returns>
        public static ChatResult Parse(string body)
        {
            ChatResult result = new ChatResult();
            JsonNode? root = JsonNode.Parse(body);
            if (root == null) return result;

            result.Answer = root["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
            result.PromptTokens = root["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;
            result.CompletionTokens = root["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;

            JsonNode? retrieval = root["retrieval"];
            if (retrieval != null)
            {
                result.RetrievalDurationMs = retrieval["duration_ms"]?.GetValue<double>() ?? 0;
                result.AnswerabilityDecision = retrieval["answerability_decision"]?.GetValue<string>();
                JsonArray? chunks = retrieval["chunks"] as JsonArray;
                if (chunks != null)
                {
                    result.Chunks = chunks
                        .Where(c => c != null)
                        .Select(c => System.Text.Json.JsonSerializer.Deserialize<RetrievedChunk>(c!.ToJsonString()) ?? new RetrievedChunk())
                        .ToList();
                }
            }

            JsonNode? citations = root["citations"];
            if (citations != null)
            {
                result.CitationsPresent = true;
                HashSet<int> referenced = new HashSet<int>();
                if (citations["referenced_indices"] is JsonArray indices)
                {
                    foreach (JsonNode? index in indices)
                    {
                        if (index != null) referenced.Add(index.GetValue<int>());
                    }
                }

                if (citations["sources"] is JsonArray sources)
                {
                    foreach (JsonNode? source in sources)
                    {
                        if (source == null) continue;
                        int index = source["index"]?.GetValue<int>() ?? -1;
                        string? documentId = source["document_id"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(documentId) && referenced.Contains(index) && !result.CitedDocumentIds.Contains(documentId))
                            result.CitedDocumentIds.Add(documentId);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
