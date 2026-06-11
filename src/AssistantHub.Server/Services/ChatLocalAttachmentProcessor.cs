namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Processes per-turn user-uploaded chat attachments into model-visible context.
    /// </summary>
    public static class ChatLocalAttachmentProcessor
    {
        private const int MaxFileBytes = 10 * 1024 * 1024;
        private const int MaxTotalBytes = 25 * 1024 * 1024;
        private const int MaxCharactersPerAttachment = 20000;
        private const int MaxTotalCharacters = 60000;

        /// <summary>
        /// Process local chat attachments.
        /// </summary>
        /// <param name="settings">Assistant settings.</param>
        /// <param name="attachments">Request attachments.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Attachment resolution.</returns>
        public static async Task<ChatLocalAttachmentResolution> ResolveAsync(
            AssistantSettings settings,
            List<ChatLocalAttachment> attachments,
            AssistantHubSettings appSettings,
            LoggingModule logging,
            CancellationToken token = default)
        {
            ChatLocalAttachmentResolution ret = new ChatLocalAttachmentResolution { Success = true };
            List<ChatLocalAttachment> normalized = NormalizeAttachments(attachments);
            if (normalized.Count < 1) return ret;

            if (settings == null || !settings.EnableDocumentAttachments)
            {
                return new ChatLocalAttachmentResolution
                {
                    Success = false,
                    StatusCode = 400,
                    ErrorMessage = "Document attachments are disabled for this assistant."
                };
            }

            if (normalized.Count > settings.DocumentAttachmentMaxCount)
            {
                return new ChatLocalAttachmentResolution
                {
                    Success = false,
                    StatusCode = 400,
                    ErrorMessage = "Too many local attachments. The assistant allows " + settings.DocumentAttachmentMaxCount + " attachment(s) per request."
                };
            }

            int totalBytes = 0;
            int totalCharacters = 0;
            DocumentAtomAtomizationService atomization = new DocumentAtomAtomizationService(appSettings.DocumentAtom, logging);

            int attachmentIndex = 0;
            foreach (ChatLocalAttachment attachment in normalized)
            {
                attachmentIndex++;
                token.ThrowIfCancellationRequested();
                string name = SanitizeName(attachment.Name);
                string contentType = String.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType.Trim();
                string text = attachment.Text;
                byte[] sourceBytes = null;
                int sizeBytes = 0;

                if (String.IsNullOrWhiteSpace(text))
                {
                    byte[] bytes;
                    try
                    {
                        bytes = DecodeBase64Content(attachment.Base64Content);
                    }
                    catch (FormatException)
                    {
                        return new ChatLocalAttachmentResolution
                        {
                            Success = false,
                            StatusCode = 400,
                            ErrorMessage = "Local attachment '" + name + "' contains invalid base64_content."
                        };
                    }

                    if (bytes == null || bytes.Length == 0)
                    {
                        return new ChatLocalAttachmentResolution
                        {
                            Success = false,
                            StatusCode = 400,
                            ErrorMessage = "Local attachment '" + name + "' is empty."
                        };
                    }

                    sizeBytes = bytes.Length;
                    sourceBytes = bytes;
                    if (sizeBytes > MaxFileBytes)
                    {
                        return new ChatLocalAttachmentResolution
                        {
                            Success = false,
                            StatusCode = 400,
                            ErrorMessage = "Local attachment '" + name + "' exceeds the " + FormatBytes(MaxFileBytes) + " per-file limit."
                        };
                    }

                    totalBytes += sizeBytes;
                    if (totalBytes > MaxTotalBytes)
                    {
                        return new ChatLocalAttachmentResolution
                        {
                            Success = false,
                            StatusCode = 400,
                            ErrorMessage = "Local attachments exceed the " + FormatBytes(MaxTotalBytes) + " per-request limit."
                        };
                    }

                    text = await ExtractTextAsync(atomization, name, contentType, bytes, token).ConfigureAwait(false);
                }
                else
                {
                    sourceBytes = Encoding.UTF8.GetBytes(text);
                    sizeBytes = sourceBytes.Length;
                }

                if (String.IsNullOrWhiteSpace(text))
                {
                    return new ChatLocalAttachmentResolution
                    {
                        Success = false,
                        StatusCode = 400,
                        ErrorMessage = "Local attachment '" + name + "' did not contain readable text."
                    };
                }

                bool truncated = false;
                string normalizedText = NormalizeText(text);
                if (normalizedText.Length > MaxCharactersPerAttachment)
                {
                    normalizedText = normalizedText.Substring(0, MaxCharactersPerAttachment);
                    truncated = true;
                }

                int remaining = MaxTotalCharacters - totalCharacters;
                if (remaining <= 0) break;
                if (normalizedText.Length > remaining)
                {
                    normalizedText = normalizedText.Substring(0, remaining);
                    truncated = true;
                }

                totalCharacters += normalizedText.Length;
                ret.Attachments.Add(new ChatLocalAttachmentContext
                {
                    AttachmentId = "local_attachment_" + attachmentIndex,
                    Name = name,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    SourceBytes = sourceBytes,
                    Text = normalizedText,
                    DocumentType = ResolveDocumentType(name, contentType),
                    Truncated = truncated
                });
            }

            return ret;
        }

        /// <summary>
        /// Count non-empty local attachments.
        /// </summary>
        /// <param name="attachments">Attachments.</param>
        /// <returns>Count.</returns>
        public static int Count(List<ChatLocalAttachment> attachments)
        {
            return NormalizeAttachments(attachments).Count;
        }

        /// <summary>
        /// Build model-visible attachment context.
        /// </summary>
        /// <param name="attachments">Processed attachments.</param>
        /// <returns>Prompt context.</returns>
        public static string BuildPromptContext(IEnumerable<ChatLocalAttachmentContext> attachments)
        {
            List<ChatLocalAttachmentContext> list = attachments?.Where(item => item != null && !String.IsNullOrWhiteSpace(item.Text)).ToList()
                ?? new List<ChatLocalAttachmentContext>();
            if (list.Count < 1) return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("User-uploaded files attached to this chat turn:");
            sb.AppendLine("Use this attachment content to answer questions about attached files. These files are not necessarily part of the assistant collection.");

            for (int i = 0; i < list.Count; i++)
            {
                ChatLocalAttachmentContext item = list[i];
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine("Attachment " + (i + 1) + ": " + item.Name);
                if (!String.IsNullOrWhiteSpace(item.AttachmentId)) sb.AppendLine("Local attachment ID: " + item.AttachmentId);
                if (!String.IsNullOrWhiteSpace(item.ContentType)) sb.AppendLine("Content type: " + item.ContentType);
                if (item.SizeBytes > 0) sb.AppendLine("Size: " + FormatBytes(item.SizeBytes));
                if (item.Truncated) sb.AppendLine("Note: attachment text was truncated to fit the chat context.");
                sb.AppendLine();
                sb.AppendLine(item.Text);
            }

            sb.AppendLine("---");
            return sb.ToString();
        }

        /// <summary>
        /// Append attachment context to a system prompt.
        /// </summary>
        /// <param name="systemPrompt">Base system prompt.</param>
        /// <param name="attachmentContext">Attachment context.</param>
        /// <returns>Combined prompt.</returns>
        public static string AppendToSystemPrompt(string systemPrompt, string attachmentContext)
        {
            if (String.IsNullOrWhiteSpace(attachmentContext)) return systemPrompt;
            if (String.IsNullOrWhiteSpace(systemPrompt)) return attachmentContext;
            return systemPrompt.TrimEnd() + Environment.NewLine + Environment.NewLine + attachmentContext;
        }

        private static async Task<string> ExtractTextAsync(
            DocumentAtomAtomizationService atomization,
            string name,
            string contentType,
            byte[] bytes,
            CancellationToken token)
        {
            if (IsTextLike(name, contentType) && TryDecodeUtf8(bytes, out string text))
                return text;

            string documentId = "chat_local_" + Guid.NewGuid().ToString("N");
            string documentType = ResolveDocumentType(name, contentType);
            if (String.IsNullOrWhiteSpace(documentType))
            {
                TypeDetectResponse detected = await atomization.DetectDocumentTypeAsync(documentId, bytes, name, token).ConfigureAwait(false);
                documentType = detected?.Type;
            }

            return await atomization.ExtractTextAsync(documentId, bytes, documentType, name, token).ConfigureAwait(false);
        }

        private static string ResolveDocumentType(string name, string contentType)
        {
            string extension = Path.GetExtension(name ?? String.Empty)?.Trim('.').ToLowerInvariant();
            if (!String.IsNullOrWhiteSpace(extension)) return extension;

            string type = (contentType ?? String.Empty).ToLowerInvariant();
            if (type.Contains("pdf")) return "pdf";
            if (type.Contains("word")) return "docx";
            if (type.Contains("spreadsheet") || type.Contains("excel")) return "xlsx";
            if (type.Contains("presentation") || type.Contains("powerpoint")) return "pptx";
            if (type.Contains("html")) return "html";
            if (type.Contains("json")) return "json";
            if (type.Contains("xml")) return "xml";
            if (type.StartsWith("text/", StringComparison.Ordinal)) return "text";
            return null;
        }

        private static bool IsTextLike(string name, string contentType)
        {
            string type = (contentType ?? String.Empty).ToLowerInvariant();
            if (type.StartsWith("text/", StringComparison.Ordinal)) return true;
            if (type.Contains("json") || type.Contains("xml") || type.Contains("csv") || type.Contains("markdown")) return true;

            string extension = Path.GetExtension(name ?? String.Empty)?.Trim('.').ToLowerInvariant();
            return extension == "txt" || extension == "md" || extension == "markdown" || extension == "json"
                || extension == "csv" || extension == "tsv" || extension == "xml" || extension == "html"
                || extension == "htm" || extension == "log";
        }

        private static bool TryDecodeUtf8(byte[] bytes, out string text)
        {
            text = null;
            if (bytes == null || bytes.Length < 1) return false;

            try
            {
                text = new UTF8Encoding(false, true).GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static byte[] DecodeBase64Content(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            string base64 = value.Trim();
            int comma = base64.IndexOf(',');
            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                base64 = base64.Substring(comma + 1);
            return Convert.FromBase64String(base64);
        }

        private static List<ChatLocalAttachment> NormalizeAttachments(List<ChatLocalAttachment> attachments)
        {
            if (attachments == null) return new List<ChatLocalAttachment>();
            return attachments
                .Where(item => item != null
                    && (!String.IsNullOrWhiteSpace(item.Text) || !String.IsNullOrWhiteSpace(item.Base64Content)))
                .ToList();
        }

        private static string NormalizeText(string text)
        {
            return (text ?? String.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal)
                .Trim();
        }

        private static string SanitizeName(string name)
        {
            string trimmed = String.IsNullOrWhiteSpace(name) ? "attachment" : name.Trim();
            return trimmed.Length <= 160 ? trimmed : trimmed.Substring(0, 160);
        }

        private static string FormatBytes(int bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return Math.Round(bytes / 1024.0, 1) + " KB";
            return Math.Round(bytes / (1024.0 * 1024.0), 1) + " MB";
        }
    }

    /// <summary>
    /// Processed local chat attachment.
    /// </summary>
    public class ChatLocalAttachmentContext
    {
        /// <summary>Stable per-turn local attachment ID.</summary>
        public string AttachmentId { get; set; } = null;

        /// <summary>Filename.</summary>
        public string Name { get; set; } = null;

        /// <summary>Content type.</summary>
        public string ContentType { get; set; } = null;

        /// <summary>Original size in bytes when supplied as a file.</summary>
        public int SizeBytes { get; set; } = 0;

        /// <summary>Original attachment bytes for per-turn tool access.</summary>
        public byte[] SourceBytes { get; set; } = null;

        /// <summary>Best-effort DocumentAtom document type.</summary>
        public string DocumentType { get; set; } = null;

        /// <summary>Extracted or supplied text.</summary>
        public string Text { get; set; } = null;

        /// <summary>Whether text was truncated for prompt safety.</summary>
        public bool Truncated { get; set; } = false;
    }

    /// <summary>
    /// Local chat attachment processing result.
    /// </summary>
    public class ChatLocalAttachmentResolution
    {
        /// <summary>Whether processing succeeded.</summary>
        public bool Success { get; set; } = false;

        /// <summary>HTTP status code for failures.</summary>
        public int StatusCode { get; set; } = 400;

        /// <summary>Error message for failures.</summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>Processed attachments.</summary>
        public List<ChatLocalAttachmentContext> Attachments { get; set; } = new List<ChatLocalAttachmentContext>();
    }
}
