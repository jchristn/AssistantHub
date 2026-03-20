namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

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
    }
}
