namespace Test.Services.Tests
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Server.Services;
    using Test.Common;

    public static class SlackAssistantUtilitiesTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("SlackAssistantUtilitiesTests");

            await runner.RunTestAsync("SlackAssistantUtilities.BuildThreadId: deterministic for same input", async ct =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                AssertHelper.AreEqual(a, b, "deterministic thread id");
                AssertHelper.StartsWith(a, "thr_slack_", "thread id prefix");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.BuildThreadId: changes when coordinates change", async ct =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.568");
                AssertHelper.AreNotEqual(a, b, "thread id uniqueness");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes configured prefix", async ct =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, summarize this", "Hey bot,", null);
                AssertHelper.AreEqual("summarize this", result, "prefix removed");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes bot mention", async ct =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("<@U123> summarize this", null, "U123");
                AssertHelper.AreEqual("summarize this", result, "mention removed");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes prefix and mention together", async ct =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, <@U123> summarize this", "Hey bot,", "U123");
                AssertHelper.AreEqual("summarize this", result, "prefix and mention removed");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.ShapeSlackText: flattens headers and links", async ct =>
            {
                string input = "# Header\nSee [docs](https://example.com)";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.IsFalse(shaped.Contains("# Header"), "header markers removed");
                AssertHelper.StringContains(shaped, "Header", "header text retained");
                AssertHelper.StringContains(shaped, "<https://example.com|docs>", "link converted");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.ShapeSlackText: preserves fenced code block content", async ct =>
            {
                string input = "Before\n```csharp\n**literal**\n```\nAfter **bold**";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.StringContains(shaped, "```csharp", "code fence retained");
                AssertHelper.StringContains(shaped, "**literal**", "code block content retained");
                AssertHelper.StringContains(shaped, "After *bold*", "non-code bold flattened");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.ChunkSlackMessage: returns single chunk for short message", async ct =>
            {
                var chunks = SlackAssistantUtilities.ChunkSlackMessage("short message", 50);
                AssertHelper.HasCount(chunks, 1, "chunk count");
                AssertHelper.AreEqual("short message", chunks[0], "chunk content");
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.ChunkSlackMessage: splits long message on boundaries", async ct =>
            {
                string longText = String.Join("\n", new[]
                {
                    new string('a', 40),
                    new string('b', 40),
                    new string('c', 40)
                });

                var chunks = SlackAssistantUtilities.ChunkSlackMessage(longText, 60);
                AssertHelper.IsTrue(chunks.Count >= 2, "multiple chunks expected");
                foreach (string chunk in chunks)
                {
                    AssertHelper.IsTrue(chunk.Length <= 60, "chunk should respect max length");
                }
            }, token);

            await runner.RunTestAsync("SlackAssistantUtilities.ChunkSlackMessage: preserves combined content modulo trimming", async ct =>
            {
                string input = "First paragraph\nSecond paragraph\nThird paragraph";
                var chunks = SlackAssistantUtilities.ChunkSlackMessage(input, 18);
                string recombined = String.Join(" ", chunks);
                AssertHelper.StringContains(recombined, "First paragraph", "first paragraph present");
                AssertHelper.StringContains(recombined, "Second paragraph", "second paragraph present");
                AssertHelper.StringContains(recombined, "Third paragraph", "third paragraph present");
            }, token);
        }
    }
}
