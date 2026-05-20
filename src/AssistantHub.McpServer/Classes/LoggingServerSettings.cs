namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// Logging server settings.
    /// </summary>
    public class LoggingServerSettings
    {
        /// <summary>
        /// Hostname.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Port.
        /// </summary>
        public int Port { get; set; } = 514;
    }
}
