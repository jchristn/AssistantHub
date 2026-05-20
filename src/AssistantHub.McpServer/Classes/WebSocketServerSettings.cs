namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// WebSocket server settings.
    /// </summary>
    public class WebSocketServerSettings
    {
        /// <summary>
        /// WebSocket hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// WebSocket port.
        /// </summary>
        public int Port { get; set; } = 8822;
    }
}
