namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Utility methods used by Slack assistant workers.
    /// </summary>
    public static class SlackAssistantUtilities
    {
        /// <summary>
        /// Build a deterministic AssistantHub thread identifier from Slack conversation coordinates.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="channelId">Slack channel identifier.</param>
        /// <param name="slackConversationTimestamp">Slack root thread timestamp or message timestamp.</param>
        /// <returns>Deterministic AssistantHub thread identifier.</returns>
        public static string BuildThreadId(string assistantId, string channelId, string slackConversationTimestamp)
        {
            string source = assistantId + "|" + channelId + "|" + slackConversationTimestamp;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
            StringBuilder sb = new StringBuilder("thr_slack_");
            for (int i = 0; i < 12; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Remove the configured prefix and bot mention from Slack text before sending it to the model.
        /// </summary>
        /// <param name="text">Incoming Slack text.</param>
        /// <param name="prefix">Configured prefix.</param>
        /// <param name="botUserId">Bot user identifier.</param>
        /// <returns>Normalized user message.</returns>
        public static string StripSlackTrigger(string text, string prefix, string botUserId)
        {
            string ret = text?.Trim() ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(prefix) && ret.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                ret = ret.Substring(prefix.Length).TrimStart();

            if (!String.IsNullOrEmpty(botUserId))
                ret = ret.Replace("<@" + botUserId + ">", "").Trim();

            return ret;
        }

        /// <summary>
        /// Shape canonical assistant text for Slack delivery.
        /// </summary>
        /// <param name="text">Canonical assistant response text.</param>
        /// <returns>Slack-safe transport text.</returns>
        public static string ShapeSlackText(string text)
        {
            if (String.IsNullOrEmpty(text)) return text;

            string ret = text.Replace("\r\n", "\n");
            ret = Regex.Replace(ret, @"^#{1,6}\s*", "", RegexOptions.Multiline);
            ret = Regex.Replace(ret, @"\[(.*?)\]\((.*?)\)", "<$2|$1>");

            List<string> lines = new List<string>();
            bool inCodeBlock = false;
            foreach (string line in ret.Split('\n'))
            {
                string current = line;
                if (current.TrimStart().StartsWith("```", StringComparison.Ordinal))
                    inCodeBlock = !inCodeBlock;

                if (!inCodeBlock)
                    current = current.Replace("**", "*");

                lines.Add(current);
            }

            return String.Join("\n", lines).Trim();
        }

        /// <summary>
        /// Chunk Slack transport text to fit within message size limits.
        /// </summary>
        /// <param name="text">Slack transport text.</param>
        /// <param name="maxLength">Maximum chunk length.</param>
        /// <returns>Chunked message parts.</returns>
        public static List<string> ChunkSlackMessage(string text, int maxLength = 3000)
        {
            List<string> chunks = new List<string>();
            if (String.IsNullOrWhiteSpace(text))
            {
                chunks.Add(text);
                return chunks;
            }

            string remaining = text.Trim();
            while (remaining.Length > maxLength)
            {
                int split = remaining.LastIndexOf('\n', maxLength);
                if (split < maxLength / 2)
                    split = remaining.LastIndexOf(' ', maxLength);
                if (split < maxLength / 2)
                    split = maxLength;

                chunks.Add(remaining.Substring(0, split).Trim());
                remaining = remaining.Substring(split).Trim();
            }

            if (!String.IsNullOrWhiteSpace(remaining))
                chunks.Add(remaining);

            return chunks;
        }

        /// <summary>
        /// Shape a safe tool-progress lifecycle event for Slack delivery.
        /// </summary>
        /// <param name="evt">Safe tool-progress event.</param>
        /// <returns>Slack-safe short status text, or null when the event should not be posted.</returns>
        public static string ShapeSlackToolProgressMessage(AssistantToolProgressEvent evt)
        {
            if (evt == null) return null;

            string label = !String.IsNullOrWhiteSpace(evt.DisplayLabel)
                ? evt.DisplayLabel.Trim()
                : BuildFallbackToolLabel(evt.ToolName);

            if (String.IsNullOrWhiteSpace(label))
                label = "assistant tool";

            bool started = String.Equals(evt.EventType, "assistant.tool_call.started", StringComparison.OrdinalIgnoreCase);
            bool completed = String.Equals(evt.EventType, "assistant.tool_call.completed", StringComparison.OrdinalIgnoreCase);
            bool failed = String.Equals(evt.EventType, "assistant.tool_call.failed", StringComparison.OrdinalIgnoreCase);
            bool denied = String.Equals(evt.EventType, "assistant.tool_call.denied", StringComparison.OrdinalIgnoreCase);

            if (started) return "Tool running: " + label + ".";

            if (completed)
            {
                string countSuffix = evt.ResultCount.HasValue
                    ? " (" + evt.ResultCount.Value.ToString() + " " + (evt.ResultCount.Value == 1 ? "result" : "results") + ")"
                    : "";
                return "Tool completed: " + label + countSuffix + ".";
            }

            if (failed) return "Tool failed: " + label + ". The assistant will continue if it can.";
            if (denied) return "Tool denied: " + label + ".";

            return null;
        }

        private static string BuildFallbackToolLabel(string toolName)
        {
            if (String.IsNullOrWhiteSpace(toolName)) return null;

            string normalized = toolName.Trim();
            normalized = normalized.Replace("_", " ").Replace("-", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized;
        }
    }
}
