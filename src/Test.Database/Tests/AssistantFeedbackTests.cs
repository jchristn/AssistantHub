namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class AssistantFeedbackTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- AssistantFeedback Tests ---");

            UserMaster user = await driver.User.CreateAsync(new UserMaster { Email = "fb-owner@example.com" }, token);
            Assistant asst = await driver.Assistant.CreateAsync(new Assistant { UserId = user.Id, Name = "Feedback Test Asst" }, token);
            string assistantId = asst.Id;
            string createdId = null;

            await runner.RunTestAsync("AssistantFeedback.Create_ThumbsUp", async ct =>
            {
                AssistantFeedback fb = new AssistantFeedback
                {
                    AssistantId = assistantId,
                    UserMessage = "What is the weather?",
                    AssistantResponse = "I don't have access to weather data.",
                    Rating = FeedbackRatingEnum.ThumbsUp,
                    FeedbackText = "Honest answer, appreciated!",
                    MessageHistory = "[{\"role\":\"user\",\"content\":\"What is the weather?\"}]"
                };

                AssistantFeedback created = await driver.AssistantFeedback.CreateAsync(fb, ct);
                AssertHelper.IsNotNull(created, "created feedback");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "afb_", "Id prefix");
                AssertHelper.AreEqual(assistantId, created.AssistantId, "AssistantId");
                AssertHelper.AreEqual("What is the weather?", created.UserMessage, "UserMessage");
                AssertHelper.AreEqual("I don't have access to weather data.", created.AssistantResponse, "AssistantResponse");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, created.Rating, "Rating");
                AssertHelper.AreEqual("Honest answer, appreciated!", created.FeedbackText, "FeedbackText");
                AssertHelper.AreEqual("[{\"role\":\"user\",\"content\":\"What is the weather?\"}]", created.MessageHistory, "MessageHistory");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Create_ThumbsDown", async ct =>
            {
                AssistantFeedback fb = new AssistantFeedback
                {
                    AssistantId = assistantId,
                    UserMessage = "Tell me a joke",
                    AssistantResponse = "Error occurred",
                    Rating = FeedbackRatingEnum.ThumbsDown,
                    FeedbackText = "Bad response"
                };

                AssistantFeedback created = await driver.AssistantFeedback.CreateAsync(fb, ct);
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsDown, created.Rating, "Rating ThumbsDown");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Create_NullableFields", async ct =>
            {
                AssistantFeedback fb = new AssistantFeedback
                {
                    AssistantId = assistantId
                };

                AssistantFeedback created = await driver.AssistantFeedback.CreateAsync(fb, ct);
                AssertHelper.IsNull(created.UserMessage, "UserMessage null");
                AssertHelper.IsNull(created.AssistantResponse, "AssistantResponse null");
                AssertHelper.IsNull(created.FeedbackText, "FeedbackText null");
                AssertHelper.IsNull(created.MessageHistory, "MessageHistory null");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, created.Rating, "default Rating");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Read", async ct =>
            {
                AssistantFeedback read = await driver.AssistantFeedback.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read feedback");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(assistantId, read.AssistantId, "AssistantId");
                AssertHelper.AreEqual("What is the weather?", read.UserMessage, "UserMessage");
                AssertHelper.AreEqual("I don't have access to weather data.", read.AssistantResponse, "AssistantResponse");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, read.Rating, "Rating");
                AssertHelper.AreEqual("Honest answer, appreciated!", read.FeedbackText, "FeedbackText");
                AssertHelper.AreEqual("[{\"role\":\"user\",\"content\":\"What is the weather?\"}]", read.MessageHistory, "MessageHistory");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Read_NotFound", async ct =>
            {
                AssistantFeedback read = await driver.AssistantFeedback.ReadAsync("afb_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent feedback");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<AssistantFeedback> result = await driver.AssistantFeedback.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 3, "objects count");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<AssistantFeedback> r1 = await driver.AssistantFeedback.EnumerateAsync(q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<AssistantFeedback> r2 = await driver.AssistantFeedback.EnumerateAsync(q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different feedback items");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Enumerate_AssistantIdFilter", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    AssistantIdFilter = assistantId
                };
                EnumerationResult<AssistantFeedback> result = await driver.AssistantFeedback.EnumerateAsync(query, ct);
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 3, "filtered count");
                foreach (AssistantFeedback fb in result.Objects)
                    AssertHelper.AreEqual(assistantId, fb.AssistantId, "AssistantId filter");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.Delete", async ct =>
            {
                AssistantFeedback fb = await driver.AssistantFeedback.CreateAsync(new AssistantFeedback { AssistantId = assistantId }, ct);
                await driver.AssistantFeedback.DeleteAsync(fb.Id, ct);

                AssistantFeedback read = await driver.AssistantFeedback.ReadAsync(fb.Id, ct);
                AssertHelper.IsNull(read, "deleted feedback");
            }, token);

            await runner.RunTestAsync("AssistantFeedback.DeleteByAssistantId", async ct =>
            {
                Assistant tempAsst = await driver.Assistant.CreateAsync(new Assistant { UserId = user.Id, Name = "FB Delete Asst" }, ct);
                await driver.AssistantFeedback.CreateAsync(new AssistantFeedback { AssistantId = tempAsst.Id, FeedbackText = "FB1" }, ct);
                await driver.AssistantFeedback.CreateAsync(new AssistantFeedback { AssistantId = tempAsst.Id, FeedbackText = "FB2" }, ct);

                await driver.AssistantFeedback.DeleteByAssistantIdAsync(tempAsst.Id, ct);

                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, AssistantIdFilter = tempAsst.Id };
                EnumerationResult<AssistantFeedback> result = await driver.AssistantFeedback.EnumerateAsync(query, ct);
                AssertHelper.AreEqual(0, result.Objects.Count, "all feedback deleted by assistant id");
            }, token);
        }
    }
}
