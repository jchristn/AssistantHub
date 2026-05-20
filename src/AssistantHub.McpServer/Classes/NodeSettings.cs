namespace AssistantHub.McpServer.Classes
{
    using System;

    /// <summary>
    /// Node information settings.
    /// </summary>
    public class NodeSettings
    {
        /// <summary>
        /// Node GUID.
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Node name.
        /// </summary>
        public string Name { get; set; } = "AssistantHub Model Context Protocol Server";

        /// <summary>
        /// Node hostname.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Instance type.
        /// </summary>
        public string InstanceType { get; set; } = "McpServer";

        /// <summary>
        /// Last start timestamp.
        /// </summary>
        public DateTime LastStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
