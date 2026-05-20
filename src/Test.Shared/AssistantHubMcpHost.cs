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

    /// <summary>
    /// Spins up a real AssistantHub server plus MCP server for end-to-end MCP testing.
    /// </summary>
    public sealed class AssistantHubMcpHost : IAsyncDisposable
    {
        private static readonly HttpClient _ReadinessClient = CreateReadinessClient();
        private static readonly TimeSpan _ProcessStartupTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan _ReadinessPollInterval = TimeSpan.FromMilliseconds(500);

        private readonly McpProcessEnvironment _environment;
        private readonly DependencyStubServer _dependencyStubServer;

        private AssistantHubMcpHost(
            McpProcessEnvironment environment,
            DependencyStubServer dependencyStubServer,
            McpHttpClient client)
        {
            _environment = environment;
            _dependencyStubServer = dependencyStubServer;
            Client = client;
        }

        /// <summary>
        /// Connected MCP HTTP client.
        /// </summary>
        public McpHttpClient Client { get; }

        /// <summary>
        /// Artifact directory containing logs and generated configuration files.
        /// </summary>
        public string ArtifactDirectory => _environment.ArtifactDirectory;

        /// <summary>
        /// AssistantHub server endpoint.
        /// </summary>
        public string ServerEndpoint => _environment.ServerEndpoint;

        /// <summary>
        /// MCP HTTP endpoint.
        /// </summary>
        public string McpHttpEndpoint => _environment.McpHttpEndpoint;

        /// <summary>
        /// MCP TCP port.
        /// </summary>
        public int McpTcpPort => _environment.McpTcpPort;

        /// <summary>
        /// MCP WebSocket endpoint.
        /// </summary>
        public string McpWebSocketEndpoint => _environment.McpWebSocketEndpoint;

        /// <summary>
        /// Path to the built MCP server assembly.
        /// </summary>
        public string McpAssemblyPath => _environment.McpAssemblyPath;

        /// <summary>
        /// Create and start the full MCP test environment.
        /// </summary>
        public static async Task<AssistantHubMcpHost> CreateAsync(CancellationToken cancellationToken = default, string apiKey = "default")
        {
            McpProcessEnvironment environment = CreateEnvironment(apiKey);
            DependencyStubServer dependencyStubServer = new DependencyStubServer(environment.DependencyStubPort);
            McpHttpClient? client = null;

            try
            {
                dependencyStubServer.Start();
                WriteServerSettingsFile(environment);

                environment.ServerProcess = StartDotnetProcess(
                    displayName: "AssistantHub.Server",
                    assemblyPath: environment.ServerAssemblyPath,
                    workingDirectory: environment.ServerWorkingDirectory,
                    environmentVariables: null);

                await WaitForHttpSuccessAsync(
                    displayName: "AssistantHub.Server",
                    endpoint: environment.ServerEndpoint,
                    managedProcess: environment.ServerProcess,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                environment.McpProcess = StartDotnetProcess(
                    displayName: "AssistantHub.McpServer",
                    assemblyPath: environment.McpAssemblyPath,
                    workingDirectory: environment.McpWorkingDirectory,
                    environmentVariables: new Dictionary<string, string>
                    {
                        { "ASSISTANTHUB_ENDPOINT", environment.ServerEndpoint },
                        { "ASSISTANTHUB_API_KEY", environment.ApiKey },
                        { "MCP_HTTP_HOSTNAME", "127.0.0.1" },
                        { "MCP_HTTP_PORT", environment.McpHttpPort.ToString() },
                        { "MCP_TCP_ADDRESS", "127.0.0.1" },
                        { "MCP_TCP_PORT", environment.McpTcpPort.ToString() },
                        { "MCP_WS_HOSTNAME", "127.0.0.1" },
                        { "MCP_WS_PORT", environment.McpWebSocketPort.ToString() },
                        { "MCP_CONSOLE_LOGGING", "1" }
                    });

                client = await ConnectMcpClientWithRetryAsync(environment, cancellationToken).ConfigureAwait(false);
                return new AssistantHubMcpHost(environment, dependencyStubServer, client);
            }
            catch
            {
                if (client != null)
                    client.Dispose();

                dependencyStubServer.Dispose();
                await StopManagedProcessAsync(environment.McpProcess).ConfigureAwait(false);
                await StopManagedProcessAsync(environment.ServerProcess).ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            _dependencyStubServer.Dispose();
            await StopManagedProcessAsync(_environment.McpProcess).ConfigureAwait(false);
            await StopManagedProcessAsync(_environment.ServerProcess).ConfigureAwait(false);

            try
            {
                if (!ShouldKeepArtifacts() && Directory.Exists(_environment.ArtifactDirectory))
                    Directory.Delete(_environment.ArtifactDirectory, recursive: true);
            }
            catch
            {
            }
        }

        private static HttpClient CreateReadinessClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            return client;
        }

        private static McpProcessEnvironment CreateEnvironment(string apiKey)
        {
            HashSet<int> reservedPorts = new HashSet<int>();
            int dependencyStubPort = ReserveAvailablePort(reservedPorts);
            int serverPort = ReserveAvailablePort(reservedPorts);
            int mcpHttpPort = ReserveAvailablePort(reservedPorts);
            int mcpTcpPort = ReserveAvailablePort(reservedPorts);
            int mcpWebSocketPort = ReserveAvailablePort(reservedPorts);

            string artifactDirectory = Path.Combine(
                Path.GetTempPath(),
                "AssistantHub.Tests",
                "McpHost",
                DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Guid.NewGuid().ToString("N"));

            string serverWorkingDirectory = Path.Combine(artifactDirectory, "assistanthub-server");
            string mcpWorkingDirectory = Path.Combine(artifactDirectory, "assistanthub-mcp");

            Directory.CreateDirectory(serverWorkingDirectory);
            Directory.CreateDirectory(mcpWorkingDirectory);

            string configuration = GetCurrentBuildConfiguration();
            string targetFramework = GetCurrentTargetFrameworkMoniker();

            return new McpProcessEnvironment
            {
                ArtifactDirectory = artifactDirectory,
                ServerWorkingDirectory = serverWorkingDirectory,
                McpWorkingDirectory = mcpWorkingDirectory,
                DependencyStubPort = dependencyStubPort,
                ServerPort = serverPort,
                McpHttpPort = mcpHttpPort,
                McpTcpPort = mcpTcpPort,
                McpWebSocketPort = mcpWebSocketPort,
                ApiKey = apiKey,
                ServerAssemblyPath = ResolveBuildOutput(
                    projectRoot: "src",
                    projectName: "AssistantHub.Server",
                    configuration: configuration,
                    targetFramework: targetFramework,
                    assemblyFileName: "AssistantHub.Server.dll"),
                McpAssemblyPath = ResolveBuildOutput(
                    projectRoot: "src",
                    projectName: "AssistantHub.McpServer",
                    configuration: configuration,
                    targetFramework: targetFramework,
                    assemblyFileName: "AssistantHub.McpServer.dll")
            };
        }

        private static void WriteServerSettingsFile(McpProcessEnvironment environment)
        {
            string dependencyEndpoint = environment.DependencyStubEndpoint;
            string serverLogDirectory = Path.Combine(environment.ServerWorkingDirectory, "logs");
            string processingLogDirectory = Path.Combine(environment.ServerWorkingDirectory, "processing-logs");

            Directory.CreateDirectory(serverLogDirectory);
            Directory.CreateDirectory(processingLogDirectory);

            AssistantHubSettings settings = new AssistantHubSettings
            {
                Webserver = new WebserverSettings
                {
                    Hostname = "127.0.0.1",
                    Port = environment.ServerPort,
                    Ssl = false
                },
                Database = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Sqlite,
                    Filename = Path.Combine(environment.ServerWorkingDirectory, "assistanthub.db")
                },
                S3 = new S3Settings
                {
                    Region = "USWest1",
                    BucketName = "default",
                    AccessKey = "default",
                    SecretKey = "default",
                    EndpointUrl = dependencyEndpoint,
                    UseSsl = false,
                    BaseUrl = dependencyEndpoint,
                    DashboardUrl = String.Empty
                },
                DocumentAtom = new DocumentAtomSettings
                {
                    Endpoint = dependencyEndpoint,
                    AccessKey = "default",
                    DashboardUrl = String.Empty
                },
                Chunking = new ChunkingSettings
                {
                    Endpoint = dependencyEndpoint,
                    AccessKey = "default",
                    EndpointId = "default",
                    DashboardUrl = String.Empty
                },
                Embeddings = new EmbeddingsSettings
                {
                    Endpoint = dependencyEndpoint,
                    AccessKey = "default",
                    EndpointId = "default"
                },
                Inference = new InferenceSettings
                {
                    Provider = InferenceProviderEnum.Ollama,
                    Endpoint = dependencyEndpoint,
                    ApiKey = "default",
                    DefaultModel = "stub-model",
                    DashboardUrl = String.Empty
                },
                RecallDb = new RecallDbSettings
                {
                    Endpoint = dependencyEndpoint,
                    AccessKey = "default",
                    DashboardUrl = String.Empty
                },
                Logging = new LoggingSettings
                {
                    ConsoleLogging = false,
                    EnableColors = false,
                    FileLogging = true,
                    LogDirectory = serverLogDirectory,
                    LogFilename = "assistanthub-server.log",
                    IncludeDateInFilename = false,
                    MinimumSeverity = 0,
                    Servers = new List<SyslogServerSettings>()
                },
                ProcessingLog = new ProcessingLogSettings
                {
                    Directory = processingLogDirectory,
                    RetentionDays = 7
                },
                DefaultTenant = new DefaultTenantSettings
                {
                    Name = "MCP Test Tenant",
                    AdminEmail = "admin@test.local",
                    AdminPassword = "testpassword123"
                },
                RequestHistory = new RequestHistorySettings
                {
                    Enabled = true,
                    RetentionDays = 7,
                    PurgeIntervalMinutes = 60,
                    MaxRequestBodyBytes = 65536,
                    MaxResponseBodyBytes = 65536,
                    CaptureHeaders = true,
                    CaptureBodies = true,
                    IncludeUnauthenticatedAssistantTraffic = true
                }
            };

            settings.AdminApiKeys = new List<string> { "assistanthubadmin" };

            string settingsJson = Serializer.SerializeJson(settings, true) ?? "{}";
            File.WriteAllText(Path.Combine(environment.ServerWorkingDirectory, "assistanthub.json"), settingsJson, Encoding.UTF8);
        }

        private static ManagedProcess StartDotnetProcess(
            string displayName,
            string assemblyPath,
            string workingDirectory,
            IReadOnlyDictionary<string, string>? environmentVariables)
        {
            Directory.CreateDirectory(workingDirectory);

            ProcessLogCapture capture = new ProcessLogCapture(
                Path.Combine(
                    workingDirectory,
                    displayName.Replace('.', '_') + ".log"));

            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add(assemblyPath);

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
                }
            }

            Process process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, args) => capture.Append("stdout", args.Data);
            process.ErrorDataReceived += (_, args) => capture.Append("stderr", args.Data);

            if (!process.Start())
            {
                capture.Dispose();
                throw new InvalidOperationException("Unable to start " + displayName);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return new ManagedProcess(displayName, process, capture);
        }

        private static async Task<McpHttpClient> ConnectMcpClientWithRetryAsync(
            McpProcessEnvironment environment,
            CancellationToken cancellationToken)
        {
            DateTime timeoutAt = DateTime.UtcNow.Add(_ProcessStartupTimeout);
            Exception? lastException = null;

            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureProcessIsRunning(environment.McpProcess);

                McpHttpClient client = new McpHttpClient();
                ConfigureMcpHttpClient(client, 30000);

                try
                {
                    bool connected = await client.ConnectAsync(
                        environment.McpHttpEndpoint,
                        "/rpc",
                        "/events",
                        cancellationToken).ConfigureAwait(false);

                    if (connected)
                        return client;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                client.Dispose();
                await Task.Delay(_ReadinessPollInterval, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                BuildProcessFailureMessage(
                    "Timed out waiting for AssistantHub.McpServer to accept MCP HTTP connections at " + environment.McpHttpEndpoint,
                    environment,
                    environment.McpProcess,
                    lastException));
        }

        private static async Task WaitForHttpSuccessAsync(
            string displayName,
            string endpoint,
            ManagedProcess? managedProcess,
            CancellationToken cancellationToken)
        {
            DateTime timeoutAt = DateTime.UtcNow.Add(_ProcessStartupTimeout);
            Exception? lastException = null;

            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (managedProcess != null)
                    EnsureProcessIsRunning(managedProcess);

                try
                {
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    using HttpResponseMessage response = await _ReadinessClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                        return;

                    lastException = new InvalidOperationException(
                        displayName + " returned HTTP " + (int)response.StatusCode + " from " + endpoint);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(_ReadinessPollInterval, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                BuildProcessFailureMessage(
                    "Timed out waiting for " + displayName + " at " + endpoint,
                    null,
                    managedProcess,
                    lastException));
        }

        private static bool ShouldKeepArtifacts()
        {
            string? value = Environment.GetEnvironmentVariable("ASSISTANTHUB_TEST_KEEP_ARTIFACTS");
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static void ConfigureMcpHttpClient(McpHttpClient client, int timeoutMs)
        {
            client.RequestTimeoutMs = timeoutMs;

            FieldInfo? httpClientField = typeof(McpHttpClient).GetField(
                "_HttpClient",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (httpClientField?.GetValue(client) is HttpClient httpClient)
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        }

        private static void EnsureProcessIsRunning(ManagedProcess? managedProcess)
        {
            if (managedProcess == null)
                throw new InvalidOperationException("Managed process has not been started");

            if (managedProcess.Process.HasExited)
            {
                string message = managedProcess.DisplayName
                    + " exited with code "
                    + managedProcess.Process.ExitCode
                    + Environment.NewLine
                    + "Log file: "
                    + managedProcess.Capture.LogFilePath
                    + Environment.NewLine
                    + managedProcess.Capture.GetRecentOutput();

                throw new InvalidOperationException(message.Trim());
            }
        }

        private static async Task StopManagedProcessAsync(ManagedProcess? managedProcess)
        {
            if (managedProcess == null)
                return;

            try
            {
                if (!managedProcess.Process.HasExited)
                {
                    managedProcess.Process.Kill(entireProcessTree: true);

                    try
                    {
                        using CancellationTokenSource waitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await managedProcess.Process.WaitForExitAsync(waitTimeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            finally
            {
                managedProcess.Capture.Dispose();
                managedProcess.Process.Dispose();
            }
        }

        private static string ResolveBuildOutput(
            string projectRoot,
            string projectName,
            string configuration,
            string targetFramework,
            string assemblyFileName)
        {
            string repositoryRoot = ResolveRepositoryRoot();
            string assemblyPath = Path.Combine(
                repositoryRoot,
                projectRoot,
                projectName,
                "bin",
                configuration,
                targetFramework,
                assemblyFileName);

            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Unable to locate build output for " + projectName, assemblyPath);

            return assemblyPath;
        }

        private static string ResolveRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(AppContext.BaseDirectory));

            while (directory != null)
            {
                bool hasSrc = Directory.Exists(Path.Combine(directory.FullName, "src"));
                bool hasSdk = Directory.Exists(Path.Combine(directory.FullName, "sdk"));
                if (hasSrc && hasSdk)
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to resolve repository root from " + AppContext.BaseDirectory);
        }

        private static int ReserveAvailablePort(HashSet<int> reservedPorts)
        {
            while (true)
            {
                int candidate;
                using (TcpListener listener = new TcpListener(IPAddress.Loopback, 0))
                {
                    listener.Start();
                    candidate = ((IPEndPoint)listener.LocalEndpoint).Port;
                }

                if (reservedPorts.Add(candidate))
                    return candidate;
            }
        }

        private static string GetCurrentBuildConfiguration()
        {
            string baseDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            DirectoryInfo frameworkDirectory = new DirectoryInfo(baseDirectory);
            DirectoryInfo? configurationDirectory = frameworkDirectory.Parent;

            if (configurationDirectory == null)
                throw new InvalidOperationException("Unable to determine build configuration from " + AppContext.BaseDirectory);

            return configurationDirectory.Name;
        }

        private static string GetCurrentTargetFrameworkMoniker()
        {
            string baseDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            string candidate = new DirectoryInfo(baseDirectory).Name;

            if (candidate.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return candidate;

            if (!string.IsNullOrEmpty(AppContext.TargetFrameworkName))
            {
                FrameworkName frameworkName = new FrameworkName(AppContext.TargetFrameworkName);
                return "net" + frameworkName.Version.Major + "." + frameworkName.Version.Minor;
            }

            throw new InvalidOperationException("Unable to determine target framework from " + AppContext.BaseDirectory);
        }

        private static string BuildProcessFailureMessage(
            string message,
            McpProcessEnvironment? environment,
            ManagedProcess? managedProcess,
            Exception? exception = null)
        {
            List<string> parts = new List<string> { message };

            if (environment != null)
                parts.Add("Artifacts: " + environment.ArtifactDirectory);

            if (managedProcess != null)
            {
                parts.Add("Log file: " + managedProcess.Capture.LogFilePath);
                string recentOutput = managedProcess.Capture.GetRecentOutput();
                if (!string.IsNullOrEmpty(recentOutput))
                {
                    parts.Add("Recent output:");
                    parts.Add(recentOutput);
                }
            }

            if (exception != null && !string.IsNullOrEmpty(exception.Message))
                parts.Add("Last error: " + exception.Message);

            return string.Join(Environment.NewLine, parts);
        }

        private sealed class McpProcessEnvironment
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

        private sealed class ManagedProcess
        {
            public ManagedProcess(string displayName, Process process, ProcessLogCapture capture)
            {
                DisplayName = displayName;
                Process = process;
                Capture = capture;
            }

            public string DisplayName { get; }
            public Process Process { get; }
            public ProcessLogCapture Capture { get; }
        }

        private sealed class ProcessLogCapture : IDisposable
        {
            private readonly object _sync = new object();
            private readonly Queue<string> _recentLines = new Queue<string>();
            private readonly StreamWriter _writer;

            public ProcessLogCapture(string logFilePath)
            {
                LogFilePath = logFilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
                _writer = new StreamWriter(
                    new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true
                };
            }

            public string LogFilePath { get; }

            public void Append(string streamName, string? line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    return;

                string entry = "[" + DateTime.UtcNow.ToString("O") + "] " + streamName + ": " + line;

                lock (_sync)
                {
                    _writer.WriteLine(entry);
                    _recentLines.Enqueue(entry);

                    while (_recentLines.Count > 80)
                    {
                        _recentLines.Dequeue();
                    }
                }
            }

            public string GetRecentOutput()
            {
                lock (_sync)
                {
                    return string.Join(Environment.NewLine, _recentLines);
                }
            }

            public void Dispose()
            {
                lock (_sync)
                {
                    _writer.Dispose();
                }
            }
        }

        private sealed class DependencyStubServer : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();
            private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
            private Task? _listenerTask;

            public DependencyStubServer(int port)
            {
                Port = port;
                BaseUrl = "http://127.0.0.1:" + port + "/";
                _listener.Prefixes.Add(BaseUrl);
            }

            public int Port { get; }

            public string BaseUrl { get; }

            public void Start()
            {
                _listener.Start();
                _listenerTask = Task.Run(() => ListenAsync(_tokenSource.Token));
            }

            public void Dispose()
            {
                _tokenSource.Cancel();

                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch
                {
                }

                try
                {
                    _listenerTask?.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                }

                _tokenSource.Dispose();
            }

            private async Task ListenAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !_listener.IsListening)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !_listener.IsListening)
                    {
                        break;
                    }

                    _ = Task.Run(() => HandleAsync(context), cancellationToken);
                }
            }

            private static async Task HandleAsync(HttpListenerContext context)
            {
                try
                {
                    string path = context.Request.Url?.AbsolutePath ?? "/";
                    string responseBody = path.Equals("/api/tags", StringComparison.OrdinalIgnoreCase)
                        ? "{\"models\":[]}"
                        : "{}";

                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";

                    if (context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.ContentLength64 = 0;
                        context.Response.Close();
                        return;
                    }

                    byte[] data = Encoding.UTF8.GetBytes(responseBody);
                    context.Response.ContentLength64 = data.Length;
                    await context.Response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                }
                catch
                {
                }
                finally
                {
                    try
                    {
                        context.Response.OutputStream.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
