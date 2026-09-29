namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    /// <summary>
    /// Provides shared telemetry and provider DTO helpers for inference services.
    /// </summary>
    public abstract class InferenceServiceTelemetryBase
    {
        #region Private-Members

        private protected string _Header = "[InferenceService] ";
        private protected InferenceSettings _Settings = null;
        private protected LoggingModule _Logging = null;
        private protected HttpClient _HttpClient = null;

        private protected JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        /// <summary>
        /// Instantiate the telemetry helper base.
        /// </summary>
        /// <param name="settings">Inference settings.</param>
        /// <param name="logging">Logging module.</param>
        protected InferenceServiceTelemetryBase(InferenceSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            // Each request is bounded by SendWithTimeoutAsync: the calling endpoint's timeout when one is in scope,
            // otherwise Inference.RequestTimeoutMs.
            _HttpClient = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        }

        #region Request-Timeouts

        private static readonly System.Threading.AsyncLocal<int?> _RequestTimeoutOverrideMs = new System.Threading.AsyncLocal<int?>();

        /// <summary>
        /// Apply a per-endpoint request timeout to the inference calls made in the current async flow until the
        /// returned scope is disposed. Null or a non-positive value keeps <c>Inference.RequestTimeoutMs</c>.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <returns>Scope that restores the previous timeout.</returns>
        public static IDisposable UseRequestTimeout(int? timeoutMs)
        {
            int? previous = _RequestTimeoutOverrideMs.Value;
            _RequestTimeoutOverrideMs.Value = timeoutMs.HasValue && timeoutMs.Value > 0 ? timeoutMs : previous;
            return new TimeoutScope(previous);
        }

        /// <summary>
        /// The request timeout in effect for the current async flow.
        /// </summary>
        public int EffectiveRequestTimeoutMs => _RequestTimeoutOverrideMs.Value ?? _Settings.RequestTimeoutMs;

        private protected async Task<HttpResponseMessage> SendWithTimeoutAsync(HttpRequestMessage request, HttpCompletionOption option, System.Threading.CancellationToken token)
        {
            int timeoutMs = EffectiveRequestTimeoutMs;
            using (System.Threading.CancellationTokenSource timeout = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(timeoutMs);
                try
                {
                    return await _HttpClient.SendAsync(request, option, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException e) when (!token.IsCancellationRequested)
                {
                    throw new TimeoutException("The inference endpoint did not respond within " + timeoutMs + " ms.", e);
                }
            }
        }

        private sealed class TimeoutScope : IDisposable
        {
            private readonly int? _Previous;
            private bool _Disposed;

            public TimeoutScope(int? previous)
            {
                _Previous = previous;
            }

            public void Dispose()
            {
                if (_Disposed) return;
                _Disposed = true;
                _RequestTimeoutOverrideMs.Value = _Previous;
            }
        }

        #endregion

        #region Private-Methods

        private protected AssistantPerformanceStage StartTelemetry(
            InferenceProviderEnum provider,
            string model,
            string endpoint,
            bool streaming)
        {
            return new AssistantPerformanceStage
            {
                Name = streaming ? "streaming_inference" : "inference",
                Kind = "inference",
                Provider = provider.ToString(),
                ApiFormat = provider.ToString(),
                Model = model,
                EndpointName = endpoint,
                EndpointType = "completion",
                StartedUtc = DateTime.UtcNow,
                Success = false,
                ClientTimings = new AssistantPerformanceClientTimings(),
                Tokens = new AssistantTokenUsageTelemetry(),
                ProviderMetrics = new AssistantProviderMetrics(),
                Metadata = new Dictionary<string, object>
                {
                    ["streaming"] = streaming
                }
            };
        }

        private protected void MarkResponseHeaders(AssistantPerformanceStage telemetry, Stopwatch stopwatch, HttpResponseMessage response)
        {
            if (telemetry == null || stopwatch == null || response == null) return;

            telemetry.ClientTimings ??= new AssistantPerformanceClientTimings();
            telemetry.ClientTimings.RequestToHeadersMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
            telemetry.HttpStatusCode = (int)response.StatusCode;

            string requestId = GetHeaderValue(response, "x-request-id")
                ?? GetHeaderValue(response, "x-openai-request-id")
                ?? GetHeaderValue(response, "x-ms-request-id")
                ?? GetHeaderValue(response, "cf-ray");

            if (!String.IsNullOrEmpty(requestId))
            {
                telemetry.ProviderMetrics ??= new AssistantProviderMetrics();
                telemetry.ProviderMetrics.RequestId = requestId;
            }
        }

        private protected void FinishTelemetry(
            AssistantPerformanceStage telemetry,
            Stopwatch stopwatch,
            bool success,
            string errorType = null,
            string errorMessage = null)
        {
            if (telemetry == null) return;

            telemetry.FinishedUtc = DateTime.UtcNow;
            telemetry.DurationMs = Math.Round(stopwatch?.Elapsed.TotalMilliseconds ?? 0, 2);
            telemetry.Success = success;
            telemetry.ErrorType = errorType;
            telemetry.ErrorMessage = errorMessage;
            telemetry.ClientTimings ??= new AssistantPerformanceClientTimings();
            telemetry.ClientTimings.TotalMs ??= telemetry.DurationMs;
        }

        private protected void MarkFirstToken(AssistantPerformanceStage telemetry, Stopwatch stopwatch)
        {
            if (telemetry == null || stopwatch == null) return;

            telemetry.ClientTimings ??= new AssistantPerformanceClientTimings();
            if (!telemetry.ClientTimings.HeadersToFirstTokenMs.HasValue)
            {
                double requestToHeaders = telemetry.ClientTimings.RequestToHeadersMs ?? 0;
                telemetry.ClientTimings.HeadersToFirstTokenMs =
                    Math.Max(0, Math.Round(stopwatch.Elapsed.TotalMilliseconds - requestToHeaders, 2));
            }
        }

        private protected void MarkLastToken(AssistantPerformanceStage telemetry, Stopwatch stopwatch)
        {
            if (telemetry == null || stopwatch == null) return;

            telemetry.ClientTimings ??= new AssistantPerformanceClientTimings();
            if (!telemetry.ClientTimings.FirstTokenToLastTokenMs.HasValue
                && telemetry.ClientTimings.HeadersToFirstTokenMs.HasValue)
            {
                double requestToHeaders = telemetry.ClientTimings.RequestToHeadersMs ?? 0;
                double firstTokenAt = requestToHeaders + telemetry.ClientTimings.HeadersToFirstTokenMs.Value;
                telemetry.ClientTimings.FirstTokenToLastTokenMs =
                    Math.Max(0, Math.Round(stopwatch.Elapsed.TotalMilliseconds - firstTokenAt, 2));
            }
        }

        private protected void ApplyUsage(AssistantPerformanceStage telemetry, ChatCompletionUsage usage)
        {
            if (telemetry == null || usage == null) return;

            telemetry.Tokens ??= new AssistantTokenUsageTelemetry();
            telemetry.Tokens.Input = usage.PromptTokens > 0 ? usage.PromptTokens : telemetry.Tokens.Input;
            telemetry.Tokens.Output = usage.CompletionTokens > 0 ? usage.CompletionTokens : telemetry.Tokens.Output;
            telemetry.Tokens.Total = usage.TotalTokens > 0 ? usage.TotalTokens : telemetry.Tokens.Total;

            int reasoningTokens = usage.ReasoningTokens > 0
                ? usage.ReasoningTokens
                : (usage.CompletionTokensDetails?.ReasoningTokens ?? 0);
            if (reasoningTokens > 0)
                telemetry.Tokens.Reasoning = reasoningTokens;

            int toolDefinitionTokens = usage.ToolDefinitionTokens > 0
                ? usage.ToolDefinitionTokens
                : (usage.ToolTokens > 0
                    ? usage.ToolTokens
                    : Math.Max(
                        usage.PromptTokensDetails?.ToolDefinitionTokens ?? 0,
                        usage.PromptTokensDetails?.ToolTokens ?? 0));
            if (toolDefinitionTokens > 0)
                telemetry.Tokens.ToolDefinitions = toolDefinitionTokens;
        }

        private protected void ApplyGeminiUsage(AssistantPerformanceStage telemetry, GeminiUsageMetadata usage)
        {
            if (telemetry == null || usage == null) return;

            telemetry.Tokens ??= new AssistantTokenUsageTelemetry();
            telemetry.Tokens.Input = usage.PromptTokenCount ?? telemetry.Tokens.Input;
            telemetry.Tokens.Output = usage.CandidatesTokenCount ?? telemetry.Tokens.Output;
            telemetry.Tokens.Total = usage.TotalTokenCount ?? telemetry.Tokens.Total;
        }

        private protected void ApplyOllamaMetrics(
            AssistantPerformanceStage telemetry,
            long? totalDuration,
            long? loadDuration,
            int? promptEvalCount,
            long? promptEvalDuration,
            int? evalCount,
            long? evalDuration)
        {
            if (telemetry == null) return;

            telemetry.Tokens ??= new AssistantTokenUsageTelemetry();
            telemetry.Tokens.Input = promptEvalCount ?? telemetry.Tokens.Input;
            telemetry.Tokens.Output = evalCount ?? telemetry.Tokens.Output;
            if (promptEvalCount.HasValue || evalCount.HasValue)
                telemetry.Tokens.Total = (promptEvalCount ?? 0) + (evalCount ?? 0);

            telemetry.ProviderMetrics ??= new AssistantProviderMetrics();
            telemetry.ProviderMetrics.TotalMs = NanosecondsToMilliseconds(totalDuration);
            telemetry.ProviderMetrics.LoadMs = NanosecondsToMilliseconds(loadDuration);
            telemetry.ProviderMetrics.PromptEvalMs = NanosecondsToMilliseconds(promptEvalDuration);
            telemetry.ProviderMetrics.GenerationMs = NanosecondsToMilliseconds(evalDuration);

            if (evalCount.HasValue && evalCount.Value > 0 && evalDuration.HasValue && evalDuration.Value > 0)
                telemetry.ProviderMetrics.TokensPerSecond = Math.Round(evalCount.Value / (evalDuration.Value / 1_000_000_000.0), 2);
        }

        private protected string GetHeaderValue(HttpResponseMessage response, string headerName)
        {
            if (response == null || String.IsNullOrWhiteSpace(headerName)) return null;

            if (response.Headers.TryGetValues(headerName, out IEnumerable<string> values))
                return values?.FirstOrDefault();

            if (response.Content?.Headers != null
                && response.Content.Headers.TryGetValues(headerName, out IEnumerable<string> contentValues))
                return contentValues?.FirstOrDefault();

            return null;
        }

        private protected double? NanosecondsToMilliseconds(long? nanoseconds)
        {
            return nanoseconds.HasValue && nanoseconds.Value > 0
                ? Math.Round(nanoseconds.Value / 1_000_000.0, 2)
                : null;
        }

        #endregion

        #region Private-Classes

        #endregion
    }
}
