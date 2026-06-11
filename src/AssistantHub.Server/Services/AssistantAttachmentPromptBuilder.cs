namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Builds short prompt context for chat turns scoped to selected assistant documents.
    /// </summary>
    public static class AssistantAttachmentPromptBuilder
    {
        private static readonly string[] _AttachmentReferencePhrases =
        {
            "this document",
            "that document",
            "the document",
            "these documents",
            "attached document",
            "attached documents",
            "selected document",
            "selected documents",
            "current document",
            "current file",
            "this file",
            "that file",
            "the file",
            "attached file",
            "attached files",
            "selected file",
            "selected files",
            "the attachment",
            "these attachments",
            "attached pdf",
            "this pdf"
        };

        /// <summary>
        /// Determine whether the user's message refers to selected/attached documents.
        /// </summary>
        /// <param name="message">Message text.</param>
        /// <returns>True if the message refers to attached document scope.</returns>
        public static bool MessageReferencesAttachedDocuments(string message)
        {
            if (String.IsNullOrWhiteSpace(message)) return false;
            string normalized = new string(message
                .Trim()
                .ToLowerInvariant()
                .Select(ch => Char.IsLetterOrDigit(ch) ? ch : ' ')
                .ToArray());
            normalized = " " + String.Join(" ", normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) + " ";
            return _AttachmentReferencePhrases.Any(phrase => normalized.Contains(" " + phrase + " ", StringComparison.Ordinal));
        }

        /// <summary>
        /// Build a retrieval gate prompt with selected-document context.
        /// </summary>
        /// <param name="template">Gate prompt template.</param>
        /// <param name="messages">Conversation messages.</param>
        /// <param name="lastUserMessage">Latest user message.</param>
        /// <param name="attachedDocuments">Selected document metadata.</param>
        /// <returns>Prompt text.</returns>
        public static string BuildRetrievalGatePrompt(
            string template,
            List<ChatCompletionMessage> messages,
            string lastUserMessage,
            IEnumerable<AssistantDocumentSelectionItem> attachedDocuments)
        {
            const int maxCharsPerMessage = 200;
            int recentCount = Math.Min(messages?.Count ?? 0, 6);
            int startIndex = (messages?.Count ?? 0) - recentCount;
            StringBuilder recentMessages = new StringBuilder();

            for (int i = startIndex; i < (messages?.Count ?? 0); i++)
            {
                ChatCompletionMessage message = messages[i];
                if (message == messages.LastOrDefault() && String.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                    continue;

                string content = message.Content ?? "";
                if (content.Length > maxCharsPerMessage)
                    content = content.Substring(0, maxCharsPerMessage) + "...";
                recentMessages.AppendLine(message.Role + ": " + content);
            }

            string prompt = (template ?? String.Empty)
                .Replace("{recentMessages}", recentMessages.ToString())
                .Replace("{lastUserMessage}", lastUserMessage ?? String.Empty);

            string context = BuildAttachmentContext(attachedDocuments);
            if (String.IsNullOrEmpty(context)) return prompt;

            prompt = prompt.Replace(
                "Rules:\n",
                "Rules:\n- RETRIEVE: Selected documents are attached and the user refers to this document, the attached file, the selected documents, or similar wording.\n",
                StringComparison.Ordinal);

            return prompt + "\n\nSelected documents for this turn:\n" + context;
        }

        /// <summary>
        /// Add selected-document context to query rewrite prompts.
        /// </summary>
        /// <param name="prompt">Prompt after normal template substitution.</param>
        /// <param name="attachedDocuments">Selected document metadata.</param>
        /// <returns>Prompt with optional attachment context.</returns>
        public static string AddQueryRewriteContext(string prompt, IEnumerable<AssistantDocumentSelectionItem> attachedDocuments)
        {
            string context = BuildAttachmentContext(attachedDocuments);
            if (String.IsNullOrEmpty(context)) return prompt;

            return (prompt ?? String.Empty)
                + "\n\nSelected documents for this turn:\n"
                + context
                + "\nUse this context only to clarify phrases like this document or attached file. Return query text only.";
        }

        /// <summary>
        /// Build a safe document-name context block without internal storage details.
        /// </summary>
        /// <param name="attachedDocuments">Selected document metadata.</param>
        /// <returns>Document context text.</returns>
        public static string BuildAttachmentContext(IEnumerable<AssistantDocumentSelectionItem> attachedDocuments)
        {
            if (attachedDocuments == null) return null;

            List<string> lines = attachedDocuments
                .Where(doc => doc != null)
                .Select((doc, idx) =>
                {
                    string name = !String.IsNullOrWhiteSpace(doc.Name) ? doc.Name : doc.OriginalFilename;
                    if (String.IsNullOrWhiteSpace(name)) name = "Selected document " + (idx + 1);

                    string filename = !String.IsNullOrWhiteSpace(doc.OriginalFilename) && !String.Equals(doc.OriginalFilename, name, StringComparison.Ordinal)
                        ? " (" + doc.OriginalFilename + ")"
                        : "";
                    string contentType = !String.IsNullOrWhiteSpace(doc.ContentType) ? " [" + doc.ContentType + "]" : "";
                    return "- " + name + filename + contentType;
                })
                .Where(line => !String.IsNullOrWhiteSpace(line))
                .Take(20)
                .ToList();

            return lines.Count > 0 ? String.Join("\n", lines) : null;
        }

        /// <summary>
        /// Defensively remove retrieved chunks outside selected document scope.
        /// </summary>
        /// <param name="chunks">Retrieved chunks.</param>
        /// <param name="attachedDocumentIds">Allowed attached document IDs.</param>
        /// <returns>Filtered chunks.</returns>
        public static List<RetrievalChunk> FilterChunksByAttachedDocuments(List<RetrievalChunk> chunks, ICollection<string> attachedDocumentIds)
        {
            if (chunks == null || chunks.Count < 1) return chunks ?? new List<RetrievalChunk>();
            if (attachedDocumentIds == null || attachedDocumentIds.Count < 1) return chunks;

            HashSet<string> allowed = new HashSet<string>(attachedDocumentIds.Where(id => !String.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (allowed.Count < 1) return chunks;

            List<RetrievalChunk> filtered = new List<RetrievalChunk>();
            foreach (RetrievalChunk chunk in chunks)
            {
                if (chunk == null || String.IsNullOrWhiteSpace(chunk.DocumentId) || !allowed.Contains(chunk.DocumentId)) continue;
                if (chunk.Neighbors != null)
                {
                    chunk.Neighbors = chunk.Neighbors
                        .Where(neighbor => neighbor != null && (String.IsNullOrWhiteSpace(neighbor.DocumentId) || allowed.Contains(neighbor.DocumentId)))
                        .ToList();
                }
                filtered.Add(chunk);
            }

            return filtered;
        }
    }
}
