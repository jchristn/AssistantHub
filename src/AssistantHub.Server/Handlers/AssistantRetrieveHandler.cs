namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;
    using Enums = AssistantHub.Core.Enums;

    /// <summary>
    /// Handles the retrieval-only diagnostic route, which runs an assistant's retrieval pipeline without final
    /// inference. Used by the benchmark harness and for diagnosing retrieval quality.
    /// </summary>
    public class AssistantRetrieveHandler : HandlerBase
    {
        private static readonly string _Header = "[AssistantRetrieveHandler] ";

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
        public AssistantRetrieveHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/retrieve - Run the assistant's retrieval pipeline without final inference.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostRetrieveAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (assistant == null || !EnforceTenantOwnership(auth, assistant.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrWhiteSpace(ctx.Request.DataAsString))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "A request body with a query or messages is required."))).ConfigureAwait(false);
                    return;
                }

                AssistantRetrieveRequest request = Serializer.DeserializeJson<AssistantRetrieveRequest>(ctx.Request.DataAsString) ?? new AssistantRetrieveRequest();

                AssistantChatService chatService = new AssistantChatService(
                    Database,
                    Logging,
                    Settings,
                    Retrieval,
                    Inference,
                    Storage,
                    inferenceEndpoints: InferenceEndpoints);
                AssistantRetrievalExecutionResult result = await chatService.ExecuteRetrievalOnlyAsync(assistantId, request).ConfigureAwait(false);

                ctx.Response.ContentType = "application/json";
                if (!result.Success)
                {
                    ctx.Response.StatusCode = result.StatusCode;
                    Enums.ApiErrorEnum error = result.StatusCode == 404 ? Enums.ApiErrorEnum.NotFound : Enums.ApiErrorEnum.BadRequest;
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(error, null, result.ErrorMessage))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(Serializer.SerializeJson(result.Response)).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Retrieve request must be valid JSON: " + e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostRetrieveAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
