namespace Test.Models.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using Test.Common;

    public static class SettingsModelTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("SettingsModelTests");

            await runner.RunTestAsync("Settings.AssistantHubSettings: loads from JSON with defaults", async ct =>
            {
                var settings = new AssistantHubSettings();
                AssertHelper.IsNotNull(settings, "settings object");
            }, token);

            await runner.RunTestAsync("Settings.DatabaseSettings: sensible defaults", async ct =>
            {
                var db = new DatabaseSettings();
                AssertHelper.IsNotNull(db, "DatabaseSettings");
            }, token);

            await runner.RunTestAsync("Settings.WebserverSettings: defaults", async ct =>
            {
                var ws = new WebserverSettings();
                AssertHelper.IsNotNull(ws, "WebserverSettings");
            }, token);

            await runner.RunTestAsync("Settings.S3Settings: defaults", async ct =>
            {
                var s3 = new S3Settings();
                AssertHelper.IsNotNull(s3, "S3Settings");
            }, token);

            await runner.RunTestAsync("Settings.InferenceSettings: defaults", async ct =>
            {
                var inf = new InferenceSettings();
                AssertHelper.IsNotNull(inf, "InferenceSettings");
            }, token);

            await runner.RunTestAsync("Settings.RecallDbSettings: defaults", async ct =>
            {
                var rdb = new RecallDbSettings();
                AssertHelper.IsNotNull(rdb, "RecallDbSettings");
            }, token);

            await runner.RunTestAsync("Settings.ChunkingSettings: defaults", async ct =>
            {
                var ch = new ChunkingSettings();
                AssertHelper.IsNotNull(ch, "ChunkingSettings");
            }, token);

            await runner.RunTestAsync("Settings.EmbeddingsSettings: defaults", async ct =>
            {
                var emb = new EmbeddingsSettings();
                AssertHelper.IsNotNull(emb, "EmbeddingsSettings");
            }, token);

            await runner.RunTestAsync("Settings.DocumentAtomSettings: defaults", async ct =>
            {
                var da = new DocumentAtomSettings();
                AssertHelper.IsNotNull(da, "DocumentAtomSettings");
            }, token);

            await runner.RunTestAsync("Settings.LoggingSettings: defaults", async ct =>
            {
                var log = new LoggingSettings();
                AssertHelper.IsNotNull(log, "LoggingSettings");
            }, token);

            await runner.RunTestAsync("Settings.CrawlSettings: defaults", async ct =>
            {
                var crawl = new CrawlSettings();
                AssertHelper.IsNotNull(crawl, "CrawlSettings");
            }, token);

            await runner.RunTestAsync("Settings.AssistantHubSettings: JSON round-trip", async ct =>
            {
                var settings = new AssistantHubSettings();
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                AssertHelper.IsTrue(json.Length > 10, "serialized JSON not empty");
                var d = JsonSerializer.Deserialize<AssistantHubSettings>(json, _jsonOptions);
                AssertHelper.IsNotNull(d, "deserialized settings");
            }, token);
        }
    }
}
