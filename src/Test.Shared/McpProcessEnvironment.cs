namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Settings;
    using Voltaic;

    internal sealed class McpProcessEnvironment
    {
        public string ArtifactDirectory { get; init; } = string.Empty;
        public string ServerWorkingDirectory { get; init; } = string.Empty;
        public string McpWorkingDirectory { get; init; } = string.Empty;
        public string ServerAssemblyPath { get; init; } = string.Empty;
        public string McpAssemblyPath { get; init; } = string.Empty;
        public string ApiKey { get; init; } = string.Empty;
        public int DependencyStubPort { get; init; }
        public int ServerPort { get; init; }
        public int McpHttpPort { get; init; }
        public int McpTcpPort { get; init; }
        public int McpWebSocketPort { get; init; }
        public ManagedProcess? ServerProcess { get; set; }
        public ManagedProcess? McpProcess { get; set; }

        public string DependencyStubEndpoint => "http://127.0.0.1:" + DependencyStubPort;
        public string ServerEndpoint => "http://127.0.0.1:" + ServerPort;
        public string McpHttpEndpoint => "http://127.0.0.1:" + McpHttpPort;
        public string McpWebSocketEndpoint => "ws://127.0.0.1:" + McpWebSocketPort + "/mcp";
    }
}
