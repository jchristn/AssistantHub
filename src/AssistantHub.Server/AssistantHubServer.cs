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
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

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
        private static StorageService _Storage = null;
        private static IngestionService _Ingestion = null;
        private static RetrievalService _Retrieval = null;
        private static InferenceService _Inference = null;
        private static ProcessingLogService _ProcessingLog = null;
        private static WatsonWebserver.Webserver _Server = null;
        private static EndpointHealthCheckService _HealthCheckService = null;
        private static CrawlSchedulerService _CrawlScheduler = null;
        private static CrawlOperationCleanupService _CrawlOperationCleanup = null;
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static bool _ShuttingDown = false;

        /// <summary>
        /// The endpoint health check service instance, accessible by handlers.
        /// </summary>
        public static EndpointHealthCheckService HealthCheckService => _HealthCheckService;

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
            await InitializeDatabaseAsync();
            await InitializeFirstRunAsync();
            InitializeServices();
            await ValidateConnectivityAsync();
            await StartHealthCheckServiceAsync();
            StartProcessingLogCleanup();
            StartChatHistoryCleanup();
            await StartCrawlServicesAsync();
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
            Console.WriteLine("Settings loaded from " + Constants.SettingsFile);
            return true;
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
            _Logging.Info(_Header + "  password: " + adminPassword);
            _Logging.Info(_Header + "  bearer token: " + credential.BearerToken);

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
                using (HttpClient http = new HttpClient())
                {
                    // Create RecallDB tenant
                    string tenantUrl = _Settings.RecallDb.Endpoint.TrimEnd('/') + "/v1.0/tenants";
                    using (HttpRequestMessage tenantReq = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, tenantUrl))
                    {
                        object tenantBody = new { Id = Constants.DefaultTenantId, Name = defaultTenant.Name };
                        tenantReq.Content = new StringContent(JsonSerializer.Serialize(tenantBody), Encoding.UTF8, "application/json");

                        if (!String.IsNullOrEmpty(_Settings.RecallDb.AccessKey))
                            tenantReq.Headers.Add("Authorization", "Bearer " + _Settings.RecallDb.AccessKey);

                        HttpResponseMessage tenantResp = await http.SendAsync(tenantReq).ConfigureAwait(false);
                        if (tenantResp.IsSuccessStatusCode)
                            _Logging.Info(_Header + "default RecallDB tenant created");
                        else
                            _Logging.Warn(_Header + "failed to create default RecallDB tenant: HTTP " + (int)tenantResp.StatusCode);
                    }

                    // Create default collection in RecallDB
                    string collUrl = _Settings.RecallDb.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + Constants.DefaultTenantId + "/collections";
                    using (HttpRequestMessage collReq = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, collUrl))
                    {
                        object collBody = new { Id = "default", Name = "default" };
                        collReq.Content = new StringContent(JsonSerializer.Serialize(collBody), Encoding.UTF8, "application/json");

                        if (!String.IsNullOrEmpty(_Settings.RecallDb.AccessKey))
                            collReq.Headers.Add("Authorization", "Bearer " + _Settings.RecallDb.AccessKey);

                        HttpResponseMessage collResp = await http.SendAsync(collReq).ConfigureAwait(false);
                        if (collResp.IsSuccessStatusCode)
                            _Logging.Info(_Header + "default RecallDB collection created");
                        else
                            _Logging.Warn(_Header + "failed to create default RecallDB collection: HTTP " + (int)collResp.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "could not provision RecallDB: " + e.Message);
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
                _Ingestion = new IngestionService(_Database, _Storage, _Settings.DocumentAtom, _Settings.Chunking, _Settings.RecallDb, _Logging, _ProcessingLog);
            }
            else
            {
                _Logging.Warn(_Header + "ingestion service unavailable (no storage configured)");
            }

            _Retrieval = new RetrievalService(_Settings.Chunking, _Settings.RecallDb, _Logging);
            _Inference = new InferenceService(_Settings.Inference, _Logging);
            _Logging.Info(_Header + "services initialized");
        }

        private static async Task ValidateConnectivityAsync()
        {
            _Logging.Info(_Header + "validating connectivity to subordinate services");

            bool allSucceeded = true;

            using (HttpClient http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);

                // S3 (Less3) - HEAD request matching Docker healthcheck pattern
                allSucceeded &= await CheckServiceAsync(http, "S3 (Less3)",
                    System.Net.Http.HttpMethod.Head,
                    _Settings.S3.EndpointUrl.TrimEnd('/') + "/");

                // DocumentAtom - HEAD request
                allSucceeded &= await CheckServiceAsync(http, "DocumentAtom",
                    System.Net.Http.HttpMethod.Head,
                    _Settings.DocumentAtom.Endpoint.TrimEnd('/') + "/");

                // Chunking (Partio) - HEAD request
                allSucceeded &= await CheckServiceAsync(http, "Chunking (Partio)",
                    System.Net.Http.HttpMethod.Head,
                    _Settings.Chunking.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Chunking.AccessKey);

                // Embeddings (Partio) - HEAD request
                allSucceeded &= await CheckServiceAsync(http, "Embeddings (Partio)",
                    System.Net.Http.HttpMethod.Head,
                    _Settings.Embeddings.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Embeddings.AccessKey);

                // Inference (Ollama) - GET request
                allSucceeded &= await CheckServiceAsync(http, "Inference (Ollama)",
                    System.Net.Http.HttpMethod.Get,
                    _Settings.Inference.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Inference.ApiKey);

                // RecallDb - HEAD request
                allSucceeded &= await CheckServiceAsync(http, "RecallDb",
                    System.Net.Http.HttpMethod.Head,
                    _Settings.RecallDb.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.RecallDb.AccessKey);
            }

            if (!allSucceeded)
            {
                _Logging.Warn(_Header + "one or more subordinate services are unreachable, aborting startup");
                throw new Exception("One or more subordinate services are unreachable. Check logs for details.");
            }

            _Logging.Info(_Header + "all subordinate services are reachable");
        }

        private static async Task<bool> CheckServiceAsync(HttpClient http, string serviceName, System.Net.Http.HttpMethod method, string url, string bearerToken = null)
        {
            try
            {
                using (HttpRequestMessage req = new HttpRequestMessage(method, url))
                {
                    if (!String.IsNullOrEmpty(bearerToken))
                        req.Headers.Add("Authorization", "Bearer " + bearerToken);

                    HttpResponseMessage resp = await http.SendAsync(req).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        _Logging.Info(_Header + serviceName + " reachable at " + url + " (HTTP " + (int)resp.StatusCode + ")");
                        return true;
                    }
                    else
                    {
                        _Logging.Warn(_Header + serviceName + " returned HTTP " + (int)resp.StatusCode + " at " + url);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + serviceName + " unreachable at " + url + ": " + e.Message);
                return false;
            }
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

        private static void InitializeWebserver()
        {
            WatsonWebserver.Core.WebserverSettings wsSettings = new WatsonWebserver.Core.WebserverSettings(_Settings.Webserver.Hostname, _Settings.Webserver.Port, _Settings.Webserver.Ssl);
            _Server = new WatsonWebserver.Webserver(wsSettings, DefaultRoute);

            // Instantiate handlers
            AuthenticationHandler authHandler = new AuthenticationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            RootHandler rootHandler = new RootHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AuthenticateHandler authenticateHandler = new AuthenticateHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            ChatHandler chatHandler = new ChatHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            TenantHandler tenantHandler = new TenantHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            UserHandler userHandler = new UserHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CredentialHandler credentialHandler = new CredentialHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CollectionHandler collectionHandler = new CollectionHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            BucketHandler bucketHandler = new BucketHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantHandler assistantHandler = new AssistantHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantSettingsHandler assistantSettingsHandler = new AssistantSettingsHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            DocumentHandler documentHandler = new DocumentHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog);
            IngestionRuleHandler ingestionRuleHandler = new IngestionRuleHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            EmbeddingEndpointHandler embeddingEndpointHandler = new EmbeddingEndpointHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CompletionEndpointHandler completionEndpointHandler = new CompletionEndpointHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            FeedbackHandler feedbackHandler = new FeedbackHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            HistoryHandler historyHandler = new HistoryHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            InferenceHandler inferenceHandler = new InferenceHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            ConfigurationHandler configurationHandler = new ConfigurationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CrawlPlanHandler crawlPlanHandler = new CrawlPlanHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog, _CrawlScheduler);
            CrawlOperationHandler crawlOperationHandler = new CrawlOperationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference, _ProcessingLog);

            // Unauthenticated routes
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/", rootHandler.GetRootAsync);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/", rootHandler.HeadRootAsync);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/authenticate", authenticateHandler.PostAuthenticateAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/public", chatHandler.GetAssistantPublicAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/chat", chatHandler.PostChatAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/feedback", chatHandler.PostFeedbackAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/compact", chatHandler.PostCompactAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/generate", chatHandler.PostGenerateAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/threads", chatHandler.PostCreateThreadAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/threads/{threadId}/history", chatHandler.GetThreadHistoryAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/documents/{documentId}/download", chatHandler.GetPublicDocumentDownloadAsync);

            // Authentication handler
            _Server.Routes.AuthenticateRequest = authHandler.HandleAuthenticateRequestAsync;

            // Authenticated routes - Tenants
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants", tenantHandler.PutTenantAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants", tenantHandler.GetTenantsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{id}", tenantHandler.GetTenantAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{id}", tenantHandler.PutTenantByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{id}", tenantHandler.DeleteTenantAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{id}", tenantHandler.HeadTenantAsync);

            // Authenticated routes - WhoAmI
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/whoami", tenantHandler.GetWhoAmIAsync);

            // Authenticated routes - Users (under tenant path)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/users", userHandler.PutUserAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users", userHandler.GetUsersAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.GetUserAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.PutUserByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.DeleteUserAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.HeadUserAsync);

            // Authenticated routes - Credentials (under tenant path)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.PutCredentialAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.GetCredentialsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.GetCredentialAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.PutCredentialByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.DeleteCredentialAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.HeadCredentialAsync);

            // Authenticated routes - Collections (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections", collectionHandler.PutCollectionAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections", collectionHandler.GetCollectionsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}", collectionHandler.GetCollectionAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections/{collectionId}", collectionHandler.PutCollectionByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/collections/{collectionId}", collectionHandler.DeleteCollectionAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/collections/{collectionId}", collectionHandler.HeadCollectionAsync);

            // Authenticated routes - Collection Records (admin only)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/collections/{collectionId}/records", collectionHandler.PutRecordAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/records", collectionHandler.GetRecordsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/collections/{collectionId}/records/{recordId}", collectionHandler.GetRecordAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/collections/{collectionId}/records/{recordId}", collectionHandler.DeleteRecordAsync);

            // Authenticated routes - Buckets (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/buckets", bucketHandler.PutBucketAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets", bucketHandler.GetBucketsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}", bucketHandler.GetBucketAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/buckets/{name}", bucketHandler.DeleteBucketAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/buckets/{name}", bucketHandler.HeadBucketAsync);

            // Authenticated routes - Bucket Objects (admin only)
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/buckets/{name}/objects", bucketHandler.PutObjectAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects", bucketHandler.GetObjectsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/buckets/{name}/objects", bucketHandler.DeleteObjectAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects/metadata", bucketHandler.GetObjectMetadataAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/buckets/{name}/objects/download", bucketHandler.DownloadObjectAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/buckets/{name}/objects/upload", bucketHandler.UploadObjectAsync);

            // Authenticated routes - Assistants
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants", assistantHandler.PutAssistantAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants", assistantHandler.GetAssistantsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}", assistantHandler.GetAssistantAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}", assistantHandler.PutAssistantByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/assistants/{assistantId}", assistantHandler.DeleteAssistantAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/assistants/{assistantId}", assistantHandler.HeadAssistantAsync);

            // Authenticated routes - Assistant Settings
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.GetSettingsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.PutSettingsAsync);

            // Authenticated routes - Ingestion Rules
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/ingestion-rules", ingestionRuleHandler.PutIngestionRuleAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules", ingestionRuleHandler.GetIngestionRulesAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.GetIngestionRuleAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.PutIngestionRuleByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.DeleteIngestionRuleAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.HeadIngestionRuleAsync);

            // Authenticated routes - Embedding Endpoints (admin only, proxied to Partio)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding", embeddingEndpointHandler.CreateEmbeddingEndpointAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/embedding/enumerate", embeddingEndpointHandler.EnumerateEmbeddingEndpointsAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/health", embeddingEndpointHandler.GetAllEmbeddingEndpointHealthAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}/health", embeddingEndpointHandler.GetEmbeddingEndpointHealthAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.GetEmbeddingEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.UpdateEmbeddingEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.DeleteEmbeddingEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/embedding/{endpointId}", embeddingEndpointHandler.HeadEmbeddingEndpointAsync);

            // Authenticated routes - Completion Endpoints (admin only, proxied to Partio)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion", completionEndpointHandler.CreateCompletionEndpointAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/completion/enumerate", completionEndpointHandler.EnumerateCompletionEndpointsAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/health", completionEndpointHandler.GetAllCompletionEndpointHealthAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}/health", completionEndpointHandler.GetCompletionEndpointHealthAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.GetCompletionEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.UpdateCompletionEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.DeleteCompletionEndpointAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/completion/{endpointId}", completionEndpointHandler.HeadCompletionEndpointAsync);

            // Authenticated routes - Documents
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/documents", documentHandler.PutDocumentAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents", documentHandler.GetDocumentsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}", documentHandler.GetDocumentAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/documents/{documentId}", documentHandler.DeleteDocumentAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/documents/{documentId}", documentHandler.HeadDocumentAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}/processing-log", documentHandler.GetDocumentProcessingLogAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}/download", documentHandler.DownloadDocumentAsync);

            // Authenticated routes - Feedback
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback", feedbackHandler.GetFeedbackListAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback/{feedbackId}", feedbackHandler.GetFeedbackAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/feedback/{feedbackId}", feedbackHandler.DeleteFeedbackAsync);

            // Authenticated routes - History
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history", historyHandler.GetHistoryListAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history/{historyId}", historyHandler.GetHistoryAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/history/{historyId}", historyHandler.DeleteHistoryAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/threads", historyHandler.GetThreadsAsync);

            // Authenticated routes - Configuration (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/configuration", configurationHandler.GetConfigurationAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/configuration", configurationHandler.PutConfigurationAsync);

            // Authenticated routes - Models
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models", inferenceHandler.GetModelsAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/models/pull", inferenceHandler.PostPullModelAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models/pull/status", inferenceHandler.GetPullStatusAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/models/{modelName}", inferenceHandler.DeleteModelAsync);

            // Authenticated routes - Crawl Plans
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/crawlplans", crawlPlanHandler.PutCrawlPlanAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans", crawlPlanHandler.GetCrawlPlansAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{id}", crawlPlanHandler.GetCrawlPlanAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/crawlplans/{id}", crawlPlanHandler.PutCrawlPlanByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{id}", crawlPlanHandler.DeleteCrawlPlanAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/crawlplans/{id}", crawlPlanHandler.HeadCrawlPlanAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/start", crawlPlanHandler.StartCrawlAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/stop", crawlPlanHandler.StopCrawlAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/crawlplans/{id}/connectivity", crawlPlanHandler.TestConnectivityAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{id}/enumerate", crawlPlanHandler.EnumerateContentsAsync);

            // Authenticated routes - Crawl Operations
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations", crawlOperationHandler.GetOperationsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/statistics", crawlOperationHandler.GetStatisticsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.GetOperationAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}/statistics", crawlOperationHandler.GetOperationStatisticsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.DeleteOperationAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}/enumeration", crawlOperationHandler.GetEnumerationAsync);

            // Post-routing
            _Server.Routes.PostRouting = async (ctx) =>
            {
                ctx.Timestamp.End = DateTime.UtcNow;

                _Logging.Debug(
                    _Header
                    + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                    + ctx.Response.StatusCode + " "
                    + "(" + ctx.Timestamp.TotalMs?.ToString("F2") + "ms)");
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
    }
}
