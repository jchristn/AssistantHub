namespace AssistantHub.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Loader;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using AssistantHub.Core.Telemetry;
    using AssistantHub.Server.Handlers;
    using AssistantHub.Server.OpenApi;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// AssistantHub server.
    /// </summary>
    public static class AssistantHubServer
    {
        #region Private-Members

        private static string _Header = "[AssistantHubServer] ";
        private static AssistantHubSettings _Settings = null;
        private static LoggingModule _Logging = null;
        private static DatabaseDriverBase _Database = null;
        private static AuthenticationService _Authentication = null;
        private static IObjectStorageService _Storage = null;
        private static IngestionService _Ingestion = null;
        private static RetrievalService _Retrieval = null;
        private static InferenceService _Inference = null;
        private static IChunkingService _ChunkingService = null;
        private static IEmbeddingEndpointService _EmbeddingEndpointService = null;
        private static IInferenceEndpointService _InferenceEndpointService = null;
        private static IVectorStoreService _VectorStore = null;
        private static IInvertedIndexService _InvertedIndex = null;
        private static ProcessingLogService _ProcessingLog = null;
        private static WatsonWebserver.Webserver _Server = null;
        private static RequestHistoryCaptureService _RequestHistoryCapture = null;
        private static EndpointHealthCheckService _HealthCheckService = null;
        private static IExternalServiceHealthService _ExternalServiceHealth = null;
        private static CrawlSchedulerService _CrawlScheduler = null;
        private static CrawlOperationCleanupService _CrawlOperationCleanup = null;
        private static ISlackAssistantConnectionManager _SlackConnectionManager = null;
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static Radiant.RadiantHost _TelemetryHost = null;
        private static bool _ShuttingDown = false;

        /// <summary>
        /// The endpoint health check service instance, accessible by handlers.
        /// </summary>
        public static EndpointHealthCheckService HealthCheckService => _HealthCheckService;

        /// <summary>
        /// The Slack assistant connection manager instance, accessible by handlers.
        /// </summary>
        public static ISlackAssistantConnectionManager SlackConnectionManager => _SlackConnectionManager;

        #endregion

        #region Entry-Point

        /// <summary>
        /// Entry point.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Welcome();
            if (!InitializeSettings()) return;
            InitializeLogging();
            _TelemetryHost = TelemetryBootstrap.Start(_Settings.Telemetry, "assistanthub-server", _Logging);
            await InitializeDatabaseAsync();
            await InitializeFirstRunAsync();
            await BackfillAssistantPerformanceTelemetryAsync();
            InitializeServices();
            await ValidateConnectivityAsync();
            await StartHealthCheckServiceAsync();
            StartProcessingLogCleanup();
            StartChatHistoryCleanup();
            StartRequestHistoryCleanup();
            await StartCrawlServicesAsync();
            await StartSlackServicesAsync();
            InitializeWebserver();

            _Logging.Info(_Header + "server started on " + _Settings.Webserver.Hostname + ":" + _Settings.Webserver.Port);

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            AssemblyLoadContext.Default.Unloading += (ctx) => waitHandle.Set();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                if (!_ShuttingDown)
                {
                    Console.WriteLine();
                    Console.WriteLine("Shutting down");
                    _TokenSource.Cancel();
                    _ShuttingDown = true;
                    waitHandle.Set();
                }
            };

            bool waitHandleSignal = false;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            }
            while (!waitHandleSignal);

            if (_SlackConnectionManager != null)
                await _SlackConnectionManager.StopAsync().ConfigureAwait(false);

            if (_TelemetryHost != null)
            {
                try
                {
                    _TelemetryHost.ForceFlush(5000);
                    _TelemetryHost.Dispose();
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "telemetry shutdown error: " + e.Message);
                }
            }

            _Logging.Info(_Header + "stopping at " + DateTime.UtcNow);
        }

        #endregion

        #region Private-Initialization

        private static void Welcome()
        {
            Console.WriteLine(
                Environment.NewLine +
                Constants.Logo +
                Environment.NewLine +
                Constants.ProductName + " v" + Constants.ProductVersion +
                Environment.NewLine);
        }

        private static bool InitializeSettings()
        {
            if (!File.Exists(Constants.SettingsFile))
            {
                AssistantHubSettings defaultSettings = new AssistantHubSettings();
                string json = Serializer.SerializeJson(defaultSettings, true);
                File.WriteAllText(Constants.SettingsFile, json);

                Console.WriteLine("NOTICE:");
                Console.WriteLine("Modify the assistanthub.json settings file to set values for API endpoints including S3, services, and LLM inference.");
                Console.WriteLine("");
                return false;
            }

            string settingsJson = File.ReadAllText(Constants.SettingsFile);
            _Settings = Serializer.DeserializeJson<AssistantHubSettings>(settingsJson);
            if (!ValidateSettings(_Settings))
                return false;

            LocalDiskCrawlPolicy.Configure(_Settings.Crawl?.AllowedLocalPaths);
            Console.WriteLine("Settings loaded from " + Constants.SettingsFile);
            return true;
        }

        private static bool ValidateSettings(AssistantHubSettings settings)
        {
            if (settings == null)
            {
                Console.WriteLine("Settings file could not be parsed.");
                return false;
            }

            List<string> errors = new List<string>();
            if (settings.Verbex != null)
                errors.AddRange(settings.Verbex.Validate());
            if (settings.ExternalSearch != null)
                errors.AddRange(ExternalSearchConfigurationHelper.Validate(settings.ExternalSearch));

            if (errors.Count < 1)
                return true;

            Console.WriteLine("Invalid settings:");
            foreach (string error in errors)
                Console.WriteLine("- " + error);

            return false;
        }

        private static void InitializeLogging()
        {
            Console.WriteLine("Initializing logging");

            List<SyslogServer> syslogServers = new List<SyslogServer>();

            if (_Settings.Logging.Servers != null && _Settings.Logging.Servers.Count > 0)
            {
                foreach (SyslogServerSettings server in _Settings.Logging.Servers)
                {
                    syslogServers.Add(new SyslogServer(server.Hostname, server.Port));
                    Console.WriteLine("| syslog://" + server.Hostname + ":" + server.Port);
                }
            }

            if (syslogServers.Count > 0)
                _Logging = new LoggingModule(syslogServers);
            else
                _Logging = new LoggingModule();

            _Logging.Settings.MinimumSeverity = (Severity)_Settings.Logging.MinimumSeverity;
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;

            if (_Settings.Logging.FileLogging
                && !String.IsNullOrEmpty(_Settings.Logging.LogDirectory)
                && !String.IsNullOrEmpty(_Settings.Logging.LogFilename))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory))
                    Directory.CreateDirectory(_Settings.Logging.LogDirectory);

                _Logging.Settings.LogFilename = Path.Combine(_Settings.Logging.LogDirectory, _Settings.Logging.LogFilename);

                if (_Settings.Logging.IncludeDateInFilename)
                    _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
                else
                    _Logging.Settings.FileLogging = FileLoggingMode.SingleLogFile;
            }

            _Logging.Info(_Header + "logging initialized");
            LogExternalSearchStatus();
        }

        private static void LogExternalSearchStatus()
        {
            ExternalSearchConfigurationStatus status = ExternalSearchConfigurationHelper.GetStatus(_Settings.ExternalSearch);
            if (!status.Enabled)
            {
                _Logging.Info(_Header + "external search disabled");
                return;
            }

            if (status.ConfiguredProviders > 0)
            {
                _Logging.Info(_Header + "external search enabled with " + status.ConfiguredProviders + " configured provider(s)");
                return;
            }

            _Logging.Warn(_Header + "external search enabled but misconfigured; no configured Tavily providers are available");
        }

        private static async Task InitializeDatabaseAsync()
        {
            _Database = await DatabaseDriverFactory.CreateAndInitializeAsync(_Settings.Database, _Logging).ConfigureAwait(false);
            _Logging.Info(_Header + "database initialized (" + _Settings.Database.Type.ToString() + ")");
        }

        private static async Task InitializeFirstRunAsync()
        {
            long tenantCount = await _Database.Tenant.GetCountAsync().ConfigureAwait(false);
            if (tenantCount > 0)
            {
                _Logging.Info(_Header + "existing tenants found, skipping first-run setup");
                return;
            }

            _Logging.Info(_Header + "no tenants found, running first-time setup");

            // Step 1: Create default tenant
            TenantMetadata defaultTenant = new TenantMetadata();
            defaultTenant.Id = Constants.DefaultTenantId;
            defaultTenant.Name = _Settings.DefaultTenant?.Name ?? Constants.DefaultTenantName;
            defaultTenant.Active = true;
            defaultTenant.IsProtected = true;
            defaultTenant.CreatedUtc = DateTime.UtcNow;
            defaultTenant.LastUpdateUtc = DateTime.UtcNow;
            defaultTenant = await _Database.Tenant.CreateAsync(defaultTenant).ConfigureAwait(false);

            _Logging.Info(_Header + "default tenant created: " + defaultTenant.Id);

            // Step 2: Create default admin user
            string adminEmail = _Settings.DefaultTenant?.AdminEmail ?? Constants.DefaultAdminEmail;
            string adminPassword = _Settings.DefaultTenant?.AdminPassword ?? Constants.DefaultAdminPassword;

            UserMaster admin = new UserMaster();
            admin.Id = IdGenerator.NewUserId();
            admin.TenantId = Constants.DefaultTenantId;
            admin.Email = adminEmail;
            admin.FirstName = Constants.DefaultAdminFirstName;
            admin.LastName = Constants.DefaultAdminLastName;
            admin.IsAdmin = true;
            admin.IsTenantAdmin = true;
            admin.Active = true;
            admin.IsProtected = true;
            admin.SetPassword(adminPassword);
            admin = await _Database.User.CreateAsync(admin).ConfigureAwait(false);

            // Step 3: Create default credential
            Credential credential = new Credential();
            credential.Id = IdGenerator.NewCredentialId();
            credential.TenantId = Constants.DefaultTenantId;
            credential.UserId = admin.Id;
            credential.Name = "Default admin credential";
            credential.BearerToken = "default";
            credential.Active = true;
            credential.IsProtected = true;
            credential = await _Database.Credential.CreateAsync(credential).ConfigureAwait(false);

            _Logging.Info(_Header + "default admin created:");
            _Logging.Info(_Header + "  tenant: " + defaultTenant.Id + " (" + defaultTenant.Name + ")");
            _Logging.Info(_Header + "  email: " + admin.Email);
            // The password and bearer token are shown once on the console below, never written to the log file.
            _Logging.Info(_Header + "  password and bearer token: shown on the console at first start only");

            Console.WriteLine("");
            Console.WriteLine("*** Default tenant credentials ***");
            Console.WriteLine("Tenant ID   : " + defaultTenant.Id);
            Console.WriteLine("Tenant Name : " + defaultTenant.Name);
            Console.WriteLine("Admin Email : " + admin.Email);
            Console.WriteLine("Password    : " + adminPassword);
            Console.WriteLine("Bearer Token: " + credential.BearerToken);
            Console.WriteLine("");

            // Step 4: Create default ingestion rule
            IngestionRule defaultRule = new IngestionRule();
            defaultRule.Id = IdGenerator.NewIngestionRuleId();
            defaultRule.TenantId = Constants.DefaultTenantId;
            defaultRule.Name = "Default";
            defaultRule.Description = "Default ingestion rule";
            defaultRule.Bucket = "default";
            defaultRule.CollectionName = "default";
            defaultRule.CollectionId = "default";
            defaultRule.VerbexIndexId = _Settings.Verbex.DefaultIndexId;
            defaultRule.Chunking = new IngestionChunkingConfig();
            defaultRule.Embedding = new IngestionEmbeddingConfig
            {
                EmbeddingEndpointId = "default",
                L2Normalization = true
            };
            defaultRule.CreatedUtc = DateTime.UtcNow;
            defaultRule.LastUpdateUtc = DateTime.UtcNow;
            defaultRule = await _Database.IngestionRule.CreateAsync(defaultRule).ConfigureAwait(false);

            _Logging.Info(_Header + "default ingestion rule created: " + defaultRule.Id);

            // Step 5: Provision RecallDB tenant and collection
            try
            {
                IVectorStoreService vectorStore = new RecallDbVectorStoreService(_Settings.RecallDb, _Logging);

                object tenantBody = new { Id = Constants.DefaultTenantId, Name = defaultTenant.Name };
                using (HttpResponseMessage tenantResp = await vectorStore.SendAsync(
                    System.Net.Http.HttpMethod.Put,
                    "/v1.0/tenants",
                    JsonSerializer.Serialize(tenantBody)).ConfigureAwait(false))
                {
                    if (tenantResp.IsSuccessStatusCode)
                        _Logging.Info(_Header + "default RecallDB tenant created");
                    else
                        _Logging.Warn(_Header + "failed to create default RecallDB tenant: HTTP " + (int)tenantResp.StatusCode);
                }

                object collBody = new { Id = "default", Name = "default" };
                using (HttpResponseMessage collResp = await vectorStore.SendAsync(
                    System.Net.Http.HttpMethod.Put,
                    "/v1.0/tenants/" + Constants.DefaultTenantId + "/collections",
                    JsonSerializer.Serialize(collBody)).ConfigureAwait(false))
                {
                    if (collResp.IsSuccessStatusCode)
                        _Logging.Info(_Header + "default RecallDB collection created");
                    else
                        _Logging.Warn(_Header + "failed to create default RecallDB collection: HTTP " + (int)collResp.StatusCode);
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "could not provision RecallDB: " + e.Message);
            }

            // Step 6: Provision Verbex tenant and default index
            IInvertedIndexService invertedIndex = new VerbexInvertedIndexService(_Settings.Verbex, _Logging);
            await EnsureFirstRunVerbexAsync(invertedIndex, _Settings.Verbex, _Logging).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensure the default Verbex tenant and index during first-run initialization.
        /// </summary>
        /// <param name="invertedIndex">Inverted index service implementation.</param>
        /// <param name="settings">Verbex settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <returns>True when the default index was ensured.</returns>
        public static async Task<bool> EnsureFirstRunVerbexAsync(IInvertedIndexService invertedIndex, VerbexSettings settings, LoggingModule logging)
        {
            if (invertedIndex == null) throw new ArgumentNullException(nameof(invertedIndex));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            try
            {
                using (HttpResponseMessage tenantResp = await invertedIndex.SendAsync(
                    System.Net.Http.HttpMethod.Get,
                    "/v1.0/tenants/" + Constants.DefaultTenantId).ConfigureAwait(false))
                {
                    if (tenantResp.IsSuccessStatusCode)
                        logging.Info(_Header + "default Verbex tenant found");
                    else
                        logging.Warn(_Header + "default Verbex tenant check returned HTTP " + (int)tenantResp.StatusCode);
                }

                string indexId = settings.DefaultIndexId;
                object indexBody = new
                {
                    Identifier = indexId,
                    TenantId = Constants.DefaultTenantId,
                    Name = indexId,
                    Description = "Default AssistantHub text search index"
                };

                using (HttpResponseMessage indexResp = await invertedIndex.SendAsync(
                    System.Net.Http.HttpMethod.Post,
                    "/v1.0/indices",
                    JsonSerializer.Serialize(indexBody)).ConfigureAwait(false))
                {
                    if (indexResp.IsSuccessStatusCode || indexResp.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        logging.Info(_Header + "default Verbex index ensured: " + indexId);
                        return true;
                    }

                    logging.Warn(_Header + "failed to create default Verbex index: HTTP " + (int)indexResp.StatusCode);
                    return false;
                }
            }
            catch (Exception e)
            {
                logging.Warn(_Header + "could not provision Verbex: " + e.Message);
                return false;
            }
        }

        private static async Task BackfillAssistantPerformanceTelemetryAsync()
        {
            try
            {
                AssistantPerformanceTelemetryBackfillService backfillService = new AssistantPerformanceTelemetryBackfillService(_Database, _Logging);
                int inserted = await backfillService.BackfillMissingEventsAsync(_TokenSource.Token).ConfigureAwait(false);
                if (inserted < 1)
                    _Logging.Debug(_Header + "assistant performance telemetry backfill found no missing event rows");
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "assistant performance telemetry backfill skipped: " + e.Message);
            }
        }

        private static void InitializeServices()
        {
            _Authentication = new AuthenticationService(_Database, _Logging, _Settings);

            _ProcessingLog = new ProcessingLogService(_Settings.ProcessingLog, _Logging);

            try
            {
                _Storage = new StorageService(_Settings.S3, _Logging);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "S3 storage not configured, document upload will be unavailable: " + e.Message);
            }

            if (_Storage != null)
            {
                _Ingestion = new IngestionService(_Database, _Storage, _Settings.DocumentAtom, _Settings.Chunking, _Settings.RecallDb, _Settings.Verbex, _Logging, _ProcessingLog);
            }
            else
            {
                _Logging.Warn(_Header + "ingestion service unavailable (no storage configured)");
            }

            _Inference = new InferenceService(_Settings.Inference, _Logging);
            _ChunkingService = new PartioChunkingService(_Settings.Chunking, _Logging);
            _EmbeddingEndpointService = new PartioEmbeddingEndpointService(_Settings.Chunking, _Logging);
            _InferenceEndpointService = new PartioInferenceEndpointService(_Settings.Chunking, _Logging);
            _VectorStore = new RecallDbVectorStoreService(_Settings.RecallDb, _Logging);
            _Retrieval = new RetrievalService(_Settings.Chunking, _Settings.RecallDb, _Logging, _VectorStore, _ChunkingService);
            _InvertedIndex = new VerbexInvertedIndexService(_Settings.Verbex, _Logging);
            _ExternalServiceHealth = new ExternalServiceHealthService(_Settings, _Logging);
            _RequestHistoryCapture = new RequestHistoryCaptureService(_Database, _Settings, _Logging);
            _SlackConnectionManager = new SlackAssistantConnectionManager(_Database, _Logging, _Settings, _Retrieval, _Inference, _Storage);
            _Logging.Info(_Header + "services initialized");
        }

        private static async Task ValidateConnectivityAsync()
        {
            bool allSucceeded = await _ExternalServiceHealth.ValidateConnectivityAsync().ConfigureAwait(false);

            if (!allSucceeded)
            {
                _Logging.Warn(_Header + "one or more subordinate services are unreachable, aborting startup");
                throw new Exception("One or more subordinate services are unreachable. Check logs for details.");
            }

            _Logging.Info(_Header + "all subordinate services are reachable");
        }

        private static void StartProcessingLogCleanup()
        {
            if (_ProcessingLog == null) return;

            _ = Task.Run(async () =>
            {
                while (!_TokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), _TokenSource.Token).ConfigureAwait(false);
                        await _ProcessingLog.CleanupOldLogsAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "processing log cleanup error: " + e.Message);
                    }
                }
            });

            _Logging.Info(_Header + "processing log cleanup loop started");
        }

        private static void StartChatHistoryCleanup()
        {
            _ = Task.Run(async () =>
            {
                while (!_TokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), _TokenSource.Token).ConfigureAwait(false);
                        await _Database.ChatHistory.DeleteExpiredAsync(_Settings.ChatHistory.RetentionDays, _TokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "chat history cleanup error: " + e.Message);
                    }
                }
            });

            _Logging.Info(_Header + "chat history cleanup loop started");
        }

        private static void StartRequestHistoryCleanup()
        {
            _ = Task.Run(async () =>
            {
                while (!_TokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(_Settings.RequestHistory.PurgeIntervalMinutes), _TokenSource.Token).ConfigureAwait(false);
                        if (_Settings.RequestHistory.Enabled)
                        {
                            await _Database.RequestHistory.DeleteExpiredAsync(_Settings.RequestHistory.RetentionDays, _TokenSource.Token).ConfigureAwait(false);
                            if (_Database.AssistantToolCall != null)
                                await _Database.AssistantToolCall.DeleteExpiredAsync(_Settings.RequestHistory.RetentionDays, _TokenSource.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "request history cleanup error: " + e.Message);
                    }
                }
            });

            _Logging.Info(_Header + "request history cleanup loop started");
        }

        private static async Task StartCrawlServicesAsync()
        {
            try
            {
                _CrawlScheduler = new CrawlSchedulerService(_Database, _Logging, _Settings, _Ingestion, _Storage, _ProcessingLog);
                _CrawlOperationCleanup = new CrawlOperationCleanupService(_Database, _Logging, _Settings);
                await _CrawlScheduler.StartAsync(_TokenSource.Token).ConfigureAwait(false);
                await _CrawlOperationCleanup.StartAsync(_TokenSource.Token).ConfigureAwait(false);
                _Logging.Info(_Header + "crawl services started");
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "crawl services failed to start: " + e.Message);
            }
        }

        private static async Task StartHealthCheckServiceAsync()
        {
            try
            {
                _HealthCheckService = new EndpointHealthCheckService(_Settings, _Logging);
                await _HealthCheckService.StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "health check service failed to start: " + e.Message);
            }
        }

        private static async Task StartSlackServicesAsync()
        {
            try
            {
                if (_SlackConnectionManager == null) return;
                await _SlackConnectionManager.StartAsync(_TokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "Slack services failed to start: " + e.Message);
            }
        }

        private static void InitializeWebserver()
        {
            WatsonWebserver.Core.WebserverSettings wsSettings = new WatsonWebserver.Core.WebserverSettings(_Settings.Webserver.Hostname, _Settings.Webserver.Port, _Settings.Webserver.Ssl);
            _Server = new WatsonWebserver.Webserver(wsSettings, DefaultRoute);

            // Instantiate handlers
            AuthenticationHandler authHandler = new AuthenticationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            RootHandler rootHandler = new RootHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AuthenticateHandler authenticateHandler = new AuthenticateHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            ChatHandler chatHandler = new ChatHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            OpenApiDocumentService openApiDocumentService = new OpenApiDocumentService(() => _Server);
            OpenApiHandler openApiHandler = new OpenApiHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, openApiDocumentService);
            TenantHandler tenantHandler = new TenantHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            UserHandler userHandler = new UserHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CredentialHandler credentialHandler = new CredentialHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CollectionHandler collectionHandler = new CollectionHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _VectorStore);
            IndexHandler indexHandler = new IndexHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _InvertedIndex);
            BucketHandler bucketHandler = new BucketHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantHandler assistantHandler = new AssistantHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantSettingsHandler assistantSettingsHandler = new AssistantSettingsHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantRetrieveHandler assistantRetrieveHandler = new AssistantRetrieveHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantAnalyticsHandler assistantAnalyticsHandler = new AssistantAnalyticsHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantToolCallHandler assistantToolCallHandler = new AssistantToolCallHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            DocumentHandler documentHandler = new DocumentHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog);
            IngestionRuleHandler ingestionRuleHandler = new IngestionRuleHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            EmbeddingEndpointHandler embeddingEndpointHandler = new EmbeddingEndpointHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _EmbeddingEndpointService);
            CompletionEndpointHandler completionEndpointHandler = new CompletionEndpointHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _InferenceEndpointService);
            FeedbackHandler feedbackHandler = new FeedbackHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            HistoryHandler historyHandler = new HistoryHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            RequestHistoryHandler requestHistoryHandler = new RequestHistoryHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            InferenceHandler inferenceHandler = new InferenceHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            ConfigurationHandler configurationHandler = new ConfigurationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            RerankerHandler rerankerHandler = new RerankerHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CrawlPlanHandler crawlPlanHandler = new CrawlPlanHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog, _CrawlScheduler);
            CrawlOperationHandler crawlOperationHandler = new CrawlOperationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog);
            AssistantChatEvalExecutor evalChatExecutor = new AssistantChatEvalExecutor(
                _Database,
                _Logging,
                _Settings,
                _Retrieval,
                _Inference,
                _Storage,
                _InvertedIndex,
                _InferenceEndpointService);
            EvalService evalService = new EvalService(_Settings, _Logging, _Database, _Inference, _InferenceEndpointService, evalChatExecutor);
            EvalHandler evalHandler = new EvalHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, evalService);

            // Unauthenticated routes
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/", rootHandler.GetRootAsync, openApiMetadata: SystemApiDocs.GetRoot);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/", rootHandler.HeadRootAsync, openApiMetadata: SystemApiDocs.HeadRoot);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/openapi.json", openApiHandler.GetOpenApiAsync, openApiMetadata: SystemApiDocs.GetOpenApi);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/openapi.json", openApiHandler.GetOpenApiAsync, openApiMetadata: SystemApiDocs.GetOpenApiVersioned);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/swagger", openApiHandler.GetSwaggerAsync, openApiMetadata: SystemApiDocs.GetSwagger);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/authenticate", authenticateHandler.PostAuthenticateAsync, openApiMetadata: SystemApiDocs.Authenticate);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/public", chatHandler.GetAssistantPublicAsync, openApiMetadata: PublicAssistantApiDocs.GetPublicInfo);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/chat/open", chatHandler.PostChatOpenAsync, openApiMetadata: PublicAssistantApiDocs.ChatOpen);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/chat", chatHandler.PostChatAsync, openApiMetadata: PublicAssistantApiDocs.Chat);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/feedback", chatHandler.PostFeedbackAsync, openApiMetadata: PublicAssistantApiDocs.SubmitFeedback);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/compact", chatHandler.PostCompactAsync, openApiMetadata: PublicAssistantApiDocs.Compact);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/generate", chatHandler.PostGenerateAsync, openApiMetadata: PublicAssistantApiDocs.Generate);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/threads", chatHandler.PostCreateThreadAsync, openApiMetadata: PublicAssistantApiDocs.CreateThread);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/threads/{threadId}/history", chatHandler.GetThreadHistoryAsync, openApiMetadata: PublicAssistantApiDocs.GetThreadHistory);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/documents", chatHandler.GetAssistantDocumentsAsync, openApiMetadata: PublicAssistantApiDocs.ListDocuments);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/documents/{documentId}/download", chatHandler.GetPublicDocumentDownloadAsync, openApiMetadata: PublicAssistantApiDocs.DownloadDocument);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/labels/distinct", chatHandler.GetAssistantDistinctLabelsAsync, openApiMetadata: PublicAssistantApiDocs.GetDistinctLabels);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/tags/distinct", chatHandler.GetAssistantDistinctTagsAsync, openApiMetadata: PublicAssistantApiDocs.GetDistinctTags);

            // Authentication handler
            _Server.Routes.AuthenticateRequest = authHandler.HandleAuthenticateRequestAsync;

            // Authenticated routes - Tenants
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants", tenantHandler.PutTenantAsync, openApiMetadata: TenantApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants", tenantHandler.GetTenantsAsync, openApiMetadata: TenantApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{id}", tenantHandler.GetTenantAsync, openApiMetadata: TenantApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{id}", tenantHandler.PutTenantByIdAsync, openApiMetadata: TenantApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{id}", tenantHandler.DeleteTenantAsync, openApiMetadata: TenantApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{id}", tenantHandler.HeadTenantAsync, openApiMetadata: TenantApiDocs.Exists);

            // Authenticated routes - WhoAmI
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/whoami", tenantHandler.GetWhoAmIAsync, openApiMetadata: TenantApiDocs.WhoAmI);

            // Authenticated routes - Users (under tenant path)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/users", userHandler.PutUserAsync, openApiMetadata: UserApiDocs.Create);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users", userHandler.GetUsersAsync, openApiMetadata: UserApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.GetUserAsync, openApiMetadata: UserApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.PutUserByIdAsync, openApiMetadata: UserApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.DeleteUserAsync, openApiMetadata: UserApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.HeadUserAsync, openApiMetadata: UserApiDocs.Exists);

            // Authenticated routes - Credentials (under tenant path)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.PutCredentialAsync, openApiMetadata: CredentialApiDocs.Create);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.GetCredentialsAsync, openApiMetadata: CredentialApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.GetCredentialAsync, openApiMetadata: CredentialApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.PutCredentialByIdAsync, openApiMetadata: CredentialApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.DeleteCredentialAsync, openApiMetadata: CredentialApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.HeadCredentialAsync, openApiMetadata: CredentialApiDocs.Exists);

            // Authenticated routes - Collections (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections", collectionHandler.PutCollectionAsync, openApiMetadata: CollectionApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections", collectionHandler.GetCollectionsAsync, openApiMetadata: CollectionApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}", collectionHandler.GetCollectionAsync, openApiMetadata: CollectionApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections/{collectionId}", collectionHandler.PutCollectionByIdAsync, openApiMetadata: CollectionApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/collections/{collectionId}", collectionHandler.DeleteCollectionAsync, openApiMetadata: CollectionApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/collections/{collectionId}", collectionHandler.HeadCollectionAsync, openApiMetadata: CollectionApiDocs.Exists);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/collections/{collectionId}/search", collectionHandler.SearchCollectionAsync, openApiMetadata: CollectionApiDocs.Search);

            // Authenticated routes - Collection Records (admin only)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections/{collectionId}/records", collectionHandler.PutRecordAsync, openApiMetadata: CollectionApiDocs.CreateRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/records", collectionHandler.GetRecordsAsync, openApiMetadata: CollectionApiDocs.ListRecords);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/records/{recordId}", collectionHandler.GetRecordAsync, openApiMetadata: CollectionApiDocs.ReadRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/collections/{collectionId}/records/{recordId}", collectionHandler.DeleteRecordAsync, openApiMetadata: CollectionApiDocs.DeleteRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/collections/{collectionId}/records/delete", collectionHandler.BatchDeleteRecordsAsync, openApiMetadata: CollectionApiDocs.DeleteRecords);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/labels/distinct", collectionHandler.GetDistinctLabelsAsync, openApiMetadata: CollectionApiDocs.DistinctLabels);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/tags/distinct", collectionHandler.GetDistinctTagsAsync, openApiMetadata: CollectionApiDocs.DistinctTags);

            // Authenticated routes - Indices (admin only, proxied to Verbex)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/indices", indexHandler.GetIndicesAsync, openApiMetadata: IndexApiDocs.List);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices", indexHandler.PutIndexAsync, openApiMetadata: IndexApiDocs.Create);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/indices/{indexId}", indexHandler.GetIndexAsync, openApiMetadata: IndexApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}", indexHandler.PutIndexByIdAsync, openApiMetadata: IndexApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/indices/{indexId}", indexHandler.DeleteIndexAsync, openApiMetadata: IndexApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/indices/{indexId}", indexHandler.HeadIndexAsync, openApiMetadata: IndexApiDocs.Exists);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/labels", indexHandler.PutIndexLabelsAsync, openApiMetadata: IndexApiDocs.UpdateLabels);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/tags", indexHandler.PutIndexTagsAsync, openApiMetadata: IndexApiDocs.UpdateTags);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/custom-metadata", indexHandler.PutIndexCustomMetadataAsync, openApiMetadata: IndexApiDocs.UpdateCustomMetadata);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/indices/{indexId}/terms/top", indexHandler.GetTopTermsAsync, openApiMetadata: IndexApiDocs.TopTerms);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/indices/{indexId}/search", indexHandler.PostSearchAsync, openApiMetadata: IndexApiDocs.Search);

            // Authenticated routes - Index Records (admin only, proxied to Verbex)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/indices/{indexId}/records", indexHandler.GetRecordsAsync, openApiMetadata: IndexApiDocs.ListRecords);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/records", indexHandler.PutRecordAsync, openApiMetadata: IndexApiDocs.CreateRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/indices/{indexId}/records/delete", indexHandler.DeleteRecordsAsync, openApiMetadata: IndexApiDocs.DeleteRecords);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/indices/{indexId}/records/batch", indexHandler.PostRecordBatchAsync, openApiMetadata: IndexApiDocs.CreateRecordBatch);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/indices/{indexId}/records/exists", indexHandler.PostRecordExistsAsync, openApiMetadata: IndexApiDocs.RecordsExist);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/indices/{indexId}/records/{recordId}", indexHandler.GetRecordAsync, openApiMetadata: IndexApiDocs.ReadRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/indices/{indexId}/records/{recordId}", indexHandler.DeleteRecordAsync, openApiMetadata: IndexApiDocs.DeleteRecord);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/indices/{indexId}/records/{recordId}", indexHandler.HeadRecordAsync, openApiMetadata: IndexApiDocs.RecordExists);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/records/{recordId}/labels", indexHandler.PutRecordLabelsAsync, openApiMetadata: IndexApiDocs.UpdateRecordLabels);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/records/{recordId}/tags", indexHandler.PutRecordTagsAsync, openApiMetadata: IndexApiDocs.UpdateRecordTags);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/indices/{indexId}/records/{recordId}/custom-metadata", indexHandler.PutRecordCustomMetadataAsync, openApiMetadata: IndexApiDocs.UpdateRecordCustomMetadata);

            // Authenticated routes - Buckets (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/buckets", bucketHandler.PutBucketAsync, openApiMetadata: BucketApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets", bucketHandler.GetBucketsAsync, openApiMetadata: BucketApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}", bucketHandler.GetBucketAsync, openApiMetadata: BucketApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/buckets/{name}", bucketHandler.DeleteBucketAsync, openApiMetadata: BucketApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/buckets/{name}", bucketHandler.HeadBucketAsync, openApiMetadata: BucketApiDocs.Exists);

            // Authenticated routes - Bucket Objects (admin only)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/buckets/{name}/objects", bucketHandler.PutObjectAsync, openApiMetadata: BucketApiDocs.CreateObject);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects", bucketHandler.GetObjectsAsync, openApiMetadata: BucketApiDocs.ListObjects);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/buckets/{name}/objects", bucketHandler.DeleteObjectAsync, openApiMetadata: BucketApiDocs.DeleteObject);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects/metadata", bucketHandler.GetObjectMetadataAsync, openApiMetadata: BucketApiDocs.ReadObjectMetadata);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects/download", bucketHandler.DownloadObjectAsync, openApiMetadata: BucketApiDocs.Download);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/buckets/{name}/objects/upload", bucketHandler.UploadObjectAsync, openApiMetadata: BucketApiDocs.Upload);

            // Authenticated routes - Assistants
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants", assistantHandler.PutAssistantAsync, openApiMetadata: AssistantApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants", assistantHandler.GetAssistantsAsync, openApiMetadata: AssistantApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}", assistantHandler.GetAssistantAsync, openApiMetadata: AssistantApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}", assistantHandler.PutAssistantByIdAsync, openApiMetadata: AssistantApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/assistants/{assistantId}", assistantHandler.DeleteAssistantAsync, openApiMetadata: AssistantApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/assistants/{assistantId}", assistantHandler.HeadAssistantAsync, openApiMetadata: AssistantApiDocs.Exists);

            // Authenticated routes - Assistant Settings
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.GetSettingsAsync, openApiMetadata: AssistantSettingsApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.PutSettingsAsync, openApiMetadata: AssistantSettingsApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/settings/slack/verify", assistantSettingsHandler.VerifySlackSettingsAsync, openApiMetadata: AssistantSettingsApiDocs.VerifySlack);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/settings/tools/validate", assistantSettingsHandler.ValidateToolsAsync, openApiMetadata: AssistantSettingsApiDocs.ValidateTools);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/settings/tools/test", assistantSettingsHandler.TestToolsAsync, openApiMetadata: AssistantSettingsApiDocs.TestTools);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/retrieve", assistantRetrieveHandler.PostRetrieveAsync, openApiMetadata: AssistantSettingsApiDocs.Retrieve);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/tools", assistantSettingsHandler.GetToolsAsync, openApiMetadata: AssistantSettingsApiDocs.Tools);

            // Authenticated routes - Assistant Analytics
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/overview", assistantAnalyticsHandler.GetOverviewAsync, openApiMetadata: AssistantAnalyticsApiDocs.Overview);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/timeseries", assistantAnalyticsHandler.GetTimeSeriesAsync, openApiMetadata: AssistantAnalyticsApiDocs.TimeSeries);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/stages", assistantAnalyticsHandler.GetStagesAsync, openApiMetadata: AssistantAnalyticsApiDocs.Stages);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/endpoints", assistantAnalyticsHandler.GetEndpointsAsync, openApiMetadata: AssistantAnalyticsApiDocs.Endpoints);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/slowest", assistantAnalyticsHandler.GetSlowestAsync, openApiMetadata: AssistantAnalyticsApiDocs.Slowest);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/analytics/feedback", assistantAnalyticsHandler.GetFeedbackAsync, openApiMetadata: AssistantAnalyticsApiDocs.Feedback);

            // Authenticated routes - Assistant Tool Calls
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/tool-calls", assistantToolCallHandler.GetAssistantToolCallsAsync, openApiMetadata: AssistantToolCallApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/assistants/{assistantId}/tool-calls", assistantToolCallHandler.DeleteAssistantToolCallsAsync, openApiMetadata: AssistantToolCallApiDocs.DeleteMany);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}", assistantToolCallHandler.GetAssistantToolCallAsync, openApiMetadata: AssistantToolCallApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}", assistantToolCallHandler.DeleteAssistantToolCallAsync, openApiMetadata: AssistantToolCallApiDocs.Delete);

            // Authenticated routes - Ingestion Rules
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/ingestion-rules", ingestionRuleHandler.PutIngestionRuleAsync, openApiMetadata: IngestionRuleApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules", ingestionRuleHandler.GetIngestionRulesAsync, openApiMetadata: IngestionRuleApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.GetIngestionRuleAsync, openApiMetadata: IngestionRuleApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.PutIngestionRuleByIdAsync, openApiMetadata: IngestionRuleApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.DeleteIngestionRuleAsync, openApiMetadata: IngestionRuleApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.HeadIngestionRuleAsync, openApiMetadata: IngestionRuleApiDocs.Exists);

            // Authenticated routes - Embedding Endpoints (admin only, proxied to Partio)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding", embeddingEndpointHandler.CreateEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/embedding/enumerate", embeddingEndpointHandler.EnumerateEmbeddingEndpointsAsync, openApiMetadata: EmbeddingEndpointApiDocs.Enumerate);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/health", embeddingEndpointHandler.GetAllEmbeddingEndpointHealthAsync, openApiMetadata: EmbeddingEndpointApiDocs.ListHealth);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}/health", embeddingEndpointHandler.GetEmbeddingEndpointHealthAsync, openApiMetadata: EmbeddingEndpointApiDocs.ReadHealth);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/embedding/{endpointId}/test", embeddingEndpointHandler.TestEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Test);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/embedding/{endpointId}/load", embeddingEndpointHandler.LoadEmbeddingEndpointModelAsync, openApiMetadata: EmbeddingEndpointApiDocs.Load);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.GetEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.UpdateEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.DeleteEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.HeadEmbeddingEndpointAsync, openApiMetadata: EmbeddingEndpointApiDocs.Exists);

            // Authenticated routes - Completion Endpoints (admin only, proxied to Partio)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion", completionEndpointHandler.CreateCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/completion/enumerate", completionEndpointHandler.EnumerateCompletionEndpointsAsync, openApiMetadata: CompletionEndpointApiDocs.Enumerate);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/health", completionEndpointHandler.GetAllCompletionEndpointHealthAsync, openApiMetadata: CompletionEndpointApiDocs.ListHealth);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}/health", completionEndpointHandler.GetCompletionEndpointHealthAsync, openApiMetadata: CompletionEndpointApiDocs.ReadHealth);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/completion/{endpointId}/test", completionEndpointHandler.TestCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Test);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/completion/{endpointId}/load", completionEndpointHandler.LoadCompletionEndpointModelAsync, openApiMetadata: CompletionEndpointApiDocs.Load);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.GetCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.UpdateCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.DeleteCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.HeadCompletionEndpointAsync, openApiMetadata: CompletionEndpointApiDocs.Exists);

            // Authenticated routes - Documents
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/documents", documentHandler.PutDocumentAsync, openApiMetadata: DocumentApiDocs.Upload);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents", documentHandler.GetDocumentsAsync, openApiMetadata: DocumentApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}", documentHandler.GetDocumentAsync, openApiMetadata: DocumentApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/documents/{documentId}", documentHandler.DeleteDocumentAsync, openApiMetadata: DocumentApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/documents/{documentId}/supersedes", documentHandler.PutDocumentSupersedesAsync, openApiMetadata: DocumentApiDocs.Supersedes);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/documents/{documentId}", documentHandler.HeadDocumentAsync, openApiMetadata: DocumentApiDocs.Exists);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/documents/delete", documentHandler.BulkDeleteDocumentsAsync, openApiMetadata: DocumentApiDocs.BulkDelete);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/documents/reindex", documentHandler.ReindexDocumentsAsync, openApiMetadata: DocumentApiDocs.ReindexBatch);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/documents/{documentId}/reindex", documentHandler.ReindexDocumentAsync, openApiMetadata: DocumentApiDocs.Reindex);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/documents/{documentId}/reprocess", documentHandler.ReprocessDocumentAsync, openApiMetadata: DocumentApiDocs.Reprocess);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}/processing-log", documentHandler.GetDocumentProcessingLogAsync, openApiMetadata: DocumentApiDocs.ProcessingLog);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}/performance", documentHandler.GetDocumentPerformanceAsync, openApiMetadata: DocumentApiDocs.Performance);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/analytics/ingestion", documentHandler.GetIngestionAnalyticsAsync, openApiMetadata: DocumentApiDocs.IngestionAnalytics);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}/download", documentHandler.DownloadDocumentAsync, openApiMetadata: DocumentApiDocs.Download);

            // Authenticated routes - Feedback
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback", feedbackHandler.GetFeedbackListAsync, openApiMetadata: FeedbackApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback/{feedbackId}", feedbackHandler.GetFeedbackAsync, openApiMetadata: FeedbackApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/feedback/{feedbackId}", feedbackHandler.DeleteFeedbackAsync, openApiMetadata: FeedbackApiDocs.Delete);

            // Authenticated routes - History
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history", historyHandler.GetHistoryListAsync, openApiMetadata: HistoryApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history/{historyId}", historyHandler.GetHistoryAsync, openApiMetadata: HistoryApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/history/{historyId}", historyHandler.DeleteHistoryAsync, openApiMetadata: HistoryApiDocs.Delete);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/threads", historyHandler.GetThreadsAsync, openApiMetadata: HistoryApiDocs.Threads);

            // Authenticated routes - Request History (admin or tenant admin)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/requesthistory", requestHistoryHandler.GetRequestHistoryAsync, openApiMetadata: RequestHistoryApiDocs.List);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/requesthistory/summary", requestHistoryHandler.GetRequestHistorySummaryAsync, openApiMetadata: RequestHistoryApiDocs.Summary);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/requesthistory/{requestId}", requestHistoryHandler.GetRequestHistoryEntryAsync, openApiMetadata: RequestHistoryApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/requesthistory/{requestId}/detail", requestHistoryHandler.GetRequestHistoryEntryDetailAsync, openApiMetadata: RequestHistoryApiDocs.ReadDetail);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/requesthistory/{requestId}", requestHistoryHandler.DeleteRequestHistoryEntryAsync, openApiMetadata: RequestHistoryApiDocs.Delete);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/requesthistory/bulk", requestHistoryHandler.DeleteRequestHistoryBulkAsync, openApiMetadata: RequestHistoryApiDocs.DeleteBulk);

            // Authenticated routes - Configuration (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/configuration", configurationHandler.GetConfigurationAsync, openApiMetadata: ConfigurationApiDocs.Read);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/rerankers", rerankerHandler.GetRerankersAsync, openApiMetadata: RerankerApiDocs.List);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/rerankers/{rerankerId}/test", rerankerHandler.TestRerankerAsync, openApiMetadata: RerankerApiDocs.Test);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/configuration/external-search/status", configurationHandler.GetExternalSearchStatusAsync, openApiMetadata: ConfigurationApiDocs.ExternalSearchStatus);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/configuration", configurationHandler.PutConfigurationAsync, openApiMetadata: ConfigurationApiDocs.Update);

            // Authenticated routes - Models
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models", inferenceHandler.GetModelsAsync, openApiMetadata: ModelApiDocs.List);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/models/pull", inferenceHandler.PostPullModelAsync, openApiMetadata: ModelApiDocs.Pull);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models/pull/status", inferenceHandler.GetPullStatusAsync, openApiMetadata: ModelApiDocs.PullStatus);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/models/{modelName}", inferenceHandler.DeleteModelAsync, openApiMetadata: ModelApiDocs.Delete);

            // Authenticated routes - Crawl Plans
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/crawlplans", crawlPlanHandler.PutCrawlPlanAsync, openApiMetadata: CrawlApiDocs.Create);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans", crawlPlanHandler.GetCrawlPlansAsync, openApiMetadata: CrawlApiDocs.List);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/connectivity", crawlPlanHandler.TestDraftConnectivityAsync, openApiMetadata: CrawlApiDocs.TestDraftConnectivity);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{id}", crawlPlanHandler.GetCrawlPlanAsync, openApiMetadata: CrawlApiDocs.Read);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/crawlplans/{id}", crawlPlanHandler.PutCrawlPlanByIdAsync, openApiMetadata: CrawlApiDocs.Update);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{id}", crawlPlanHandler.DeleteCrawlPlanAsync, openApiMetadata: CrawlApiDocs.Delete);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/crawlplans/{id}", crawlPlanHandler.HeadCrawlPlanAsync, openApiMetadata: CrawlApiDocs.Exists);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/start", crawlPlanHandler.StartCrawlAsync, openApiMetadata: CrawlApiDocs.Start);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/stop", crawlPlanHandler.StopCrawlAsync, openApiMetadata: CrawlApiDocs.Stop);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/connectivity", crawlPlanHandler.TestConnectivityAsync, openApiMetadata: CrawlApiDocs.TestConnectivity);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{id}/enumerate", crawlPlanHandler.EnumerateContentsAsync, openApiMetadata: CrawlApiDocs.Enumerate);

            // Authenticated routes - Crawl Operations
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations", crawlOperationHandler.GetOperationsAsync, openApiMetadata: CrawlApiDocs.ListOperations);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/statistics", crawlOperationHandler.GetStatisticsAsync, openApiMetadata: CrawlApiDocs.PlanStatistics);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.GetOperationAsync, openApiMetadata: CrawlApiDocs.ReadOperation);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}/statistics", crawlOperationHandler.GetOperationStatisticsAsync, openApiMetadata: CrawlApiDocs.OperationStatistics);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.DeleteOperationAsync, openApiMetadata: CrawlApiDocs.DeleteOperation);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}/enumeration", crawlOperationHandler.GetEnumerationAsync, openApiMetadata: CrawlApiDocs.ReadEnumeration);

            // Authenticated routes - Evaluation
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/eval/facts", evalHandler.PutFactAsync, openApiMetadata: EvalApiDocs.CreateFact);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/facts", evalHandler.GetFactsAsync, openApiMetadata: EvalApiDocs.ListFacts);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/facts/{factId}", evalHandler.GetFactAsync, openApiMetadata: EvalApiDocs.ReadFact);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/eval/facts/{factId}", evalHandler.PutFactByIdAsync, openApiMetadata: EvalApiDocs.UpdateFact);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/eval/facts/{factId}", evalHandler.DeleteFactAsync, openApiMetadata: EvalApiDocs.DeleteFact);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/eval/runs", evalHandler.PostRunAsync, openApiMetadata: EvalApiDocs.StartRun);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/runs", evalHandler.GetRunsAsync, openApiMetadata: EvalApiDocs.ListRuns);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/runs/{runId}", evalHandler.GetRunAsync, openApiMetadata: EvalApiDocs.ReadRun);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/eval/runs/{runId}", evalHandler.DeleteRunAsync, openApiMetadata: EvalApiDocs.DeleteRun);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/runs/{runId}/results", evalHandler.GetRunResultsAsync, openApiMetadata: EvalApiDocs.ListRunResults);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/runs/{runId}/stream", evalHandler.GetRunStreamAsync, openApiMetadata: EvalApiDocs.StreamRun);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/results/{resultId}", evalHandler.GetResultAsync, openApiMetadata: EvalApiDocs.ReadResult);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/eval/judge-prompt/default", evalHandler.GetDefaultJudgePromptAsync, openApiMetadata: EvalApiDocs.DefaultJudgePrompt);

            // Preflight - Watson invokes this exclusively for OPTIONS (CORS preflight) requests
            _Server.Routes.Preflight = async (ctx) =>
            {
                ApplyCorsHeaders(ctx.Response, ctx.Request);
                ctx.Response.StatusCode = 204;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(ctx.Token).ConfigureAwait(false);
            };

            // Pre-routing - apply CORS headers to every non-OPTIONS response before it is handled
            _Server.Routes.PreRouting = async (ctx) =>
            {
                ApplyCorsHeaders(ctx.Response, ctx.Request);
                AssistantHubTelemetry.IncrementHttpActive();
                await Task.CompletedTask.ConfigureAwait(false);
            };

            // Post-routing
            _Server.Routes.PostRouting = async (ctx) =>
            {
                ctx.Timestamp.End = DateTime.UtcNow;

                _Logging.Debug(
                    _Header
                    + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                    + ctx.Response.StatusCode + " "
                    + "(" + ctx.Timestamp.TotalMs?.ToString("F2") + "ms)");

                if (_RequestHistoryCapture != null)
                    _RequestHistoryCapture.Capture(ctx);

                // Telemetry: record HTTP server metrics and emit a request span. Never let telemetry break a response.
                try
                {
                    string method = ctx.Request.Method.ToString();
                    string route = ResolveRoute(ctx);
                    int status = ctx.Response.StatusCode;
                    double seconds = (ctx.Timestamp.TotalMs ?? 0.0) / 1000.0;
                    DateTime startUtc = ctx.Timestamp.Start;
                    DateTime endUtc = ctx.Timestamp.End ?? DateTime.UtcNow;

                    AssistantHubTelemetry.RecordHttpRequest(method, route, status, seconds);
                    AssistantHubTelemetry.EmitHttpSpan(method, route, status, startUtc, endUtc);
                }
                catch (Exception te)
                {
                    _Logging.Debug(_Header + "telemetry post-routing error: " + te.Message);
                }
                finally
                {
                    AssistantHubTelemetry.DecrementHttpActive();
                }
            };

            _Server.Start();
            _Logging.Info(_Header + "webserver started");
        }

        #endregion

        #region Default-Route-Handler

        private static async Task DefaultRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
        }

        #endregion

        #region Telemetry-Helpers

        /// <summary>
        /// Resolve the low-cardinality route template for a request (for example /v1.0/tenants/{id}) so it can
        /// be used as a metric label. Returns null when no route matched (default/404), which the telemetry
        /// layer records as "unmatched".
        /// </summary>
        private static string ResolveRoute(HttpContextBase ctx)
        {
            switch (ctx.Route)
            {
                case WatsonWebserver.Core.Routing.StaticRoute staticRoute:
                    return staticRoute.Path;
                case WatsonWebserver.Core.Routing.ParameterRoute parameterRoute:
                    return parameterRoute.Path;
                case WatsonWebserver.Core.Routing.ContentRoute contentRoute:
                    return contentRoute.Path;
                default:
                    return null;
            }
        }

        #endregion

        #region CORS

        private static void ApplyCorsHeaders(HttpResponseBase response, HttpRequestBase request)
        {
            CorsSettings cors = _Settings?.Webserver?.Cors;
            if (cors == null || !cors.Enable) return;

            string origin = request.Headers.Get("Origin");
            if (String.IsNullOrEmpty(origin)) return;

            bool originAllowed = false;
            string allowedOriginValue = null;

            if (cors.AllowOrigins.Contains("*"))
            {
                originAllowed = true;
                allowedOriginValue = cors.AllowCredentials ? origin : "*";
            }
            else
            {
                foreach (string allowedOrigin in cors.AllowOrigins)
                {
                    if (String.Equals(allowedOrigin, origin, StringComparison.OrdinalIgnoreCase))
                    {
                        originAllowed = true;
                        allowedOriginValue = origin;
                        break;
                    }
                }
            }

            if (!originAllowed) return;

            response.Headers.Add("Access-Control-Allow-Origin", allowedOriginValue);

            // When the allowed origin is echoed rather than "*", advertise that responses vary by origin
            if (!String.Equals(allowedOriginValue, "*", StringComparison.Ordinal))
                response.Headers.Add("Vary", "Origin");

            if (cors.AllowMethods != null && cors.AllowMethods.Count > 0)
                response.Headers.Add("Access-Control-Allow-Methods", String.Join(", ", cors.AllowMethods));

            if (cors.AllowHeaders != null && cors.AllowHeaders.Count > 0)
                response.Headers.Add("Access-Control-Allow-Headers", String.Join(", ", cors.AllowHeaders));

            if (cors.ExposeHeaders != null && cors.ExposeHeaders.Count > 0)
                response.Headers.Add("Access-Control-Expose-Headers", String.Join(", ", cors.ExposeHeaders));

            if (cors.AllowCredentials)
                response.Headers.Add("Access-Control-Allow-Credentials", "true");

            if (request.Method == WatsonWebserver.Core.HttpMethod.OPTIONS && cors.MaxAgeSeconds > 0)
                response.Headers.Add("Access-Control-Max-Age", cors.MaxAgeSeconds.ToString());
        }

        #endregion
    }
}
