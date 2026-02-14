namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
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
        protected readonly StorageService Storage;

        /// <summary>Ingestion service instance.</summary>
        protected readonly IngestionService Ingestion;

        /// <summary>Retrieval service instance.</summary>
        protected readonly RetrievalService Retrieval;

        /// <summary>Inference service instance.</summary>
        protected readonly InferenceService Inference;

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
        protected HandlerBase(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            Storage = storage;
            Ingestion = ingestion;
            Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            Inference = inference ?? throw new ArgumentNullException(nameof(inference));
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

            return query;
        }
    }
}
