namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// Debug settings.
    /// </summary>
    public class DebugSettings
    {
        /// <summary>
        /// Enable exception debug output.
        /// </summary>
        public bool Exceptions { get; set; } = true;

        /// <summary>
        /// Enable request debug output.
        /// </summary>
        public bool Requests { get; set; } = false;

        /// <summary>
        /// Enable MCP operation debug output.
        /// </summary>
        public bool McpOperations { get; set; } = false;
    }
}
