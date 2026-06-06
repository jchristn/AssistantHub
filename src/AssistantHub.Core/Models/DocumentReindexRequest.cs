namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Request body for document Verbex reindex operations.
    /// </summary>
    public class DocumentReindexRequest
    {
        #region Public-Members

        /// <summary>
        /// Optional explicit document identifiers to reindex. When omitted, the server enumerates documents for the caller's tenant.
        /// </summary>
        public List<string> DocumentIds { get; set; } = null;

        /// <summary>
        /// Reindex documents even when Verbex metadata is already present.
        /// </summary>
        public bool IncludeAlreadyIndexed { get; set; } = false;

        #endregion
    }
}
