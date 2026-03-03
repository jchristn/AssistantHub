namespace Test.Services.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using Test.Common;

    public static class InferenceServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("InferenceServiceTests");

            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;

            await runner.RunTestAsync("InferenceService: constructor throws on null settings", async ct =>
            {
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    new InferenceService(null, logging);
                }, "should throw on null settings");
            }, token);

            await runner.RunTestAsync("InferenceService: constructor throws on null logging", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    new InferenceService(settings, null);
                }, "should throw on null logging");
            }, token);

            await runner.RunTestAsync("InferenceService: IsPullSupported true for Ollama", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.Ollama;
                InferenceService svc = new InferenceService(settings, logging);
                AssertHelper.AreEqual(true, svc.IsPullSupported, "should be true for Ollama");
            }, token);

            await runner.RunTestAsync("InferenceService: IsPullSupported false for OpenAI", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.OpenAI;
                InferenceService svc = new InferenceService(settings, logging);
                AssertHelper.AreEqual(false, svc.IsPullSupported, "should be false for OpenAI");
            }, token);

            await runner.RunTestAsync("InferenceService: IsDeleteSupported true for Ollama", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.Ollama;
                InferenceService svc = new InferenceService(settings, logging);
                AssertHelper.AreEqual(true, svc.IsDeleteSupported, "should be true for Ollama");
            }, token);

            await runner.RunTestAsync("InferenceService: IsDeleteSupported false for OpenAI", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.OpenAI;
                InferenceService svc = new InferenceService(settings, logging);
                AssertHelper.AreEqual(false, svc.IsDeleteSupported, "should be false for OpenAI");
            }, token);

            await runner.RunTestAsync("InferenceService: PullModelAsync returns false for OpenAI", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.OpenAI;
                InferenceService svc = new InferenceService(settings, logging);
                bool result = await svc.PullModelAsync("some-model");
                AssertHelper.AreEqual(false, result, "should return false for unsupported provider");
            }, token);

            await runner.RunTestAsync("InferenceService: PullModelAsync throws on null model name", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.Ollama;
                InferenceService svc = new InferenceService(settings, logging);
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await svc.PullModelAsync(null);
                }, "should throw on null model name");
            }, token);

            await runner.RunTestAsync("InferenceService: ListModelsAsync returns empty list on unreachable endpoint", async ct =>
            {
                InferenceSettings settings = new InferenceSettings();
                settings.Provider = InferenceProviderEnum.Ollama;
                settings.Endpoint = "http://localhost:1"; // unreachable
                InferenceService svc = new InferenceService(settings, logging);
                var models = await svc.ListModelsAsync();
                AssertHelper.IsNotNull(models, "should return empty list, not null");
                AssertHelper.AreEqual(0, models.Count, "should be empty");
            }, token);
        }
    }
}
