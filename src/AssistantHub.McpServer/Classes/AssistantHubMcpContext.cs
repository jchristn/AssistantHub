namespace AssistantHub.McpServer.Classes
{
    using AssistantHub.Sdk;
    using SyslogLogging;

    /// <summary>
    /// Shared MCP execution context.
    /// </summary>
    public class AssistantHubMcpContext
    {
        /// <summary>
        /// Settings.
        /// </summary>
        public AssistantHubMcpServerSettings Settings { get; init; } = null!;

        /// <summary>
        /// Upstream SDK client.
        /// </summary>
        public AssistantHubClient Sdk { get; init; } = null!;

        /// <summary>
        /// Logger.
        /// </summary>
        public LoggingModule Logging { get; init; } = null!;

        /// <summary>
        /// Redactor.
        /// </summary>
        public AssistantHubMcpRedactor Redactor { get; init; } = null!;
    }
}
