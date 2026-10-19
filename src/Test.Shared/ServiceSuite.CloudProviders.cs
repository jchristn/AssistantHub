namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using Test.Shared;

    public partial class ServiceSuite
    {
        private async Task RunCloudProviderTestsAsync()
        {
            foreach (InferenceProviderEnum provider in new[] { InferenceProviderEnum.OllamaCloud, InferenceProviderEnum.xAI })
            {
                await ExecuteTestAsync("InferenceService: " + provider + " discovery, chat, streaming, and lifecycle", async () =>
                {
                    bool ollama = provider == InferenceProviderEnum.OllamaCloud;
                    string simulation = Environment.GetEnvironmentVariable("ASSISTANTHUB_PROVIDER_SIMULATION_URL");
                    string endpoint = simulation ?? "https://provider.test";
                    MockHttpMessageHandler handler = new MockHttpMessageHandler()
                        .When(ollama ? "/api/tags" : "/v1/models", HttpStatusCode.OK,
                            ollama ? "{\"models\":[{\"name\":\"cloud-model\"}]}" : "{\"data\":[{\"id\":\"cloud-model\"}]}")
                        .When(ollama ? "/api/chat" : "/v1/chat/completions", HttpStatusCode.OK,
                            ollama ? "{\"message\":{\"content\":\"Cloud answer\"},\"done\":true}" : "{\"choices\":[{\"message\":{\"content\":\"Cloud answer\"}}]}");
                    using HttpClient client = simulation == null ? handler.CreateClient() : new HttpClient();
                    InferenceService service = new InferenceService(new InferenceSettings
                    {
                        Provider = provider, Endpoint = endpoint, ApiKey = "cloud-test-key", DefaultModel = "cloud-model"
                    }, CreateSilentLogging(), client);
                    AssertHelper.IsFalse(service.IsPullSupported, "cloud pull disabled");
                    AssertHelper.IsFalse(service.IsDeleteSupported, "cloud delete disabled");
                    var models = await service.ListModelsAsync().ConfigureAwait(false);
                    AssertHelper.HasCount(models, 1, "authenticated model discovery");
                    AssertHelper.AreEqual("cloud-model", models[0].Name, "model name");
                    AssertHelper.IsFalse(models[0].PullSupported, "cloud model pull disabled");
                    var messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "Answer" } };
                    InferenceResult result = await service.GenerateResponseAsync(messages, "cloud-model", 64, 0.1, 1, provider, endpoint, "cloud-test-key").ConfigureAwait(false);
                    AssertHelper.IsTrue(result.Success, "cloud inference succeeds");
                    AssertHelper.AreEqual("Cloud answer", result.Content, "cloud response");
                    AssertHelper.AreEqual(provider.ToString(), result.Telemetry.Provider, "provider telemetry identity");
                    InferenceResult toolResult = await service.GenerateResponseWithToolsAsync(messages, "cloud-model", 64, 0.1, 1,
                        provider, endpoint, "cloud-test-key", CreateInferenceToolDefinitions()).ConfigureAwait(false);
                    AssertHelper.IsTrue(toolResult.Success, "cloud tool-enabled inference succeeds");
                    AssertHelper.AreEqual(provider.ToString(), toolResult.Telemetry.Provider, "tool provider telemetry identity");
                    if (simulation == null)
                    {
                        AssertHelper.AreEqual("Bearer cloud-test-key", handler.Requests[0].Headers.Authorization.ToString(), "discovery auth");
                    }
                    else
                    {
                        StringBuilder content = new StringBuilder();
                        string streamError = null;
                        await service.GenerateResponseStreamingAsync(messages, "cloud-model", 64, 0.1, 1, provider, endpoint, "cloud-test-key",
                            delta => { content.Append(delta); return Task.CompletedTask; },
                            complete => Task.CompletedTask,
                            error => { streamError = error; return Task.CompletedTask; }).ConfigureAwait(false);
                        AssertHelper.IsTrue(streamError == null, "stream succeeds: " + streamError);
                        AssertHelper.AreEqual("Cloud answer", content.ToString(), "stream content");
                    }
                });
            }
        }
    }
}
