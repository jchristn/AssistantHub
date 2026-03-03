namespace Test.Integration
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Minimal in-process HTTP server for integration testing.
    /// Uses SQLite and skips external service dependencies (S3, RecallDB, etc.).
    /// </summary>
    public class TestServer : IDisposable
    {
        public DatabaseDriverBase Database { get; private set; }
        public AssistantHubSettings Settings { get; private set; }
        public LoggingModule Logging { get; private set; }
        public AuthenticationService Authentication { get; private set; }
        public RetrievalService Retrieval { get; private set; }
        public InferenceService Inference { get; private set; }
        public string BaseUrl { get; private set; }
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Bearer token for the default admin credential.
        /// </summary>
        public string AdminBearerToken { get; private set; }

        /// <summary>
        /// The default tenant ID created during initialization.
        /// </summary>
        public string DefaultTenantId { get; private set; }

        private Webserver _server;
        private string _dbFilename;
        private int _port;

        public static async Task<TestServer> CreateAsync(int port = 0)
        {
            TestServer ts = new TestServer();
            await ts.InitializeAsync(port);
            return ts;
        }

        private async Task InitializeAsync(int port)
        {
            // Use a random port if 0
            _port = port == 0 ? new Random().Next(10000, 60000) : port;

            // Setup SQLite database
            _dbFilename = "integration_test_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".db";

            Settings = new AssistantHubSettings();
            Settings.Database = new DatabaseSettings
            {
                Type = AssistantHub.Core.Enums.DatabaseTypeEnum.Sqlite,
                Filename = _dbFilename
            };
            Settings.Webserver = new AssistantHub.Core.Settings.WebserverSettings
            {
                Hostname = "localhost",
                Port = _port
            };
            Settings.Inference = new InferenceSettings
            {
                Provider = AssistantHub.Core.Enums.InferenceProviderEnum.Ollama,
                Endpoint = "http://localhost:11434"
            };
            Settings.RecallDb = new RecallDbSettings
            {
                Endpoint = "http://localhost:8501"
            };
            Settings.Chunking = new ChunkingSettings
            {
                Endpoint = "http://localhost:8511"
            };

            Logging = new LoggingModule();
            Logging.Settings.EnableConsole = false;

            // Initialize database
            Database = await DatabaseDriverFactory.CreateAndInitializeAsync(Settings.Database, Logging);

            // Create default tenant, user, and credential
            TenantMetadata tenant = new TenantMetadata { Name = "Integration Test Tenant", Active = true };
            tenant = await Database.Tenant.CreateAsync(tenant);
            DefaultTenantId = tenant.Id;

            UserMaster adminUser = new UserMaster
            {
                TenantId = tenant.Id,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@test.local",
                Active = true,
                IsAdmin = true,
                IsTenantAdmin = true
            };
            adminUser.SetPassword("testpassword123");
            adminUser = await Database.User.CreateAsync(adminUser);

            Credential adminCred = new Credential
            {
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                Active = true,
                BearerToken = "test-admin-token-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            adminCred = await Database.Credential.CreateAsync(adminCred);
            AdminBearerToken = adminCred.BearerToken;

            // Initialize services
            Authentication = new AuthenticationService(Database, Logging, Settings);
            Retrieval = new RetrievalService(Settings.Chunking, Settings.RecallDb, Logging);
            Inference = new InferenceService(Settings.Inference, Logging);

            // Create and configure Watson Webserver
            WatsonWebserver.Core.WebserverSettings wsSettings = new WatsonWebserver.Core.WebserverSettings("localhost", _port, false);
            _server = new Webserver(wsSettings, DefaultRouteAsync);

            // Create handlers
            AuthenticationHandler authHandler = new AuthenticationHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            RootHandler rootHandler = new RootHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            AuthenticateHandler authenticateHandler = new AuthenticateHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            TenantHandler tenantHandler = new TenantHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            UserHandler userHandler = new UserHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            CredentialHandler credentialHandler = new CredentialHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            AssistantHandler assistantHandler = new AssistantHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            AssistantSettingsHandler assistantSettingsHandler = new AssistantSettingsHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            FeedbackHandler feedbackHandler = new FeedbackHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            HistoryHandler historyHandler = new HistoryHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            IngestionRuleHandler ingestionRuleHandler = new IngestionRuleHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference);
            CrawlSchedulerService crawlScheduler = new CrawlSchedulerService(Database, Logging, Settings, null, null, null);
            CrawlPlanHandler crawlPlanHandler = new CrawlPlanHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference, null, crawlScheduler);
            CrawlOperationHandler crawlOperationHandler = new CrawlOperationHandler(Database, Logging, Settings, Authentication, null, null, Retrieval, Inference, null);

            // Unauthenticated routes
            _server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/", rootHandler.GetRootAsync);
            _server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/", rootHandler.HeadRootAsync);
            _server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1.0/authenticate", authenticateHandler.PostAuthenticateAsync);

            // Authentication handler
            _server.Routes.AuthenticateRequest = authHandler.HandleAuthenticateRequestAsync;

            // Authenticated routes - Tenants
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants", tenantHandler.PutTenantAsync);
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants", tenantHandler.GetTenantsAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{id}", tenantHandler.GetTenantAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{id}", tenantHandler.DeleteTenantAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/tenants/{id}", tenantHandler.HeadTenantAsync);
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/whoami", tenantHandler.GetWhoAmIAsync);

            // Authenticated routes - Users
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/users", userHandler.PutUserAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users", userHandler.GetUsersAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.GetUserAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/users/{userId}", userHandler.DeleteUserAsync);

            // Authenticated routes - Credentials
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.PutCredentialAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/tenants/{tenantId}/credentials", credentialHandler.GetCredentialsAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/tenants/{tenantId}/credentials/{credentialId}", credentialHandler.DeleteCredentialAsync);

            // Authenticated routes - Assistants
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants", assistantHandler.PutAssistantAsync);
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants", assistantHandler.GetAssistantsAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}", assistantHandler.GetAssistantAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}", assistantHandler.PutAssistantByIdAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/assistants/{assistantId}", assistantHandler.DeleteAssistantAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/assistants/{assistantId}", assistantHandler.HeadAssistantAsync);

            // Authenticated routes - Assistant Settings
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.GetSettingsAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/assistants/{assistantId}/settings", assistantSettingsHandler.PutSettingsAsync);

            // Authenticated routes - Ingestion Rules
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/ingestion-rules", ingestionRuleHandler.PutIngestionRuleAsync);
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules", ingestionRuleHandler.GetIngestionRulesAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.GetIngestionRuleAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/ingestion-rules/{ruleId}", ingestionRuleHandler.DeleteIngestionRuleAsync);

            // Authenticated routes - Feedback
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback", feedbackHandler.GetFeedbackListAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/feedback/{feedbackId}", feedbackHandler.GetFeedbackAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/feedback/{feedbackId}", feedbackHandler.DeleteFeedbackAsync);

            // Authenticated routes - History
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history", historyHandler.GetHistoryListAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/history/{historyId}", historyHandler.GetHistoryAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/history/{historyId}", historyHandler.DeleteHistoryAsync);

            // Authenticated routes - Crawl Plans
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/crawlplans", crawlPlanHandler.PutCrawlPlanAsync);
            _server.Routes.PostAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans", crawlPlanHandler.GetCrawlPlansAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{id}", crawlPlanHandler.GetCrawlPlanAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{id}", crawlPlanHandler.DeleteCrawlPlanAsync);

            // Authenticated routes - Crawl Operations
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations", crawlOperationHandler.GetOperationsAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.GET, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.GetOperationAsync);
            _server.Routes.PostAuthentication.Parameter.Add(WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/crawlplans/{planId}/operations/{id}", crawlOperationHandler.DeleteOperationAsync);

            // Start the server
            _server.Start();
            BaseUrl = $"http://localhost:{_port}";

            // Create HttpClient with auth header
            Client = new HttpClient();
            Client.BaseAddress = new Uri(BaseUrl);
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AdminBearerToken}");

            // Wait a moment for the server to be ready
            await Task.Delay(100);
        }

        private static async Task DefaultRouteAsync(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send("{\"Error\":\"NotFound\"}").ConfigureAwait(false);
        }

        public void Dispose()
        {
            try { _server?.Stop(); } catch { }
            try { _server?.Dispose(); } catch { }
            try { Client?.Dispose(); } catch { }

            // Cleanup SQLite file
            if (!string.IsNullOrEmpty(_dbFilename) && System.IO.File.Exists(_dbFilename))
            {
                try { System.IO.File.Delete(_dbFilename); } catch { }
            }
        }
    }
}
