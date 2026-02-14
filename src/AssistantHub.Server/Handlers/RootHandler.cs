namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles GET / and HEAD / routes.
    /// </summary>
    public class RootHandler : HandlerBase
    {
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
        public RootHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// GET / - Returns product name, version, and timestamp.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetRootAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new
            {
                Product = Constants.ProductName,
                Version = Constants.ProductVersion,
                Timestamp = DateTime.UtcNow
            })).ConfigureAwait(false);
        }

        /// <summary>
        /// HEAD / - Returns 200.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadRootAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send().ConfigureAwait(false);
        }
    }
}
