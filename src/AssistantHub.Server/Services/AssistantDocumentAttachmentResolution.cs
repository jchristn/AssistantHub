namespace AssistantHub.Server.Services
{
    using System.Collections.Generic;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Result from resolving chat document attachments.
    /// </summary>
    public class AssistantDocumentAttachmentResolution
    {
        /// <summary>
        /// Indicates whether resolution succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// HTTP status code to use when resolution fails.
        /// </summary>
        public int StatusCode { get; set; } = 400;

        /// <summary>
        /// Error message when resolution fails.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Validated document identifiers.
        /// </summary>
        public List<string> DocumentIds { get; set; } = new List<string>();

        /// <summary>
        /// Safe document metadata.
        /// </summary>
        public List<AssistantDocumentSelectionItem> Documents { get; set; } = new List<AssistantDocumentSelectionItem>();
    }
}
