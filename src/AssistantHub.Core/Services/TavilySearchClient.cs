namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Client for Tavily search.
    /// </summary>
    public class TavilySearchClient : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Default Tavily search endpoint.
        /// </summary>
        public const string DefaultEndpoint = "https://api.tavily.com/search";

        #endregion

        #region Private-Members

        private readonly ExternalSearchProviderSettings _Provider;
        private readonly HttpClient _HttpClient;
        private readonly bool _OwnsHttpClient;
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="provider">Provider settings.</param>
        /// <param name="httpClient">Optional externally-owned HTTP client.</param>
        public TavilySearchClient(ExternalSearchProviderSettings provider, HttpClient httpClient = null)
        {
            _Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _HttpClient = httpClient ?? new HttpClient();
            _OwnsHttpClient = httpClient == null;

            if (_Provider.TimeoutMs > 0)
                _HttpClient.Timeout = TimeSpan.FromMilliseconds(_Provider.TimeoutMs);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Search Tavily.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search response.</returns>
        public async Task<TavilySearchResponse> SearchAsync(TavilySearchQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Validate();

            string endpoint = ExternalSearchConfigurationHelper.ResolveConfiguredValue(
                String.IsNullOrWhiteSpace(_Provider.Endpoint) ? DefaultEndpoint : _Provider.Endpoint);
            string apiKey = ResolveSecret(_Provider.ApiKey);
            if (String.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Tavily API key is not configured.");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = CreateJsonContent(new
            {
                query = query.Query,
                search_depth = query.SearchDepth,
                topic = query.Topic,
                max_results = query.MaxResults,
                chunks_per_source = query.ChunksPerSource,
                time_range = query.TimeRange,
                start_date = query.StartDate,
                end_date = query.EndDate,
                include_answer = NormalizeOptionalMode(query.IncludeAnswerMode),
                include_raw_content = NormalizeOptionalMode(query.IncludeRawContentMode),
                include_images = query.IncludeImages,
                include_image_descriptions = query.IncludeImageDescriptions,
                include_favicon = query.IncludeFavicon,
                include_domains = query.IncludeDomains.Count > 0 ? query.IncludeDomains : null,
                exclude_domains = query.ExcludeDomains.Count > 0 ? query.ExcludeDomains : null,
                country = query.Country,
                auto_parameters = query.AutoParameters,
                exact_match = query.ExactMatch,
                include_usage = query.IncludeUsage,
                safe_search = query.SafeSearch
            });

            using HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    "Tavily search failed with status code " + ((int)response.StatusCode) + " (" + response.ReasonPhrase + ").");
            }

            using JsonDocument document = JsonDocument.Parse(responseBody);
            return ParseResponse(document.RootElement, query);
        }

        /// <summary>
        /// Dispose owned HTTP resources.
        /// </summary>
        public void Dispose()
        {
            if (_OwnsHttpClient)
                _HttpClient.Dispose();
        }

        /// <summary>
        /// Resolve an API key or environment-variable reference.
        /// </summary>
        /// <param name="value">Raw configured value.</param>
        /// <returns>Resolved secret value.</returns>
        public static string ResolveSecret(string value)
        {
            return ExternalSearchConfigurationHelper.ResolveConfiguredValue(value);
        }

        #endregion

        #region Private-Methods

        private static StringContent CreateJsonContent(object payload)
        {
            string json = JsonSerializer.Serialize(payload, _JsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static object NormalizeOptionalMode(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;
            if (Boolean.TryParse(value, out bool boolValue)) return boolValue;
            return value.Trim();
        }

        private static TavilySearchResponse ParseResponse(JsonElement root, TavilySearchQuery query)
        {
            TavilySearchResponse response = new TavilySearchResponse
            {
                ProviderName = "Tavily",
                Query = GetStringOrNull(root, "query") ?? query.Query,
                Answer = GetStringOrNull(root, "answer"),
                RequestId = GetStringOrNull(root, "request_id"),
                LatencySeconds = GetDoubleOrNull(root, "response_time"),
                Images = ParseImages(GetPropertyOrNull(root, "images")),
                Results = ParseResults(GetPropertyOrNull(root, "results"))
            };

            JsonElement? autoParameters = GetPropertyOrNull(root, "auto_parameters");
            if (autoParameters.HasValue && autoParameters.Value.ValueKind == JsonValueKind.Object)
            {
                response.AutoParameters = new TavilyAutoParameters
                {
                    Topic = GetStringOrNull(autoParameters.Value, "topic"),
                    SearchDepth = GetStringOrNull(autoParameters.Value, "search_depth")
                };
            }

            JsonElement? usage = GetPropertyOrNull(root, "usage");
            if (usage.HasValue && usage.Value.ValueKind == JsonValueKind.Object)
            {
                response.Usage = new TavilyUsage
                {
                    CreditsUsed = GetInt32OrNull(usage.Value, "credits_used") ?? GetInt32OrNull(usage.Value, "credits")
                };
            }

            return response;
        }

        private static List<TavilySearchResult> ParseResults(JsonElement? resultsElement)
        {
            List<TavilySearchResult> results = new List<TavilySearchResult>();
            if (!resultsElement.HasValue || resultsElement.Value.ValueKind != JsonValueKind.Array) return results;

            foreach (JsonElement item in resultsElement.Value.EnumerateArray())
            {
                results.Add(new TavilySearchResult
                {
                    Title = GetStringOrNull(item, "title"),
                    Url = GetStringOrNull(item, "url"),
                    Content = GetStringOrNull(item, "content"),
                    Score = GetDoubleOrNull(item, "score"),
                    RawContent = GetStringOrNull(item, "raw_content"),
                    FaviconUrl = GetStringOrNull(item, "favicon"),
                    PublishedAt = GetDateTimeOffsetOrNull(item, "published_date"),
                    Images = ParseImages(GetPropertyOrNull(item, "images"))
                });
            }

            return results;
        }

        private static List<TavilySearchImage> ParseImages(JsonElement? imagesElement)
        {
            List<TavilySearchImage> images = new List<TavilySearchImage>();
            if (!imagesElement.HasValue || imagesElement.Value.ValueKind != JsonValueKind.Array) return images;

            foreach (JsonElement image in imagesElement.Value.EnumerateArray())
            {
                if (image.ValueKind == JsonValueKind.String)
                {
                    string url = image.GetString();
                    if (!String.IsNullOrWhiteSpace(url))
                    {
                        images.Add(new TavilySearchImage { Url = url.Trim() });
                    }

                    continue;
                }

                if (image.ValueKind == JsonValueKind.Object)
                {
                    string url = GetStringOrNull(image, "url") ?? GetStringOrNull(image, "image_url");
                    if (!String.IsNullOrWhiteSpace(url))
                    {
                        images.Add(new TavilySearchImage
                        {
                            Url = url.Trim(),
                            Description = GetStringOrNull(image, "description") ?? GetStringOrNull(image, "alt")
                        });
                    }
                }
            }

            return images;
        }

        private static JsonElement? GetPropertyOrNull(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement value))
                return value;

            return null;
        }

        private static string GetStringOrNull(JsonElement element, string propertyName)
        {
            JsonElement? value = GetPropertyOrNull(element, propertyName);
            if (!value.HasValue) return null;
            if (value.Value.ValueKind == JsonValueKind.String) return value.Value.GetString();
            if (value.Value.ValueKind == JsonValueKind.Number) return value.Value.GetRawText();
            if (value.Value.ValueKind == JsonValueKind.True) return Boolean.TrueString;
            if (value.Value.ValueKind == JsonValueKind.False) return Boolean.FalseString;
            return null;
        }

        private static double? GetDoubleOrNull(JsonElement element, string propertyName)
        {
            JsonElement? value = GetPropertyOrNull(element, propertyName);
            if (!value.HasValue) return null;
            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDouble(out double numeric)) return numeric;
            if (value.Value.ValueKind == JsonValueKind.String && Double.TryParse(value.Value.GetString(), out double parsed)) return parsed;
            return null;
        }

        private static int? GetInt32OrNull(JsonElement element, string propertyName)
        {
            JsonElement? value = GetPropertyOrNull(element, propertyName);
            if (!value.HasValue) return null;
            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out int numeric)) return numeric;
            if (value.Value.ValueKind == JsonValueKind.String && Int32.TryParse(value.Value.GetString(), out int parsed)) return parsed;
            return null;
        }

        private static DateTimeOffset? GetDateTimeOffsetOrNull(JsonElement element, string propertyName)
        {
            string value = GetStringOrNull(element, propertyName);
            if (String.IsNullOrWhiteSpace(value)) return null;
            if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed)) return parsed;
            return null;
        }

        #endregion
    }
}
