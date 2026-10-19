namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using Test.Shared;

    public partial class ServiceSuite
    {
        private async Task RunPartioProviderTestsAsync()
        {
            await ExecuteTestAsync("Partio credentials: reveal, old-server fallback, and denied/masked reads", async () =>
            {
                foreach (HttpStatusCode status in new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden })
                {
                    var service = new CredentialEndpointService(status, "{\"ApiKey\":\"plain\"}");
                    using var response = await PartioEndpointRuntime.ReadAsync(service, "cep_test");
                    AssertHelper.AreEqual(status == HttpStatusCode.NotFound ? 2 : 1, service.Paths.Count, "fallback only on 404");
                    AssertHelper.StringContains(service.Paths[0], "/reveal", "explicit reveal first");
                    AssertHelper.AreEqual(status == HttpStatusCode.NotFound ? HttpStatusCode.OK : status, response.StatusCode, "status preserved");
                }
                var masked = new CredentialEndpointService(HttpStatusCode.NotFound, "{\"ApiKey\":\"****last\"}");
                bool rejected = false;
                try { using var response = await PartioEndpointRuntime.ReadAsync(masked, "cep_test"); }
                catch (InvalidOperationException) { rejected = true; }
                AssertHelper.IsTrue(rejected, "masked credentials never become inference credentials");
            });
            await ExecuteTestAsync("Partio provider settings: async-flow isolation and no global fallback", async () =>
            {
                PartioEndpointRuntime.Remember(new PartioEndpointConfig { Id = "cep_a", ProviderSettings = new ProviderSettings { Auth = new EndpointAuthSettings { Mode = EndpointAuthModes.Header, HeaderName = "X-A" } } });
                PartioEndpointRuntime.Remember(new PartioEndpointConfig { Id = "cep_b" });
                async Task Check(string id, string expected)
                {
                    using var scope = PartioEndpointRuntime.Use(id);
                    await Task.Yield();
                    AssertHelper.IsTrue(PartioEndpointRuntime.IsManaged, "managed null settings stay managed");
                    AssertHelper.AreEqual(expected, PartioEndpointRuntime.Current?.Auth?.HeaderName, "flow-local settings");
                }
                await Task.WhenAll(Check("cep_a", "X-A"), Check("cep_b", null));
                AssertHelper.IsFalse(PartioEndpointRuntime.IsManaged, "scope restored");
            });

            await ExecuteTestAsync("Inference configuration: redact and preserve nested provider credentials", async () =>
            {
                var settings = new AssistantHubSettings();
                settings.Inference.ApiKey = "dummy-key";
                settings.Inference.ProviderSettings = new ProviderSettings
                {
                    Bedrock = new BedrockProviderSettings { SecretAccessKey = "dummy-secret", SessionToken = "dummy-token" },
                    VertexAI = new VertexAiProviderSettings { ServiceAccountJson = "dummy-json" }
                };
                var redacted = AssistantHub.Server.Services.ConfigurationSettingsRedactor.CreateRedactedCopy(settings);
                string json = JsonSerializer.Serialize(redacted.Inference);
                AssertHelper.IsFalse(json.Contains("dummy-"), "configuration API redacts nested secrets");
                AssistantHub.Server.Services.ConfigurationSettingsRedactor.PreserveRedactedExternalSearchSecrets(redacted, settings);
                AssertHelper.AreEqual("dummy-secret", redacted.Inference.ProviderSettings.Bedrock.SecretAccessKey, "redacted update preserves credential");
                AssertHelper.AreEqual("dummy-json", redacted.Inference.ProviderSettings.VertexAI.ServiceAccountJson, "redacted update preserves JSON credential");
            });

            string simulation = Environment.GetEnvironmentVariable("ASSISTANTHUB_PROVIDER_SIMULATION_URL");
            if (String.IsNullOrEmpty(simulation)) return; // opt-in real HTTP simulation; no provider credentials needed
            await ExecuteTestAsync("Partio health simulation: completion probe delegates authenticated snapshot", async () =>
            {
                var settings = new AssistantHubSettings();
                settings.Chunking.Endpoint = simulation;
                settings.Chunking.AccessKey = "cloud-test-key";
                var health = new AssistantHub.Server.Services.EndpointHealthCheckService(settings, CreateSilentLogging());
                try
                {
                    health.OnEndpointCreated("""{"Id":"cep_health","Model":"model","ApiFormat":"Bedrock","Active":true,"HealthCheckEnabled":true,"HealthCheckIntervalMs":10,"HealthCheckTimeoutMs":1000,"HealthyThreshold":5,"ProviderSettings":{"Bedrock":{"Region":"us-east-1","SecretAccessKey":"****last"}}}""", "completion");
                    DateTime deadline = DateTime.UtcNow.AddSeconds(3);
                    while (!health.IsHealthy("cep_health") && DateTime.UtcNow < deadline) await Task.Delay(10);
                    AssertHelper.IsTrue(health.IsHealthy("cep_health"), "Partio health accepted without applying thresholds twice");
                    AssertHelper.IsTrue(health.GetHealthState("cep_health").ConsecutiveSuccesses > 0, "probe executed");
                }
                finally { health.OnEndpointDeleted("cep_health"); }
            });
            foreach (var (provider, route) in new[] { (InferenceProviderEnum.AzureOpenAI, "azure"), (InferenceProviderEnum.Anthropic, "anthropic"), (InferenceProviderEnum.Cohere, "cohere"), (InferenceProviderEnum.Bedrock, "bedrock"), (InferenceProviderEnum.VertexAI, "vertex") })
            {
                await ExecuteTestAsync("Partio native simulation: " + provider + " conversation, tools, auth and streaming", async () =>
                {
                    string endpoint = simulation + "/native/" + route;
                    ProviderSettings settings = new()
                    {
                        AzureOpenAI = new AzureOpenAiProviderSettings { ApiVersion = "2024-10-21", AuthMode = AzureAuthModes.ApiKey },
                        Anthropic = new AnthropicProviderSettings { AnthropicVersion = "2023-06-01" },
                        Bedrock = new BedrockProviderSettings { Region = "us-east-1", CredentialSource = AwsCredentialSources.Static, AccessKeyId = "dummy-access", SecretAccessKey = "dummy-secret" },
                        VertexAI = new VertexAiProviderSettings { ProjectId = "test-project", Region = "us-central1", CredentialSource = GcpCredentialSources.AccessToken }
                    };
                    using HttpClient http = new();
                    var service = new InferenceService(new InferenceSettings { Provider = provider, Endpoint = endpoint, DefaultModel = "cloud-model", ApiKey = "cloud-test-key", ProviderSettings = settings }, CreateSilentLogging(), http);
                    var messages = new List<ChatCompletionMessage> { new() { Role = "system", Content = "Keep history" }, new() { Role = "user", Content = "First" }, new() { Role = "assistant", Content = "Earlier" }, new() { Role = "user", Content = "Answer" } };
                    var result = await service.GenerateResponseAsync(messages, "cloud-model", 64, 0.1, 1, provider, endpoint, "cloud-test-key");
                    AssertHelper.IsTrue(result.Success, "native response: " + result.ErrorMessage);
                    AssertHelper.AreEqual("Native answer", result.Content, "native content");
                    AssertHelper.AreEqual(provider.ToString(), result.Telemetry.Provider, "provider identity");
                    AssertHelper.AreEqual(200, result.Telemetry.HttpStatusCode, "HTTP status");
                    var tools = await service.GenerateResponseWithToolsAsync(messages, "cloud-model", 64, 0.1, 1, provider, endpoint, "cloud-test-key", CreateInferenceToolDefinitions());
                    AssertHelper.IsTrue(tools.Success, "native tool response: " + tools.ErrorMessage);
                    AssertHelper.HasCount(tools.ToolCalls, 1, "native tools parsed");
                    AssertHelper.AreEqual("collection_search", tools.ToolCalls[0].Function.Name, "tool name");
                    AssertHelper.StringContains(tools.ToolCalls[0].Function.Arguments, "alpha", "tool JSON");
                    if (provider == InferenceProviderEnum.VertexAI) AssertHelper.AreEqual("opaque-signature", tools.ToolCalls[0].ThoughtSignature, "opaque Gemini thought signature");
                    // Bedrock streams use AWS's binary event-stream protocol, rather than SSE.
                    if (provider == InferenceProviderEnum.Bedrock) return;
                    StringBuilder text = new(); string error = null;
                    await service.GenerateResponseStreamingAsync(messages, "cloud-model", 64, 0.1, 1, provider, endpoint, "cloud-test-key",
                        delta => { text.Append(delta); return Task.CompletedTask; }, complete => Task.CompletedTask,
                        failure => { error = failure; return Task.CompletedTask; });
                    AssertHelper.IsTrue(error == null, "native stream: " + error);
                    AssertHelper.AreEqual("Native answer", text.ToString(), "native stream content");
                });
            }
            foreach (string mode in new[] { EndpointAuthModes.None, EndpointAuthModes.Bearer, EndpointAuthModes.Header, EndpointAuthModes.QueryParameter, EndpointAuthModes.Basic })
            {
                await ExecuteTestAsync("Partio auth simulation: OpenAI " + mode + " discovery and chat", async () =>
                {
                    string endpoint = simulation + "/native/auth/" + mode;
                    var settings = new ProviderSettings { Auth = new EndpointAuthSettings { Mode = mode, HeaderName = "X-Endpoint-Key", QueryParameterName = "token", Username = "user" } };
                    using HttpClient http = new();
                    var service = new InferenceService(new InferenceSettings { Provider = InferenceProviderEnum.OpenAI, Endpoint = endpoint, DefaultModel = "cloud-model", ApiKey = "cloud-test-key", ProviderSettings = settings }, CreateSilentLogging(), http);
                    AssertHelper.HasCount(await service.ListModelsAsync(), 1, "discovery uses selected auth");
                    var messages = new List<ChatCompletionMessage> { new() { Role = "user", Content = "Earlier" }, new() { Role = "user", Content = "Answer" } };
                    var result = await service.GenerateResponseAsync(messages, "cloud-model", 64, 0.1, 1, InferenceProviderEnum.OpenAI, endpoint, "cloud-test-key");
                    AssertHelper.IsTrue(result.Success, "chat uses selected auth: " + result.ErrorMessage);
                });
            }
            await ExecuteTestAsync("Partio native simulation: upstream 429 remains retryable telemetry", async () =>
            {
                using HttpClient http = new();
                var service = new InferenceService(new InferenceSettings { Provider = InferenceProviderEnum.Anthropic, Endpoint = simulation + "/native/failure", ApiKey = "cloud-test-key" }, CreateSilentLogging(), http);
                var result = await service.GenerateResponseAsync(new List<ChatCompletionMessage> { new() { Role = "user", Content = "Answer" } }, "model", 32, 0, 1, InferenceProviderEnum.Anthropic, simulation + "/native/failure", "cloud-test-key");
                AssertHelper.IsFalse(result.Success, "failure retained");
                AssertHelper.AreEqual(429, result.Telemetry.HttpStatusCode, "retryable HTTP status retained");
            });
        }

        private sealed class CredentialEndpointService(HttpStatusCode revealStatus, string body) : IInferenceEndpointService
        {
            public List<string> Paths { get; } = new();
            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string requestBody = null, CancellationToken token = default)
            {
                Paths.Add(relativePathAndQuery);
                return Task.FromResult(new HttpResponseMessage(relativePathAndQuery.EndsWith("/reveal") ? revealStatus : HttpStatusCode.OK) { Content = new StringContent(body) });
            }
        }
    }
}
