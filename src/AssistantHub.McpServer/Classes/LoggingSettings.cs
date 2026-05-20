namespace AssistantHub.McpServer.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Logging servers.
        /// </summary>
        public List<LoggingServerSettings> Servers { get; set; } = new List<LoggingServerSettings>();

        /// <summary>
        /// Log directory.
        /// </summary>
        public string LogDirectory { get; set; } = "./logs/";

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = "assistanthub-mcp.log";

        /// <summary>
        /// Enable console logging.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable colors in console.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Minimum severity level.
        /// </summary>
        public int MinimumSeverity { get; set; } = 0;
    }
}
