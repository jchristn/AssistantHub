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

            await ExecuteTestAsync("Auth.GlobalAdminBypass: audits stable bypass reasons", async () =>
            {
                string root = GetRepositoryRoot();
                string handlerBaseSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "HandlerBase.cs"));
                AssertHelper.StringContains(handlerBaseSource, "global admin bypass audit", "global admin bypass audit log");
                AssertHelper.StringContains(handlerBaseSource, "tenant_access_validation", "tenant access bypass reason");
                AssertHelper.StringContains(handlerBaseSource, "tenant_ownership_enforcement", "tenant ownership bypass reason");
                AssertHelper.StringContains(handlerBaseSource, "targetTenantId", "target tenant is logged");
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

            await ExecuteTestAsync("OpenAPI: attached-document chat contract is documented", async () =>
            {
                string root = GetRepositoryRoot();
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
                JsonElement paths = document.RootElement.GetProperty("paths");
                JsonElement schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/documents", "get", "public assistant documents OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/chat", "post", "public assistant chat OpenAPI route");

                JsonElement documentsOperation = paths.GetProperty("/v1.0/assistants/{assistantId}/documents").GetProperty("get");
                AssertHelper.IsTrue(HasParameter(documentsOperation, "query", "query"), "assistant documents query parameter");
                AssertHelper.IsTrue(HasParameter(documentsOperation, "contentType", "query"), "assistant documents contentType parameter");

                JsonElement documentSelectionSchema = schemas.GetProperty("AssistantDocumentSelectionItem");
                JsonElement documentSelectionProperties = documentSelectionSchema.GetProperty("properties");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("Id", out _), "selection Id");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("Name", out _), "selection Name");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("OriginalFilename", out _), "selection OriginalFilename");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("ContentType", out _), "selection ContentType");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("SizeBytes", out _), "selection SizeBytes");
                AssertHelper.IsTrue(documentSelectionProperties.TryGetProperty("SourceUrl", out _), "selection SourceUrl");
                AssertHelper.IsFalse(documentSelectionProperties.TryGetProperty("S3Key", out _), "selection hides S3Key");
                AssertHelper.IsFalse(documentSelectionProperties.TryGetProperty("BucketName", out _), "selection hides BucketName");

                JsonElement chatRequestProperties = schemas.GetProperty("ChatRequest").GetProperty("properties");
                AssertHelper.IsTrue(chatRequestProperties.TryGetProperty("attached_document_ids", out JsonElement attachedIdsSchema), "chat attached_document_ids");
                AssertHelper.AreEqual("array", attachedIdsSchema.GetProperty("type").GetString(), "attached_document_ids is array");

                JsonElement retrievalProperties = schemas.GetProperty("ChatCompletionRetrieval").GetProperty("properties");
                AssertHelper.IsTrue(retrievalProperties.TryGetProperty("attached_document_ids", out _), "retrieval attached_document_ids");
                AssertHelper.IsTrue(retrievalProperties.TryGetProperty("attached_documents", out _), "retrieval attached_documents");
                AssertHelper.IsTrue(retrievalProperties.TryGetProperty("document_filter_applied", out _), "retrieval document_filter_applied");

                JsonElement chatResultProperties = schemas.GetProperty("ChatResult").GetProperty("properties");
                AssertHelper.IsTrue(chatResultProperties.TryGetProperty("tool_calls", out JsonElement toolCallsSchema), "chat result tool_calls");
                AssertHelper.AreEqual("array", toolCallsSchema.GetProperty("type").GetString(), "chat result tool_calls is array");

                JsonElement toolTraceProperties = schemas.GetProperty("ChatCompletionToolTrace").GetProperty("properties");
                AssertHelper.IsTrue(toolTraceProperties.TryGetProperty("tool_name", out _), "tool trace tool_name");
                AssertHelper.IsTrue(toolTraceProperties.TryGetProperty("display_label", out _), "tool trace display_label");
                AssertHelper.IsTrue(toolTraceProperties.TryGetProperty("duration_ms", out _), "tool trace duration_ms");
                AssertHelper.IsTrue(toolTraceProperties.TryGetProperty("credits_used", out _), "tool trace credits_used");
                AssertHelper.IsTrue(toolTraceProperties.TryGetProperty("provider_latency_ms", out _), "tool trace provider_latency_ms");
                AssertHelper.IsFalse(toolTraceProperties.TryGetProperty("arguments_json", out _), "tool trace hides arguments");
                AssertHelper.IsFalse(toolTraceProperties.TryGetProperty("output_json", out _), "tool trace hides raw output");
            });

            await ExecuteTestAsync("OpenAPI: assistant tool-call trace routes are documented", async () =>
            {
                string root = GetRepositoryRoot();
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "openapi.json")));
                JsonElement paths = document.RootElement.GetProperty("paths");
                JsonElement schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/tool-calls", "get", "assistant tool-call list OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}", "get", "assistant tool-call get OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}", "delete", "assistant tool-call delete OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/assistants/{assistantId}/settings/tools/test", "post", "assistant tool diagnostics OpenAPI route");
                AssertOpenApiOperation(paths, "/v1.0/configuration/external-search/status", "get", "external-search status OpenAPI route");

                JsonElement listOperation = paths.GetProperty("/v1.0/assistants/{assistantId}/tool-calls").GetProperty("get");
                AssertHelper.IsTrue(HasParameter(listOperation, "toolName", "query"), "tool-call list toolName parameter");
                AssertHelper.IsTrue(HasParameter(listOperation, "traceId", "query"), "tool-call list traceId parameter");
                AssertHelper.IsTrue(HasParameter(listOperation, "requestHistoryId", "query"), "tool-call list requestHistoryId parameter");
                AssertHelper.IsTrue(HasParameter(listOperation, "success", "query"), "tool-call list success parameter");
                AssertHelper.IsTrue(HasParameter(listOperation, "denied", "query"), "tool-call list denied parameter");

                JsonElement traceSchema = schemas.GetProperty("AssistantToolCallRecord");
                JsonElement traceProperties = traceSchema.GetProperty("properties");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("ArgumentsJson", out _), "tool-call trace ArgumentsJson");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("OutputJson", out _), "tool-call trace OutputJson");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("ResultSummaryJson", out _), "tool-call trace ResultSummaryJson");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("InputBytes", out _), "tool-call trace InputBytes");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("OutputBytes", out _), "tool-call trace OutputBytes");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("ErrorType", out _), "tool-call trace ErrorType");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("Provider", out _), "tool-call trace Provider");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("Model", out _), "tool-call trace Model");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("LastUpdateUtc", out _), "tool-call trace LastUpdateUtc");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("RequestHistoryId", out _), "tool-call trace RequestHistoryId");
                AssertHelper.IsTrue(traceProperties.TryGetProperty("ChatHistoryId", out _), "tool-call trace ChatHistoryId");

                JsonElement toolPolicySchema = schemas.GetProperty("AssistantToolPolicy");
                JsonElement toolPolicyExample = toolPolicySchema.GetProperty("example");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableToolCalls").GetBoolean(), "tool policy example disables tool calls");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableCollectionSearchTool").GetBoolean(), "tool policy example disables collection search");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableVerbexFullTextSearchTool").GetBoolean(), "tool policy example disables Verbex search");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableS3ObjectReadTool").GetBoolean(), "tool policy example disables S3 object read");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableBucketEnumerateObjectsTool").GetBoolean(), "tool policy example disables bucket enumeration");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("EnableWebSearchTool").GetBoolean(), "tool policy example disables web search");
                AssertHelper.AreEqual(1000, toolPolicyExample.GetProperty("MaxDocumentsConsideredPerSearch").GetInt32(), "tool policy example max documents considered");
                AssertHelper.AreEqual(1000, toolPolicyExample.GetProperty("MaxResultsConsideredPerSearch").GetInt32(), "tool policy example max results considered");
                AssertHelper.AreEqual(false, toolPolicyExample.GetProperty("ReturnFullSearchContent").GetBoolean(), "tool policy example uses excerpt-only collection search");
                AssertHelper.AreEqual(JsonValueKind.Array, toolPolicyExample.GetProperty("AllowedBucketPrefixes").ValueKind, "tool policy example includes AllowedBucketPrefixes");
                AssertHelper.AreEqual(JsonValueKind.Array, toolPolicyExample.GetProperty("AllowedWebDomains").ValueKind, "tool policy example includes AllowedWebDomains");

                JsonElement externalSearchStatusSchema = schemas.GetProperty("ExternalSearchConfigurationStatus").GetProperty("properties");
                AssertHelper.IsTrue(externalSearchStatusSchema.TryGetProperty("Enabled", out _), "external-search status Enabled");
                AssertHelper.IsTrue(externalSearchStatusSchema.TryGetProperty("EnabledProviders", out _), "external-search status EnabledProviders");
                AssertHelper.IsTrue(externalSearchStatusSchema.TryGetProperty("ConfiguredProviders", out _), "external-search status ConfiguredProviders");
                AssertHelper.IsTrue(externalSearchStatusSchema.TryGetProperty("MisconfiguredProviders", out _), "external-search status MisconfiguredProviders");
            });

            await ExecuteTestAsync("Database contracts: chat history persists attached-document metadata across providers", async () =>
            {
                string root = GetRepositoryRoot();
                string[] queryFiles =
                {
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Sqlite", "Queries", "TableQueries.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Postgresql", "Queries", "TableQueries.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Mysql", "Queries", "TableQueries.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "SqlServer", "Queries", "TableQueries.cs")
                };

                foreach (string file in queryFiles)
                {
                    string source = File.ReadAllText(file);
                    AssertHelper.StringContains(source, "attached_document_ids_json", Path.GetFileName(file) + " attached_document_ids_json column");
                    AssertHelper.StringContains(source, "attached_documents_json", Path.GetFileName(file) + " attached_documents_json column");
                    AssertHelper.StringContains(source, "AddChatHistoryAttachedDocumentIdsJsonColumn", Path.GetFileName(file) + " attached IDs migration");
                    AssertHelper.StringContains(source, "AddChatHistoryAttachedDocumentsJsonColumn", Path.GetFileName(file) + " attached docs migration");
                }

                string[] implementationFiles =
                {
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Sqlite", "Implementations", "ChatHistoryMethods.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Postgresql", "Implementations", "ChatHistoryMethods.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "Mysql", "Implementations", "ChatHistoryMethods.cs"),
                    Path.Combine(root, "src", "AssistantHub.Core", "Database", "SqlServer", "Implementations", "ChatHistoryMethods.cs")
                };

                foreach (string file in implementationFiles)
                {
                    string source = File.ReadAllText(file);
                    AssertHelper.StringContains(source, "attached_document_ids_json", Path.GetFileName(file) + " inserts attached_document_ids_json");
                    AssertHelper.StringContains(source, "AttachedDocumentIdsJson", Path.GetFileName(file) + " writes AttachedDocumentIdsJson");
                    AssertHelper.StringContains(source, "attached_documents_json", Path.GetFileName(file) + " inserts attached_documents_json");
                    AssertHelper.StringContains(source, "AttachedDocumentsJson", Path.GetFileName(file) + " writes AttachedDocumentsJson");
                }

                string sqliteDriver = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Database", "Sqlite", "SqliteDatabaseDriver.cs"));
                string mysqlDriver = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Database", "Mysql", "MysqlDatabaseDriver.cs"));
                string postgresqlDriver = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Database", "Postgresql", "PostgresqlDatabaseDriver.cs"));
                string sqlServerDriver = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Database", "SqlServer", "SqlServerDatabaseDriver.cs"));

                foreach (string source in new[] { sqliteDriver, mysqlDriver, postgresqlDriver, sqlServerDriver })
                {
                    AssertHelper.StringContains(source, "AddChatHistoryAttachedDocumentIdsJsonColumn", "driver attached IDs migration");
                    AssertHelper.StringContains(source, "AddChatHistoryAttachedDocumentsJsonColumn", "driver attached docs migration");
                }
            });

            await ExecuteTestAsync("API route contracts: backend, OpenAPI, Postman, REST docs, and explorer stay aligned", async () =>
            {
                string root = GetRepositoryRoot();
                SortedSet<string> backendRoutes = ExtractBackendRoutes(root);
                SortedSet<string> openApiRoutes = ExtractOpenApiRoutes(root);
                SortedSet<string> postmanRoutes = ExtractPostmanRoutes(root);
                SortedSet<string> restRoutes = ExtractRestApiRoutes(root);

                AssertHelper.AreEqual(192, backendRoutes.Count, "backend route count");
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
                string chatRequestSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Models", "ChatCompletionRequest.cs"));
                string chatResponseSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Core", "Models", "ChatCompletionResponse.cs"));
                string chatHandlerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "ChatHandler.cs"));
                string chatHandlerBaseSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "ChatHandlerExecutionBase.cs"));
                string assistantChatServiceSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Services", "AssistantChatService.cs"));
                string slackWorkerSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Services", "SlackAssistantWorker.cs"));
                string slackUtilitiesSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Services", "SlackAssistantUtilities.cs"));
                string chatPanelSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "components", "ChatPanel.jsx"));
                string appCssSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "App.css"));
                string mcpAssistantRegistrationSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.McpServer", "Registrations", "AssistantRegistrations.cs"));
                string mcpAssistantSettingsRegistrationSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.McpServer", "Registrations", "AssistantSettingsRegistrations.cs"));
                string mcpAssistantToolCallRegistrationSource = File.ReadAllText(Path.Combine(root, "src", "AssistantHub.McpServer", "Registrations", "AssistantToolCallRegistrations.cs"));
                string csharpSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "csharp", "AssistantHub.Sdk", "AssistantHubClientResourceParityBase.cs"));
                string jsSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "js", "src", "client.ts"));
                string pythonSdkSource = File.ReadAllText(Path.Combine(root, "sdk", "python", "assistanthub_sdk", "client.py"));
                string mcpDocs = File.ReadAllText(Path.Combine(root, "MCP_API.md"));
                AssertHelper.StringContains(serverSource, "Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.GET, \"/swagger\", openApiHandler.GetSwaggerAsync)", "Swagger UI pre-auth route");
                AssertHelper.StringContains(serverSource, "/v1.0/configuration/external-search/status", "external-search status backend route");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "src", "AssistantHub.Server", "Handlers", "ConfigurationHandler.cs")), "GetExternalSearchStatusAsync", "external-search status handler");
                AssertHelper.StringContains(explorerSource, "operation.tags?.[0] === 'Assistant Public APIs'", "API Explorer public assistant OpenAPI merge");
                AssertHelper.StringContains(explorerUtilsSource, "POST:/v1.0/crawlplans/connectivity", "API Explorer draft crawl connectivity template");
                AssertHelper.StringContains(explorerUtilsSource, "assistant-chat-open", "API Explorer chat-open template");
                AssertHelper.StringContains(explorerUtilsSource, "assistant-document-download", "API Explorer document download template");
                AssertHelper.StringContains(crawlPlanModalSource, "Test Connectivity", "Create Crawl Plan modal connectivity button");
                AssertHelper.StringContains(dashboardApiSource, "/v1.0/crawlplans/connectivity", "Dashboard draft crawl connectivity client route");
                AssertHelper.StringContains(dashboardApiSource, "getAssistantToolCalls", "Dashboard assistant tool-call list method");
                AssertHelper.StringContains(dashboardApiSource, "/v1.0/assistants/${assistantId}/tool-calls", "Dashboard assistant tool-call route");
                AssertHelper.StringContains(dashboardApiSource, "testAssistantToolPolicy", "Dashboard assistant tool-policy diagnostics method");
                AssertHelper.StringContains(dashboardApiSource, "getExternalSearchStatus", "Dashboard external-search status client method");
                AssertHelper.StringContains(dashboardApiSource, "/v1.0/configuration/external-search/status", "Dashboard external-search status client route");
                AssertHelper.StringContains(dashboardApiSource, "assistant.tool_", "Dashboard streaming tool event parser");
                AssertHelper.StringContains(dashboardApiSource, "assistant.tool_call.interrupted", "Dashboard marks interrupted tool-progress streams");
                AssertHelper.StringContains(dashboardApiSource, "tool_stream_interrupted", "Dashboard interrupted stream status code");
                AssertHelper.StringContains(dashboardApiSource, "One tool failed; trying another source", "Dashboard safe tool failure status");
                AssertHelper.StringContains(dashboardApiSource, "Running tool:", "Dashboard running tool status copy");
                AssertHelper.StringContains(dashboardApiSource, "still running", "Dashboard heartbeat tool status");
                AssertHelper.StringContains(dashboardApiSource, "searches", "Dashboard coalesces repeated tool search status");
                AssertHelper.StringContains(chatRequestSource, "attached_document_ids", "Chat request supports document attachments");
                AssertHelper.IsFalse(chatRequestSource.Contains("ToolPolicy"), "Public chat request must not expose tool policy override fields");
                AssertHelper.StringContains(chatResponseSource, "tool_calls", "Chat response supports safe tool trace metadata");
                AssertHelper.StringContains(chatHandlerSource, "HandleToolAwareStreamingChatAsync", "Chat handler streams tool-aware chat through shared service");
                AssertHelper.StringContains(chatHandlerBaseSource, "WriteSseNamedEvent", "Chat handler can write named SSE events");
                AssertHelper.StringContains(assistantChatServiceSource, "assistant.tool_call.started", "Assistant chat service emits tool started events");
                AssertHelper.StringContains(assistantChatServiceSource, "assistant.tool_call.heartbeat", "Assistant chat service emits long-running tool heartbeat events");
                AssertHelper.StringContains(assistantChatServiceSource, "assistant.tool_call.completed", "Assistant chat service emits tool completed events");
                AssertHelper.StringContains(assistantChatServiceSource, "assistant.tool_call.failed", "Assistant chat service emits tool failed events");
                AssertHelper.StringContains(assistantChatServiceSource, "assistant.tool_call.denied", "Assistant chat service emits tool denied events");
                AssertHelper.StringContains(assistantChatServiceSource, "Checking tools", "Assistant chat service emits well-formed iteration status label");
                AssertHelper.StringContains(assistantChatServiceSource, "tool policy denial", "Assistant chat service logs tool policy denial audit events");
                AssertHelper.StringContains(assistantChatServiceSource, "tool audit event", "Assistant chat service logs sensitive tool audit events");
                AssertHelper.StringContains(assistantChatServiceSource, "IsSensitiveToolAuditTool", "Assistant chat service scopes sensitive tool audit logging");
                AssertHelper.StringContains(slackWorkerSource, "EnableSlackToolProgressMessages", "Slack worker respects tool progress setting");
                AssertHelper.StringContains(slackWorkerSource, "ToolProgress = emitSlackToolProgress", "Slack worker registers tool progress callback");
                AssertHelper.StringContains(slackWorkerSource, "SendSlackToolProgressAsync", "Slack worker emits tool progress messages");
                AssertHelper.StringContains(slackUtilitiesSource, "ShapeSlackToolProgressMessage", "Slack utility shapes safe tool progress text");
                AssertHelper.StringContains(slackUtilitiesSource, "Tool running:", "Slack running tool progress text");
                AssertHelper.StringContains(slackUtilitiesSource, "Tool completed:", "Slack completed tool progress text");
                AssertHelper.StringContains(slackUtilitiesSource, "Tool failed:", "Slack failed tool progress text");
                AssertHelper.StringContains(chatPanelSource, "chat-pending-content-wrap", "Dashboard tool status shares assistant pending content column");
                AssertHelper.StringContains(chatPanelSource, "title={toolStatus || waitMessage}", "Dashboard long tool status has hover text");
                AssertHelper.StringContains(chatPanelSource, "The assistant tool stream was interrupted", "Dashboard interrupted tool stream copy");
                AssertHelper.StringContains(appCssSource, ".chat-pending-content-wrap", "Dashboard pending status column CSS");
                AssertHelper.StringContains(appCssSource, "text-overflow: ellipsis", "Dashboard tool status truncates long labels");
                AssertHelper.StringContains(appCssSource, "max-width: min(420px, 100%)", "Dashboard tool status constrained to message column");
                string assistantToolTraceSectionSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "components", "modals", "AssistantToolCallTraceSection.jsx"));
                AssertHelper.StringContains(assistantToolTraceSectionSource, "ToolCallTimeline", "Dashboard admin tool-call progress timeline");
                AssertHelper.StringContains(assistantToolTraceSectionSource, "ResultSummaryJson", "Dashboard admin tool-call result summary");
                AssertHelper.StringContains(assistantToolTraceSectionSource, "InputBytes", "Dashboard admin tool-call input bytes");
                AssertHelper.StringContains(assistantToolTraceSectionSource, "OutputBytes", "Dashboard admin tool-call output bytes");
                AssertHelper.StringContains(assistantToolTraceSectionSource, "runtimeLabel", "Dashboard admin tool-call provider/model display");
                AssertHelper.StringContains(appCssSource, ".tool-call-timeline", "Dashboard admin tool-call timeline CSS");
                string assistantSettingsViewSource = File.ReadAllText(Path.Combine(root, "dashboard", "src", "views", "AssistantSettingsView.jsx"));
                AssertHelper.StringContains(assistantSettingsViewSource, "Reset Disabled", "Assistant settings tool policy reset action");
                AssertHelper.StringContains(assistantSettingsViewSource, "Search Collection", "Assistant settings collection tool toggle");
                AssertHelper.StringContains(assistantSettingsViewSource, "Full-Text Search", "Assistant settings Verbex tool toggle");
                AssertHelper.StringContains(assistantSettingsViewSource, "Read Objects", "Assistant settings S3 tool toggle");
                AssertHelper.StringContains(assistantSettingsViewSource, "Tavily Web Search", "Assistant settings Tavily tool toggle");
                AssertHelper.StringContains(assistantSettingsViewSource, "System Tavily:", "Assistant settings displays global Tavily status");
                AssertHelper.StringContains(assistantSettingsViewSource, "externalSearchStatus", "Assistant settings loads external-search status");
                AssertHelper.StringContains(assistantSettingsViewSource, "ErrorCodes", "Assistant settings displays tool policy validation error codes");
                AssertHelper.StringContains(assistantSettingsViewSource, "Run Diagnostics", "Assistant settings exposes tool diagnostics action");
                AssertHelper.StringContains(assistantSettingsViewSource, "api.getIndices({ maxResults: 1000 })", "Assistant settings loads Verbex indices for tool policy selects");
                AssertHelper.StringContains(assistantSettingsViewSource, "api.getBuckets({ maxResults: 1000 })", "Assistant settings loads S3 buckets for tool policy selects");
                AssertHelper.StringContains(assistantSettingsViewSource, "handleToolPolicyChange('DefaultIndexId', e.target.value)", "Assistant settings Default Index ID dropdown updates tool policy");
                AssertHelper.StringContains(assistantSettingsViewSource, "handleToolPolicyChange('AllowedVerbexIndexIds', getMultiSelectValues(e))", "Assistant settings Allowed Index IDs multi-select updates tool policy");
                AssertHelper.StringContains(assistantSettingsViewSource, "handleToolPolicyChange('AllowedBucketNames', getMultiSelectValues(e))", "Assistant settings Allowed Buckets multi-select updates tool policy");
                AssertAssistantSettingsTextboxTitles(assistantSettingsViewSource);
                AssertHelper.StringContains(mcpAssistantRegistrationSource, "assistant/documents/list", "MCP assistant document list tool registration");
                AssertHelper.StringContains(mcpAssistantRegistrationSource, "ListAssistantDocumentsAsync", "MCP assistant document list SDK call");
                AssertHelper.StringContains(mcpAssistantSettingsRegistrationSource, "assistant/settings/tools/list", "MCP effective assistant tools registration");
                AssertHelper.StringContains(mcpAssistantSettingsRegistrationSource, "ValidateAssistantToolPolicyAsync", "MCP assistant tool-policy validation SDK call");
                AssertHelper.StringContains(mcpAssistantSettingsRegistrationSource, "assistant/settings/tools/test", "MCP assistant tool-policy diagnostics registration");
                AssertHelper.StringContains(mcpAssistantSettingsRegistrationSource, "TestAssistantToolPolicyAsync", "MCP assistant tool-policy diagnostics SDK call");
                AssertHelper.StringContains(mcpAssistantToolCallRegistrationSource, "assistant/tool-calls/list", "MCP assistant tool-call list registration");
                AssertHelper.StringContains(mcpAssistantToolCallRegistrationSource, "DeleteAssistantToolCallsAsync", "MCP assistant tool-call bulk delete SDK call");
                AssertHelper.StringContains(csharpSdkSource, "TestCrawlPlanDraftConnectivityAsync", "C# SDK draft crawl connectivity method");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "sdk", "csharp", "AssistantHub.Sdk", "AssistantHubClientAssistantParityBase.cs")), "ListAssistantToolCallsAsync", "C# SDK assistant tool-call list method");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "sdk", "csharp", "AssistantHub.Sdk", "AssistantHubClientAssistantParityBase.cs")), "TestAssistantToolPolicyAsync", "C# SDK assistant tool-policy diagnostics method");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "sdk", "csharp", "AssistantHub.Sdk", "AssistantHubClientManagementBase.cs")), "GetExternalSearchStatusAsync", "C# SDK external-search status method");
                AssertHelper.StringContains(jsSdkSource, "testCrawlPlanDraftConnectivity", "JS SDK draft crawl connectivity method");
                AssertHelper.StringContains(jsSdkSource, "listAssistantToolCalls", "JS SDK assistant tool-call list method");
                AssertHelper.StringContains(jsSdkSource, "testAssistantToolPolicy", "JS SDK assistant tool-policy diagnostics method");
                AssertHelper.StringContains(jsSdkSource, "getExternalSearchStatus", "JS SDK external-search status method");
                AssertHelper.StringContains(pythonSdkSource, "test_crawl_plan_draft_connectivity", "Python SDK draft crawl connectivity method");
                AssertHelper.StringContains(pythonSdkSource, "list_assistant_tool_calls", "Python SDK assistant tool-call list method");
                AssertHelper.StringContains(pythonSdkSource, "test_assistant_tool_policy", "Python SDK assistant tool-policy diagnostics method");
                AssertHelper.StringContains(pythonSdkSource, "get_external_search_status", "Python SDK external-search status method");
                AssertHelper.StringContains(mcpDocs, "`GET /swagger`", "MCP docs Swagger UI route");
                AssertHelper.StringContains(mcpDocs, "`assistant/documents/list`", "MCP docs assistant public document list tool");
                AssertHelper.StringContains(mcpDocs, "`assistant/settings/tools/list`", "MCP docs effective assistant tools helper");
                AssertHelper.StringContains(mcpDocs, "`assistant/settings/tools/test`", "MCP docs assistant tool diagnostics helper");
                AssertHelper.StringContains(mcpDocs, "`assistant/tool-calls/list`", "MCP docs assistant tool-call trace list");
                AssertHelper.StringContains(mcpDocs, "`crawl plan draft connectivity` | None | Deferred", "MCP docs draft crawl connectivity deferred");
                AssertHelper.StringContains(mcpDocs, "`GET /v1.0/openapi.json`", "MCP docs versioned OpenAPI route");
                AssertHelper.StringContains(mcpDocs, "`embedding endpoint load` | None | Deferred", "MCP docs embedding load deferred");
                AssertHelper.StringContains(mcpDocs, "`completion endpoint load` | None | Deferred", "MCP docs completion load deferred");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "openapi.json")), "\"ErrorCodes\"", "OpenAPI exposes tool policy validation error codes");
                AssertHelper.StringContains(File.ReadAllText(Path.Combine(root, "openapi.json")), "\"AssistantToolPolicyTestResult\"", "OpenAPI exposes tool policy diagnostics result");
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

            await ExecuteTestAsync("Postman: attached-document chat requests are present", async () =>
            {
                string root = GetRepositoryRoot();
                string postman = File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json"));
                JsonDocument.Parse(postman).Dispose();

                AssertHelper.StringContains(postman, "\"key\": \"documentId\"", "documentId Postman variable");
                AssertHelper.StringContains(postman, "\"key\": \"documentId2\"", "documentId2 Postman variable");
                AssertHelper.StringContains(postman, "\"key\": \"documentId3\"", "documentId3 Postman variable");
                AssertHelper.StringContains(postman, "\"key\": \"otherCollectionDocumentId\"", "other collection document Postman variable");
                AssertHelper.StringContains(postman, "\"name\": \"List Assistant Documents\"", "assistant documents Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/assistants/:assistantId/documents?maxResults=100&contentType=application/pdf", "assistant documents Postman route");
                AssertHelper.StringContains(postman, "\"key\": \"contentType\"", "assistant documents Postman contentType parameter");
                AssertHelper.StringContains(postman, "\"name\": \"Chat (with attached document)\"", "attached-document chat Postman request");
                AssertHelper.StringContains(postman, "\"name\": \"Chat (attached documents disabled error)\"", "attached-document disabled error Postman request");
                AssertHelper.StringContains(postman, "\"name\": \"Chat (invalid attached document ID error)\"", "attached-document invalid ID error Postman request");
                AssertHelper.StringContains(postman, "\"name\": \"Chat (cross-collection attached document error)\"", "attached-document cross-collection error Postman request");
                AssertHelper.StringContains(postman, "\"name\": \"Chat (too many attached documents error)\"", "attached-document too many error Postman request");
                AssertHelper.StringContains(postman, "\\\"attached_document_ids\\\"", "attached_document_ids Postman request body");
                AssertHelper.StringContains(postman, "{{documentId}}", "document ID Postman variable usage");
            });

            await ExecuteTestAsync("Postman: assistant tool-call trace requests are present", async () =>
            {
                string root = GetRepositoryRoot();
                string postman = File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json"));
                JsonDocument.Parse(postman).Dispose();

                AssertHelper.StringContains(postman, "\"name\": \"Assistant Tool Calls\"", "assistant tool-call Postman folder");
                AssertHelper.StringContains(postman, "\"key\": \"toolCallRecordId\"", "toolCallRecordId Postman variable");
                AssertHelper.StringContains(postman, "\"name\": \"Test Assistant Tool Policy\"", "tool diagnostics Postman request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/assistants/:assistantId/settings/tools/test", "tool diagnostics Postman route");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/assistants/:assistantId/tool-calls?maxResults=100", "tool-call list Postman route");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/assistants/:assistantId/tool-calls/:toolCallRecordId", "tool-call item Postman route");
            });

            await ExecuteTestAsync("Postman: ExternalSearch configuration example is present", async () =>
            {
                string root = GetRepositoryRoot();
                string postman = File.ReadAllText(Path.Combine(root, "postman", "AssistantHub.postman_collection.json"));
                JsonDocument.Parse(postman).Dispose();

                AssertHelper.StringContains(postman, "\\\"ExternalSearch\\\"", "Postman ExternalSearch config");
                AssertHelper.StringContains(postman, "\\\"ProviderType\\\": \\\"Tavily\\\"", "Postman Tavily provider config");
                AssertHelper.StringContains(postman, "\\\"SafeSearch\\\": true", "Postman SafeSearch config");
                AssertHelper.StringContains(postman, "\\\"AllowRawContent\\\": false", "Postman AllowRawContent config");
                AssertHelper.StringContains(postman, "{{tavilyApiKey}}", "Postman Tavily API key variable");
                AssertHelper.StringContains(postman, "responses redact ExternalSearch provider API keys", "Postman config redaction description");
                AssertHelper.StringContains(postman, "\"name\": \"Get ExternalSearch Status\"", "Postman external-search status request");
                AssertHelper.StringContains(postman, "{{baseUrl}}/v1.0/configuration/external-search/status", "Postman external-search status route");
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

        private static void AssertAssistantSettingsTextboxTitles(string assistantSettingsViewSource)
        {
            Regex fieldRegex = new Regex(
                "<input\\b[\\s\\S]*?/>|<PasswordInput\\b[\\s\\S]*?/>|<textarea\\b[\\s\\S]*?</textarea>",
                RegexOptions.Singleline);

            List<string> missingTitles = new List<string>();
            foreach (Match match in fieldRegex.Matches(assistantSettingsViewSource))
            {
                string field = match.Value;
                if (field.Contains("type=\"checkbox\"", StringComparison.Ordinal)
                    || field.Contains("type=\"range\"", StringComparison.Ordinal))
                    continue;

                if (field.Contains("title=", StringComparison.Ordinal))
                    continue;

                int line = assistantSettingsViewSource.Substring(0, match.Index).Count(ch => ch == '\n') + 1;
                string summary = Regex.Replace(field, "\\s+", " ").Trim();
                missingTitles.Add(line + ": " + summary);
            }

            AssertHelper.IsTrue(
                missingTitles.Count == 0,
                "Assistant settings textboxes expose native hover titles: " + String.Join("; ", missingTitles.Take(10)));
        }

        private static void AssertOpenApiOperation(JsonElement paths, string path, string method, string name)
        {
            AssertHelper.IsTrue(paths.TryGetProperty(path, out JsonElement pathItem), name + " path");
            AssertHelper.IsTrue(pathItem.TryGetProperty(method, out _), name + " method");
        }

        private static bool HasParameter(JsonElement operation, string parameterName, string parameterLocation)
        {
            if (!operation.TryGetProperty("parameters", out JsonElement parameters)) return false;
            if (parameters.ValueKind != JsonValueKind.Array) return false;

            foreach (JsonElement parameter in parameters.EnumerateArray())
            {
                if (!parameter.TryGetProperty("name", out JsonElement nameElement)) continue;
                if (!parameter.TryGetProperty("in", out JsonElement inElement)) continue;

                if (String.Equals(nameElement.GetString(), parameterName, StringComparison.Ordinal)
                    && String.Equals(inElement.GetString(), parameterLocation, StringComparison.Ordinal))
                    return true;
            }

            return false;
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
