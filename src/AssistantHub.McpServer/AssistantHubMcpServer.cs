namespace AssistantHub.McpServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Runtime.Loader;
    using System.Text;
    using AssistantHub.Core.Helpers;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.McpServer.Registrations;
    using AssistantHub.Sdk;
    using SyslogLogging;
    using Voltaic;

    /// <summary>
    /// AssistantHub MCP Server.
    /// </summary>
    public static class AssistantHubMcpServer
    {
        private static readonly string _Header = "[AssistantHub.McpServer] ";
        private static readonly string _SoftwareVersion = Constants.Version;
        private static readonly int _ProcessId = Environment.ProcessId;

        private static bool _ShowConfiguration = false;
        private static bool _RunInstall = false;
        private static bool _DryRun = false;

        private static AssistantHubMcpServerSettings _Settings = new AssistantHubMcpServerSettings();
        private static LoggingModule _Logging = null!;
        private static AssistantHubClient _Sdk = null!;
        private static AssistantHubMcpContext _Context = null!;

        private static McpHttpServer? _McpHttpServer = null;
        private static McpTcpServer? _McpTcpServer = null;
        private static McpWebsocketsServer? _McpWebSocketServer = null;

        private static readonly System.Threading.CancellationTokenSource _TokenSource = new System.Threading.CancellationTokenSource();
        private static readonly System.Threading.CancellationToken _Token = _TokenSource.Token;

        /// <summary>
        /// Entrypoint.
        /// </summary>
        public static void Main(string[] args)
        {
            Welcome();
            ParseArguments(args);
            InitializeSettings();

            if (_RunInstall)
            {
                RunInstall(_DryRun);
                Environment.Exit(0);
            }

            InitializeGlobals();

            _Logging.Info(_Header + "starting at " + DateTime.UtcNow + " using process ID " + _ProcessId + Environment.NewLine);

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            AssemblyLoadContext.Default.Unloading += _ => waitHandle.Set();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _Logging.Info(_Header + "termination signal received");
                waitHandle.Set();
                eventArgs.Cancel = true;
            };

            bool waitHandleSignal;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            }
            while (!waitHandleSignal);

            _TokenSource.Cancel();
            _Logging.Info(_Header + "stopping at " + DateTime.UtcNow);
        }

        private static void Welcome()
        {
            Console.WriteLine(
                Environment.NewLine
                + Constants.Logo
                + Environment.NewLine
                + Constants.ProductName
                + Environment.NewLine
                + Constants.Copyright
                + Environment.NewLine);
        }

        private static void ParseArguments(string[] args)
        {
            if (args == null || args.Length < 1)
                return;

            foreach (string arg in args)
            {
                if (arg.StartsWith("--config=", StringComparison.OrdinalIgnoreCase))
                    Constants.SettingsFile = arg.Substring(9);
                else if (arg.Equals("--showconfig", StringComparison.OrdinalIgnoreCase))
                    _ShowConfiguration = true;
                else if (arg.Equals("install", StringComparison.OrdinalIgnoreCase))
                    _RunInstall = true;
                else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
                    _DryRun = true;
                else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    ShowHelp();
                    Environment.Exit(0);
                }
            }
        }

        private static void InitializeSettings()
        {
            Console.WriteLine("Using settings file '" + Constants.SettingsFile + "'");

            if (!File.Exists(Constants.SettingsFile))
            {
                Console.WriteLine("Settings file '" + Constants.SettingsFile + "' does not exist. Creating default configuration...");
                _Settings.SoftwareVersion = _SoftwareVersion;
                File.WriteAllBytes(Constants.SettingsFile, Encoding.UTF8.GetBytes(Serializer.SerializeJson(_Settings, true)));
                Console.WriteLine("Created settings file '" + Constants.SettingsFile + "' with default configuration");
            }
            else
            {
                _Settings = Serializer.DeserializeJson<AssistantHubMcpServerSettings>(File.ReadAllText(Constants.SettingsFile)) ?? new AssistantHubMcpServerSettings();
                _Settings.Node.LastStartUtc = DateTime.UtcNow;
                File.WriteAllBytes(Constants.SettingsFile, Encoding.UTF8.GetBytes(Serializer.SerializeJson(_Settings, true)));
            }

            if (_ShowConfiguration)
            {
                Console.WriteLine();
                Console.WriteLine("Configuration:");
                Console.WriteLine(Serializer.SerializeJson(_Settings, true));
                Console.WriteLine();
                Environment.Exit(0);
            }
        }

        private static void InitializeGlobals()
        {
            ApplyEnvironmentOverrides();
            InitializeLogging();
            InitializeStorage();
            InitializeSdk();
            InitializeMcpServers();
        }

        private static void ApplyEnvironmentOverrides()
        {
            string? endpoint = Environment.GetEnvironmentVariable(Constants.AssistantHubEndpointEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(endpoint))
                _Settings.AssistantHub.Endpoint = endpoint;

            string? apiKey = Environment.GetEnvironmentVariable(Constants.AssistantHubApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(apiKey))
                _Settings.AssistantHub.ApiKey = apiKey;

            string? httpHostname = Environment.GetEnvironmentVariable(Constants.McpHttpHostnameEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(httpHostname))
                _Settings.Http.Hostname = httpHostname;

            string? httpPort = Environment.GetEnvironmentVariable(Constants.McpHttpPortEnvironmentVariable);
            if (int.TryParse(httpPort, out int parsedHttpPort) && parsedHttpPort > 0 && parsedHttpPort <= 65535)
                _Settings.Http.Port = parsedHttpPort;

            string? tcpAddress = Environment.GetEnvironmentVariable(Constants.McpTcpAddressEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(tcpAddress))
                _Settings.Tcp.Address = tcpAddress;

            string? tcpPort = Environment.GetEnvironmentVariable(Constants.McpTcpPortEnvironmentVariable);
            if (int.TryParse(tcpPort, out int parsedTcpPort) && parsedTcpPort > 0 && parsedTcpPort <= 65535)
                _Settings.Tcp.Port = parsedTcpPort;

            string? wsHostname = Environment.GetEnvironmentVariable(Constants.McpWebSocketHostnameEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(wsHostname))
                _Settings.WebSocket.Hostname = wsHostname;

            string? wsPort = Environment.GetEnvironmentVariable(Constants.McpWebSocketPortEnvironmentVariable);
            if (int.TryParse(wsPort, out int parsedWsPort) && parsedWsPort > 0 && parsedWsPort <= 65535)
                _Settings.WebSocket.Port = parsedWsPort;

            string? consoleLogging = Environment.GetEnvironmentVariable(Constants.ConsoleLoggingEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(consoleLogging))
                _Settings.Logging.ConsoleLogging = consoleLogging == "1" || bool.TryParse(consoleLogging, out bool enabled) && enabled;
        }

        private static void InitializeLogging()
        {
            Console.WriteLine("Initializing logging");

            List<SyslogServer> syslogServers = new List<SyslogServer>();
            if (_Settings.Logging.Servers != null)
            {
                foreach (LoggingServerSettings server in _Settings.Logging.Servers)
                {
                    syslogServers.Add(new SyslogServer
                    {
                        Hostname = server.Hostname,
                        Port = server.Port
                    });

                    Console.WriteLine("| syslog://" + server.Hostname + ":" + server.Port);
                }
            }

            _Logging = syslogServers.Count > 0 ? new LoggingModule(syslogServers) : new LoggingModule();
            _Logging.Settings.MinimumSeverity = (Severity)_Settings.Logging.MinimumSeverity;
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;

            if (!string.IsNullOrWhiteSpace(_Settings.Logging.LogDirectory))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory))
                    Directory.CreateDirectory(_Settings.Logging.LogDirectory);

                _Settings.Logging.LogFilename = Path.Combine(_Settings.Logging.LogDirectory, _Settings.Logging.LogFilename);
            }

            if (!string.IsNullOrWhiteSpace(_Settings.Logging.LogFilename))
            {
                _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
                _Logging.Settings.LogFilename = _Settings.Logging.LogFilename;
            }
        }

        private static void InitializeStorage()
        {
            Console.WriteLine("Initializing storage");

            if (!string.IsNullOrWhiteSpace(_Settings.Storage.BackupsDirectory) && !Directory.Exists(_Settings.Storage.BackupsDirectory))
                Directory.CreateDirectory(_Settings.Storage.BackupsDirectory);

            if (!string.IsNullOrWhiteSpace(_Settings.Storage.TempDirectory) && !Directory.Exists(_Settings.Storage.TempDirectory))
                Directory.CreateDirectory(_Settings.Storage.TempDirectory);
        }

        private static void InitializeSdk()
        {
            if (string.IsNullOrWhiteSpace(_Settings.AssistantHub.Endpoint))
                throw new InvalidOperationException("AssistantHub endpoint is required. Configure 'AssistantHub.Endpoint' or set ASSISTANTHUB_ENDPOINT.");

            _Sdk = new AssistantHubClient(_Settings.AssistantHub.Endpoint, _Settings.AssistantHub.ApiKey);
            _Context = new AssistantHubMcpContext
            {
                Settings = _Settings,
                Sdk = _Sdk,
                Logging = _Logging,
                Redactor = new AssistantHubMcpRedactor()
            };
        }

        private static void InitializeMcpServers()
        {
            Console.WriteLine(
                "Starting MCP servers on:" + Environment.NewLine
                + "| HTTP         : http://" + _Settings.Http.Hostname + ":" + _Settings.Http.Port + "/rpc" + Environment.NewLine
                + "| TCP          : tcp://" + (_Settings.Tcp.Address.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : _Settings.Tcp.Address) + ":" + _Settings.Tcp.Port + Environment.NewLine
                + "| WebSocket    : ws://" + _Settings.WebSocket.Hostname + ":" + _Settings.WebSocket.Port + "/mcp");

            string tcpAddressForBinding = _Settings.Tcp.Address.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? "127.0.0.1"
                : _Settings.Tcp.Address;

            _McpHttpServer = new McpHttpServer(_Settings.Http.Hostname, _Settings.Http.Port, "/rpc", "/events", includeDefaultMethods: true);
            _McpTcpServer = new McpTcpServer(IPAddress.Parse(tcpAddressForBinding), _Settings.Tcp.Port, includeDefaultMethods: true);
            _McpWebSocketServer = new McpWebsocketsServer(_Settings.WebSocket.Hostname, _Settings.WebSocket.Port, "/mcp", includeDefaultMethods: true);

            _McpHttpServer.ServerName = "AssistantHub.McpServer";
            _McpHttpServer.ServerVersion = AssistantHub.Core.Constants.ProductVersion;
            _McpTcpServer.ServerName = "AssistantHub.McpServer";
            _McpTcpServer.ServerVersion = AssistantHub.Core.Constants.ProductVersion;
            _McpWebSocketServer.ServerName = "AssistantHub.McpServer";
            _McpWebSocketServer.ServerVersion = AssistantHub.Core.Constants.ProductVersion;

            _McpHttpServer.ClientConnected += ClientConnected;
            _McpHttpServer.ClientDisconnected += ClientDisconnected;
            _McpHttpServer.RequestReceived += ClientRequestReceived;
            _McpHttpServer.ResponseSent += ClientResponseSent;

            _McpTcpServer.ClientConnected += ClientConnected;
            _McpTcpServer.ClientDisconnected += ClientDisconnected;
            _McpTcpServer.RequestReceived += ClientRequestReceived;
            _McpTcpServer.ResponseSent += ClientResponseSent;

            _McpWebSocketServer.ClientConnected += ClientConnected;
            _McpWebSocketServer.ClientDisconnected += ClientDisconnected;
            _McpWebSocketServer.RequestReceived += ClientRequestReceived;
            _McpWebSocketServer.ResponseSent += ClientResponseSent;

            RegisterMcpTools();

            _ = _McpHttpServer.StartAsync(_Token);
            _ = _McpTcpServer.StartAsync(_Token);
            _ = _McpWebSocketServer.StartAsync(_Token);

            Console.WriteLine();
        }

        private static void RegisterMcpTools()
        {
            if (_McpHttpServer == null || _McpTcpServer == null || _McpWebSocketServer == null)
                throw new InvalidOperationException("MCP servers are not initialized");

            RegisterTransport(SystemRegistrations.RegisterHttpTools, SystemRegistrations.RegisterTcpMethods, SystemRegistrations.RegisterWebSocketMethods);
            RegisterTransport(AuthenticationRegistrations.RegisterHttpTools, AuthenticationRegistrations.RegisterTcpMethods, AuthenticationRegistrations.RegisterWebSocketMethods);
            RegisterTransport(TenantRegistrations.RegisterHttpTools, TenantRegistrations.RegisterTcpMethods, TenantRegistrations.RegisterWebSocketMethods);
            RegisterTransport(UserRegistrations.RegisterHttpTools, UserRegistrations.RegisterTcpMethods, UserRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CredentialRegistrations.RegisterHttpTools, CredentialRegistrations.RegisterTcpMethods, CredentialRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CollectionRegistrations.RegisterHttpTools, CollectionRegistrations.RegisterTcpMethods, CollectionRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CollectionRecordRegistrations.RegisterHttpTools, CollectionRecordRegistrations.RegisterTcpMethods, CollectionRecordRegistrations.RegisterWebSocketMethods);
            RegisterTransport(BucketRegistrations.RegisterHttpTools, BucketRegistrations.RegisterTcpMethods, BucketRegistrations.RegisterWebSocketMethods);
            RegisterTransport(BucketObjectRegistrations.RegisterHttpTools, BucketObjectRegistrations.RegisterTcpMethods, BucketObjectRegistrations.RegisterWebSocketMethods);
            RegisterTransport(AssistantRegistrations.RegisterHttpTools, AssistantRegistrations.RegisterTcpMethods, AssistantRegistrations.RegisterWebSocketMethods);
            RegisterTransport(AssistantSettingsRegistrations.RegisterHttpTools, AssistantSettingsRegistrations.RegisterTcpMethods, AssistantSettingsRegistrations.RegisterWebSocketMethods);
            RegisterTransport(DocumentRegistrations.RegisterHttpTools, DocumentRegistrations.RegisterTcpMethods, DocumentRegistrations.RegisterWebSocketMethods);
            RegisterTransport(IngestionRuleRegistrations.RegisterHttpTools, IngestionRuleRegistrations.RegisterTcpMethods, IngestionRuleRegistrations.RegisterWebSocketMethods);
            RegisterTransport(FeedbackRegistrations.RegisterHttpTools, FeedbackRegistrations.RegisterTcpMethods, FeedbackRegistrations.RegisterWebSocketMethods);
            RegisterTransport(HistoryRegistrations.RegisterHttpTools, HistoryRegistrations.RegisterTcpMethods, HistoryRegistrations.RegisterWebSocketMethods);
            RegisterTransport(RequestHistoryRegistrations.RegisterHttpTools, RequestHistoryRegistrations.RegisterTcpMethods, RequestHistoryRegistrations.RegisterWebSocketMethods);
            RegisterTransport(EmbeddingEndpointRegistrations.RegisterHttpTools, EmbeddingEndpointRegistrations.RegisterTcpMethods, EmbeddingEndpointRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CompletionEndpointRegistrations.RegisterHttpTools, CompletionEndpointRegistrations.RegisterTcpMethods, CompletionEndpointRegistrations.RegisterWebSocketMethods);
            RegisterTransport(ModelRegistrations.RegisterHttpTools, ModelRegistrations.RegisterTcpMethods, ModelRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CrawlPlanRegistrations.RegisterHttpTools, CrawlPlanRegistrations.RegisterTcpMethods, CrawlPlanRegistrations.RegisterWebSocketMethods);
            RegisterTransport(CrawlOperationRegistrations.RegisterHttpTools, CrawlOperationRegistrations.RegisterTcpMethods, CrawlOperationRegistrations.RegisterWebSocketMethods);
            RegisterTransport(EvalRegistrations.RegisterHttpTools, EvalRegistrations.RegisterTcpMethods, EvalRegistrations.RegisterWebSocketMethods);
            RegisterTransport(ConfigurationRegistrations.RegisterHttpTools, ConfigurationRegistrations.RegisterTcpMethods, ConfigurationRegistrations.RegisterWebSocketMethods);
        }

        private static void RegisterTransport(
            Action<McpHttpServer, AssistantHubMcpContext> httpRegister,
            Action<McpTcpServer, AssistantHubMcpContext> tcpRegister,
            Action<McpWebsocketsServer, AssistantHubMcpContext> wsRegister)
        {
            httpRegister(_McpHttpServer!, _Context);
            tcpRegister(_McpTcpServer!, _Context);
            wsRegister(_McpWebSocketServer!, _Context);
        }

        private static void ClientConnected(object? sender, ClientConnection e)
        {
            _Logging.Debug(_Header + "client connection started with session ID " + e.SessionId + " (" + e.Type + ")");
        }

        private static void ClientDisconnected(object? sender, ClientConnection e)
        {
            _Logging.Debug(_Header + "client connection terminated with session ID " + e.SessionId + " (" + e.Type + ")");
        }

        private static void ClientRequestReceived(object? sender, JsonRpcRequestEventArgs e)
        {
            _Logging.Debug(_Header + "client session " + e.Client.SessionId + " request " + e.Method);
        }

        private static void ClientResponseSent(object? sender, JsonRpcResponseEventArgs e)
        {
            _Logging.Debug(_Header + "client session " + e.Client.SessionId + " request " + e.Method + " completed (" + e.Duration.TotalMilliseconds + "ms)");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("AssistantHub MCP Server");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  AssistantHub.McpServer [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --config=<file>        Settings file path (default: ./assistanthub-mcp.json)");
            Console.WriteLine("  --showconfig           Display configuration and exit");
            Console.WriteLine("  install                Auto-configure Claude Code (writes ~/.claude.json and agent definition)");
            Console.WriteLine("  install --dry-run      Preview install changes without writing files");
            Console.WriteLine("  --help, -h             Show this help message");
            Console.WriteLine();
            Console.WriteLine("Environment variables:");
            Console.WriteLine("  ASSISTANTHUB_ENDPOINT  Override AssistantHub endpoint");
            Console.WriteLine("  ASSISTANTHUB_API_KEY   Override AssistantHub bearer token");
            Console.WriteLine("  MCP_HTTP_HOSTNAME      Override MCP HTTP hostname");
            Console.WriteLine("  MCP_HTTP_PORT          Override MCP HTTP port");
            Console.WriteLine("  MCP_TCP_ADDRESS        Override MCP TCP bind address");
            Console.WriteLine("  MCP_TCP_PORT           Override MCP TCP port");
            Console.WriteLine("  MCP_WS_HOSTNAME        Override MCP WebSocket hostname");
            Console.WriteLine("  MCP_WS_PORT            Override MCP WebSocket port");
            Console.WriteLine("  MCP_CONSOLE_LOGGING    Override console logging (1/0 or true/false)");
        }

        private static void RunInstall(bool dryRun)
        {
            string httpHostname = _Settings.Http.Hostname;
            int httpPort = _Settings.Http.Port;
            string mcpUrl = "http://" + httpHostname + ":" + httpPort + "/rpc";

            Console.WriteLine(dryRun ? "[DRY RUN] Previewing install changes..." : "Installing AssistantHub MCP configuration...");
            Console.WriteLine();

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string claudeJsonPath = Path.Combine(homeDirectory, ".claude.json");

            System.Text.Json.Nodes.JsonNode claudeJsonRoot;
            if (File.Exists(claudeJsonPath))
            {
                string existingContent = File.ReadAllText(claudeJsonPath);
                claudeJsonRoot = System.Text.Json.Nodes.JsonNode.Parse(existingContent) ?? new System.Text.Json.Nodes.JsonObject();
            }
            else
            {
                claudeJsonRoot = new System.Text.Json.Nodes.JsonObject();
            }

            System.Text.Json.Nodes.JsonObject mcpServersNode;
            if (claudeJsonRoot["mcpServers"] is System.Text.Json.Nodes.JsonObject existingMcpServers)
            {
                mcpServersNode = existingMcpServers;
            }
            else
            {
                mcpServersNode = new System.Text.Json.Nodes.JsonObject();
                claudeJsonRoot["mcpServers"] = mcpServersNode;
            }

            mcpServersNode["assistanthub"] = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "http",
                ["url"] = mcpUrl
            };

            System.Text.Json.JsonSerializerOptions jsonWriteOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            string claudeJsonOutput = claudeJsonRoot.ToJsonString(jsonWriteOptions);

            if (dryRun)
            {
                Console.WriteLine("[DRY RUN] Would write to: " + claudeJsonPath);
                Console.WriteLine(claudeJsonOutput);
                Console.WriteLine();
            }
            else
            {
                File.WriteAllText(claudeJsonPath, claudeJsonOutput);
                Console.WriteLine("Updated: " + claudeJsonPath);
                Console.WriteLine();
            }

            string claudeAgentsDirectory = Path.Combine(homeDirectory, ".claude", "agents");
            string agentFilePath = Path.Combine(claudeAgentsDirectory, "assistanthub.md");
            string agentContent =
                "---" + Environment.NewLine
                + "name: assistanthub" + Environment.NewLine
                + "description: AssistantHub administration and platform management agent for tenants, assistants, credentials, ingestion, endpoints, crawl, eval, history, and configuration." + Environment.NewLine
                + "allowedTools:" + Environment.NewLine
                + "  - mcp__assistanthub__*" + Environment.NewLine
                + "---" + Environment.NewLine
                + Environment.NewLine
                + "You are an AssistantHub management assistant. Use the AssistantHub MCP tools to inspect and manage the platform." + Environment.NewLine
                + Environment.NewLine
                + "Guidelines:" + Environment.NewLine
                + "- Confirm tenant, assistant, and endpoint identifiers before making destructive changes." + Environment.NewLine
                + "- Treat credential, Slack, endpoint, and configuration secrets carefully." + Environment.NewLine
                + "- Prefer list/get calls before create/update/delete when the current state is unclear." + Environment.NewLine;

            if (dryRun)
            {
                Console.WriteLine("[DRY RUN] Would create directory: " + claudeAgentsDirectory);
                Console.WriteLine("[DRY RUN] Would write to: " + agentFilePath);
                Console.WriteLine(agentContent);
                Console.WriteLine();
            }
            else
            {
                if (!Directory.Exists(claudeAgentsDirectory))
                {
                    Directory.CreateDirectory(claudeAgentsDirectory);
                    Console.WriteLine("Created directory: " + claudeAgentsDirectory);
                }

                File.WriteAllText(agentFilePath, agentContent);
                Console.WriteLine("Created: " + agentFilePath);
                Console.WriteLine();
            }

            Console.WriteLine("Cursor (.cursor/mcp.json):");
            Console.WriteLine("{");
            Console.WriteLine("  \"mcpServers\": {");
            Console.WriteLine("    \"assistanthub\": {");
            Console.WriteLine("      \"url\": \"" + mcpUrl + "\"");
            Console.WriteLine("    }");
            Console.WriteLine("  }");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine(dryRun ? "[DRY RUN] No files were modified." : "Installation complete. Restart the MCP client to pick up the new configuration.");
        }
    }
}
