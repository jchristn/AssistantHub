namespace Test.Models.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using Test.Common;

    public static class EnumTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("EnumTests");

            // DatabaseTypeEnum — 4 values parse from string
            await runner.RunTestAsync("Enum.DatabaseTypeEnum: all 4 values parse from string", async ct =>
            {
                AssertHelper.AreEqual(DatabaseTypeEnum.Sqlite, Enum.Parse<DatabaseTypeEnum>("Sqlite"), "Sqlite");
                AssertHelper.AreEqual(DatabaseTypeEnum.Postgresql, Enum.Parse<DatabaseTypeEnum>("Postgresql"), "Postgresql");
                AssertHelper.AreEqual(DatabaseTypeEnum.SqlServer, Enum.Parse<DatabaseTypeEnum>("SqlServer"), "SqlServer");
                AssertHelper.AreEqual(DatabaseTypeEnum.Mysql, Enum.Parse<DatabaseTypeEnum>("Mysql"), "Mysql");
            }, token);

            // DocumentStatusEnum — round-trip through JSON
            await runner.RunTestAsync("Enum.DocumentStatusEnum: all values round-trip through JSON", async ct =>
            {
                foreach (DocumentStatusEnum val in Enum.GetValues<DocumentStatusEnum>())
                {
                    string json = JsonSerializer.Serialize(val, _jsonOptions);
                    DocumentStatusEnum deserialized = JsonSerializer.Deserialize<DocumentStatusEnum>(json, _jsonOptions);
                    AssertHelper.AreEqual(val, deserialized, $"DocumentStatusEnum.{val}");
                }
                AssertHelper.AreEqual(12, Enum.GetValues<DocumentStatusEnum>().Length, "DocumentStatusEnum count");
            }, token);

            // InferenceProviderEnum — Ollama, OpenAI
            await runner.RunTestAsync("Enum.InferenceProviderEnum: Ollama and OpenAI", async ct =>
            {
                AssertHelper.AreEqual(InferenceProviderEnum.Ollama, Enum.Parse<InferenceProviderEnum>("Ollama"), "Ollama");
                AssertHelper.AreEqual(InferenceProviderEnum.OpenAI, Enum.Parse<InferenceProviderEnum>("OpenAI"), "OpenAI");
                AssertHelper.AreEqual(2, Enum.GetValues<InferenceProviderEnum>().Length, "count");
            }, token);

            // FeedbackRatingEnum — all values
            await runner.RunTestAsync("Enum.FeedbackRatingEnum: all values", async ct =>
            {
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, Enum.Parse<FeedbackRatingEnum>("ThumbsUp"), "ThumbsUp");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsDown, Enum.Parse<FeedbackRatingEnum>("ThumbsDown"), "ThumbsDown");
                AssertHelper.AreEqual(2, Enum.GetValues<FeedbackRatingEnum>().Length, "count");
            }, token);

            // ApiErrorEnum — all values exist and are distinct
            await runner.RunTestAsync("Enum.ApiErrorEnum: all values exist and are distinct", async ct =>
            {
                var values = Enum.GetValues<ApiErrorEnum>();
                AssertHelper.AreEqual(6, values.Length, "count");
                var set = new System.Collections.Generic.HashSet<int>();
                foreach (var v in values)
                {
                    AssertHelper.IsTrue(set.Add((int)v), $"ApiErrorEnum.{v} should be distinct");
                }
            }, token);

            // ScheduleIntervalEnum — all values
            await runner.RunTestAsync("Enum.ScheduleIntervalEnum: all values", async ct =>
            {
                AssertHelper.AreEqual(ScheduleIntervalEnum.OneTime, Enum.Parse<ScheduleIntervalEnum>("OneTime"), "OneTime");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Minutes, Enum.Parse<ScheduleIntervalEnum>("Minutes"), "Minutes");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Hours, Enum.Parse<ScheduleIntervalEnum>("Hours"), "Hours");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Days, Enum.Parse<ScheduleIntervalEnum>("Days"), "Days");
                AssertHelper.AreEqual(ScheduleIntervalEnum.Weeks, Enum.Parse<ScheduleIntervalEnum>("Weeks"), "Weeks");
                AssertHelper.AreEqual(5, Enum.GetValues<ScheduleIntervalEnum>().Length, "count");
            }, token);

            // CrawlOperationStateEnum — all states
            await runner.RunTestAsync("Enum.CrawlOperationStateEnum: all states", async ct =>
            {
                var values = Enum.GetValues<CrawlOperationStateEnum>();
                AssertHelper.AreEqual(8, values.Length, "count");
                foreach (var val in values)
                {
                    string json = JsonSerializer.Serialize(val, _jsonOptions);
                    CrawlOperationStateEnum deserialized = JsonSerializer.Deserialize<CrawlOperationStateEnum>(json, _jsonOptions);
                    AssertHelper.AreEqual(val, deserialized, $"CrawlOperationStateEnum.{val}");
                }
            }, token);

            // CrawlPlanStateEnum — all states
            await runner.RunTestAsync("Enum.CrawlPlanStateEnum: all states", async ct =>
            {
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, Enum.Parse<CrawlPlanStateEnum>("Stopped"), "Stopped");
                AssertHelper.AreEqual(CrawlPlanStateEnum.Running, Enum.Parse<CrawlPlanStateEnum>("Running"), "Running");
                AssertHelper.AreEqual(2, Enum.GetValues<CrawlPlanStateEnum>().Length, "count");
            }, token);

            // EnumerationOrderEnum — CreatedAscending, CreatedDescending
            await runner.RunTestAsync("Enum.EnumerationOrderEnum: CreatedAscending and CreatedDescending", async ct =>
            {
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedAscending, Enum.Parse<EnumerationOrderEnum>("CreatedAscending"), "Ascending");
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedDescending, Enum.Parse<EnumerationOrderEnum>("CreatedDescending"), "Descending");
                AssertHelper.AreEqual(2, Enum.GetValues<EnumerationOrderEnum>().Length, "count");
            }, token);

            // RepositoryTypeEnum — Web
            await runner.RunTestAsync("Enum.RepositoryTypeEnum: Web", async ct =>
            {
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, Enum.Parse<RepositoryTypeEnum>("Web"), "Web");
                AssertHelper.AreEqual(1, Enum.GetValues<RepositoryTypeEnum>().Length, "count");
            }, token);

            // WebAuthTypeEnum — None, Basic, ApiKey, BearerToken
            await runner.RunTestAsync("Enum.WebAuthTypeEnum: all values", async ct =>
            {
                AssertHelper.AreEqual(WebAuthTypeEnum.None, Enum.Parse<WebAuthTypeEnum>("None"), "None");
                AssertHelper.AreEqual(WebAuthTypeEnum.Basic, Enum.Parse<WebAuthTypeEnum>("Basic"), "Basic");
                AssertHelper.AreEqual(WebAuthTypeEnum.ApiKey, Enum.Parse<WebAuthTypeEnum>("ApiKey"), "ApiKey");
                AssertHelper.AreEqual(WebAuthTypeEnum.BearerToken, Enum.Parse<WebAuthTypeEnum>("BearerToken"), "BearerToken");
                AssertHelper.AreEqual(4, Enum.GetValues<WebAuthTypeEnum>().Length, "count");
            }, token);

            // SummarizationOrderEnum — all values
            await runner.RunTestAsync("Enum.SummarizationOrderEnum: all values", async ct =>
            {
                AssertHelper.AreEqual(SummarizationOrderEnum.BottomUp, Enum.Parse<SummarizationOrderEnum>("BottomUp"), "BottomUp");
                AssertHelper.AreEqual(SummarizationOrderEnum.TopDown, Enum.Parse<SummarizationOrderEnum>("TopDown"), "TopDown");
                AssertHelper.AreEqual(2, Enum.GetValues<SummarizationOrderEnum>().Length, "count");
            }, token);
        }
    }
}
