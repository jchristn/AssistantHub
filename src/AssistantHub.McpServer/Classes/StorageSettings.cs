namespace AssistantHub.McpServer.Classes
{
    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        /// <summary>
        /// Backups directory.
        /// </summary>
        public string BackupsDirectory { get; set; } = "./backups/";

        /// <summary>
        /// Temporary directory.
        /// </summary>
        public string TempDirectory { get; set; } = "./temp/";

        /// <summary>
        /// Maximum inline binary payload size in bytes.
        /// </summary>
        public long MaxInlineBinaryBytes { get; set; } = 5 * 1024 * 1024;
    }
}
