namespace Test.Models.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using Test.Common;

    public static class ApiContractModelTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("ApiContractModelTests");

            // ChatCompletionRequest
            await runner.RunTestAsync("ApiContract.ChatCompletionRequest: deserialization", async ct =>
            {
                string json = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}";
                var req = JsonSerializer.Deserialize<ChatCompletionRequest>(json, _jsonOptions);
                AssertHelper.IsNotNull(req, "deserialized request");
                AssertHelper.AreEqual("gpt-4", req.Model, "Model");
                AssertHelper.IsNotNull(req.Messages, "Messages");
                AssertHelper.IsTrue(req.Messages.Count > 0, "Messages has items");
            }, token);

            // ChatCompletionResponse / Choice / Usage — OpenAI-compatible structure
            await runner.RunTestAsync("ApiContract.ChatCompletionResponse: structure", async ct =>
            {
                var resp = new ChatCompletionResponse
                {
                    Id = "chatcmpl-test",
                    Object = "chat.completion",
                    Model = "gpt-4",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessage { Role = "assistant", Content = "Hello!" },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 10,
                        CompletionTokens = 5,
                        TotalTokens = 15
                    }
                };

                string json = JsonSerializer.Serialize(resp);
                var d = JsonSerializer.Deserialize<ChatCompletionResponse>(json, _jsonOptions);
                AssertHelper.AreEqual("chatcmpl-test", d.Id, "Id");
                AssertHelper.AreEqual("chat.completion", d.Object, "Object");
                AssertHelper.AreEqual(1, d.Choices.Count, "Choices count");
                AssertHelper.AreEqual("Hello!", d.Choices[0].Message.Content, "Choice message");
                AssertHelper.IsNotNull(d.Usage, "Usage");
                AssertHelper.AreEqual(15, d.Usage.TotalTokens, "TotalTokens");
            }, token);

            // AuthenticateRequest / AuthenticateResult
            await runner.RunTestAsync("ApiContract.AuthenticateRequest: round-trip", async ct =>
            {
                var req = new AuthenticateRequest { Email = "test@example.com", Password = "pass123" };
                string json = JsonSerializer.Serialize(req, _jsonOptions);
                var d = JsonSerializer.Deserialize<AuthenticateRequest>(json, _jsonOptions);
                AssertHelper.AreEqual("test@example.com", d.Email, "Email");
                AssertHelper.AreEqual("pass123", d.Password, "Password");
            }, token);

            await runner.RunTestAsync("ApiContract.AuthenticateResult: round-trip", async ct =>
            {
                var res = new AuthenticateResult
                {
                    Success = true,
                    TenantId = "ten_test",
                    IsGlobalAdmin = true,
                    IsTenantAdmin = false
                };
                string json = JsonSerializer.Serialize(res, _jsonOptions);
                var d = JsonSerializer.Deserialize<AuthenticateResult>(json, _jsonOptions);
                AssertHelper.AreEqual(true, d.Success, "Success");
                AssertHelper.AreEqual("ten_test", d.TenantId, "TenantId");
                AssertHelper.AreEqual(true, d.IsGlobalAdmin, "IsGlobalAdmin");
            }, token);

            // ApiErrorResponse
            await runner.RunTestAsync("ApiContract.ApiErrorResponse: error enum and message", async ct =>
            {
                var err = new ApiErrorResponse(ApiErrorEnum.NotFound);
                AssertHelper.AreEqual(ApiErrorEnum.NotFound, err.Error, "Error enum");
                AssertHelper.AreEqual(404, err.StatusCode, "StatusCode");
                AssertHelper.StringContains(err.Message, "not found", "Message contains 'not found'");

                string json = JsonSerializer.Serialize(err, _jsonOptions);
                AssertHelper.IsTrue(json.Contains("NotFound") || json.Contains("notFound"), "error enum in JSON");
            }, token);

            // EnumerationQuery
            await runner.RunTestAsync("ApiContract.EnumerationQuery: default MaxResults and ordering", async ct =>
            {
                var q = new EnumerationQuery();
                AssertHelper.AreEqual(100, q.MaxResults, "default MaxResults");
                AssertHelper.AreEqual(EnumerationOrderEnum.CreatedDescending, q.Ordering, "default Ordering");
                AssertHelper.IsNull(q.ContinuationToken, "default ContinuationToken");
            }, token);

            await runner.RunTestAsync("ApiContract.EnumerationQuery: MaxResults validation", async ct =>
            {
                bool threw = false;
                try { var q = new EnumerationQuery(); q.MaxResults = 0; }
                catch (ArgumentException) { threw = true; }
                AssertHelper.IsTrue(threw, "MaxResults = 0 should throw");

                threw = false;
                try { var q = new EnumerationQuery(); q.MaxResults = 1001; }
                catch (ArgumentException) { threw = true; }
                AssertHelper.IsTrue(threw, "MaxResults = 1001 should throw");
            }, token);

            // EnumerationResult
            await runner.RunTestAsync("ApiContract.EnumerationResult: structure", async ct =>
            {
                var r = new EnumerationResult<string>();
                AssertHelper.AreEqual(true, r.EndOfResults, "default EndOfResults");
                AssertHelper.IsNull(r.ContinuationToken, "default ContinuationToken");
                AssertHelper.AreEqual(0L, r.TotalRecords, "default TotalRecords");
                AssertHelper.AreEqual(0L, r.RecordsRemaining, "default RecordsRemaining");
                AssertHelper.IsNotNull(r.Objects, "Objects not null");
                AssertHelper.AreEqual(0, r.Objects.Count, "Objects empty");
            }, token);
        }
    }
}
