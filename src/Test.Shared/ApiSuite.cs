namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using SyslogLogging;
    using Test.Shared;

    public class ApiSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            MockDatabaseDriver db = new MockDatabaseDriver();
            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;
            AssistantHubSettings settings = new AssistantHubSettings();
            AuthenticationService authService = new AuthenticationService(db, logging, settings);
            InferenceSettings infSettings = new InferenceSettings();
            InferenceService inference = new InferenceService(infSettings, logging);
            ChunkingSettings chunkSettings = new ChunkingSettings();
            RecallDbSettings recallSettings = new RecallDbSettings();
            RetrievalService retrieval = new RetrievalService(chunkSettings, recallSettings, logging);

            TestableHandler handler = new TestableHandler(db, logging, settings, authService, retrieval, inference);

            // --- ValidateTenantAccess tests ---

            await ExecuteTestAsync("Auth.ValidateTenantAccess: null auth returns false", async () =>
            {
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(null, "ten_abc"), "should be false");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: global admin can access any tenant", async () =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_abc"), "global admin should access any tenant");
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_xyz"), "global admin should access any tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: tenant admin can access own tenant", async () =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(tenantAdmin, "ten_abc"), "should access own tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: tenant admin cannot access other tenant", async () =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(tenantAdmin, "ten_xyz"), "should NOT access other tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: regular user can access own tenant", async () =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(regularUser, "ten_abc"), "should access own tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: regular user cannot access other tenant", async () =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(regularUser, "ten_xyz"), "should NOT access other tenant");
            });

            // --- EnforceTenantOwnership tests ---

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: null auth returns false", async () =>
            {
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(null, "ten_abc"), "should be false");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: global admin bypasses ownership check", async () =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_abc"), "global admin bypasses");
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_xyz"), "global admin bypasses");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: matching tenant passes", async () =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(user, "ten_abc"), "matching tenant should pass");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: mismatched tenant fails", async () =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(user, "ten_xyz"), "mismatched tenant should fail");
            });

            // --- AuthContext model tests ---

            await ExecuteTestAsync("AuthContext: defaults are not authenticated", async () =>
            {
                AuthContext ctx = new AuthContext();
                AssertHelper.AreEqual(false, ctx.IsAuthenticated, "IsAuthenticated default");
                AssertHelper.AreEqual(false, ctx.IsGlobalAdmin, "IsGlobalAdmin default");
                AssertHelper.AreEqual(false, ctx.IsTenantAdmin, "IsTenantAdmin default");
                AssertHelper.IsNull(ctx.TenantId, "TenantId default");
                AssertHelper.IsNull(ctx.UserId, "UserId default");
                AssertHelper.IsNull(ctx.CredentialId, "CredentialId default");
                AssertHelper.IsNull(ctx.Email, "Email default");
                AssertHelper.IsNull(ctx.Tenant, "Tenant default");
                AssertHelper.IsNull(ctx.User, "User default");
            });

            await ExecuteTestAsync("AuthContext: global admin auth context properties", async () =>
            {
                AuthContext ctx = new AuthContext
                {
                    IsAuthenticated = true,
                    IsGlobalAdmin = true,
                    TenantId = null,
                    UserId = null
                };
                AssertHelper.AreEqual(true, ctx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(true, ctx.IsGlobalAdmin, "IsGlobalAdmin");
                AssertHelper.IsNull(ctx.TenantId, "TenantId null for admin key");
                AssertHelper.IsNull(ctx.UserId, "UserId null for admin key");
            });

            await ExecuteTestAsync("AuthContext: regular user auth context properties", async () =>
            {
                AuthContext ctx = new AuthContext
                {
                    IsAuthenticated = true,
                    IsGlobalAdmin = false,
                    IsTenantAdmin = false,
                    TenantId = "ten_abc",
                    UserId = "usr_123",
                    CredentialId = "cred_456",
                    Email = "user@example.com"
                };
                AssertHelper.AreEqual(true, ctx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(false, ctx.IsGlobalAdmin, "IsGlobalAdmin");
                AssertHelper.AreEqual(false, ctx.IsTenantAdmin, "IsTenantAdmin");
                AssertHelper.AreEqual("ten_abc", ctx.TenantId, "TenantId");
                AssertHelper.AreEqual("usr_123", ctx.UserId, "UserId");
                AssertHelper.AreEqual("cred_456", ctx.CredentialId, "CredentialId");
                AssertHelper.AreEqual("user@example.com", ctx.Email, "Email");
            });

            await ExecuteTestAsync("Endpoint model load: AssistantHub routes proxy through Partio handlers", async () =>
            {
                string root = GetRepositoryRoot();
                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string embeddingHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "EmbeddingEndpointHandler.cs"));
                string completionHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "CompletionEndpointHandler.cs"));

                AssertHelper.StringContains(serverSource, "/v1.0/endpoints/embedding/{endpointId}/load", "embedding load route");
                AssertHelper.StringContains(serverSource, "LoadEmbeddingEndpointModelAsync", "embedding load route handler");
                AssertHelper.StringContains(serverSource, "/v1.0/endpoints/completion/{endpointId}/load", "completion load route");
                AssertHelper.StringContains(serverSource, "LoadCompletionEndpointModelAsync", "completion load route handler");
                AssertHelper.StringContains(embeddingHandlerSource, "/v1.0/endpoints/embedding/\" + endpointId + \"/load", "embedding Partio load path");
                AssertHelper.StringContains(completionHandlerSource, "/v1.0/endpoints/completion/\" + endpointId + \"/load", "completion Partio load path");
                AssertHelper.StringContains(embeddingHandlerSource, "CopyModelLoadHeaders(resp, ctx)", "embedding model-load headers");
                AssertHelper.StringContains(completionHandlerSource, "CopyModelLoadHeaders(resp, ctx)", "completion model-load headers");
            });

            await ExecuteTestAsync("Endpoint model load: dashboard actions and client methods are wired", async () =>
            {
                string root = GetRepositoryRoot();
                string apiClientSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "utils", "api.js"));
                string embeddingViewSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "EmbeddingEndpointsView.jsx"));
                string inferenceViewSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "InferenceEndpointsView.jsx"));
                string apiExplorerSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "apiExplorerUtils.js"));

                AssertHelper.StringContains(apiClientSource, "loadEmbeddingEndpointModel", "embedding load API client method");
                AssertHelper.StringContains(apiClientSource, "/v1.0/endpoints/embedding/${id}/load", "embedding load API client route");
                AssertHelper.StringContains(apiClientSource, "loadCompletionEndpointModel", "completion load API client method");
                AssertHelper.StringContains(apiClientSource, "/v1.0/endpoints/completion/${id}/load", "completion load API client route");
                AssertHelper.StringContains(embeddingViewSource, "label: 'Load Model'", "embedding load action");
                AssertHelper.StringContains(inferenceViewSource, "label: 'Load Model'", "inference load action");
                AssertHelper.StringContains(apiExplorerSource, "POST:/v1.0/endpoints/embedding/{endpointId}/load", "embedding load explorer template");
                AssertHelper.StringContains(apiExplorerSource, "POST:/v1.0/endpoints/completion/{endpointId}/load", "completion load explorer template");
            });

            await ExecuteTestAsync("Verbex index proxy: routes marshal through AssistantHub", async () =>
            {
                string root = GetRepositoryRoot();
                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string indexHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "IndexHandler.cs"));

                AssertHelper.StringContains(serverSource, "/v1.0/indices", "index list/create route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}", "index item route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/labels", "index labels route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/tags", "index tags route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/custom-metadata", "index custom metadata route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/terms/top", "index top terms route");
                AssertHelper.StringContains(indexHandlerSource, "ProxyAsync(ctx, NetHttpMethod.Get, AppendRequestQuery(ctx, \"/v1.0/indices\"))", "index list proxy");
                AssertHelper.StringContains(indexHandlerSource, "ProxyAuthorizedAsync(ctx, NetHttpMethod.Post, \"/v1.0/indices\", body)", "index create proxy");
                AssertHelper.StringContains(indexHandlerSource, "ProxyIndexAsync(ctx, NetHttpMethod.Put, \"labels\", ctx.Request.DataAsString)", "index labels proxy");
                AssertHelper.StringContains(indexHandlerSource, "ProxyIndexAsync(ctx, NetHttpMethod.Get, AppendRequestQuery(ctx, \"terms/top\"))", "top terms proxy");
            });

            await ExecuteTestAsync("Verbex index record proxy: records map to upstream documents", async () =>
            {
                string root = GetRepositoryRoot();
                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string indexHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "IndexHandler.cs"));

                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/records", "index records route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/records/batch", "index record batch route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/records/exists", "index record exists route");
                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/records/{recordId}", "index record item route");
                AssertHelper.StringContains(indexHandlerSource, "\"/documents\"", "records proxy uses Verbex documents path");
                AssertHelper.StringContains(indexHandlerSource, "+ \"/documents/\" + Uri.EscapeDataString(recordId)", "record item proxy uses Verbex documents path");
                AssertHelper.StringContains(indexHandlerSource, "PopulateIndexRecordNames(ctx.Request.DataAsString)", "record create populates names before proxy");
                AssertHelper.StringContains(indexHandlerSource, "ProxyRecordCollectionAsync(ctx, NetHttpMethod.Post, \"batch\", PopulateIndexRecordNames(ctx.Request.DataAsString), false)", "batch create records proxy");
                AssertHelper.StringContains(indexHandlerSource, "ProxyRecordCollectionAsync(ctx, NetHttpMethod.Delete, null, null, true)", "batch delete records proxy");
            });

            await ExecuteTestAsync("Verbex search proxy: route forwards request body", async () =>
            {
                string root = GetRepositoryRoot();
                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string indexHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "IndexHandler.cs"));

                AssertHelper.StringContains(serverSource, "/v1.0/indices/{indexId}/search", "index search route");
                AssertHelper.StringContains(indexHandlerSource, "PostSearchAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Post, \"search\", ctx.Request.DataAsString)", "index search proxy body forwarding");
            });

            await ExecuteTestAsync("RecallDB collection search proxy: route forwards request body", async () =>
            {
                string root = GetRepositoryRoot();
                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string collectionHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "CollectionHandler.cs"));

                AssertHelper.StringContains(serverSource, "/v1.0/collections/{collectionId}/search", "collection search route");
                AssertHelper.StringContains(serverSource, "collectionHandler.SearchCollectionAsync", "collection search handler registration");
                AssertHelper.StringContains(collectionHandlerSource, "SearchCollectionAsync(HttpContextBase ctx)", "collection search handler");
                AssertHelper.StringContains(collectionHandlerSource, "BuildRecallDbPath(auth.TenantId, collectionId + \"/search\"), body", "collection search proxy body forwarding");
            });

            await ExecuteTestAsync("OpenAPI: search artifact routes and request bodies are documented", async () =>
            {
                string root = GetRepositoryRoot();
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
                JsonElement paths = document.RootElement.GetProperty("paths");

                AssertOpenApiOperation(paths, "/v1.0/collections/{collectionId}/search", "post", "collection search OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices", "get", "index list OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices", "put", "index create OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/search", "post", "index search OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/labels", "put", "index labels OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/tags", "put", "index tags OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/custom-metadata", "put", "index custom metadata OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/terms/top", "get", "index top terms OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records", "get", "index record list OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records", "put", "index record create OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records", "delete", "index record batch delete OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/batch", "post", "index record batch create OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/exists", "post", "index record exists OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/{recordId}", "get", "index record get OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/{recordId}", "delete", "index record delete OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/{recordId}/labels", "put", "index record labels OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/{recordId}/tags", "put", "index record tags OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/indices/{indexId}/records/{recordId}/custom-metadata", "put", "index record custom metadata OpenAPI route");

                JsonElement indexSearch = paths.GetProperty("/v1.0/indices/{indexId}/search").GetProperty("post");
                AssertHelper.IsTrue(indexSearch.TryGetProperty("requestBody", out _), "Index search OpenAPI request body");
                JsonElement indexSearchSchema = indexSearch
                    .GetProperty("requestBody")
                    .GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema");
                JsonElement indexSearchProperties = indexSearchSchema.TryGetProperty("properties", out JsonElement inlineProperties)
                    ? inlineProperties
                    : document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("SearchRequest").GetProperty("properties");
                AssertHelper.IsTrue(indexSearchProperties.TryGetProperty("IncludeMatchedTerms", out _), "Index search OpenAPI IncludeMatchedTerms");
                AssertHelper.IsTrue(indexSearchProperties.TryGetProperty("IncludeTermDetails", out _), "Index search OpenAPI IncludeTermDetails");
                AssertHelper.IsTrue(indexSearchProperties.TryGetProperty("IncludeDocumentTermStats", out _), "Index search OpenAPI IncludeDocumentTermStats");
                JsonElement collectionSearch = paths.GetProperty("/v1.0/collections/{collectionId}/search").GetProperty("post");
                AssertHelper.IsTrue(collectionSearch.TryGetProperty("requestBody", out _), "Collection search OpenAPI request body");
            });

            await ExecuteTestAsync("OpenAPI: crawl plan repository types are documented", async () =>
            {
                string root = GetRepositoryRoot();
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
                JsonElement paths = document.RootElement.GetProperty("paths");
                JsonElement schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

                AssertOpenApiOperation(paths, "/v1.0/crawlplans", "put", "crawl plan create OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/crawlplans", "get", "crawl plan list OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/crawlplans/connectivity", "post", "crawl plan draft connectivity OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/crawlplans/{id}", "get", "crawl plan get OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/crawlplans/{id}/connectivity", "post", "crawl plan connectivity OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/crawlplans/{id}/enumerate", "get", "crawl plan enumerate OpenAPI route");

                JsonElement repositoryTypeEnum = schemas.GetProperty("RepositoryTypeEnum").GetProperty("enum");
                AssertHelper.IsTrue(repositoryTypeEnum.ToString().Contains("Web"), "OpenAPI repository type Web");
                AssertHelper.IsTrue(repositoryTypeEnum.ToString().Contains("CIFS"), "OpenAPI repository type CIFS");
                AssertHelper.IsTrue(repositoryTypeEnum.ToString().Contains("NFS"), "OpenAPI repository type NFS");

                AssertHelper.IsTrue(schemas.TryGetProperty("CrawlRepositorySettings", out JsonElement settingsSchema), "OpenAPI CrawlRepositorySettings schema");
                AssertHelper.IsTrue(settingsSchema.TryGetProperty("oneOf", out _), "OpenAPI CrawlRepositorySettings oneOf");
                AssertHelper.IsTrue(schemas.TryGetProperty("CifsCrawlRepositorySettings", out _), "OpenAPI CIFS settings schema");
                AssertHelper.IsTrue(schemas.TryGetProperty("NfsCrawlRepositorySettings", out _), "OpenAPI NFS settings schema");
                AssertHelper.IsTrue(schemas.TryGetProperty("NfsVersionEnum", out _), "OpenAPI NFS version schema");
            });

            await ExecuteTestAsync("API route contracts: backend, OpenAPI, Postman, REST docs, and explorer stay aligned", async () =>
            {
                string root = GetRepositoryRoot();
                SortedSet<string> backendRoutes = ExtractBackendRoutes(root);
                SortedSet<string> openApiRoutes = ExtractOpenApiRoutes(root);
                SortedSet<string> postmanRoutes = ExtractPostmanRoutes(root);
                SortedSet<string> restRoutes = ExtractRestApiRoutes(root);

                AssertHelper.AreEqual(183, backendRoutes.Count, "backend route count");
                AssertRouteSetsEqual(backendRoutes, openApiRoutes, "OpenAPI");
                AssertRouteSetsEqual(backendRoutes, postmanRoutes, "Postman");
                AssertRouteSetsEqual(backendRoutes, restRoutes, "REST_API.md");

                using JsonDocument routeDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
                JsonElement routePaths = routeDocument.RootElement.GetProperty("paths");
                JsonElement swaggerOperation = routePaths.GetProperty("/swagger").GetProperty("get");
                JsonElement swaggerSecurity = swaggerOperation.GetProperty("security");
                JsonElement securitySchemes = routeDocument.RootElement.GetProperty("components").GetProperty("securitySchemes");
                JsonElement bearerAuth;
                AssertHelper.AreEqual(0, swaggerSecurity.GetArrayLength(), "Swagger UI OpenAPI route should not require auth");
                AssertHelper.IsTrue(securitySchemes.TryGetProperty("BearerAuth", out bearerAuth), "OpenAPI BearerAuth security scheme");

                string serverSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                string explorerSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "ApiExplorerView.jsx"));
                string explorerUtilsSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "apiExplorerUtils.js"));
                string crawlPlanModalSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "components", "modals", "CrawlPlanFormModal.jsx"));
                string dashboardApiSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "utils", "api.js"));
                string csharpSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "csharp", "AssistantHub.Sdk", "AssistantHubClientResourceParityBase.cs"));
                string jsSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "js", "src", "client.ts"));
                string pythonSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "python", "assistanthub_sdk", "client.py"));
                string mcpDocs = File.ReadAllText(Path.Combine(root, "MCP_API.md"));
                AssertHelper.StringContains(serverSource, "Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, \"/swagger\", openApiHandler.GetSwaggerAsync)", "Swagger UI pre-auth route");
                AssertHelper.StringContains(explorerSource, "operation.tags?.[0] === 'Assistant Public APIs'", "API Explorer public assistant OpenAPI merge");
                AssertHelper.StringContains(explorerUtilsSource, "POST:/v1.0/crawlplans/connectivity", "API Explorer draft crawl connectivity template");
                AssertHelper.StringContains(explorerUtilsSource, "assistant-chat-open", "API Explorer chat-open template");
                AssertHelper.StringContains(explorerUtilsSource, "assistant-document-download", "API Explorer document download template");
                AssertHelper.StringContains(crawlPlanModalSource, "Test Connectivity", "Create Crawl Plan modal connectivity button");
                AssertHelper.StringContains(dashboardApiSource, "/v1.0/crawlplans/connectivity", "Dashboard draft crawl connectivity client route");
                AssertHelper.StringContains(csharpSdkSource, "TestCrawlPlanDraftConnectivityAsync", "C# SDK draft crawl connectivity method");
                AssertHelper.StringContains(jsSdkSource, "testCrawlPlanDraftConnectivity", "JS SDK draft crawl connectivity method");
                AssertHelper.StringContains(pythonSdkSource, "test_crawl_plan_draft_connectivity", "Python SDK draft crawl connectivity method");
                AssertHelper.StringContains(mcpDocs, "`GET /swagger`", "MCP docs Swagger UI route");
                AssertHelper.StringContains(mcpDocs, "`crawl plan draft connectivity` | None | Deferred", "MCP docs draft crawl connectivity deferred");
                AssertHelper.StringContains(mcpDocs, "`GET /v1.0/openapi.json`", "MCP docs versioned OpenAPI route");
                AssertHelper.StringContains(mcpDocs, "`embedding endpoint load` | None | Deferred", "MCP docs embedding load deferred");
                AssertHelper.StringContains(mcpDocs, "`completion endpoint load` | None | Deferred", "MCP docs completion load deferred");
            });

            await ExecuteTestAsync("Postman: search artifact requests are present", async () =>
            {
                string root = GetRepositoryRoot();
                string postman = File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json"));
                JsonDocument.Parse(postman).Dispose();

                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/collections/{{collectionId}}/search", "collection search Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/search", "index search Postman request");
                AssertHelper.StringContains(postman, "\\\"IncludeMatchedTerms\\\": true", "index search Postman IncludeMatchedTerms");
                AssertHelper.StringContains(postman, "\\\"IncludeTermDetails\\\": true", "index search Postman IncludeTermDetails");
                AssertHelper.StringContains(postman, "\\\"IncludeDocumentTermStats\\\": true", "index search Postman IncludeDocumentTermStats");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/terms/top", "index top terms Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/custom-metadata", "index custom metadata Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/records/{{recordId}}/labels", "index record labels Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/records/{{recordId}}/custom-metadata", "index record custom metadata Postman request");
            });

            await ExecuteTestAsync("Postman: crawl plan repository examples are present", async () =>
            {
                string root = GetRepositoryRoot();
                string postman = File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json"));
                JsonDocument.Parse(postman).Dispose();

                AssertHelper.StringContains(postman, "\"name\": \"Create Web Crawl Plan\"", "Postman web crawl plan request");
                AssertHelper.StringContains(postman, "\"name\": \"Create CIFS Crawl Plan\"", "Postman CIFS crawl plan request");
                AssertHelper.StringContains(postman, "\"name\": \"Create NFS Crawl Plan\"", "Postman NFS crawl plan request");
                AssertHelper.StringContains(postman, "\"key\": \"cifsHostname\"", "Postman CIFS hostname variable");
                AssertHelper.StringContains(postman, "\"key\": \"nfsHostname\"", "Postman NFS hostname variable");
                AssertHelper.StringContains(postman, "\\\"CifsHostname\\\": \\\"{{cifsHostname}}\\\"", "Postman CIFS settings payload");
                AssertHelper.StringContains(postman, "\\\"NfsUserId\\\": {{nfsUserId}}", "Postman NFS user ID payload");
            });

            return GetResults();
        }

        private static void AssertOpenApiOperation(JsonElement paths, string path, string method, string name)
        {
            AssertHelper.IsTrue(paths.TryGetProperty(path, out JsonElement pathItem), name + " path");
            AssertHelper.IsTrue(pathItem.TryGetProperty(method, out _), name + " method");
        }

        private static void AssertRouteSetsEqual(SortedSet<string> expected, SortedSet<string> actual, string artifactName)
        {
            List<string> missing = expected.Where(route => !actual.Contains(route)).ToList();
            List<string> extra = actual.Where(route => !expected.Contains(route)).ToList();

            AssertHelper.IsTrue(
                missing.Count == 0,
                artifactName + " missing backend routes: " + String.Join(", ", missing.Take(20)));

            AssertHelper.IsTrue(
                extra.Count == 0,
                artifactName + " has routes not registered by backend: " + String.Join(", ", extra.Take(20)));
        }

        private static SortedSet<string> ExtractBackendRoutes(string root)
        {
            string source = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
            Regex regex = new Regex("Routes\\.(PreAuthentication|PostAuthentication)\\.(Static|Parameter)\\.Add\\(WatsonWebserver\\.Core\\.HttpMethod\\.(\\w+),\\s*\"([^\"]+)\"");
            MatchCollection matches = regex.Matches(source);
            SortedSet<string> routes = new SortedSet<string>();

            foreach (Match match in matches)
            {
                routes.Add(match.Groups[3].Value.ToUpperInvariant() + " " + match.Groups[4].Value);
            }

            return routes;
        }

        private static SortedSet<string> ExtractOpenApiRoutes(string root)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
            SortedSet<string> routes = new SortedSet<string>();
            JsonElement paths = document.RootElement.GetProperty("paths");

            foreach (JsonProperty pathProperty in paths.EnumerateObject())
            {
                foreach (JsonProperty methodProperty in pathProperty.Value.EnumerateObject())
                {
                    string method = methodProperty.Name.ToUpperInvariant();
                    if (IsHttpMethod(method))
                    {
                        routes.Add(method + " " + pathProperty.Name);
                    }
                }
            }

            return routes;
        }

        private static SortedSet<string> ExtractPostmanRoutes(string root)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json")));
            SortedSet<string> routes = new SortedSet<string>();
            JsonElement items = document.RootElement.GetProperty("item");
            ExtractPostmanRoutesRecursive(items, routes);
            return routes;
        }

        private static void ExtractPostmanRoutesRecursive(JsonElement items, SortedSet<string> routes)
        {
            foreach (JsonElement item in items.EnumerateArray())
            {
                if (item.TryGetProperty("item", out JsonElement children))
                {
                    ExtractPostmanRoutesRecursive(children, routes);
                }

                if (!item.TryGetProperty("request", out JsonElement request)) continue;
                if (!request.TryGetProperty("method", out JsonElement methodElement)) continue;
                if (!request.TryGetProperty("url", out JsonElement urlElement)) continue;

                string method = methodElement.GetString()?.ToUpperInvariant();
                string rawUrl = null;

                if (urlElement.ValueKind == JsonValueKind.String)
                {
                    rawUrl = urlElement.GetString();
                }
                else if (urlElement.ValueKind == JsonValueKind.Object && urlElement.TryGetProperty("raw", out JsonElement rawElement))
                {
                    rawUrl = rawElement.GetString();
                }

                if (String.IsNullOrEmpty(method) || String.IsNullOrEmpty(rawUrl)) continue;
                string path = rawUrl.Replace("{{baseUrl}}", "");
                routes.Add(method + " " + NormalizeRoutePath(path));
            }
        }

        private static SortedSet<string> ExtractRestApiRoutes(string root)
        {
            string source = File.ReadAllText(Path.Combine(root, "REST_API.md"));
            Regex regex = new Regex("^###\\s+(GET|POST|PUT|DELETE|HEAD|PATCH)\\s+([^\\s]+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(source);
            SortedSet<string> routes = new SortedSet<string>();

            foreach (Match match in matches)
            {
                routes.Add(match.Groups[1].Value.ToUpperInvariant() + " " + NormalizeRoutePath(match.Groups[2].Value));
            }

            return routes;
        }

        private static string NormalizeRoutePath(string path)
        {
            string normalized = path;
            int queryIndex = normalized.IndexOf("?", StringComparison.Ordinal);
            if (queryIndex >= 0) normalized = normalized.Substring(0, queryIndex);
            normalized = Regex.Replace(normalized, "\\{\\{([A-Za-z0-9_]+)\\}\\}", "{$1}");
            normalized = Regex.Replace(normalized, ":([A-Za-z0-9_]+)", "{$1}");
            return normalized;
        }

        private static bool IsHttpMethod(string method)
        {
            return
                method == "GET" ||
                method == "POST" ||
                method == "PUT" ||
                method == "DELETE" ||
                method == "HEAD" ||
                method == "PATCH";
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                bool hasDashboard = Directory.Exists(Path.Combine(directory.FullName, "dashboard"));
                bool hasSource = Directory.Exists(Path.Combine(directory.FullName, "src"));
                bool hasRestApi = File.Exists(Path.Combine(directory.FullName, "REST_API.md"));

                if (hasDashboard && hasSource && hasRestApi)
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the AssistantHub repository root.");
        }
    }
}
