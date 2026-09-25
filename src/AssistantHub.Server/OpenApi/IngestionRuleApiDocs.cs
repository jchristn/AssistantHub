namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for ingestion rule routes.
    /// </summary>
    public static class IngestionRuleApiDocs
    {
        #region Private-Members

        private const string _Tag = "Ingestion Rules";

        private const string _RuleId = "irule_01JH3ZD1A3B5C7D9E1F3G5H7J9";

        private static IngestionRule ExampleRule(string bucket, DateTime lastUpdateUtc)
        {
            return new IngestionRule
            {
                Id = _RuleId,
                TenantId = ApiExamples.TenantId,
                Name = "Knowledge Base Documents",
                Description = "Process PDF and text documents for the support knowledge base.",
                Bucket = bucket,
                CollectionName = "support-kb",
                CollectionId = ApiExamples.CollectionId,
                VerbexIndexId = "default",
                Labels = new List<string> { "support", "knowledge-base" },
                Tags = new Dictionary<string, string> { { "department", "support" }, { "priority", "high" } },
                Summarization = new IngestionSummarizationConfig
                {
                    CompletionEndpointId = "cep_01JH3ZA1B3C5D7E9F1G3H5J7K9",
                    SummarizationPrompt = "Summarize the following text concisely: {content}",
                    MaxSummaryTokens = 1024,
                    MinCellLength = 128,
                    MaxParallelTasks = 1,
                    MaxRetriesPerSummary = 3,
                    MaxRetries = 9,
                    TimeoutMs = 300000
                },
                Chunking = new IngestionChunkingConfig
                {
                    Strategy = "FixedTokenCount",
                    FixedTokenCount = 256,
                    OverlapCount = 32,
                    RowGroupSize = 5
                },
                Embedding = new IngestionEmbeddingConfig
                {
                    EmbeddingEndpointId = "ep_01JH3ZA3D5E7F9G1H3J5K7M9N1",
                    L2Normalization = false
                },
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = lastUpdateUtc
            };
        }

        private static IngestionRule ExampleRule()
        {
            return ExampleRule(ApiExamples.TenantId + "_kb-documents", ApiExamples.Updated);
        }

        private static IngestionRule ExampleRuleRequest()
        {
            return ExampleRule("kb-documents", ApiExamples.Created);
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/ingestion-rules.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create ingestion rule", _Tag)
            .Describe("Creates an ingestion rule in the caller's tenant describing how documents are stored, summarized, chunked, and embedded. Global or tenant administrators only. Name is required; Summarization and Chunking settings are validated when supplied. "
                + "Id, TenantId, CreatedUtc, and LastUpdateUtc are assigned by the server. For callers other than global administrators, the bucket name is prefixed with the tenant identifier and an underscore when it does not already carry that prefix.")
            .Body("Ingestion rule to create.", ExampleRuleRequest())
            .Returns(201, "Ingestion rule created.", ExampleRule())
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/ingestion-rules.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List ingestion rules", _Tag)
            .Describe("Enumerates the ingestion rules in the caller's tenant. Available to any authenticated user.")
            .Paged()
            .Returns(200, "Page of ingestion rules.", ApiExamples.Page(ExampleRule()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/ingestion-rules/{ruleId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get ingestion rule", _Tag)
            .Describe("Returns one ingestion rule. Rules in another tenant return 404 unless the caller is a global administrator. Available to any authenticated user.")
            .Returns(200, "Ingestion rule.", ExampleRule())
            .Errors(400, 401, 404, 500);

        /// <summary>PUT /v1.0/ingestion-rules/{ruleId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update ingestion rule", _Tag)
            .Describe("Replaces an ingestion rule. Global or tenant administrators only. Summarization and Chunking settings are validated when supplied. Id, TenantId, and CreatedUtc are preserved from the stored record and LastUpdateUtc is set by the server. "
                + "For callers other than global administrators, the bucket name is prefixed with the tenant identifier and an underscore when it does not already carry that prefix.")
            .Body("Updated ingestion rule values.", ExampleRuleRequest())
            .Returns(200, "Updated ingestion rule.", ExampleRule())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/ingestion-rules/{ruleId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete ingestion rule", _Tag)
            .Describe("Deletes an ingestion rule. Global or tenant administrators only. Rules in another tenant return 404 unless the caller is a global administrator.")
            .ReturnsNoContent(204, "Ingestion rule deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/ingestion-rules/{ruleId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check ingestion rule existence", _Tag)
            .Describe("Returns 200 when the ingestion rule exists and belongs to the caller's tenant (or the caller is a global administrator), 404 otherwise. No response body.")
            .ReturnsNoContent(200, "Ingestion rule exists.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(401, "Authentication failed.")
            .ReturnsNoContent(404, "Ingestion rule not found.")
            .ReturnsNoContent(500, "Internal server error.");

        #endregion
    }
}
