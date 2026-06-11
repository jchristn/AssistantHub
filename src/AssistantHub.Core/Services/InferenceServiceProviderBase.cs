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
    /// Provides streaming provider request helpers for inference services.
    /// </summary>
    public abstract class InferenceServiceProviderBase : InferenceServiceResponseBase
    {

        /// <summary>
        /// Instantiate the streaming provider helper base.
        /// </summary>
        /// <param name="settings">Inference settings.</param>
        /// <param name="logging">Logging module.</param>
        protected InferenceServiceProviderBase(InferenceSettings settings, LoggingModule logging)
            : base(settings, logging)
        {
        }

        #region Private-Methods

        private protected async Task GenerateOpenAIStreamingAsync(
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
            CancellationToken token,
            Func<string, Task> onThinkingDelta = null)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.OpenAI, model, true);

            List<object> msgObjects = BuildProviderMessages(messages);

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
                                    OpenAIStreamingDelta delta = chunk?.Choices != null && chunk.Choices.Count > 0
                                        ? chunk.Choices[0].Delta
                                        : null;
                                    string deltaThinking = !String.IsNullOrEmpty(delta?.Thinking)
                                        ? delta.Thinking
                                        : delta?.ReasoningContent;
                                    if (!String.IsNullOrEmpty(deltaThinking))
                                    {
                                        MarkFirstToken(telemetry, telemetrySw);
                                        if (onThinkingDelta != null)
                                            await onThinkingDelta(deltaThinking).ConfigureAwait(false);
                                    }
                                    string deltaContent = chunk?.Choices != null && chunk.Choices.Count > 0
                                        ? delta?.Content
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

        private protected async Task GenerateGeminiStreamingAsync(
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
            CancellationToken token,
            Func<string, Task> onThinkingDelta = null)
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

        private protected async Task GenerateOllamaStreamingAsync(
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
            CancellationToken token,
            Func<string, Task> onThinkingDelta = null)
        {
            string url = endpoint.TrimEnd('/') + "/api/chat";

            List<object> msgObjects = BuildOllamaProviderMessages(messages);

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
                                string deltaThinking = streamLine?.Message?.Thinking;
                                if (!String.IsNullOrEmpty(deltaThinking))
                                {
                                    MarkFirstToken(telemetry, telemetrySw);
                                    if (onThinkingDelta != null)
                                        await onThinkingDelta(deltaThinking).ConfigureAwait(false);
                                }
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

        #endregion
    }
}
