namespace AssistantHub.Server.Services
{
    using System.Collections.Generic;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Retrieved context for one chat turn, ready to inject into the prompt: the retrieval stage outcome, the chunks
    /// in prompt order, their text, citation labels and sources, and any note for the system prompt.
    /// </summary>
    public sealed class AssistantPromptContext
    {
        /// <summary>
        /// Outcome of the retrieval stages.
        /// </summary>
        public AssistantRetrievalStagesResult Stages { get; set; } = new AssistantRetrievalStagesResult();

        /// <summary>
        /// Chunks in prompt order (after ContextOrder).
        /// </summary>
        public List<RetrievalChunk> Chunks { get; set; } = new List<RetrievalChunk>();

        /// <summary>
        /// Text injected for each chunk.
        /// </summary>
        public List<string> ContextChunks { get; set; } = new List<string>();

        /// <summary>
        /// Citation label per chunk when citations are enabled, otherwise null.
        /// </summary>
        public List<string> ChunkLabels { get; set; } = null;

        /// <summary>
        /// Citation source per chunk when citations are enabled, otherwise null.
        /// </summary>
        public List<CitationSource> CitationSources { get; set; } = null;

        /// <summary>
        /// Text to append to the system prompt for this turn (for example, that nothing relevant was found), or null.
        /// </summary>
        public string PromptNote { get; set; } = null;
    }
}
