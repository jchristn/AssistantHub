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
    /// Provides non-streaming provider request helpers for inference services.
    /// </summary>
    public abstract class InferenceServiceResponseBase : InferenceServiceTelemetryBase
    {

        /// <summary>
        /// Instantiate the non-streaming provider helper base.
        /// </summary>
        /// <param name="settings">Inference settings.</param>
        /// <param name="logging">Logging module.</param>
        protected InferenceServiceResponseBase(InferenceSettings settings, LoggingModule logging)
            : base(settings, logging)
        {
        }

        #region Private-Methods

        private protected async Task<InferenceResult> GenerateOpenAIResponseAsync(
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

        private protected async Task<InferenceResult> GenerateGeminiResponseAsync(
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

        private protected async Task<InferenceResult> GenerateOllamaResponseAsync(
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

        private protected async Task<List<InferenceModel>> ListOllamaModelsAsync()
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

        private protected async Task<List<InferenceModel>> ListOpenAIModelsAsync()
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

        private protected async Task<List<InferenceModel>> ListGeminiModelsAsync()
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

        private protected async Task<InferenceResult> GenerateOpenAIResponseFromMessagesAsync(
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

            List<object> msgObjects = BuildProviderMessages(messages);

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
                        OpenAIChoice choice = chatResponse.Choices[0];
                        string content = choice.Message?.Content;
                        _Logging.Debug(_Header + "OpenAI response received (" + (content != null ? content.Length : 0) + " characters)");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(
                            content,
                            telemetry,
                            choice.FinishReason ?? "stop",
                            NormalizeToolCalls(choice.Message?.ToolCalls));
                    }

                    _Logging.Warn(_Header + "OpenAI response contained no choices");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoChoices", "OpenAI response contained no choices.");
                    return InferenceResult.FromError("OpenAI response contained no choices.", telemetry);
                }
            }
        }

        private protected async Task<InferenceResult> GenerateOllamaResponseFromMessagesAsync(
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

            List<object> msgObjects = BuildOllamaProviderMessages(messages);

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
                        return InferenceResult.FromSuccess(
                            content,
                            telemetry,
                            chatResponse.DoneReason ?? "stop",
                            NormalizeToolCalls(chatResponse.Message.ToolCalls));
                    }

                    _Logging.Warn(_Header + "Ollama response contained no message");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoMessage", "Ollama response contained no message.");
                    return InferenceResult.FromError("Ollama response contained no message.", telemetry);
                }
            }
        }

        private protected async Task<InferenceResult> GenerateOpenAIResponseWithToolsFromMessagesAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            IEnumerable<AssistantModelToolDefinition> tools,
            string toolChoice,
            CancellationToken token)
        {
            string url = InferenceProviderHelper.GetCompletionUrl(endpoint, InferenceProviderEnum.OpenAI, model, false);

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = BuildProviderMessages(messages),
                ["max_tokens"] = maxTokens,
                ["temperature"] = temperature,
                ["top_p"] = topP
            };

            List<AssistantModelToolDefinition> toolList = NormalizeToolDefinitions(tools);
            if (toolList.Count > 0)
            {
                requestBody["tools"] = toolList;
                if (!String.IsNullOrWhiteSpace(toolChoice))
                    requestBody["tool_choice"] = toolChoice.Trim();
            }

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
                        _Logging.Warn(_Header + "OpenAI tool-capable API returned status " + (int)response.StatusCode + ": " + responseBody);
                        string error = "OpenAI API returned " + (int)response.StatusCode;
                        FinishTelemetry(telemetry, telemetrySw, false, "HttpStatus", error);
                        return InferenceResult.FromError(error, telemetry);
                    }

                    OpenAIChatResponse chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseBody, _JsonOptions);
                    ApplyUsage(telemetry, chatResponse?.Usage);

                    if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                    {
                        OpenAIChoice choice = chatResponse.Choices[0];
                        List<AssistantModelToolCall> toolCalls = NormalizeToolCalls(choice.Message?.ToolCalls);
                        string content = choice.Message?.Content;
                        string finishReason = !String.IsNullOrWhiteSpace(choice.FinishReason)
                            ? choice.FinishReason
                            : (toolCalls.Count > 0 ? "tool_calls" : "stop");

                        _Logging.Debug(_Header + "OpenAI tool-capable response received (" + (content != null ? content.Length : 0) + " characters, " + toolCalls.Count + " tool call(s))");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(content, telemetry, finishReason, toolCalls);
                    }

                    _Logging.Warn(_Header + "OpenAI tool-capable response contained no choices");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoChoices", "OpenAI response contained no choices.");
                    return InferenceResult.FromError("OpenAI response contained no choices.", telemetry);
                }
            }
        }

        private protected async Task<InferenceResult> GenerateOllamaResponseWithToolsFromMessagesAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            string endpoint,
            string apiKey,
            IEnumerable<AssistantModelToolDefinition> tools,
            CancellationToken token)
        {
            string url = endpoint.TrimEnd('/') + "/api/chat";

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = BuildOllamaProviderMessages(messages),
                ["stream"] = false,
                ["options"] = new
                {
                    temperature = temperature,
                    top_p = topP,
                    num_predict = maxTokens
                }
            };

            List<AssistantModelToolDefinition> toolList = NormalizeToolDefinitions(tools);
            if (toolList.Count > 0)
                requestBody["tools"] = toolList;

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            AssistantPerformanceStage telemetry = StartTelemetry(InferenceProviderEnum.Ollama, model, endpoint, false);
            Stopwatch telemetrySw = Stopwatch.StartNew();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    MarkResponseHeaders(telemetry, telemetrySw, response);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "Ollama tool-capable API returned status " + (int)response.StatusCode + ": " + responseBody);
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
                        List<AssistantModelToolCall> toolCalls = NormalizeToolCalls(chatResponse.Message.ToolCalls);
                        string content = chatResponse.Message.Content;
                        string finishReason = !String.IsNullOrWhiteSpace(chatResponse.DoneReason)
                            ? chatResponse.DoneReason
                            : (toolCalls.Count > 0 ? "tool_calls" : "stop");

                        _Logging.Debug(_Header + "Ollama tool-capable response received (" + (content != null ? content.Length : 0) + " characters, " + toolCalls.Count + " tool call(s))");
                        FinishTelemetry(telemetry, telemetrySw, true);
                        return InferenceResult.FromSuccess(content, telemetry, finishReason, toolCalls);
                    }

                    _Logging.Warn(_Header + "Ollama tool-capable response contained no message");
                    FinishTelemetry(telemetry, telemetrySw, false, "NoMessage", "Ollama response contained no message.");
                    return InferenceResult.FromError("Ollama response contained no message.", telemetry);
                }
            }
        }

        private protected async Task<InferenceResult> GenerateGeminiResponseFromMessagesAsync(
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

        private protected object BuildGeminiRequestBody(
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

            Dictionary<string, object> requestBody = new Dictionary<string, object>
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

        private protected string ExtractGeminiText(GeminiResponse response)
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

        private protected static List<object> BuildProviderMessages(List<ChatCompletionMessage> messages)
        {
            List<object> msgObjects = new List<object>();
            if (messages == null) return msgObjects;

            foreach (ChatCompletionMessage msg in messages)
            {
                if (msg == null || String.IsNullOrWhiteSpace(msg.Role)) continue;

                Dictionary<string, object> message = new Dictionary<string, object>
                {
                    ["role"] = msg.Role
                };

                if (msg.Content != null) message["content"] = msg.Content;
                if (msg.ToolCalls != null && msg.ToolCalls.Count > 0) message["tool_calls"] = NormalizeToolCalls(msg.ToolCalls);
                if (!String.IsNullOrWhiteSpace(msg.ToolCallId)) message["tool_call_id"] = msg.ToolCallId.Trim();
                if (!String.IsNullOrWhiteSpace(msg.Name)) message["name"] = msg.Name.Trim();

                msgObjects.Add(message);
            }

            return msgObjects;
        }

        private protected static List<object> BuildOllamaProviderMessages(List<ChatCompletionMessage> messages)
        {
            List<object> msgObjects = new List<object>();
            if (messages == null) return msgObjects;

            foreach (ChatCompletionMessage msg in messages)
            {
                if (msg == null || String.IsNullOrWhiteSpace(msg.Role)) continue;

                Dictionary<string, object> message = new Dictionary<string, object>
                {
                    ["role"] = msg.Role
                };

                if (msg.Content != null) message["content"] = msg.Content;

                if (String.Equals(msg.Role, "tool", StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.IsNullOrWhiteSpace(msg.Name))
                        message["tool_name"] = msg.Name.Trim();

                    msgObjects.Add(message);
                    continue;
                }

                if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                    message["tool_calls"] = BuildOllamaToolCalls(msg.ToolCalls);

                msgObjects.Add(message);
            }

            return msgObjects;
        }

        private protected static List<object> BuildOllamaToolCalls(IEnumerable<AssistantModelToolCall> toolCalls)
        {
            List<object> ollamaCalls = new List<object>();
            int index = 0;

            foreach (AssistantModelToolCall call in NormalizeToolCalls(toolCalls))
            {
                Dictionary<string, object> function = new Dictionary<string, object>
                {
                    ["index"] = index,
                    ["name"] = call.Function.Name,
                    ["arguments"] = ParseToolArguments(call.Function.Arguments)
                };

                ollamaCalls.Add(new Dictionary<string, object>
                {
                    ["type"] = String.IsNullOrWhiteSpace(call.Type) ? "function" : call.Type.Trim(),
                    ["function"] = function
                });

                index++;
            }

            return ollamaCalls;
        }

        private protected static object ParseToolArguments(string arguments)
        {
            string json = String.IsNullOrWhiteSpace(arguments) ? "{}" : arguments.Trim();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    return document.RootElement.Clone();
                }
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        private protected static List<AssistantModelToolDefinition> NormalizeToolDefinitions(IEnumerable<AssistantModelToolDefinition> tools)
        {
            if (tools == null) return new List<AssistantModelToolDefinition>();

            return tools
                .Where(tool => tool != null
                    && tool.Function != null
                    && !String.IsNullOrWhiteSpace(tool.Function.Name))
                .Select(tool =>
                {
                    tool.Type = String.IsNullOrWhiteSpace(tool.Type) ? "function" : tool.Type.Trim();
                    tool.Function.Name = tool.Function.Name.Trim();
                    return tool;
                })
                .ToList();
        }

        private protected static List<AssistantModelToolCall> NormalizeToolCalls(IEnumerable<AssistantModelToolCall> toolCalls)
        {
            if (toolCalls == null) return new List<AssistantModelToolCall>();

            return toolCalls
                .Where(call => call != null
                    && call.Function != null
                    && !String.IsNullOrWhiteSpace(call.Function.Name))
                .Select(call =>
                {
                    call.Type = String.IsNullOrWhiteSpace(call.Type) ? "function" : call.Type.Trim();
                    call.Function.Name = call.Function.Name.Trim();
                    call.Function.Arguments = String.IsNullOrWhiteSpace(call.Function.Arguments)
                        ? "{}"
                        : call.Function.Arguments.Trim();
                    return call;
                })
                .ToList();
        }

        #endregion
    }
}
