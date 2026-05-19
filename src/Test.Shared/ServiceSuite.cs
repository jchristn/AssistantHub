namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AssistantHub.Server.Services;
    using Test.Shared;

    public class ServiceSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            await ExecuteTestAsync("SlackAssistantUtilities.BuildThreadId: deterministic for same input", async () =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                AssertHelper.AreEqual(a, b, "deterministic thread id");
                AssertHelper.StartsWith(a, "thr_slack_", "thread id prefix");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.BuildThreadId: changes when coordinates change", async () =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.568");
                AssertHelper.AreNotEqual(a, b, "thread id uniqueness");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes configured prefix", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, summarize this", "Hey bot,", null);
                AssertHelper.AreEqual("summarize this", result, "prefix removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes bot mention", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("<@U123> summarize this", null, "U123");
                AssertHelper.AreEqual("summarize this", result, "mention removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes prefix and mention together", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, <@U123> summarize this", "Hey bot,", "U123");
                AssertHelper.AreEqual("summarize this", result, "prefix and mention removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ShapeSlackText: flattens headers and links", async () =>
            {
                string input = "# Header\nSee [docs](https://example.com)";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.IsFalse(shaped.Contains("# Header"), "header markers removed");
                AssertHelper.StringContains(shaped, "Header", "header text retained");
                AssertHelper.StringContains(shaped, "<https://example.com|docs>", "link converted");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ShapeSlackText: preserves fenced code block content", async () =>
            {
                string input = "Before\n```csharp\n**literal**\n```\nAfter **bold**";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.StringContains(shaped, "```csharp", "code fence retained");
                AssertHelper.StringContains(shaped, "**literal**", "code block content retained");
                AssertHelper.StringContains(shaped, "After *bold*", "non-code bold flattened");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: returns single chunk for short message", async () =>
            {
                var chunks = SlackAssistantUtilities.ChunkSlackMessage("short message", 50);
                AssertHelper.HasCount(chunks, 1, "chunk count");
                AssertHelper.AreEqual("short message", chunks[0], "chunk content");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: splits long message on boundaries", async () =>
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
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: preserves combined content modulo trimming", async () =>
            {
                string input = "First paragraph\nSecond paragraph\nThird paragraph";
                var chunks = SlackAssistantUtilities.ChunkSlackMessage(input, 18);
                string recombined = String.Join(" ", chunks);
                AssertHelper.StringContains(recombined, "First paragraph", "first paragraph present");
                AssertHelper.StringContains(recombined, "Second paragraph", "second paragraph present");
                AssertHelper.StringContains(recombined, "Third paragraph", "third paragraph present");
            });

            return GetResults();
        }
    }
}
