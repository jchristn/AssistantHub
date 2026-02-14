namespace AssistantHub.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Loader;
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
        private static WatsonWebserver.Webserver _Server = null;
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static bool _ShuttingDown = false;

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
            long userCount = await _Database.User.GetCountAsync().ConfigureAwait(false);
            if (userCount > 0)
            {
                _Logging.Info(_Header + "existing users found, skipping first-run setup");
                return;
            }

            _Logging.Info(_Header + "no users found, creating default admin user");

            UserMaster admin = new UserMaster();
            admin.Id = IdGenerator.NewUserId();
            admin.Email = Constants.DefaultAdminEmail;
            admin.FirstName = Constants.DefaultAdminFirstName;
            admin.LastName = Constants.DefaultAdminLastName;
            admin.IsAdmin = true;
            admin.Active = true;
            admin.SetPassword(Constants.DefaultAdminPassword);
            admin = await _Database.User.CreateAsync(admin).ConfigureAwait(false);

            Credential credential = new Credential();
            credential.Id = IdGenerator.NewCredentialId();
            credential.UserId = admin.Id;
            credential.Name = "Default admin credential";
            credential.BearerToken = "default";
            credential.Active = true;
            credential = await _Database.Credential.CreateAsync(credential).ConfigureAwait(false);

            _Logging.Info(_Header + "default admin created:");
            _Logging.Info(_Header + "  email: " + admin.Email);
            _Logging.Info(_Header + "  password: " + Constants.DefaultAdminPassword);
            _Logging.Info(_Header + "  bearer token: " + credential.BearerToken);

            Console.WriteLine("");
            Console.WriteLine("*** Default admin credentials ***");
            Console.WriteLine("Email       : " + admin.Email);
            Console.WriteLine("Password    : " + Constants.DefaultAdminPassword);
            Console.WriteLine("Bearer token: " + credential.BearerToken);
            Console.WriteLine("");
        }

        private static void InitializeServices()
        {
            _Authentication = new AuthenticationService(_Database, _Logging);

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
                _Ingestion = new IngestionService(_Database, _Storage, _Settings.DocumentAtom, _Settings.Chunking, _Settings.RecallDb, _Logging);
            }
            else
            {
                _Logging.Warn(_Header + "ingestion service unavailable (no storage configured)");
            }

            _Retrieval = new RetrievalService(_Settings.Chunking, _Settings.RecallDb, _Logging);
            _Inference = new InferenceService(_Settings.Inference, _Logging);
            _Logging.Info(_Header + "services initialized");
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
            UserHandler userHandler = new UserHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CredentialHandler credentialHandler = new CredentialHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            CollectionHandler collectionHandler = new CollectionHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            BucketHandler bucketHandler = new BucketHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantHandler assistantHandler = new AssistantHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            AssistantSettingsHandler assistantSettingsHandler = new AssistantSettingsHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            DocumentHandler documentHandler = new DocumentHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            FeedbackHandler feedbackHandler = new FeedbackHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            InferenceHandler inferenceHandler = new InferenceHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);
            ConfigurationHandler configurationHandler = new ConfigurationHandler(_Database, _Logging, _Settings, _Authentication, _Storage, _Ingestion, _Retrieval, _Inference);

            // Unauthenticated routes
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/", rootHandler.GetRootAsync);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/", rootHandler.HeadRootAsync);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/authenticate", authenticateHandler.PostAuthenticateAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/public", chatHandler.GetAssistantPublicAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/chat", chatHandler.PostChatAsync);
            _Server.Routes.PreAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/assistants/{assistantId}/feedback", chatHandler.PostFeedbackAsync);

            // Authentication handler
            _Server.Routes.AuthenticateRequest = authHandler.HandleAuthenticateRequestAsync;

            // Authenticated routes - Users (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/users", userHandler.PutUserAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/users", userHandler.GetUsersAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/users/{userId}", userHandler.GetUserAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/users/{userId}", userHandler.PutUserByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/users/{userId}", userHandler.DeleteUserAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/users/{userId}", userHandler.HeadUserAsync);

            // Authenticated routes - Credentials (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/credentials", credentialHandler.PutCredentialAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/credentials", credentialHandler.GetCredentialsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/credentials/{credentialId}", credentialHandler.GetCredentialAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/credentials/{credentialId}", credentialHandler.PutCredentialByIdAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/credentials/{credentialId}", credentialHandler.DeleteCredentialAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/credentials/{credentialId}", credentialHandler.HeadCredentialAsync);

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

            // Authenticated routes - Documents
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/documents", documentHandler.PutDocumentAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents", documentHandler.GetDocumentsAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/documents/{documentId}", documentHandler.GetDocumentAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/documents/{documentId}", documentHandler.DeleteDocumentAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/documents/{documentId}", documentHandler.HeadDocumentAsync);

            // Authenticated routes - Feedback
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback", feedbackHandler.GetFeedbackListAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback/{feedbackId}", feedbackHandler.GetFeedbackAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/feedback/{feedbackId}", feedbackHandler.DeleteFeedbackAsync);

            // Authenticated routes - Configuration (admin only)
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/configuration", configurationHandler.GetConfigurationAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/configuration", configurationHandler.PutConfigurationAsync);

            // Authenticated routes - Models
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models", inferenceHandler.GetModelsAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/models/pull", inferenceHandler.PostPullModelAsync);
            _Server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/models/pull/status", inferenceHandler.GetPullStatusAsync);
            _Server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/models/{modelName}", inferenceHandler.DeleteModelAsync);

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
