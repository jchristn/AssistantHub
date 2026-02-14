namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Threading.Tasks;
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
    /// Handles bearer token authentication for protected routes.
    /// </summary>
    public class AuthenticationHandler : HandlerBase
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
        public AuthenticationHandler(
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
        /// Extract and validate a bearer token from the request, setting user metadata on the context.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HandleAuthenticateRequestAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            string bearerToken = null;
            string authHeader = ctx.Request.Headers.Get("Authorization");

            if (!String.IsNullOrEmpty(authHeader))
            {
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    bearerToken = authHeader.Substring(7).Trim();
                }
                else
                {
                    bearerToken = authHeader.Trim();
                }
            }

            if (String.IsNullOrEmpty(bearerToken))
            {
                bearerToken = ctx.Request.Query.Elements.Get("token");
            }

            if (String.IsNullOrEmpty(bearerToken))
            {
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                return;
            }

            AuthenticateResult authResult = await Authentication.AuthenticateByBearerTokenAsync(bearerToken).ConfigureAwait(false);
            if (!authResult.Success || authResult.User == null)
            {
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                return;
            }

            GetMetadata(ctx)["user"] = authResult.User;
            GetMetadata(ctx)["credential"] = authResult.Credential;
            GetMetadata(ctx)["isAdmin"] = authResult.User.IsAdmin;
        }
    }
}
