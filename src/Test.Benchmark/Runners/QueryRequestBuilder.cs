namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;

    /// <summary>
    /// Builds the chat and retrieve request fields shared by every runner: the message list (prior turns, then the
    /// question, prefixed with its date when it has one), the metadata filter, and attached documents mapped to
    /// AssistantHub ids.
    /// </summary>
    public static class QueryRequestBuilder
    {
        #region Public-Methods

        /// <summary>
        /// Message list for a query.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <returns>JSON array of messages.</returns>
        public static JsonArray Messages(BenchmarkQuery query)
        {
            JsonArray messages = new JsonArray();
            foreach (BenchmarkTurn turn in query.Conversation ?? new List<BenchmarkTurn>())
            {
                messages.Add(new JsonObject { ["role"] = turn.Role, ["content"] = turn.Content });
            }

            string text = string.IsNullOrEmpty(query.Date) ? query.Text : "(Current date: " + query.Date + ") " + query.Text;
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = text });
            return messages;
        }

        /// <summary>
        /// Add the metadata filter and attached documents to a request body.
        /// </summary>
        /// <param name="body">Request body (modified).</param>
        /// <param name="query">Query.</param>
        /// <param name="collection">Provisioned collection (for id mapping).</param>
        public static void AddScope(JsonObject body, BenchmarkQuery query, ProvisionedCollection collection)
        {
            if (query.MetadataFilter != null)
                body["metadata_filter"] = JsonNode.Parse(JsonSerializer.Serialize(query.MetadataFilter, DatasetStore.Json));

            if (query.AttachedDocuments != null && query.AttachedDocuments.Count > 0)
            {
                JsonArray ids = new JsonArray();
                foreach (string datasetId in query.AttachedDocuments)
                {
                    if (collection.DocumentIdByDatasetId.TryGetValue(datasetId, out string? id)) ids.Add(id);
                }

                body["attached_document_ids"] = ids;
            }
        }

        /// <summary>
        /// Whether a dataset document is inside a query's filter or attachment scope.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="document">Document, or null when unknown.</param>
        /// <returns>True when in scope.</returns>
        public static bool InScope(BenchmarkQuery query, BenchmarkDocument? document)
        {
            if (document == null) return false;
            if (query.AttachedDocuments != null && query.AttachedDocuments.Count > 0 && !query.AttachedDocuments.Contains(document.Id)) return false;
            if (query.MetadataFilter != null && !query.MetadataFilter.Matches(document)) return false;
            return true;
        }

        /// <summary>
        /// Whether a query constrains scope.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <returns>True for filter or attachment queries.</returns>
        public static bool HasScope(BenchmarkQuery query)
        {
            return query.MetadataFilter != null || (query.AttachedDocuments != null && query.AttachedDocuments.Any());
        }

        #endregion
    }
}
