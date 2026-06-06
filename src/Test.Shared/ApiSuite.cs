namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
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
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/records/{{indexRecordId}}/labels", "index record labels Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/indices/{{indexId}}/records/{{indexRecordId}}/custom-metadata", "index record custom metadata Postman request");
            });

            return GetResults();
        }

        private static void AssertOpenApiOperation(JsonElement paths, string path, string method, string name)
        {
            AssertHelper.IsTrue(paths.TryGetProperty(path, out JsonElement pathItem), name + " path");
            AssertHelper.IsTrue(pathItem.TryGetProperty(method, out _), name + " method");
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
