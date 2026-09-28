namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Client for cross-encoder rerank services: HuggingFace text-embeddings-inference ("Tei", POST /rerank) and
    /// Cohere-compatible services ("Cohere", POST /v1/rerank).
    /// </summary>
    public class CrossEncoderRerankClient
    {
        #region Private-Members

        private static readonly HttpClient _SharedHttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private readonly HttpClient _HttpClient;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="httpClient">Optional HTTP client (for tests); a shared client is used otherwise.</param>
        public CrossEncoderRerankClient(HttpClient httpClient = null)
        {
            _HttpClient = httpClient ?? _SharedHttpClient;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Score passages against a query.
        /// </summary>
        /// <param name="reranker">Reranker settings.</param>
        /// <param name="query">Query text.</param>
        /// <param name="passages">Passages to score.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>One relevance score per passage, in passage order.</returns>
        /// <exception cref="InvalidOperationException">The service failed or returned an unusable response.</exception>
        /// <exception cref="TimeoutException">The request exceeded the reranker's timeout.</exception>
        public async Task<List<double>> ScoreAsync(RerankerSettings reranker, string query, IReadOnlyList<string> passages, CancellationToken token = default)
        {
            if (reranker == null) throw new ArgumentNullException(nameof(reranker));
            if (String.IsNullOrWhiteSpace(reranker.Endpoint)) throw new InvalidOperationException("Reranker " + reranker.Id + " has no endpoint.");
            if (passages == null || passages.Count == 0) return new List<double>();

            List<string> texts = passages
                .Select(p => (p ?? "").Length > reranker.MaxPassageCharacters ? p.Substring(0, reranker.MaxPassageCharacters) : (p ?? ""))
                .ToList();
            bool cohere = String.Equals(reranker.Format, "Cohere", StringComparison.OrdinalIgnoreCase);
            string url = reranker.Endpoint.TrimEnd('/') + (cohere ? "/v1/rerank" : "/rerank");
            object body = cohere
                ? new { model = reranker.Model, query = query ?? "", documents = texts, top_n = texts.Count }
                : new { query = query ?? "", texts = texts, raw_scores = false, truncate = true };

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                if (!String.IsNullOrEmpty(reranker.ApiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reranker.ApiKey);
                timeout.CancelAfter(reranker.TimeoutMs);

                string responseBody;
                try
                {
                    using (HttpResponseMessage response = await _HttpClient.SendAsync(request, timeout.Token).ConfigureAwait(false))
                    {
                        responseBody = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidOperationException("Reranker " + reranker.Id + " returned " + (int)response.StatusCode + ".");
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    throw new TimeoutException("Reranker " + reranker.Id + " did not respond within " + reranker.TimeoutMs + " ms.");
                }

                return ParseScores(responseBody, texts.Count, cohere);
            }
        }

        /// <summary>
        /// Parse a rerank response into one score per passage.
        /// </summary>
        /// <param name="responseBody">Response body.</param>
        /// <param name="passageCount">Number of passages sent.</param>
        /// <param name="cohere">Whether the response is Cohere-format.</param>
        /// <returns>Scores in passage order; passages the service did not score get 0.</returns>
        public static List<double> ParseScores(string responseBody, int passageCount, bool cohere)
        {
            double[] scores = new double[passageCount];
            bool any = false;
            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody ?? "");
                JsonElement results = document.RootElement;
                if (cohere || results.ValueKind == JsonValueKind.Object)
                {
                    if (!results.TryGetProperty("results", out results)) throw new InvalidOperationException("Rerank response has no results.");
                }
                if (results.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("Rerank response is not a list.");

                foreach (JsonElement item in results.EnumerateArray())
                {
                    if (!item.TryGetProperty("index", out JsonElement indexElement) || !indexElement.TryGetInt32(out int index)) continue;
                    double score;
                    if (item.TryGetProperty("relevance_score", out JsonElement relevance) && relevance.TryGetDouble(out score)) { }
                    else if (item.TryGetProperty("score", out JsonElement plain) && plain.TryGetDouble(out score)) { }
                    else continue;
                    if (index < 0 || index >= passageCount) continue;
                    scores[index] = score;
                    any = true;
                }
            }
            catch (JsonException e)
            {
                throw new InvalidOperationException("Rerank response is not valid JSON: " + e.Message);
            }

            if (!any && passageCount > 0) throw new InvalidOperationException("Rerank response scored no passages.");
            return scores.ToList();
        }

        #endregion
    }
}
