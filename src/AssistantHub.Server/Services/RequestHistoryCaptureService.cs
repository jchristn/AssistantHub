namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Captures request and response history from the HTTP pipeline.
    /// </summary>
    public class RequestHistoryCaptureService
    {
        private readonly string _Header = "[RequestHistoryCaptureService] ";
        private readonly DatabaseDriverBase _Database;
        private readonly AssistantHubSettings _Settings;
        private readonly LoggingModule _Logging;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryCaptureService(DatabaseDriverBase database, AssistantHubSettings settings, LoggingModule logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        /// <summary>
        /// Capture the supplied HTTP context asynchronously.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public void Capture(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (!_Settings.RequestHistory.Enabled) return;
            if (ShouldSkip(ctx)) return;

            RequestHistoryEntry entry = BuildEntry(ctx);
            if (entry == null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _Database.RequestHistory.CreateAsync(entry).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "failed to persist request history: " + e.Message);
                }
            });
        }

        private bool ShouldSkip(HttpContextBase ctx)
        {
            string path = ctx.Request?.Url?.RawWithoutQuery ?? ctx.Request?.Url?.RawWithQuery ?? String.Empty;
            if (String.IsNullOrEmpty(path)) return false;
            if (path.StartsWith("/v1.0/requesthistory", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private RequestHistoryEntry BuildEntry(HttpContextBase ctx)
        {
            Dictionary<string, object> metadata = ctx.Metadata as Dictionary<string, object> ?? new Dictionary<string, object>();
            AuthContext auth = metadata.ContainsKey("authContext") ? metadata["authContext"] as AuthContext : null;

            string path = ctx.Request?.Url?.RawWithoutQuery ?? "/";
            string fullUrl = ctx.Request?.Url?.RawWithQuery ?? path;
            bool assistantApi = path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase);

            RequestHistoryEntry entry = new RequestHistoryEntry
            {
                Id = AssistantHub.Core.Helpers.IdGenerator.NewRequestHistoryId(),
                TenantId = auth?.TenantId,
                UserId = auth?.UserId,
                CredentialId = auth?.CredentialId,
                AssistantId = GetAssistantId(ctx, assistantApi),
                ThreadId = GetThreadId(ctx),
                PrincipalName = auth?.Email ?? auth?.User?.Email,
                RequestType = assistantApi ? "AssistantApi" : "SystemApi",
                SourceType = ClassifySourceType(ctx, auth, assistantApi),
                HttpMethod = ctx.Request?.Method.ToString() ?? "GET",
                RouteTemplate = path,
                RequestPath = path,
                RequestUrl = fullUrl,
                SourceIp = ctx.Connection?.Source?.IpAddress,
                StatusCode = ctx.Response?.StatusCode ?? 0,
                Success = ctx.Response != null && ctx.Response.StatusCode >= 200 && ctx.Response.StatusCode < 400,
                DurationMs = ctx.Timestamp?.TotalMs ?? 0,
                RequestContentType = ctx.Request?.ContentType,
                ResponseContentType = ctx.Response?.ContentType,
                RouteParameters = ToDictionary(ctx.Request?.Url?.Parameters),
                QueryParameters = ToDictionary(ctx.Request?.Query?.Elements),
                CreatedUtc = ctx.Timestamp?.Start ?? DateTime.UtcNow,
                LastUpdateUtc = ctx.Timestamp?.End ?? DateTime.UtcNow
            };

            if (_Settings.RequestHistory.CaptureHeaders)
            {
                entry.RequestHeaders = CaptureHeaders(ctx.Request?.Headers);
                entry.ResponseHeaders = CaptureHeaders(ctx.Response?.Headers);
            }

            byte[] requestBytes = ctx.Request?.DataAsBytes;
            byte[] responseBytes = ctx.Response?.DataAsBytes;
            entry.RequestSizeBytes = requestBytes?.LongLength ?? 0;
            entry.ResponseSizeBytes = responseBytes?.LongLength ?? 0;

            if (_Settings.RequestHistory.CaptureBodies)
            {
                CapturedBody requestBody = CaptureBody(
                    ctx.Request?.ContentType,
                    requestBytes,
                    ctx.Request?.DataAsString,
                    _Settings.RequestHistory.MaxRequestBodyBytes,
                    false);

                CapturedBody responseBody = CaptureBody(
                    ctx.Response?.ContentType,
                    responseBytes,
                    ctx.Response?.DataAsString,
                    _Settings.RequestHistory.MaxResponseBodyBytes,
                    ctx.Response?.ServerSentEvents ?? false);

                entry.RequestBody = requestBody.Body;
                entry.RequestBodyTruncated = requestBody.Truncated;
                entry.RequestBodyIsBinary = requestBody.IsBinary;
                entry.ResponseBody = responseBody.Body;
                entry.ResponseBodyTruncated = responseBody.Truncated;
                entry.ResponseBodyIsBinary = responseBody.IsBinary;
            }

            return entry;
        }

        private string GetAssistantId(HttpContextBase ctx, bool assistantApi)
        {
            if (!assistantApi) return null;
            return ctx.Request?.Url?.Parameters?["assistantId"];
        }

        private string GetThreadId(HttpContextBase ctx)
        {
            string threadId = ctx.Request?.Headers?.Get(Constants.ThreadIdHeader);
            if (!String.IsNullOrEmpty(threadId)) return threadId;
            return ctx.Request?.Url?.Parameters?["threadId"];
        }

        private string ClassifySourceType(HttpContextBase ctx, AuthContext auth, bool assistantApi)
        {
            if (assistantApi && auth == null) return "public-assistant";

            string userAgent = ctx.Request?.Headers?.Get("User-Agent") ?? String.Empty;
            if (auth != null && userAgent.Contains("Mozilla", StringComparison.OrdinalIgnoreCase))
                return "dashboard";

            return auth != null ? "api" : "public";
        }

        private Dictionary<string, string> CaptureHeaders(NameValueCollection headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null || headers.AllKeys == null) return ret;

            foreach (string key in headers.AllKeys)
            {
                if (String.IsNullOrEmpty(key)) continue;
                string value = headers.Get(key);
                ret[key] = ShouldRedactHeader(key) ? "[redacted]" : value;
            }

            return ret;
        }

        private bool ShouldRedactHeader(string key)
        {
            if (String.IsNullOrEmpty(key)) return false;
            string lowered = key.ToLowerInvariant();
            foreach (string redacted in _Settings.RequestHistory.RedactedHeaders)
            {
                if (String.Equals(lowered, redacted.ToLowerInvariant(), StringComparison.Ordinal)
                    || lowered.Contains("api-key", StringComparison.Ordinal)
                    || lowered.Contains("token", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private bool ShouldTreatAsBinary(string contentType, bool serverSentEvents)
        {
            if (serverSentEvents) return false;
            if (String.IsNullOrEmpty(contentType)) return false;

            string lowered = contentType.ToLowerInvariant();
            if (lowered.StartsWith("text/", StringComparison.Ordinal)) return false;
            if (lowered.Contains("json", StringComparison.Ordinal)) return false;
            if (lowered.Contains("xml", StringComparison.Ordinal)) return false;
            if (lowered.Contains("javascript", StringComparison.Ordinal)) return false;
            if (lowered.Contains("x-www-form-urlencoded", StringComparison.Ordinal)) return false;

            foreach (string excluded in _Settings.RequestHistory.ExcludedContentTypes)
            {
                if (lowered.StartsWith(excluded.ToLowerInvariant(), StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private CapturedBody CaptureBody(string contentType, byte[] bytes, string bodyString, int maxBytes, bool serverSentEvents)
        {
            CapturedBody ret = new CapturedBody();
            if (bytes == null || bytes.Length < 1) return ret;

            ret.SizeBytes = bytes.LongLength;

            if (serverSentEvents)
            {
                ret.Body = "[server-sent events stream omitted]";
                return ret;
            }

            if (ShouldTreatAsBinary(contentType, serverSentEvents))
            {
                ret.IsBinary = true;
                ret.Body = "[binary payload omitted]";
                return ret;
            }

            int byteCount = bytes.Length;
            int captureBytes = Math.Min(byteCount, maxBytes);
            string captured = !String.IsNullOrEmpty(bodyString)
                ? Encoding.UTF8.GetString(bytes, 0, captureBytes)
                : Encoding.UTF8.GetString(bytes, 0, captureBytes);

            ret.Truncated = byteCount > maxBytes;
            if (ret.Truncated)
                captured += Environment.NewLine + "[truncated]";

            ret.Body = RedactJson(contentType, captured);
            return ret;
        }

        private string RedactJson(string contentType, string body)
        {
            if (String.IsNullOrEmpty(body)) return body;
            if (String.IsNullOrEmpty(contentType) || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                return body;

            try
            {
                JsonNode node = JsonNode.Parse(body);
                RedactJsonNode(node);
                return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? body;
            }
            catch
            {
                return body;
            }
        }

        private void RedactJsonNode(JsonNode node)
        {
            if (node == null) return;

            if (node is JsonObject obj)
            {
                List<string> keys = new List<string>();
                foreach (KeyValuePair<string, JsonNode?> kvp in obj)
                    keys.Add(kvp.Key);

                foreach (string key in keys)
                {
                    if (ShouldRedactJsonField(key))
                    {
                        obj[key] = "[redacted]";
                    }
                    else
                    {
                        RedactJsonNode(obj[key]);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (JsonNode child in arr)
                    RedactJsonNode(child);
            }
        }

        private bool ShouldRedactJsonField(string key)
        {
            if (String.IsNullOrEmpty(key)) return false;
            foreach (string redacted in _Settings.RequestHistory.RedactedJsonFields)
            {
                if (String.Equals(key, redacted, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private Dictionary<string, string> ToDictionary(NameValueCollection collection)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (collection == null || collection.AllKeys == null) return ret;

            foreach (string key in collection.AllKeys)
            {
                if (String.IsNullOrEmpty(key)) continue;
                ret[key] = collection.Get(key);
            }

            return ret;
        }

        private struct CapturedBody
        {
            public string Body;
            public bool Truncated;
            public bool IsBinary;
            public long SizeBytes;
        }
    }
}
