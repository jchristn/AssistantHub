namespace AssistantHub.Core.Services
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Provider-agnostic web search service.
    /// </summary>
    public class WebSearchService : IWebSearchService
    {
        private readonly ExternalSearchProviderSettings _Provider;
        private readonly HttpClient _HttpClient;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="provider">Provider settings.</param>
        /// <param name="httpClient">Optional externally-owned HTTP client.</param>
        public WebSearchService(ExternalSearchProviderSettings provider, HttpClient httpClient = null)
        {
            _Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _HttpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Normalize();
            if (String.IsNullOrWhiteSpace(request.Query))
                throw new ArgumentException("A web search query is required.", nameof(request));

            string providerType = String.IsNullOrWhiteSpace(_Provider.ProviderType) ? "Tavily" : _Provider.ProviderType.Trim();
            if (!String.Equals(providerType, "Tavily", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Web search provider type is not supported: " + providerType + ".");

            Stopwatch sw = Stopwatch.StartNew();
            WebSearchProviderAttempt attempt = new WebSearchProviderAttempt
            {
                ProviderName = String.IsNullOrWhiteSpace(_Provider.Name) ? "default" : _Provider.Name,
                ProviderType = "Tavily"
            };

            try
            {
                using TavilySearchClient client = new TavilySearchClient(_Provider, _HttpClient);
                TavilySearchResponse tavily = await client.SearchAsync(ToTavilyRequest(request), token).ConfigureAwait(false);
                sw.Stop();

                attempt.Success = true;
                attempt.DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
                attempt.CreditsUsed = tavily.Usage?.CreditsUsed;

                WebSearchResponse response = FromTavilyResponse(tavily);
                response.Attempts.Add(attempt);
                return response;
            }
            catch (Exception e)
            {
                sw.Stop();
                attempt.Success = false;
                attempt.DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
                attempt.ErrorMessage = e.Message;
                throw;
            }
        }

        private static TavilySearchQuery ToTavilyRequest(WebSearchRequest request)
        {
            return new TavilySearchQuery
            {
                Query = request.Query,
                MaxResults = request.MaxResults,
                SearchDepth = request.SearchDepth,
                Topic = request.Topic,
                TimeRange = request.TimeRange,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IncludeAnswerMode = request.IncludeAnswerMode,
                IncludeRawContentMode = request.IncludeRawContentMode,
                IncludeImages = request.IncludeImages,
                IncludeImageDescriptions = request.IncludeImageDescriptions,
                Country = request.Country,
                SafeSearch = request.SafeSearch,
                IncludeDomains = request.IncludeDomains,
                ExcludeDomains = request.ExcludeDomains
            };
        }

        private static WebSearchResponse FromTavilyResponse(TavilySearchResponse tavily)
        {
            return new WebSearchResponse
            {
                ProviderName = tavily.ProviderName,
                Query = tavily.Query,
                Answer = tavily.Answer,
                RequestId = tavily.RequestId,
                LatencySeconds = tavily.LatencySeconds,
                Images = tavily.Images,
                CreditsUsed = tavily.Usage?.CreditsUsed,
                Results = tavily.Results
                    .Select(result => new WebSearchResultItem
                    {
                        Title = result.Title,
                        Url = result.Url,
                        Content = result.Content,
                        Score = result.Score,
                        RawContent = result.RawContent,
                        FaviconUrl = result.FaviconUrl,
                        PublishedAt = result.PublishedAt,
                        Images = result.Images
                    })
                    .ToList()
            };
        }
    }
}
