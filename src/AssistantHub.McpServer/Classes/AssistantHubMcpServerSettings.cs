namespace AssistantHub.McpServer.Classes
{
    using System;

    /// <summary>
    /// AssistantHub MCP Server settings.
    /// </summary>
    public class AssistantHubMcpServerSettings
    {
        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Created by identifier.
        /// </summary>
        public string CreatedBy { get; set; } = "Setup";

        /// <summary>
        /// Deployment type.
        /// </summary>
        public string DeploymentType { get; set; } = "Private";

        /// <summary>
        /// Software version.
        /// </summary>
        public string SoftwareVersion { get; set; } = Constants.Version;

        /// <summary>
        /// Node information.
        /// </summary>
        public NodeSettings Node { get; set; } = new NodeSettings();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging { get; set; } = new LoggingSettings();

        /// <summary>
        /// Upstream AssistantHub connection settings.
        /// </summary>
        public AssistantHubServiceSettings AssistantHub { get; set; } = new AssistantHubServiceSettings();

        /// <summary>
        /// HTTP transport settings.
        /// </summary>
        public HttpServerSettings Http { get; set; } = new HttpServerSettings();

        /// <summary>
        /// TCP transport settings.
        /// </summary>
        public TcpServerSettings Tcp { get; set; } = new TcpServerSettings();

        /// <summary>
        /// WebSocket transport settings.
        /// </summary>
        public WebSocketServerSettings WebSocket { get; set; } = new WebSocketServerSettings();

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage { get; set; } = new StorageSettings();

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings Debug { get; set; } = new DebugSettings();
    }
}
