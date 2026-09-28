namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Server.Handlers;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for reranker routes.
    /// </summary>
    public static class RerankerApiDocs
    {
        #region Private-Members

        private const string _Tag = "Rerankers";

        private const string _Access = "Rerankers are configured in the server settings (Rerankers) by the operator; API keys are never returned. Requires a global or tenant administrator. ";

        private static List<RerankerSummary> ExampleList()
        {
            return new List<RerankerSummary>
            {
                new RerankerSummary
                {
                    Id = "cross-encoder",
                    Name = "MiniLM cross-encoder",
                    Format = "Tei",
                    Endpoint = "http://reranker:80",
                    Model = null,
                    TimeoutMs = 10000,
                    HasApiKey = false
                }
            };
        }

        private static RerankerTestRequest ExampleTestRequest()
        {
            return new RerankerTestRequest
            {
                Query = "What is the engine's rated speed?",
                Documents = new List<string>
                {
                    "The engine is rated at 3000 rpm under continuous load.",
                    "Bananas are rich in potassium."
                }
            };
        }

        private static RerankerTestResult ExampleTestResult()
        {
            return new RerankerTestResult
            {
                RerankerId = "cross-encoder",
                Success = true,
                Scores = new List<double> { 0.969249, 0.000013 },
                ErrorMessage = null,
                DurationMs = 12.4
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/rerankers.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List rerankers", _Tag)
            .Describe("Lists the cross-encoder rerank services an assistant can use with RerankerType CrossEncoder and RerankEndpointId. " + _Access)
            .Returns(200, "Configured rerankers.", ExampleList())
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/rerankers/{rerankerId}/test.</summary>
        public static OpenApiRouteMetadata Test => ApiDoc.Create("Test a reranker", _Tag)
            .Describe("Scores up to 100 passages against a query with a configured reranker and returns one score per passage, in request order. "
                + "A reranker that fails or times out returns 200 with Success false and the error. " + _Access)
            .Body("Query and passages to score.", ExampleTestRequest())
            .Returns(200, "Scores, or the reranker's error.", ExampleTestResult())
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
