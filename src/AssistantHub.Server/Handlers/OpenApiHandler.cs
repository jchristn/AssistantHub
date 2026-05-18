namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles runtime OpenAPI document requests.
    /// </summary>
    public class OpenApiHandler : HandlerBase
    {
        private readonly OpenApiDocumentService _OpenApi;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenApiHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            OpenApiDocumentService openApi)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _OpenApi = openApi ?? throw new ArgumentNullException(nameof(openApi));
        }

        /// <summary>
        /// GET /openapi.json - runtime OpenAPI document.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetOpenApiAsync(HttpContextBase ctx)
        {
            try
            {
                string document = _OpenApi.BuildDocument(ctx);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(document).ConfigureAwait(false);
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
