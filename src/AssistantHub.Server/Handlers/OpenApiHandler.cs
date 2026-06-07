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
        private const string SwaggerHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>AssistantHub Swagger</title>
  <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
  <style>
    html, body { margin: 0; min-height: 100%; background: #ffffff; }
    #swagger-ui { min-height: 100vh; }
  </style>
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
  <script>
    window.onload = function () {
      window.ui = SwaggerUIBundle({
        url: "/openapi.json",
        dom_id: "#swagger-ui",
        deepLinking: true,
        displayRequestDuration: true,
        persistAuthorization: true,
        presets: [
          SwaggerUIBundle.presets.apis,
          SwaggerUIStandalonePreset
        ],
        layout: "StandaloneLayout"
      });
    };
  </script>
</body>
</html>
""";

        private readonly OpenApiDocumentService _OpenApi;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenApiHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
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

        /// <summary>
        /// GET /swagger - Swagger UI.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetSwaggerAsync(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.Send(SwaggerHtml).ConfigureAwait(false);
        }
    }
}
