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
    /// Handles POST /v1.0/authenticate route.
    /// </summary>
    public class AuthenticateHandler : HandlerBase
    {
        private static readonly string _Header = "[AuthenticateHandler] ";

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
        public AuthenticateHandler(
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
        /// POST /v1.0/authenticate - Authenticate by email+password or bearer token.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostAuthenticateAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                string body = ctx.Request.DataAsString;
                AuthenticateRequest req = Serializer.DeserializeJson<AuthenticateRequest>(body);
                if (req == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AuthenticateResult result = null;

                if (!String.IsNullOrEmpty(req.BearerToken))
                {
                    result = await Authentication.AuthenticateByBearerTokenAsync(req.BearerToken).ConfigureAwait(false);
                }
                else if (!String.IsNullOrEmpty(req.Email) && !String.IsNullOrEmpty(req.Password))
                {
                    result = await Authentication.AuthenticateByEmailPasswordAsync(req.TenantId, req.Email, req.Password).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Provide either BearerToken or Email+Password."))).ConfigureAwait(false);
                    return;
                }

                if (result.Success)
                {
                    ctx.Response.StatusCode = 200;
                }
                else
                {
                    ctx.Response.StatusCode = 401;
                }

                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostAuthenticateAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
