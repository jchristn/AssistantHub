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
    /// Inference service for generating responses from language models.
    /// </summary>
    public class InferenceService
    {
        #region Public-Members

        /// <summary>
        /// Whether the configured provider supports pulling models.
        /// </summary>
        public bool IsPullSupported
        {
            get { return _Settings.Provider == InferenceProviderEnum.Ollama; }
        }

        /// <summary>
        /// Whether the configured provider supports deleting models.
        /// </summary>
        public bool IsDeleteSupported
        {
            get { return _Settings.Provider == InferenceProviderEnum.Ollama; }
        }

        #endregion

        #region Private-Members

        private string _Header = "[InferenceService] ";
        private InferenceSettings _Settings = null;
        private LoggingModule _Logging = null;
        private HttpClient _HttpClient = null;

        private JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Inference settings.</param>
        /// <param name="logging">Logging module.</param>
        public InferenceService(InferenceSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _HttpClient = new HttpClient();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// List models available on the configured inference provider.
        /// </summary>
        /// <returns>List of available models.</returns>
        public async Task<List<InferenceModel>> ListModelsAsync()
        {
            try
            {
                switch (_Settings.Provider)
                {
                    case InferenceProviderEnum.Ollama:
                        return await ListOllamaModelsAsync().ConfigureAwait(false);

                    case InferenceProviderEnum.OpenAI:
                        return await ListOpenAIModelsAsync().ConfigureAwait(false);

                    case InferenceProviderEnum.Gemini:
                        return await ListGeminiModelsAsync().ConfigureAwait(false);

                    default:
                        _Logging.Warn(_Header + "unsupported inference provider for listing models: " + _Settings.Provider.ToString());
                        return new List<InferenceModel>();
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception listing models: " + e.Message);
                return new List<InferenceModel>();
            }
        }

        /// <summary>
        /// Pull a model on the configured inference provider.
        /// </summary>
        /// <param name="modelName">Name of the model to pull.</param>
        /// <returns>True on success, false on failure or if not supported.</returns>
        public async Task<bool> PullModelAsync(string modelName)
        {
            if (String.IsNullOrEmpty(modelName)) throw new ArgumentNullException(nameof(modelName));

            if (_Settings.Provider != InferenceProviderEnum.Ollama)
            {
                _Logging.Warn(_Header + "pull not supported for provider: " + _Settings.Provider.ToString());
                return false;
            }

            try
            {
                string url = _Settings.Endpoint.TrimEnd('/') + "/api/pull";

                object requestBody = new { name = modelName, stream = false };
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _HttpClient.SendAsync(request).ConfigureAwait(false);
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "Ollama pull returned " + (int)response.StatusCode + ": " + responseBody);
                        return false;
                    }

                    _Logging.Info(_Header + "successfully pulled model: " + modelName);
                    return true;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception pulling model " + modelName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Pull a model with streaming progress, invoking a callback for each progress update.
        /// </summary>
        /// <param name="modelName">Name of the model to pull.</param>
        /// <param name="onProgress">Callback invoked for each progress event.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task PullModelWithProgressAsync(string modelName, Func<PullProgress, Task> onProgress, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(modelName)) throw new ArgumentNullException(nameof(modelName));
            if (onProgress == null) throw new ArgumentNullException(nameof(onProgress));

            if (_Settings.Provider != InferenceProviderEnum.Ollama)
            {
                _Logging.Warn(_Header + "pull not supported for provider: " + _Settings.Provider.ToString());
                PullProgress errorProgress = new PullProgress { ModelName = modelName, HasError = true, ErrorMessage = "Pull is not supported by the configured provider.", IsComplete = true };
                await onProgress(errorProgress).ConfigureAwait(false);
                return;
            }

            PullProgress progress = new PullProgress { ModelName = modelName, Status = "starting", StartedUtc = DateTime.UtcNow };
            await onProgress(progress).ConfigureAwait(false);

            try
            {
                string url = _Settings.Endpoint.TrimEnd('/') + "/api/pull";
                object requestBody = new { name = modelName, stream = true };
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                            _Logging.Warn(_Header + "Ollama pull streaming returned " + (int)response.StatusCode + ": " + errorBody);
                            progress.HasError = true;
                            progress.ErrorMessage = "Pull failed with status " + (int)response.StatusCode + ": " + errorBody;
                            progress.IsComplete = true;
                            await onProgress(progress).ConfigureAwait(false);
                            return;
                        }

                        using (Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
                            {
                                if (String.IsNullOrWhiteSpace(line)) continue;

                                try
                                {
                                    OllamaPullStreamLine streamLine = JsonSerializer.Deserialize<OllamaPullStreamLine>(line, _JsonOptions);

                                    progress.Status = streamLine.Status ?? progress.Status;
                                    progress.Digest = streamLine.Digest ?? progress.Digest;

                                    if (streamLine.Total > 0)
                                    {
                                        progress.TotalBytes = streamLine.Total;
                                        progress.CompletedBytes = streamLine.Completed;
                                        progress.PercentComplete = (int)((double)streamLine.Completed / streamLine.Total * 100);
                                    }

                                    await onProgress(progress).ConfigureAwait(false);
                                }
                                catch (JsonException)
                                {
                                    _Logging.Debug(_Header + "skipping unparseable pull stream line");
                                }
                            }
                        }
                    }
                }

                progress.IsComplete = true;
                progress.PercentComplete = 100;
                progress.Status = "success";
                await onProgress(progress).ConfigureAwait(false);

                _Logging.Info(_Header + "successfully pulled model (streaming): " + modelName);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during streaming pull of " + modelName + ": " + e.Message);
                progress.HasError = true;
                progress.ErrorMessage = e.Message;
                progress.IsComplete = true;
                await onProgress(progress).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Delete a model from the configured inference provider.
        /// </summary>
        /// <param name="modelName">Name of the model to delete.</param>
        /// <returns>True on success, false on failure or if not supported.</returns>
        public async Task<bool> DeleteModelAsync(string modelName)
        {
            if (String.IsNullOrEmpty(modelName)) throw new ArgumentNullException(nameof(modelName));

            if (_Settings.Provider != InferenceProviderEnum.Ollama)
            {
                _Logging.Warn(_Header + "delete not supported for provider: " + _Settings.Provider.ToString());
                return false;
            }

            try
            {
                string url = _Settings.Endpoint.TrimEnd('/') + "/api/delete";

                object requestBody = new { name = modelName };
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _HttpClient.SendAsync(request).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        _Logging.Warn(_Header + "Ollama delete returned " + (int)response.StatusCode + ": " + responseBody);
                        return false;
                    }

                    _Logging.Info(_Header + "successfully deleted model: " + modelName);
                    return true;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception deleting model " + modelName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Generate a response from a language model using the provided context and user message.
        /// </summary>
        /// <param name="systemPrompt">System prompt for the model.</param>
        /// <param name="contextChunks">List of context chunks retrieved from documents.</param>
        /// <param name="userMessage">User message to respond to.</param>
        /// <param name="model">Model name or identifier.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="topP">Top-p nucleus sampling (0.0 to 1.0).</param>
        /// <param name="provider">Inference provider type.</param>
        /// <param name="endpoint">Inference provider endpoint URL.</param>
        /// <param name="apiKey">Inference provider API key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Inference result containing the response or error details.</returns>
        public async Task<InferenceResult> GenerateResponseAsync(
            string systemPrompt,
            List<string> contextChunks,
            string userMessage,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            InferenceProviderEnum provider,
            string endpoint,
            string apiKey,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(userMessage)) throw new ArgumentNullException(nameof(userMessage));

            string effectiveModel = !String.IsNullOrEmpty(model) ? model : _Settings.DefaultModel;
            string effectiveEndpoint = !String.IsNullOrEmpty(endpoint) ? endpoint : _Settings.Endpoint;
            string effectiveApiKey = !String.IsNullOrEmpty(apiKey) ? apiKey : _Settings.ApiKey;

            // Build the system message with context
            string fullSystemMessage = BuildSystemMessage(systemPrompt, contextChunks);

            _Logging.Debug(_Header + "generating response using provider " + provider.ToString() + " model " + effectiveModel);

            try
            {
                switch (provider)
                {
                    case InferenceProviderEnum.OpenAI:
                        return await GenerateOpenAIResponseAsync(
                            fullSystemMessage,
                            userMessage,
                            effectiveModel,
                            maxTokens,
                            temperature,
                            topP,
                            effectiveEndpoint,
                            effectiveApiKey,
                            token).ConfigureAwait(false);

                    case InferenceProviderEnum.Ollama:
                        return await GenerateOllamaResponseAsync(
                            fullSystemMessage,
                            userMessage,
                            effectiveModel,
                            maxTokens,
                            temperature,
                            topP,
                            effectiveEndpoint,
                            effectiveApiKey,
                            token).ConfigureAwait(false);

                    case InferenceProviderEnum.Gemini:
                        return await GenerateGeminiResponseAsync(
                            fullSystemMessage,
                            userMessage,
                            effectiveModel,
                            maxTokens,
                            temperature,
                            topP,
                            effectiveEndpoint,
                            effectiveApiKey,
                            token).ConfigureAwait(false);

                    default:
                        _Logging.Warn(_Header + "unsupported inference provider: " + provider.ToString());
                        return InferenceResult.FromError("Unsupported inference provider: " + provider.ToString());
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during inference: " + e.Message);
                return InferenceResult.FromError("Inference exception: " + e.Message);
            }
        }

        /// <summary>
        /// Generate a response from a language model using a multi-message conversation.
        /// </summary>
        /// <param name="messages">List of chat completion messages.</param>
        /// <param name="model">Model name or identifier.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="topP">Top-p nucleus sampling (0.0 to 1.0).</param>
        /// <param name="provider">Inference provider type.</param>
        /// <param name="endpoint">Inference provider endpoint URL.</param>
        /// <param name="apiKey">Inference provider API key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Inference result containing the response or error details.</returns>
        public async Task<InferenceResult> GenerateResponseAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            InferenceProviderEnum provider,
            string endpoint,
            string apiKey,
            CancellationToken token = default)
        {
            if (messages == null || messages.Count == 0) throw new ArgumentNullException(nameof(messages));

            string effectiveModel = !String.IsNullOrEmpty(model) ? model : _Settings.DefaultModel;
            string effectiveEndpoint = !String.IsNullOrEmpty(endpoint) ? endpoint : _Settings.Endpoint;
            string effectiveApiKey = !String.IsNullOrEmpty(apiKey) ? apiKey : _Settings.ApiKey;

            _Logging.Debug(_Header + "generating multi-message response using provider " + provider.ToString() + " model " + effectiveModel);

            try
            {
                switch (provider)
                {
                    case InferenceProviderEnum.OpenAI:
                        return await GenerateOpenAIResponseFromMessagesAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey, token).ConfigureAwait(false);

                    case InferenceProviderEnum.Ollama:
                        return await GenerateOllamaResponseFromMessagesAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey, token).ConfigureAwait(false);

                    case InferenceProviderEnum.Gemini:
                        return await GenerateGeminiResponseFromMessagesAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey, token).ConfigureAwait(false);

                    default:
                        _Logging.Warn(_Header + "unsupported inference provider: " + provider.ToString());
                        return InferenceResult.FromError("Unsupported inference provider: " + provider.ToString());
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during inference: " + e.Message);
                return InferenceResult.FromError("Inference exception: " + e.Message);
            }
        }

        /// <summary>
        /// Generate a streaming response from a language model using a multi-message conversation.
        /// </summary>
        /// <param name="messages">List of chat completion messages.</param>
        /// <param name="model">Model name or identifier.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="topP">Top-p nucleus sampling (0.0 to 1.0).</param>
        /// <param name="provider">Inference provider type.</param>
        /// <param name="endpoint">Inference provider endpoint URL.</param>
        /// <param name="apiKey">Inference provider API key.</param>
        /// <param name="onDelta">Callback invoked for each content delta.</param>
        /// <param name="onComplete">Callback invoked when generation is complete, with the full accumulated content.</param>
        /// <param name="onError">Callback invoked on error.</param>
        /// <param name="onConnectionEstablished">Callback invoked on connection establishment.</param>
        /// <param name="onTelemetry">Callback invoked with provider-agnostic inference telemetry.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task GenerateResponseStreamingAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            InferenceProviderEnum provider,
            string endpoint,
            string apiKey,
            Func<string, Task> onDelta,
            Func<string, Task> onComplete,
            Func<string, Task> onError,
            Action onConnectionEstablished = null,
            Action<AssistantPerformanceStage> onTelemetry = null,
            CancellationToken token = default)
        {
            if (messages == null || messages.Count == 0) throw new ArgumentNullException(nameof(messages));
            if (onDelta == null) throw new ArgumentNullException(nameof(onDelta));
            if (onComplete == null) throw new ArgumentNullException(nameof(onComplete));
            if (onError == null) throw new ArgumentNullException(nameof(onError));

            string effectiveModel = !String.IsNullOrEmpty(model) ? model : _Settings.DefaultModel;
            string effectiveEndpoint = !String.IsNullOrEmpty(endpoint) ? endpoint : _Settings.Endpoint;
            string effectiveApiKey = !String.IsNullOrEmpty(apiKey) ? apiKey : _Settings.ApiKey;

            _Logging.Debug(_Header + "generating streaming response using provider " + provider.ToString() + " model " + effectiveModel);

            try
            {
                switch (provider)
                {
                    case InferenceProviderEnum.OpenAI:
                        await GenerateOpenAIStreamingAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey, 
                            onDelta, onComplete, onError, onConnectionEstablished, onTelemetry, token).ConfigureAwait(false);
                        break;

                    case InferenceProviderEnum.Ollama:
                        await GenerateOllamaStreamingAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey,
                            onDelta, onComplete, onError, onConnectionEstablished, onTelemetry, token).ConfigureAwait(false);
                        break;

                    case InferenceProviderEnum.Gemini:
                        await GenerateGeminiStreamingAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey,
                            onDelta, onComplete, onError, onConnectionEstablished, onTelemetry, token).ConfigureAwait(false);
                        break;

                    default:
                        string error = "Unsupported inference provider: " + provider.ToString();
                        onTelemetry?.Invoke(new AssistantPerformanceStage
                        {
                            Name = "streaming_inference",
                            Kind = "inference",
                            Provider = provider.ToString(),
                            ApiFormat = provider.ToString(),
                            Model = effectiveModel,
                            EndpointName = effectiveEndpoint,
                            EndpointType = "completion",
                            Success = false,
                            ErrorType = "UnsupportedProvider",
                            ErrorMessage = error
                        });
                        await onError(error).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during streaming inference: " + e.Message);
                onTelemetry?.Invoke(new AssistantPerformanceStage
                {
                    Name = "streaming_inference",
                    Kind = "inference",
                    Provider = provider.ToString(),
                    ApiFormat = provider.ToString(),
                    Model = effectiveModel,
                    EndpointName = effectiveEndpoint,
                    EndpointType = "completion",
                    Success = false,
                    ErrorType = e.GetType().Name,
                    ErrorMessage = e.Message
                });
                await onError("Inference exception: " + e.Message).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Build the full system message including context chunks.
        /// </summary>
        /// <param name="systemPrompt">Base system prompt.</param>
        /// <param name="contextChunks">List of context chunks.</param>
        /// <param name="enableCitations">Whether to use indexed citation format.</param>
        /// <param name="chunkLabels">Source labels for each chunk when citations are enabled.</param>
        /// <returns>Complete system message with context.</returns>
        public string BuildSystemMessage(
            string systemPrompt,
            List<string> contextChunks,
            bool enableCitations = false,
            List<string> chunkLabels = null)
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrEmpty(systemPrompt))
            {
                sb.Append(systemPrompt);
            }

            if (contextChunks != null && contextChunks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();

                if (enableCitations && chunkLabels != null && chunkLabels.Count == contextChunks.Count)
                {
                    sb.AppendLine("Use the following numbered sources to answer the user's question.");
                    sb.AppendLine("When citing a source, place its number in square brackets inline, e.g. [1], [2]. For multiple sources use [1][2][3], not [1, 2, 3].");
                    sb.AppendLine("IMPORTANT: The number inside the brackets MUST match the source number from the list below. [1] means source [1] below, [2] means source [2] below, etc.");
                    sb.AppendLine("Do NOT renumber or reorder the sources. Do NOT create your own reference list or bibliography at the end of your response.");
                    sb.AppendLine("Only cite sources numbered [1] through [" + contextChunks.Count + "]. Do not invent citation numbers.");
                    sb.AppendLine();
                    sb.AppendLine("Sources:");

                    for (int i = 0; i < contextChunks.Count; i++)
                    {
                        sb.AppendLine();
                        sb.AppendLine("[" + (i + 1) + "] " + chunkLabels[i]);
                        sb.AppendLine(contextChunks[i]);
                    }

                    sb.AppendLine();
                    sb.AppendLine("Cite sources using their EXACT number from the list above (e.g. [7] for source [7]). Do NOT add a bibliography or reference list at the end.");
                }
                else
                {
                    // Original behavior when citations are disabled
                    sb.AppendLine("Use the following context to answer the user's question:");
                    sb.AppendLine();
                    sb.AppendLine("Context:");

                    foreach (string chunk in contextChunks)
                    {
                        sb.AppendLine("---");
                        sb.AppendLine(chunk);
                    }

                    sb.AppendLine("---");
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Private-Methods

        private AssistantPerformanceStage StartTelemetry(
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

        private void MarkResponseHeaders(AssistantPerformanceStage telemetry, Stopwatch stopwatch, HttpResponseMessage response)
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

        private void FinishTelemetry(
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

        private void MarkFirstToken(AssistantPerformanceStage telemetry, Stopwatch stopwatch)
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

        private void MarkLastToken(AssistantPerformanceStage telemetry, Stopwatch stopwatch)
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

        private void ApplyUsage(AssistantPerformanceStage telemetry, ChatCompletionUsage usage)
        {
            if (telemetry == null || usage == null) return;

            telemetry.Tokens ??= new AssistantTokenUsageTelemetry();
            telemetry.Tokens.Input = usage.PromptTokens > 0 ? usage.PromptTokens : telemetry.Tokens.Input;
            telemetry.Tokens.Output = usage.CompletionTokens > 0 ? usage.CompletionTokens : telemetry.Tokens.Output;
            telemetry.Tokens.Total = usage.TotalTokens > 0 ? usage.TotalTokens : telemetry.Tokens.Total;
        }

        private void ApplyGeminiUsage(AssistantPerformanceStage telemetry, GeminiUsageMetadata usage)
        {
            if (telemetry == null || usage == null) return;

            telemetry.Tokens ??= new AssistantTokenUsageTelemetry();
            telemetry.Tokens.Input = usage.PromptTokenCount ?? telemetry.Tokens.Input;
            telemetry.Tokens.Output = usage.CandidatesTokenCount ?? telemetry.Tokens.Output;
            telemetry.Tokens.Total = usage.TotalTokenCount ?? telemetry.Tokens.Total;
        }

        private void ApplyOllamaMetrics(
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

        private string GetHeaderValue(HttpResponseMessage response, string headerName)
        {
            if (response == null || String.IsNullOrWhiteSpace(headerName)) return null;

            if (response.Headers.TryGetValues(headerName, out IEnumerable<string> values))
                return values?.FirstOrDefault();

            if (response.Content?.Headers != null
                && response.Content.Headers.TryGetValues(headerName, out IEnumerable<string> contentValues))
                return contentValues?.FirstOrDefault();

            return null;
        }

        private double? NanosecondsToMilliseconds(long? nanoseconds)
        {
            return nanoseconds.HasValue && nanoseconds.Value > 0
                ? Math.Round(nanoseconds.Value / 1_000_000.0, 2)
                : null;
        }

        private async Task<InferenceResult> GenerateOpenAIResponseAsync(
            string systemMessage,
            string userMessage,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>();
            if (!String.IsNullOrEmpty(systemMessage))
                messages.Add(new ChatCompletionMessage { Role = "system", Content = systemMessage });

            messages.Add(new ChatCompletionMessage { Role = "user", Content = userMessage });

            return await GenerateOpenAIResponseFromMessagesAsync(
                messages,
                model,
                maxTokens,
                temperature,
                topP,
                endpoint,
                apiKey,
                token).ConfigureAwait(false);
        }

        private async Task<InferenceResult> GenerateGeminiResponseAsync(
            string systemMessage,
            string userMessage,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>();
            if (!String.IsNullOrEmpty(systemMessage))
                messages.Add(new ChatCompletionMessage { Role = "system", Content = systemMessage });

            messages.Add(new ChatCompletionMessage { Role = "user", Content = userMessage });

            return await GenerateGeminiResponseFromMessagesAsync(
                messages,
                model,
                maxTokens,
                temperature,
                topP,
                endpoint,
                apiKey,
                token).ConfigureAwait(false);
        }

        private async Task<InferenceResult> GenerateOllamaResponseAsync(
            string systemMessage,
            string userMessage,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>();
            if (!String.IsNullOrEmpty(systemMessage))
                messages.Add(new ChatCompletionMessage { Role = "system", Content = systemMessage });

            messages.Add(new ChatCompletionMessage { Role = "user", Content = userMessage });

            return await GenerateOllamaResponseFromMessagesAsync(
                messages,
                model,
                maxTokens,
                temperature,
                topP,
                endpoint,
                apiKey,
                token).ConfigureAwait(false);
        }

        private async Task<List<InferenceModel>> ListOllamaModelsAsync()
        {
            string url = InferenceProviderHelper.GetModelsUrl(_Settings.Endpoint, InferenceProviderEnum.Ollama);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                HttpResponseMessage response = await _HttpClient.SendAsync(request).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "Ollama list models returned " + (int)response.StatusCode + ": " + responseBody);
                    return new List<InferenceModel>();
                }

                OllamaTagsResponse tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(responseBody, _JsonOptions);
                List<InferenceModel> models = new List<InferenceModel>();

                if (tagsResponse?.Models != null)
                {
                    foreach (OllamaModelEntry entry in tagsResponse.Models)
                    {
                        models.Add(new InferenceModel
                        {
                            Name = entry.Name,
                            SizeBytes = entry.Size,
                            ModifiedUtc = entry.ModifiedAt,
                            OwnedBy = null,
                            PullSupported = true
                        });
                    }
                }

                _Logging.Debug(_Header + "Ollama returned " + models.Count + " models");
                return models;
            }
        }

        private async Task<List<InferenceModel>> ListOpenAIModelsAsync()
        {
            string url = InferenceProviderHelper.GetModelsUrl(_Settings.Endpoint, InferenceProviderEnum.OpenAI);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.OpenAI, _Settings.ApiKey);

                HttpResponseMessage response = await _HttpClient.SendAsync(request).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "OpenAI list models returned " + (int)response.StatusCode + ": " + responseBody);
                    return new List<InferenceModel>();
                }

                OpenAIModelsResponse modelsResponse = JsonSerializer.Deserialize<OpenAIModelsResponse>(responseBody, _JsonOptions);
                List<InferenceModel> models = new List<InferenceModel>();

                if (modelsResponse?.Data != null)
                {
                    foreach (OpenAIModelEntry entry in modelsResponse.Data)
                    {
                        DateTime? created = null;
                        if (entry.Created > 0)
                        {
                            created = DateTimeOffset.FromUnixTimeSeconds(entry.Created).UtcDateTime;
                        }

                        models.Add(new InferenceModel
                        {
                            Name = entry.Id,
                            SizeBytes = 0,
                            ModifiedUtc = created,
                            OwnedBy = entry.OwnedBy,
                            PullSupported = false
                        });
                    }
                }

                _Logging.Debug(_Header + "OpenAI returned " + models.Count + " models");
                return models;
            }
        }

        private async Task<List<InferenceModel>> ListGeminiModelsAsync()
        {
            string url = InferenceProviderHelper.GetModelsUrl(_Settings.Endpoint, InferenceProviderEnum.Gemini);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.Gemini, _Settings.ApiKey);

                HttpResponseMessage response = await _HttpClient.SendAsync(request).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "Gemini list models returned " + (int)response.StatusCode + ": " + responseBody);
                    return new List<InferenceModel>();
                }

                GeminiModelsResponse modelsResponse = JsonSerializer.Deserialize<GeminiModelsResponse>(responseBody, _JsonOptions);
                List<InferenceModel> models = new List<InferenceModel>();

                if (modelsResponse?.Models != null)
                {
                    foreach (GeminiModelEntry entry in modelsResponse.Models)
                    {
                        string name = entry.Name ?? String.Empty;
                        if (name.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
                            name = name.Substring("models/".Length);

                        models.Add(new InferenceModel
                        {
                            Name = name,
                            SizeBytes = 0,
                            ModifiedUtc = null,
                            OwnedBy = "Google",
                            PullSupported = false
                        });
                    }
                }

                _Logging.Debug(_Header + "Gemini returned " + models.Count + " models");
                return models;
            }
        }

        private async Task<InferenceResult> GenerateOpenAIResponseFromMessagesAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.OpenAI, model, false);

            List<object> msgObjects = new List<object>();
            foreach (ChatCompletionMessage msg in messages)
            {
                msgObjects.Add(new { role = msg.Role, content = msg.Content });
            }

            object requestBody = new
            {
                model = model,
                messages = msgObjects,
                max_tokens = maxTokens,
                temperature = temperature,
                top_p = topP
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.OpenAI, model, endpoint, false);
            Stopwatch telemetrySw = Stopwatch.StartNew();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.OpenAI, apiKey);

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(
                            _Header +
                            "OpenAI API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| Bearer token  : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + responseBody);
                        string error = "OpenAI API returned " + (int)response.StatusCode;
                        FinishTelemetry(telemetry, telemetrySw, false, "HttpStatus", error);
                        return InferenceResult.FromError(error, telemetry);
                    }

                    OpenAIChatResponse chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseBody, _JsonOptions);
                    ApplyUsage(telemetry, chatResponse?.Usage);

                    if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                    {
                        string content = chatResponse.Choices[0].Message?.Content;
                        _Logging.Debug(_Header + "OpenAI response received (" + (content != null ? content.Length : 0) + " characters)");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(content, telemetry);
                    }

                    _Logging.Warn(_Header + "OpenAI response contained no choices");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoChoices", "OpenAI response contained no choices.");
                    return InferenceResult.FromError("OpenAI response contained no choices.", telemetry);
                }
            }
        }

        private async Task<InferenceResult> GenerateOllamaResponseFromMessagesAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            string url = endpoint.TrimEnd('/') + "/api/chat";

            List<object> msgObjects = new List<object>();
            foreach (ChatCompletionMessage msg in messages)
            {
                msgObjects.Add(new { role = msg.Role, content = msg.Content });
            }

            object requestBody = new
            {
                model = model,
                messages = msgObjects,
                stream = false,
                options = new
                {
                    temperature = temperature,
                    top_p = topP,
                    num_predict = maxTokens
                }
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.Ollama, model, endpoint, false);
            Stopwatch telemetrySw = Stopwatch.StartNew();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(
                            _Header +
                            "Ollama API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| Bearer token  : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + responseBody);
                        string error = "Ollama API returned " + (int)response.StatusCode;
                        FinishTelemetry(telemetry, telemetrySw, false, "HttpStatus", error);
                        return InferenceResult.FromError(error, telemetry);
                    }

                    OllamaChatResponse chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, _JsonOptions);
                    ApplyOllamaMetrics(
                        telemetry,
                        chatResponse?.TotalDuration,
                        chatResponse?.LoadDuration,
                        chatResponse?.PromptEvalCount,
                        chatResponse?.PromptEvalDuration,
                        chatResponse?.EvalCount,
                        chatResponse?.EvalDuration);

                    if (chatResponse?.Message != null)
                    {
                        string content = chatResponse.Message.Content;
                        _Logging.Debug(_Header + "Ollama response received (" + (content != null ? content.Length : 0) + " characters)");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(content, telemetry);
                    }

                    _Logging.Warn(_Header + "Ollama response contained no message");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoMessage", "Ollama response contained no message.");
                    return InferenceResult.FromError("Ollama response contained no message.", telemetry);
                }
            }
        }

        private async Task<InferenceResult> GenerateGeminiResponseFromMessagesAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            CancellationToken token)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.Gemini, model, false);
            object requestBody = BuildGeminiRequestBody(messages, maxTokens, temperature, topP);
            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.Gemini, model, endpoint, false);
            Stopwatch telemetrySw = Stopwatch.StartNew();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.Gemini, apiKey);

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(
                            _Header +
                            "Gemini API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| API key       : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + responseBody);
                        string error = "Gemini API returned " + (int)response.StatusCode;
                        FinishTelemetry(telemetry, telemetrySw, false, "HttpStatus", error);
                        return InferenceResult.FromError(error, telemetry);
                    }

                    GeminiResponse geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, _JsonOptions);
                    ApplyGeminiUsage(telemetry, geminiResponse?.UsageMetadata);
                    string content = ExtractGeminiText(geminiResponse);
                    if (!String.IsNullOrEmpty(content))
                    {
                        _Logging.Debug(_Header + "Gemini response received (" + content.Length + " characters)");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(content, telemetry);
                    }

                    _Logging.Warn(_Header + "Gemini response contained no candidate content");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoCandidateContent", "Gemini response contained no candidate content.");
                    return InferenceResult.FromError("Gemini response contained no candidate content.", telemetry);
                }
            }
        }

        private async Task GenerateOpenAIStreamingAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            Func<string, Task> onDelta,
            Func<string, Task> onComplete,
            Func<string, Task> onError,
            Action onConnectionEstablished,
            Action<AssistantPerformanceStage> onTelemetry,
            CancellationToken token)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.OpenAI, model, true);

            List<object> msgObjects = new List<object>();
            foreach (ChatCompletionMessage msg in messages)
            {
                msgObjects.Add(new { role = msg.Role, content = msg.Content });
            }

            object requestBody = new
            {
                model = model,
                messages = msgObjects,
                max_tokens = maxTokens,
                temperature = temperature,
                top_p = topP,
                stream = true,
                stream_options = new
                {
                    include_usage = true
                }
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            StringBuilder fullContent = new StringBuilder();
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.OpenAI, model, endpoint, true);
            Stopwatch telemetrySw = Stopwatch.StartNew();
            bool telemetryEmitted = false;

            void EmitTelemetry(bool success, string errorType = null, string errorMessage = null)
            {
                if (telemetryEmitted) return;
                telemetryEmitted = true;
                MarkLastToken(telemetry, telemetrySw);
                FinishTelemetry(telemetry, telemetrySw, success, errorType, errorMessage);
                onTelemetry?.Invoke(telemetry);
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.OpenAI, apiKey);

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    onConnectionEstablished?.Invoke();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        _Logging.Warn(
                            _Header +
                            "OpenAI API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| Bearer token  : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + errorBody);
                        _Logging.Warn(_Header + "OpenAI streaming returned " + (int)response.StatusCode);
                        string error = "OpenAI API returned " + (int)response.StatusCode + ": " + errorBody;
                        EmitTelemetry(false, "HttpStatus", error);
                        await onError(error).ConfigureAwait(false);
                        return;
                    }

                    using (Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
                        {
                            if (String.IsNullOrWhiteSpace(line)) continue;

                            if (line.StartsWith("data: "))
                            {
                                string data = line.Substring(6);

                                if (data == "[DONE]")
                                {
                                    EmitTelemetry(true);
                                    await onComplete(fullContent.ToString()).ConfigureAwait(false);
                                    return;
                                }

                                try
                                {
                                    OpenAIStreamingChunk chunk = JsonSerializer.Deserialize<OpenAIStreamingChunk>(data, _JsonOptions);
                                    ApplyUsage(telemetry, chunk?.Usage);
                                    string deltaContent = chunk?.Choices != null && chunk.Choices.Count > 0
                                        ? chunk.Choices[0].Delta?.Content
                                        : null;
                                    if (!String.IsNullOrEmpty(deltaContent))
                                    {
                                        MarkFirstToken(telemetry, telemetrySw);
                                        fullContent.Append(deltaContent);
                                        await onDelta(deltaContent).ConfigureAwait(false);
                                    }
                                }
                                catch (JsonException)
                                {
                                    _Logging.Debug(_Header + "skipping unparseable SSE line");
                                }
                            }
                        }
                    }
                }
            }

            EmitTelemetry(true);
            await onComplete(fullContent.ToString()).ConfigureAwait(false);
        }

        private async Task GenerateGeminiStreamingAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            Func<string, Task> onDelta,
            Func<string, Task> onComplete,
            Func<string, Task> onError,
            Action onConnectionEstablished,
            Action<AssistantPerformanceStage> onTelemetry,
            CancellationToken token)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.Gemini, model, true);
            object requestBody = BuildGeminiRequestBody(messages, maxTokens, temperature, topP);
            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            StringBuilder fullContent = new StringBuilder();
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.Gemini, model, endpoint, true);
            Stopwatch telemetrySw = Stopwatch.StartNew();
            bool telemetryEmitted = false;

            void EmitTelemetry(bool success, string errorType = null, string errorMessage = null)
            {
                if (telemetryEmitted) return;
                telemetryEmitted = true;
                MarkLastToken(telemetry, telemetrySw);
                FinishTelemetry(telemetry, telemetrySw, success, errorType, errorMessage);
                onTelemetry?.Invoke(telemetry);
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                InferenceProviderHelper.ApplyAuthentication(request, InferenceProviderEnum.Gemini, apiKey);

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    onConnectionEstablished?.Invoke();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        _Logging.Warn(
                            _Header +
                            "Gemini API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| API key       : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + errorBody);
                        string error = "Gemini API returned " + (int)response.StatusCode + ": " + errorBody;
                        EmitTelemetry(false, "HttpStatus", error);
                        await onError(error).ConfigureAwait(false);
                        return;
                    }

                    using (Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
                        {
                            if (String.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                                continue;

                            string data = line.Substring(6);
                            if (String.IsNullOrWhiteSpace(data))
                                continue;

                            try
                            {
                                GeminiResponse geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(data, _JsonOptions);
                                ApplyGeminiUsage(telemetry, geminiResponse?.UsageMetadata);
                                string chunkText = ExtractGeminiText(geminiResponse);
                                if (!String.IsNullOrEmpty(chunkText))
                                {
                                    string deltaContent = chunkText;
                                    string accumulated = fullContent.ToString();
                                    if (!String.IsNullOrEmpty(accumulated)
                                        && chunkText.StartsWith(accumulated, StringComparison.Ordinal))
                                    {
                                        deltaContent = chunkText.Substring(accumulated.Length);
                                    }

                                    if (!String.IsNullOrEmpty(deltaContent))
                                    {
                                        MarkFirstToken(telemetry, telemetrySw);
                                        fullContent.Append(deltaContent);
                                        await onDelta(deltaContent).ConfigureAwait(false);
                                    }
                                }
                            }
                            catch (JsonException)
                            {
                                _Logging.Debug(_Header + "skipping unparseable Gemini SSE line");
                            }
                        }
                    }
                }
            }

            EmitTelemetry(true);
            await onComplete(fullContent.ToString()).ConfigureAwait(false);
        }

        private async Task GenerateOllamaStreamingAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            Func<string, Task> onDelta,
            Func<string, Task> onComplete,
            Func<string, Task> onError,
            Action onConnectionEstablished,
            Action<AssistantPerformanceStage> onTelemetry,
            CancellationToken token)
        {
            string url = endpoint.TrimEnd('/') + "/api/chat";

            List<object> msgObjects = new List<object>();
            foreach (ChatCompletionMessage msg in messages)
            {
                msgObjects.Add(new { role = msg.Role, content = msg.Content });
            }

            object requestBody = new
            {
                model = model,
                messages = msgObjects,
                stream = true,
                options = new
                {
                    temperature = temperature,
                    top_p = topP,
                    num_predict = maxTokens
                }
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            StringBuilder fullContent = new StringBuilder();
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.Ollama, model, endpoint, true);
            Stopwatch telemetrySw = Stopwatch.StartNew();
            bool telemetryEmitted = false;

            void EmitTelemetry(bool success, string errorType = null, string errorMessage = null)
            {
                if (telemetryEmitted) return;
                telemetryEmitted = true;
                MarkLastToken(telemetry, telemetrySw);
                FinishTelemetry(telemetry, telemetrySw, success, errorType, errorMessage);
                onTelemetry?.Invoke(telemetry);
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    onConnectionEstablished?.Invoke();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        _Logging.Warn(
                            _Header +
                            "Ollama API returned status " + (int)response.StatusCode + Environment.NewLine +
                            "| URL           : " + url + Environment.NewLine +
                            "| Bearer token  : " + apiKey + Environment.NewLine +
                            "| Response body : " + Environment.NewLine + errorBody);
                        _Logging.Warn(_Header + "Ollama streaming returned " + (int)response.StatusCode);
                        string error = "Ollama API returned " + (int)response.StatusCode + ": " + errorBody;
                        EmitTelemetry(false, "HttpStatus", error);
                        await onError(error).ConfigureAwait(false);
                        return;
                    }

                    using (Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
                        {
                            if (String.IsNullOrWhiteSpace(line)) continue;

                            try
                            {
                                OllamaChatStreamLine streamLine = JsonSerializer.Deserialize<OllamaChatStreamLine>(line, _JsonOptions);

                                if (streamLine?.Done == true)
                                {
                                    ApplyOllamaMetrics(
                                        telemetry,
                                        streamLine.TotalDuration,
                                        streamLine.LoadDuration,
                                        streamLine.PromptEvalCount,
                                        streamLine.PromptEvalDuration,
                                        streamLine.EvalCount,
                                        streamLine.EvalDuration);
                                    EmitTelemetry(true);
                                    await onComplete(fullContent.ToString()).ConfigureAwait(false);
                                    return;
                                }

                                string deltaContent = streamLine?.Message?.Content;
                                if (!String.IsNullOrEmpty(deltaContent))
                                {
                                    MarkFirstToken(telemetry, telemetrySw);
                                    fullContent.Append(deltaContent);
                                    await onDelta(deltaContent).ConfigureAwait(false);
                                }
                            }
                            catch (JsonException)
                            {
                                _Logging.Debug(_Header + "skipping unparseable Ollama stream line");
                            }
                        }
                    }
                }
            }

            EmitTelemetry(true);
            await onComplete(fullContent.ToString()).ConfigureAwait(false);
        }

        private object BuildGeminiRequestBody(
            List<ChatCompletionMessage> messages,
            int maxTokens,
            double temperature,
            double topP)
        {
            List<object> contents = new List<object>();
            List<string> systemParts = new List<string>();

            foreach (ChatCompletionMessage msg in messages)
            {
                if (msg == null || String.IsNullOrEmpty(msg.Content)) continue;

                if (String.Equals(msg.Role, "system", StringComparison.OrdinalIgnoreCase))
                {
                    systemParts.Add(msg.Content);
                    continue;
                }

                string role = String.Equals(msg.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                contents.Add(new
                {
                    role = role,
                    parts = new[]
                    {
                        new { text = msg.Content }
                    }
                });
            }

            var requestBody = new Dictionary<string, object>
            {
                ["contents"] = contents,
                ["generationConfig"] = new
                {
                    maxOutputTokens = maxTokens,
                    temperature = temperature,
                    topP = topP
                }
            };

            if (systemParts.Count > 0)
            {
                requestBody["system_instruction"] = new
                {
                    parts = new[]
                    {
                        new { text = String.Join(Environment.NewLine + Environment.NewLine, systemParts) }
                    }
                };
            }

            return requestBody;
        }

        private string ExtractGeminiText(GeminiResponse response)
        {
            if (response?.Candidates == null || response.Candidates.Count < 1)
                return null;

            StringBuilder sb = new StringBuilder();

            foreach (GeminiCandidate candidate in response.Candidates)
            {
                if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count < 1)
                    continue;

                foreach (GeminiContentPart part in candidate.Content.Parts)
                {
                    if (!String.IsNullOrEmpty(part?.Text))
                        sb.Append(part.Text);
                }
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }

        #endregion

        #region Private-Classes

        private class OllamaPullStreamLine
        {
            public string Status { get; set; } = null;
            public string Digest { get; set; } = null;
            public long Total { get; set; } = 0;
            public long Completed { get; set; } = 0;
        }

        private class OllamaTagsResponse
        {
            /// <summary>
            /// List of models.
            /// </summary>
            public List<OllamaModelEntry> Models { get; set; } = null;
        }

        private class OllamaModelEntry
        {
            /// <summary>
            /// Model name.
            /// </summary>
            public string Name { get; set; } = null;

            /// <summary>
            /// Model size in bytes.
            /// </summary>
            public long Size { get; set; } = 0;

            /// <summary>
            /// Last modified timestamp.
            /// </summary>
            [JsonPropertyName("modified_at")]
            public DateTime? ModifiedAt { get; set; } = null;
        }

        private class OpenAIModelsResponse
        {
            /// <summary>
            /// List of model entries.
            /// </summary>
            public List<OpenAIModelEntry> Data { get; set; } = null;
        }

        private class GeminiModelsResponse
        {
            /// <summary>
            /// List of Gemini models.
            /// </summary>
            public List<GeminiModelEntry> Models { get; set; } = null;
        }

        private class GeminiModelEntry
        {
            /// <summary>
            /// Model resource name.
            /// </summary>
            public string Name { get; set; } = null;
        }

        private class OpenAIModelEntry
        {
            /// <summary>
            /// Model identifier.
            /// </summary>
            public string Id { get; set; } = null;

            /// <summary>
            /// Creation timestamp (Unix seconds).
            /// </summary>
            public long Created { get; set; } = 0;

            /// <summary>
            /// Model owner.
            /// </summary>
            [JsonPropertyName("owned_by")]
            public string OwnedBy { get; set; } = null;
        }

        private class OpenAIChatResponse
        {
            /// <summary>
            /// Response choices.
            /// </summary>
            public List<OpenAIChoice> Choices { get; set; } = null;

            /// <summary>
            /// Token usage reported by OpenAI-compatible providers.
            /// </summary>
            public ChatCompletionUsage Usage { get; set; } = null;
        }

        private class OpenAIChoice
        {
            /// <summary>
            /// Message in the choice.
            /// </summary>
            public OpenAIMessage Message { get; set; } = null;
        }

        private class OpenAIMessage
        {
            /// <summary>
            /// Message role.
            /// </summary>
            public string Role { get; set; } = null;

            /// <summary>
            /// Message content.
            /// </summary>
            public string Content { get; set; } = null;
        }

        private class OllamaChatResponse
        {
            /// <summary>
            /// Response message.
            /// </summary>
            public OllamaMessage Message { get; set; } = null;

            [JsonPropertyName("total_duration")]
            public long? TotalDuration { get; set; } = null;

            [JsonPropertyName("load_duration")]
            public long? LoadDuration { get; set; } = null;

            [JsonPropertyName("prompt_eval_count")]
            public int? PromptEvalCount { get; set; } = null;

            [JsonPropertyName("prompt_eval_duration")]
            public long? PromptEvalDuration { get; set; } = null;

            [JsonPropertyName("eval_count")]
            public int? EvalCount { get; set; } = null;

            [JsonPropertyName("eval_duration")]
            public long? EvalDuration { get; set; } = null;
        }

        private class OllamaMessage
        {
            /// <summary>
            /// Message role.
            /// </summary>
            public string Role { get; set; } = null;

            /// <summary>
            /// Message content.
            /// </summary>
            public string Content { get; set; } = null;
        }

        #endregion
    }
}
