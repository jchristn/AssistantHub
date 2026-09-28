namespace AssistantHub.Core.Services
{
    using System;

    /// <summary>
    /// Task prefixes that embedding models were trained with. Models such as nomic-embed-text and e5 expect queries
    /// and documents to be marked ("search_query: " / "search_document: "); embedding without them costs quality.
    /// </summary>
    public static class EmbeddingModelProfiles
    {
        #region Private-Members

        private const string _RetrievalInstruction = "Represent this sentence for searching relevant passages: ";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve the task prefixes for an embedding model name.
        /// </summary>
        /// <param name="model">Model name, for example "nomic-embed-text:latest".</param>
        /// <returns>The query and document prefixes; both empty when the model uses none or is unknown.</returns>
        public static (string QueryPrefix, string DocumentPrefix) Resolve(string model)
        {
            if (String.IsNullOrWhiteSpace(model)) return ("", "");
            string m = model.Trim().ToLowerInvariant();

            if (m.Contains("nomic-embed")) return ("search_query: ", "search_document: ");
            if (m.Contains("snowflake-arctic-embed") || m.Contains("arctic-embed")) return (_RetrievalInstruction, "");
            if (m.Contains("mxbai-embed")) return (_RetrievalInstruction, "");
            if (m.Contains("bge-") || m.StartsWith("bge", StringComparison.Ordinal)) return (_RetrievalInstruction, "");
            if (m.Contains("e5-") || m.Contains("-e5") || m.StartsWith("e5", StringComparison.Ordinal)) return ("query: ", "passage: ");
            return ("", "");
        }

        #endregion
    }
}
