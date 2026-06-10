namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Base class for route handlers providing shared dependencies and helper methods.
    /// </summary>
    public abstract class HandlerBase
    {
        /// <summary>Database driver instance.</summary>
        protected readonly DatabaseDriverBase Database;

        /// <summary>Logging module instance.</summary>
        protected readonly LoggingModule Logging;

        /// <summary>Application settings.</summary>
        protected readonly AssistantHubSettings Settings;

        /// <summary>Authentication service instance.</summary>
        protected readonly AuthenticationService Authentication;

        /// <summary>Storage service instance.</summary>
        protected readonly IObjectStorageService Storage;

        /// <summary>Ingestion service instance.</summary>
        protected readonly IngestionService Ingestion;

        /// <summary>Retrieval service instance.</summary>
        protected readonly RetrievalService Retrieval;

        /// <summary>Inference service instance.</summary>
        protected readonly InferenceService Inference;

        /// <summary>Partio chunking service instance.</summary>
        protected readonly IChunkingService ChunkingService;

        /// <summary>Partio embedding endpoint service instance.</summary>
        protected readonly IEmbeddingEndpointService EmbeddingEndpoints;

        /// <summary>Partio inference endpoint service instance.</summary>
        protected readonly IInferenceEndpointService InferenceEndpoints;

        /// <summary>RecallDB vector-store service instance.</summary>
        protected readonly IVectorStoreService VectorStore;

        /// <summary>Processing log service instance.</summary>
        protected readonly ProcessingLogService ProcessingLog;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        /// <param name="processingLog">Processing log service.</param>
        protected HandlerBase(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            ProcessingLogService processingLog = null)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            Storage = storage;
            Ingestion = ingestion;
            Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            Inference = inference ?? throw new ArgumentNullException(nameof(inference));
            ChunkingService = new PartioChunkingService(Settings.Chunking, Logging);
            EmbeddingEndpoints = new PartioEmbeddingEndpointService(Settings.Chunking, Logging);
            InferenceEndpoints = new PartioInferenceEndpointService(Settings.Chunking, Logging);
            VectorStore = new RecallDbVectorStoreService(Settings.RecallDb, Logging);
            ProcessingLog = processingLog;
        }

        /// <summary>
        /// Retrieve or initialize the metadata dictionary on the context.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Metadata dictionary.</returns>
        protected Dictionary<string, object> GetMetadata(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (ctx.Metadata == null)
                ctx.Metadata = new Dictionary<string, object>();
            return (Dictionary<string, object>)ctx.Metadata;
        }

        /// <summary>
        /// Check whether the authenticated user is an admin.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>True if admin.</returns>
        protected bool IsAdmin(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            return GetMetadata(ctx).ContainsKey("isAdmin") && (bool)GetMetadata(ctx)["isAdmin"];
        }

        /// <summary>
        /// Retrieve the authenticated user from context metadata.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Authenticated user.</returns>
        protected UserMaster GetUser(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            return (UserMaster)GetMetadata(ctx)["user"];
        }

        /// <summary>
        /// Get the AuthContext from the request metadata.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>AuthContext or null.</returns>
        protected AuthContext GetAuthContext(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            Dictionary<string, object> metadata = GetMetadata(ctx);
            if (metadata.ContainsKey("authContext"))
                return (AuthContext)metadata["authContext"];
            return null;
        }

        /// <summary>
        /// Require any authenticated user. Returns AuthContext or sends 401.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>AuthContext or null if 401 was sent.</returns>
        protected AuthContext RequireAuth(HttpContextBase ctx)
        {
            AuthContext auth = GetAuthContext(ctx);
            if (auth == null || !auth.IsAuthenticated)
                return null;
            return auth;
        }

        /// <summary>
        /// Require global admin. Returns AuthContext or null (caller should send 403).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>AuthContext if global admin, null otherwise.</returns>
        protected AuthContext RequireGlobalAdmin(HttpContextBase ctx)
        {
            AuthContext auth = RequireAuth(ctx);
            if (auth == null) return null;
            if (!auth.IsGlobalAdmin) return null;
            return auth;
        }

        /// <summary>
        /// Require global admin or tenant admin. Returns AuthContext or null.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>AuthContext if admin, null otherwise.</returns>
        protected AuthContext RequireAdmin(HttpContextBase ctx)
        {
            AuthContext auth = RequireAuth(ctx);
            if (auth == null) return null;
            if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin) return null;
            return auth;
        }

        /// <summary>
        /// Validate that the caller can access the given tenant.
        /// Global admins can access any tenant. Non-global-admins can only access their own.
        /// </summary>
        /// <param name="auth">Auth context.</param>
        /// <param name="tenantId">Tenant ID to check.</param>
        /// <returns>True if access is allowed.</returns>
        protected bool ValidateTenantAccess(AuthContext auth, string tenantId)
        {
            if (auth == null) return false;
            if (auth.IsGlobalAdmin)
            {
                AuditGlobalAdminBypass(auth, tenantId, "tenant_access_validation");
                return true;
            }
            return String.Equals(auth.TenantId, tenantId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Enforce that a loaded record belongs to the caller's tenant.
        /// Global admins bypass this check.
        /// </summary>
        /// <param name="auth">Auth context.</param>
        /// <param name="recordTenantId">Tenant ID on the record.</param>
        /// <returns>True if ownership is valid.</returns>
        protected bool EnforceTenantOwnership(AuthContext auth, string recordTenantId)
        {
            if (auth == null) return false;
            if (auth.IsGlobalAdmin)
            {
                AuditGlobalAdminBypass(auth, recordTenantId, "tenant_ownership_enforcement");
                return true;
            }
            return String.Equals(auth.TenantId, recordTenantId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Emit a metadata-only audit log when a global admin intentionally bypasses tenant ownership checks.
        /// </summary>
        /// <param name="auth">Auth context.</param>
        /// <param name="targetTenantId">Target tenant identifier.</param>
        /// <param name="reason">Stable bypass reason.</param>
        protected void AuditGlobalAdminBypass(AuthContext auth, string targetTenantId, string reason)
        {
            if (auth == null || !auth.IsGlobalAdmin) return;

            string actor = !String.IsNullOrWhiteSpace(auth.UserId)
                ? auth.UserId
                : (!String.IsNullOrWhiteSpace(auth.CredentialId)
                    ? auth.CredentialId
                    : (!String.IsNullOrWhiteSpace(auth.Email) ? auth.Email : "unknown"));

            Logging.Info(
                "[HandlerBase] global admin bypass audit: reason=" + reason +
                " actor=" + actor +
                " targetTenantId=" + (String.IsNullOrWhiteSpace(targetTenantId) ? "[none]" : targetTenantId));
        }

        /// <summary>
        /// Build an enumeration query from request query parameters.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Enumeration query.</returns>
        protected EnumerationQuery BuildEnumerationQuery(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            EnumerationQuery query = new EnumerationQuery();

            string maxResultsStr = ctx.Request.Query.Elements.Get("maxResults");
            if (!String.IsNullOrEmpty(maxResultsStr) && Int32.TryParse(maxResultsStr, out int maxResults))
            {
                if (maxResults >= 1 && maxResults <= 1000)
                    query.MaxResults = maxResults;
            }

            string continuationToken = ctx.Request.Query.Elements.Get("continuationToken");
            if (!String.IsNullOrEmpty(continuationToken))
            {
                query.ContinuationToken = continuationToken;
            }

            string orderingStr = ctx.Request.Query.Elements.Get("ordering");
            if (!String.IsNullOrEmpty(orderingStr))
            {
                if (Enum.TryParse<Enums.EnumerationOrderEnum>(orderingStr, true, out Enums.EnumerationOrderEnum ordering))
                {
                    query.Ordering = ordering;
                }
            }

            string assistantIdFilter = ctx.Request.Query.Elements.Get("assistantId");
            if (!String.IsNullOrEmpty(assistantIdFilter))
            {
                query.AssistantIdFilter = assistantIdFilter;
            }

            string bucketNameFilter = ctx.Request.Query.Elements.Get("bucketName");
            if (!String.IsNullOrEmpty(bucketNameFilter))
            {
                query.BucketNameFilter = bucketNameFilter;
            }

            string collectionIdFilter = ctx.Request.Query.Elements.Get("collectionId");
            if (!String.IsNullOrEmpty(collectionIdFilter))
            {
                query.CollectionIdFilter = collectionIdFilter;
            }

            string threadIdFilter = ctx.Request.Query.Elements.Get("threadId");
            if (!String.IsNullOrEmpty(threadIdFilter))
            {
                query.ThreadIdFilter = threadIdFilter;
            }

            string requestHistoryIdFilter = ctx.Request.Query.Elements.Get("requestHistoryId");
            if (!String.IsNullOrEmpty(requestHistoryIdFilter))
            {
                query.RequestHistoryIdFilter = requestHistoryIdFilter;
            }

            string chatHistoryIdFilter = ctx.Request.Query.Elements.Get("chatHistoryId");
            if (!String.IsNullOrEmpty(chatHistoryIdFilter))
            {
                query.ChatHistoryIdFilter = chatHistoryIdFilter;
            }

            string traceIdFilter = ctx.Request.Query.Elements.Get("traceId");
            if (!String.IsNullOrEmpty(traceIdFilter))
            {
                query.TraceIdFilter = traceIdFilter;
            }

            string toolNameFilter = ctx.Request.Query.Elements.Get("toolName");
            if (!String.IsNullOrEmpty(toolNameFilter))
            {
                query.ToolNameFilter = toolNameFilter;
            }

            query.SuccessFilter = ParseOptionalBoolean(ctx.Request.Query.Elements.Get("success"));
            query.DeniedFilter = ParseOptionalBoolean(ctx.Request.Query.Elements.Get("denied"));

            string startUtc = ctx.Request.Query.Elements.Get("startUtc");
            if (!String.IsNullOrEmpty(startUtc)
                && DateTime.TryParse(startUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedStartUtc))
            {
                query.StartUtc = parsedStartUtc;
            }

            string endUtc = ctx.Request.Query.Elements.Get("endUtc");
            if (!String.IsNullOrEmpty(endUtc)
                && DateTime.TryParse(endUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedEndUtc))
            {
                query.EndUtc = parsedEndUtc;
            }

            return query;
        }

        private bool? ParseOptionalBoolean(string value)
        {
            if (String.IsNullOrEmpty(value)) return null;
            if (Boolean.TryParse(value, out bool parsed)) return parsed;
            if (String.Equals(value, "1", StringComparison.Ordinal)) return true;
            if (String.Equals(value, "0", StringComparison.Ordinal)) return false;
            return null;
        }
    }
}
