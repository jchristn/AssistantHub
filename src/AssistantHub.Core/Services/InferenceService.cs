namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
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
                            onDelta, onComplete, onError, onConnectionEstablished, token).ConfigureAwait(false);
                        break;

                    case InferenceProviderEnum.Ollama:
                        await GenerateOllamaStreamingAsync(
                            messages, effectiveModel, maxTokens, temperature, topP,
                            effectiveEndpoint, effectiveApiKey,
                            onDelta, onComplete, onError, onConnectionEstablished, token).ConfigureAwait(false);
                        break;

                    default:
                        await onError("Unsupported inference provider: " + provider.ToString()).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during streaming inference: " + e.Message);
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
                    sb.AppendLine("When your answer uses information from a source, cite it using bracket notation like [1], [2], etc.");
                    sb.AppendLine("You may cite multiple sources for a single claim like [1][3].");
                    sb.AppendLine("Only cite sources numbered [1] through [" + contextChunks.Count + "] listed below. Do not fabricate citations.");
                    sb.AppendLine("Do not escape the brackets with backslashes. Write [1], not \\[1\\].");
                    sb.AppendLine("Ignore any [N] references from earlier messages in this conversation; only cite from the current source list.");
                    sb.AppendLine();
                    sb.AppendLine("Sources:");

                    for (int i = 0; i < contextChunks.Count; i++)
                    {
                        sb.AppendLine();
                        sb.AppendLine("[" + (i + 1) + "] " + chunkLabels[i]);
                        sb.AppendLine(contextChunks[i]);
                    }
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
            string url = endpoint.TrimEnd('/') + "/chat/completions";

            List<object> messages = new List<object>();
            messages.Add(new { role = "system", content = systemMessage });
            messages.Add(new { role = "user", content = userMessage });

            object requestBody = new
            {
                model = model,
                messages = messages,
                max_tokens = maxTokens,
                temperature = temperature,
                top_p = topP
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(
                        _Header +
                        "OpenAI API returned status " + (int)response.StatusCode + Environment.NewLine +
                        "| URL           : " + url + Environment.NewLine +
                        "| Bearer token  : " + apiKey + Environment.NewLine +
                        "| Response body : " + Environment.NewLine + responseBody);
                    return InferenceResult.FromError("OpenAI API returned " + (int)response.StatusCode);
                }

                OpenAIChatResponse chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseBody, _JsonOptions);

                if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                {
                    string content = chatResponse.Choices[0].Message?.Content;
                    _Logging.Debug(_Header + "OpenAI response received (" + (content != null ? content.Length : 0) + " characters)");
                    return InferenceResult.FromSuccess(content);
                }

                _Logging.Warn(_Header + "OpenAI response contained no choices");
                return InferenceResult.FromError("OpenAI response contained no choices.");
            }
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
            string url = endpoint.TrimEnd('/') + "/api/chat";

            List<object> messages = new List<object>();
            messages.Add(new { role = "system", content = systemMessage });
            messages.Add(new { role = "user", content = userMessage });

            object requestBody = new
            {
                model = model,
                messages = messages,
                stream = false,
                options = new
                {
                    temperature = temperature,
                    top_p = topP,
                    num_predict = maxTokens
                }
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(
                        _Header + 
                        "Ollama API returned status " + (int)response.StatusCode + Environment.NewLine + 
                        "| URL           : " + url + Environment.NewLine +
                        "| Bearer token  : " + apiKey + Environment.NewLine + 
                        "| Response body : " + Environment.NewLine + responseBody);
                    return InferenceResult.FromError("Ollama API returned " + (int)response.StatusCode);
                }

                OllamaChatResponse chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, _JsonOptions);

                if (chatResponse?.Message != null)
                {
                    string content = chatResponse.Message.Content;
                    _Logging.Debug(_Header + "Ollama response received (" + (content != null ? content.Length : 0) + " characters)");
                    return InferenceResult.FromSuccess(content);
                }

                _Logging.Warn(_Header + "Ollama response contained no message");
                return InferenceResult.FromError("Ollama response contained no message.");
            }
        }

        private async Task<List<InferenceModel>> ListOllamaModelsAsync()
        {
            string url = _Settings.Endpoint.TrimEnd('/') + "/api/tags";

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
            string url = _Settings.Endpoint.TrimEnd('/') + "/models";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (!String.IsNullOrEmpty(_Settings.ApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _Settings.ApiKey);
                }

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
            string url = endpoint.TrimEnd('/') + "/chat/completions";

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

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(
                        _Header +
                        "OpenAI API returned status " + (int)response.StatusCode + Environment.NewLine +
                        "| URL           : " + url + Environment.NewLine +
                        "| Bearer token  : " + apiKey + Environment.NewLine +
                        "| Response body : " + Environment.NewLine + responseBody);
                    return InferenceResult.FromError("OpenAI API returned " + (int)response.StatusCode);
                }

                OpenAIChatResponse chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseBody, _JsonOptions);

                if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                {
                    string content = chatResponse.Choices[0].Message?.Content;
                    _Logging.Debug(_Header + "OpenAI response received (" + (content != null ? content.Length : 0) + " characters)");
                    return InferenceResult.FromSuccess(content);
                }

                _Logging.Warn(_Header + "OpenAI response contained no choices");
                return InferenceResult.FromError("OpenAI response contained no choices.");
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

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(
                        _Header +
                        "Ollama API returned status " + (int)response.StatusCode + Environment.NewLine +
                        "| URL           : " + url + Environment.NewLine +
                        "| Bearer token  : " + apiKey + Environment.NewLine +
                        "| Response body : " + Environment.NewLine + responseBody);
                    return InferenceResult.FromError("Ollama API returned " + (int)response.StatusCode);
                }

                OllamaChatResponse chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, _JsonOptions);

                if (chatResponse?.Message != null)
                {
                    string content = chatResponse.Message.Content;
                    _Logging.Debug(_Header + "Ollama response received (" + (content != null ? content.Length : 0) + " characters)");
                    return InferenceResult.FromSuccess(content);
                }

                _Logging.Warn(_Header + "Ollama response contained no message");
                return InferenceResult.FromError("Ollama response contained no message.");
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
            CancellationToken token)
        {
            string url = endpoint.TrimEnd('/') + "/chat/completions";

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
                stream = true
            };

            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            StringBuilder fullContent = new StringBuilder();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
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
                        await onError("OpenAI API returned " + (int)response.StatusCode + ": " + errorBody).ConfigureAwait(false);
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
                                    await onComplete(fullContent.ToString()).ConfigureAwait(false);
                                    return;
                                }

                                try
                                {
                                    using (JsonDocument doc = JsonDocument.Parse(data))
                                    {
                                        JsonElement root = doc.RootElement;
                                        if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                                        {
                                            JsonElement choice = choices[0];
                                            if (choice.TryGetProperty("delta", out JsonElement delta) &&
                                                delta.TryGetProperty("content", out JsonElement contentElement))
                                            {
                                                string deltaContent = contentElement.GetString();
                                                if (!String.IsNullOrEmpty(deltaContent))
                                                {
                                                    fullContent.Append(deltaContent);
                                                    await onDelta(deltaContent).ConfigureAwait(false);
                                                }
                                            }
                                        }
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

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
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
                        await onError("Ollama API returned " + (int)response.StatusCode + ": " + errorBody).ConfigureAwait(false);
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
                                using (JsonDocument doc = JsonDocument.Parse(line))
                                {
                                    JsonElement root = doc.RootElement;

                                    if (root.TryGetProperty("done", out JsonElement doneElement) && doneElement.GetBoolean())
                                    {
                                        await onComplete(fullContent.ToString()).ConfigureAwait(false);
                                        return;
                                    }

                                    if (root.TryGetProperty("message", out JsonElement messageElement) &&
                                        messageElement.TryGetProperty("content", out JsonElement contentElement))
                                    {
                                        string deltaContent = contentElement.GetString();
                                        if (!String.IsNullOrEmpty(deltaContent))
                                        {
                                            fullContent.Append(deltaContent);
                                            await onDelta(deltaContent).ConfigureAwait(false);
                                        }
                                    }
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

            await onComplete(fullContent.ToString()).ConfigureAwait(false);
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
