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
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles admin assistant tool-call trace routes.
    /// </summary>
    public class AssistantToolCallHandler : HandlerBase
    {
        private static readonly string _Header = "[AssistantToolCallHandler] ";

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantToolCallHandler(
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
        /// GET /v1.0/assistants/{assistantId}/tool-calls - enumerate assistant tool-call traces.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantToolCallsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await LoadAssistantAsync(ctx, auth).ConfigureAwait(false);
                if (assistant == null) return;

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                query.AssistantIdFilter = assistant.Id;

                EnumerationResult<AssistantToolCallRecord> result = Database.AssistantToolCall != null
                    ? await Database.AssistantToolCall.EnumerateAsync(assistant.TenantId, query, assistant.Id).ConfigureAwait(false)
                    : new EnumerationResult<AssistantToolCallRecord> { MaxResults = query.MaxResults };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantToolCallsAsync: " + e.Message);
                await SendInternalError(ctx).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId} - get one assistant tool-call trace.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantToolCallAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await LoadAssistantAsync(ctx, auth).ConfigureAwait(false);
                if (assistant == null) return;

                string recordId = ctx.Request.Url.Parameters["toolCallRecordId"];
                if (String.IsNullOrEmpty(recordId))
                {
                    await SendBadRequest(ctx).ConfigureAwait(false);
                    return;
                }

                AssistantToolCallRecord record = Database.AssistantToolCall != null
                    ? await Database.AssistantToolCall.ReadAsync(recordId).ConfigureAwait(false)
                    : null;

                if (record == null
                    || !String.Equals(record.AssistantId, assistant.Id, StringComparison.Ordinal)
                    || !EnforceTenantOwnership(auth, record.TenantId))
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(record)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantToolCallAsync: " + e.Message);
                await SendInternalError(ctx).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId} - delete one assistant tool-call trace.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteAssistantToolCallAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await LoadAssistantAsync(ctx, auth).ConfigureAwait(false);
                if (assistant == null) return;

                string recordId = ctx.Request.Url.Parameters["toolCallRecordId"];
                if (String.IsNullOrEmpty(recordId))
                {
                    await SendBadRequest(ctx).ConfigureAwait(false);
                    return;
                }

                AssistantToolCallRecord record = Database.AssistantToolCall != null
                    ? await Database.AssistantToolCall.ReadAsync(recordId).ConfigureAwait(false)
                    : null;

                if (record == null
                    || !String.Equals(record.AssistantId, assistant.Id, StringComparison.Ordinal)
                    || !EnforceTenantOwnership(auth, record.TenantId))
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await Database.AssistantToolCall.DeleteAsync(recordId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteAssistantToolCallAsync: " + e.Message);
                await SendInternalError(ctx).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/assistants/{assistantId}/tool-calls - delete filtered assistant tool-call traces.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteAssistantToolCallsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await LoadAssistantAsync(ctx, auth).ConfigureAwait(false);
                if (assistant == null) return;

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                query.AssistantIdFilter = assistant.Id;

                int deletedCount = Database.AssistantToolCall != null
                    ? await Database.AssistantToolCall.DeleteByFilterAsync(assistant.TenantId, query, assistant.Id).ConfigureAwait(false)
                    : 0;

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { DeletedCount = deletedCount })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteAssistantToolCallsAsync: " + e.Message);
                await SendInternalError(ctx).ConfigureAwait(false);
            }
        }

        private async Task<Assistant> LoadAssistantAsync(HttpContextBase ctx, AuthContext auth)
        {
            string assistantId = ctx.Request.Url.Parameters["assistantId"];
            if (String.IsNullOrEmpty(assistantId))
            {
                await SendBadRequest(ctx).ConfigureAwait(false);
                return null;
            }

            Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
            if (assistant == null || !EnforceTenantOwnership(auth, assistant.TenantId))
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return null;
            }

            return assistant;
        }

        private async Task SendBadRequest(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
        }

        private async Task SendAuthzFailure(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
        }

        private async Task SendNotFound(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
        }

        private async Task SendInternalError(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
        }
    }
}
