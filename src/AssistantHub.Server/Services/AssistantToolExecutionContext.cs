namespace AssistantHub.Server.Services
{
    using System.Collections.Generic;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Context for executing a model-directed server-side tool.
    /// </summary>
    public class AssistantToolExecutionContext
    {
        /// <summary>
        /// Assistant that owns the chat turn.
        /// </summary>
        public Assistant Assistant { get; set; } = null;

        /// <summary>
        /// Assistant settings.
        /// </summary>
        public AssistantSettings Settings { get; set; } = null;

        /// <summary>
        /// Normalized tool policy.
        /// </summary>
        public AssistantToolPolicy Policy { get; set; } = null;

        /// <summary>
        /// Optional trace identifier for audit correlation.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Per-turn user-uploaded attachments available to tools.
        /// </summary>
        public List<ChatLocalAttachmentContext> LocalAttachments { get; set; } = new List<ChatLocalAttachmentContext>();
    }
}
