namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Safe public metadata for documents that may be selected in assistant chat.
    /// </summary>
    public class AssistantDocumentSelectionItem
    {
        /// <summary>
        /// Assistant document identifier.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Display name for the document.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Original filename as uploaded or crawled.
        /// </summary>
        public string OriginalFilename { get; set; } = null;

        /// <summary>
        /// MIME content type.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Size in bytes.
        /// </summary>
        public long SizeBytes { get; set; } = 0;

        /// <summary>
        /// Source URL when explicitly allowed by assistant settings.
        /// </summary>
        public string SourceUrl { get; set; } = null;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Create safe selection metadata from a document.
        /// </summary>
        /// <param name="document">Document.</param>
        /// <param name="includeSourceUrl">Include source URL.</param>
        /// <returns>Safe selection item.</returns>
        public static AssistantDocumentSelectionItem FromDocument(AssistantDocument document, bool includeSourceUrl)
        {
            if (document == null) return null;

            return new AssistantDocumentSelectionItem
            {
                Id = document.Id,
                Name = document.Name,
                OriginalFilename = document.OriginalFilename,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                SourceUrl = includeSourceUrl ? document.SourceUrl : null,
                CreatedUtc = document.CreatedUtc,
                LastUpdateUtc = document.LastUpdateUtc
            };
        }
    }
}
