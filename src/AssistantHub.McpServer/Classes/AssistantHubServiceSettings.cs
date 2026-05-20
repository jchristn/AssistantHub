namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// Upstream AssistantHub service settings.
    /// </summary>
    public class AssistantHubServiceSettings
    {
        /// <summary>
        /// Upstream endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:8800";

        /// <summary>
        /// Bearer token used for upstream authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}
