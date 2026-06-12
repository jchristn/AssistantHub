namespace AssistantHub.Core.Services
{
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// Atom response from DocumentAtom.
    /// </summary>
    internal class AtomResponse
    {
        /// <summary>
        /// Atom type.
        /// </summary>
        public JsonElement Type { get; set; }

        /// <summary>
        /// Title.
        /// </summary>
        public string Title { get; set; } = null;

        /// <summary>
        /// Subtitle.
        /// </summary>
        public string Subtitle { get; set; } = null;

        /// <summary>
        /// Text content of the atom.
        /// </summary>
        public string Text { get; set; } = null;

        /// <summary>
        /// Ordered list content.
        /// </summary>
        public List<string> OrderedList { get; set; } = null;

        /// <summary>
        /// Unordered list content.
        /// </summary>
        public List<string> UnorderedList { get; set; } = null;

        /// <summary>
        /// Table content.
        /// </summary>
        public JsonElement Table { get; set; }

        /// <summary>
        /// Structural child atoms.
        /// </summary>
        public List<AtomResponse> Quarks { get; set; } = null;

        /// <summary>
        /// Text chunks produced by DocumentAtom chunking.
        /// </summary>
        public List<AtomChunkResponse> Chunks { get; set; } = null;
    }

    /// <summary>
    /// Chunk response from DocumentAtom.
    /// </summary>
    internal class AtomChunkResponse
    {
        /// <summary>
        /// Chunk text content.
        /// </summary>
        public string Text { get; set; } = null;
    }
}
