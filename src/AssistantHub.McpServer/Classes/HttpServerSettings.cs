namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// HTTP server settings.
    /// </summary>
    public class HttpServerSettings
    {
        /// <summary>
        /// HTTP hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// HTTP port.
        /// </summary>
        public int Port { get; set; } = 8820;
    }
}
