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
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using PolyPrompt.Auth;
    using PolyPrompt.Clients;
    using P = PolyPrompt.Models;

    public partial class InferenceService
    {
        private ProviderSettings EffectiveProviderSettings => PartioEndpointRuntime.IsManaged ? PartioEndpointRuntime.Current : _Settings.ProviderSettings;
        private bool RequiresProviderAdapter(InferenceProviderEnum provider, bool withTools = false) => provider is InferenceProviderEnum.AzureOpenAI
            or InferenceProviderEnum.Anthropic or InferenceProviderEnum.Cohere or InferenceProviderEnum.Bedrock or InferenceProviderEnum.VertexAI
            || (provider == InferenceProviderEnum.Gemini && withTools)
            || ((provider is InferenceProviderEnum.Ollama or InferenceProviderEnum.OpenAI or InferenceProviderEnum.vLLM) && EffectiveProviderSettings?.Auth != null);

        private CompletionClientBase CreateProviderClient(InferenceProviderEnum provider, string endpoint, string key, string model, HttpClient http)
        {
            ProviderSettings settings = EffectiveProviderSettings ?? new ProviderSettings();
            switch (provider)
            {
                case InferenceProviderEnum.AzureOpenAI:
                    return settings.AzureOpenAI?.AuthMode == AzureAuthModes.BearerToken
                        ? new AzureOpenAiCompletionClient(endpoint, model, new StaticTokenCredential(key), settings.AzureOpenAI.ApiVersion, _Logging, http)
                        : new AzureOpenAiCompletionClient(endpoint, model, key, settings.AzureOpenAI?.ApiVersion, _Logging, http);
                case InferenceProviderEnum.Anthropic:
                    AnthropicCompletionClient anthropic = new(endpoint, key, _Logging, http);
                    if (!String.IsNullOrEmpty(settings.Anthropic?.AnthropicVersion)) anthropic.AnthropicVersion = settings.Anthropic.AnthropicVersion;
                    if (!String.IsNullOrEmpty(settings.Anthropic?.WorkspaceId)) anthropic.WorkspaceId = settings.Anthropic.WorkspaceId;
                    return anthropic;
                case InferenceProviderEnum.Cohere: return new CohereCompletionClient(endpoint, key, _Logging, http);
                case InferenceProviderEnum.Bedrock:
                    BedrockProviderSettings aws = settings.Bedrock ?? throw new ArgumentException("Bedrock requires ProviderSettings.Bedrock.");
                    return new BedrockCompletionClient(ProviderCredentialCache.GetAwsCredential(aws), aws.Region, endpoint, _Logging, http);
                case InferenceProviderEnum.VertexAI:
                    VertexAiProviderSettings gcp = settings.VertexAI ?? throw new ArgumentException("Vertex AI requires ProviderSettings.VertexAI.");
                    return new VertexAiCompletionClient(gcp.ProjectId, gcp.Region, ProviderCredentialCache.GetVertexCredential(gcp, key), endpoint, _Logging, http);
                case InferenceProviderEnum.Gemini: return new GeminiCompletionClient(endpoint, key, _Logging, http);
                case InferenceProviderEnum.Ollama: return new OllamaCompletionClient(endpoint, key, _Logging, http);
                default: return new OpenAiCompletionClient(endpoint, key, _Logging, http);
            }
        }

        private P.ToolChatRequest AdapterRequest(List<ChatCompletionMessage> messages, string model, int maxTokens, double temperature, double topP,
            IEnumerable<AssistantModelToolDefinition> tools, string choice) => new()
        {
            Options = new P.CompletionOptions { Model = model, MaxTokens = maxTokens, Temperature = temperature, TopP = topP },
            ToolChoice = choice,
            Messages = messages.Select(message => new P.ChatMessage
            {
                Role = message.Role, Content = message.Content, ToolCallId = message.ToolCallId, ToolName = message.Name,
                ToolCalls = message.ToolCalls?.Select(call => new P.ToolCall { Id = call.Id, Name = call.Function.Name, ArgumentsJson = call.Function.Arguments, ThoughtSignature = call.ThoughtSignature }).ToList() ?? new()
            }).ToList(),
            Tools = tools?.Select(tool => new P.ToolDefinition
            {
                Name = tool.Function.Name, Description = tool.Function.Description,
                Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(tool.Function.Parameters)) ?? new()
            }).ToList() ?? new()
        };

        private async Task<InferenceResult> GenerateAdapterAsync(List<ChatCompletionMessage> messages, string model, int maxTokens, double temperature, double topP,
            InferenceProviderEnum provider, string endpoint, string key, CancellationToken token, IEnumerable<AssistantModelToolDefinition> tools = null, string choice = "auto")
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(EffectiveRequestTimeoutMs);
            using HttpClient http = AdapterTransport(provider, key, out string clientKey);
            using CompletionClientBase client = CreateProviderClient(provider, endpoint, clientKey, model, http);
            AssistantPerformanceStage telemetry = StartTelemetry(provider, model, endpoint, false);
            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            P.ToolChatResponse response = await client.ToolChatAsync(AdapterRequest(messages, model, maxTokens, temperature, topP, tools, choice), timeout.Token).ConfigureAwait(false);
            FinishTelemetry(telemetry, elapsed, response.Success, response.Success ? null : "UpstreamError", response.Error);
            telemetry.HttpStatusCode = response.StatusCode;
            telemetry.Success = response.Success;
            telemetry.Tokens.Input = response.Usage?.PromptTokens;
            telemetry.Tokens.Output = response.Usage?.CompletionTokens;
            telemetry.Tokens.Total = response.Usage?.TotalTokens;
            telemetry.Tokens.Reasoning = response.Usage?.ReasoningTokens;
            return new InferenceResult
            {
                Success = response.Success, Content = response.Text, Thinking = response.Reasoning, FinishReason = response.FinishReason,
                ErrorMessage = response.Error, Telemetry = telemetry,
                ToolCalls = response.ToolCalls.Select(call => new AssistantModelToolCall
                { Id = call.Id, ThoughtSignature = call.ThoughtSignature, Function = new AssistantModelToolFunctionCall { Name = call.Name, Arguments = call.ArgumentsJson } }).ToList()
            };
        }

        private async Task GenerateAdapterStreamingAsync(List<ChatCompletionMessage> messages, string model, int maxTokens, double temperature, double topP,
            InferenceProviderEnum provider, string endpoint, string key, Func<string, Task> onDelta, Func<string, Task> onComplete, Func<string, Task> onError,
            Action onConnected, Action<AssistantPerformanceStage> onTelemetry, CancellationToken token, Func<string, Task> onThinking)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(EffectiveRequestTimeoutMs);
            using HttpClient http = AdapterTransport(provider, key, out string clientKey);
            using CompletionClientBase client = CreateProviderClient(provider, endpoint, clientKey, model, http);
            AssistantPerformanceStage telemetry = StartTelemetry(provider, model, endpoint, true);
            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            P.ToolChatStreamingResponse response = await client.ToolChatStreamingAsync(AdapterRequest(messages, model, maxTokens, temperature, topP, null, "auto"), timeout.Token).ConfigureAwait(false);
            onConnected?.Invoke();
            telemetry.HttpStatusCode = response.StatusCode;
            if (!response.Success)
            {
                FinishTelemetry(telemetry, elapsed, false, "UpstreamError", response.Error);
                onTelemetry?.Invoke(telemetry);
                await onError(response.Error ?? "Provider streaming request failed."); return;
            }
            StringBuilder content = new();
            await foreach (P.ToolChatStreamingChunk chunk in response.Chunks.WithCancellation(timeout.Token))
            {
                if (!String.IsNullOrEmpty(chunk.Text)) { MarkFirstToken(telemetry, elapsed); content.Append(chunk.Text); await onDelta(chunk.Text); }
                if (onThinking != null && !String.IsNullOrEmpty(chunk.ReasoningText)) await onThinking(chunk.ReasoningText);
            }
            FinishTelemetry(telemetry, elapsed, response.Success, response.Success ? null : "UpstreamError", response.Error);
            telemetry.HttpStatusCode = response.StatusCode;
            telemetry.Success = response.Success;
            telemetry.Tokens.Input = response.Usage?.PromptTokens;
            telemetry.Tokens.Output = response.Usage?.CompletionTokens;
            telemetry.Tokens.Total = response.Usage?.TotalTokens;
            onTelemetry?.Invoke(telemetry);
            if (response.Success) await onComplete(content.ToString());
            else await onError(response.Error ?? "Provider stream failed.");
        }

        private HttpClient AdapterTransport(InferenceProviderEnum provider, string key, out string clientKey)
        {
            EndpointAuthSettings auth = (provider is InferenceProviderEnum.Ollama or InferenceProviderEnum.OpenAI or InferenceProviderEnum.vLLM) ? EffectiveProviderSettings?.Auth : null;
            clientKey = auth != null && auth.Mode != EndpointAuthModes.Bearer ? null : key;
            return new HttpClient(new ProviderTransport(_HttpClient, auth, key)) { Timeout = Timeout.InfiniteTimeSpan };
        }

        // Clone once when bridging an injected HttpClient: a request cannot be sent by two clients.
        private sealed class ProviderTransport(HttpClient inner, EndpointAuthSettings auth, string key) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                using HttpRequestMessage forwarded = new(request.Method, request.RequestUri);
                foreach (var header in request.Headers) forwarded.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (request.Content != null)
                {
                    forwarded.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(token));
                    foreach (var header in request.Content.Headers) forwarded.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                switch (auth?.Mode)
                {
                    case EndpointAuthModes.None: forwarded.Headers.Authorization = null; break;
                    case EndpointAuthModes.Header:
                        forwarded.Headers.TryAddWithoutValidation(auth.HeaderName, key); break;
                    case EndpointAuthModes.Basic:
                        forwarded.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(auth.Username + ":" + key))); break;
                    case EndpointAuthModes.QueryParameter:
                        UriBuilder uri = new(forwarded.RequestUri);
                        uri.Query = uri.Query.TrimStart('?') + (String.IsNullOrEmpty(uri.Query) ? "" : "&") + Uri.EscapeDataString(auth.QueryParameterName) + "=" + Uri.EscapeDataString(key);
                        forwarded.RequestUri = uri.Uri; break;
                }
                return await inner.SendAsync(forwarded, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            }
        }
    }
}
