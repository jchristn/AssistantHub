namespace AssistantHub.McpServer.Classes
{
    using System;

    /// <summary>
    /// Constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Software version.
        /// </summary>
        public static string Version = "v" + AssistantHub.Core.Constants.ProductVersion;

        /// <summary>
        /// Logo.
        /// </summary>
        public static string Logo = AssistantHub.Core.Constants.Logo;

        /// <summary>
        /// Product name.
        /// </summary>
        public static string ProductName = " AssistantHub MCP Server";

        /// <summary>
        /// Copyright.
        /// </summary>
        public static string Copyright = " (c)2025 Joel Christner";

        /// <summary>
        /// Settings file path.
        /// </summary>
        public static string SettingsFile = "./assistanthub-mcp.json";

        /// <summary>
        /// AssistantHub endpoint environment variable.
        /// </summary>
        public static string AssistantHubEndpointEnvironmentVariable = "ASSISTANTHUB_ENDPOINT";

        /// <summary>
        /// AssistantHub API key environment variable.
        /// </summary>
        public static string AssistantHubApiKeyEnvironmentVariable = "ASSISTANTHUB_API_KEY";

        /// <summary>
        /// MCP HTTP hostname environment variable.
        /// </summary>
        public static string McpHttpHostnameEnvironmentVariable = "MCP_HTTP_HOSTNAME";

        /// <summary>
        /// MCP HTTP port environment variable.
        /// </summary>
        public static string McpHttpPortEnvironmentVariable = "MCP_HTTP_PORT";

        /// <summary>
        /// MCP TCP address environment variable.
        /// </summary>
        public static string McpTcpAddressEnvironmentVariable = "MCP_TCP_ADDRESS";

        /// <summary>
        /// MCP TCP port environment variable.
        /// </summary>
        public static string McpTcpPortEnvironmentVariable = "MCP_TCP_PORT";

        /// <summary>
        /// MCP WebSocket hostname environment variable.
        /// </summary>
        public static string McpWebSocketHostnameEnvironmentVariable = "MCP_WS_HOSTNAME";

        /// <summary>
        /// MCP WebSocket port environment variable.
        /// </summary>
        public static string McpWebSocketPortEnvironmentVariable = "MCP_WS_PORT";

        /// <summary>
        /// Console logging environment variable.
        /// </summary>
        public static string ConsoleLoggingEnvironmentVariable = "MCP_CONSOLE_LOGGING";
    }
}
