namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
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
    /// Handles chat history list, read, and delete routes.
    /// </summary>
    public class HistoryHandler : HandlerBase
    {
        private static readonly string _Header = "[HistoryHandler] ";

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
        public HistoryHandler(
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
        /// GET /v1.0/history - List chat history entries.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetHistoryListAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                AuthContext auth = GetAuthContext(ctx);
                EnumerationResult<ChatHistory> result = await Database.ChatHistory.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);

                // Non-admin users: filter to only their assistants' history
                if (!isAdmin && result != null && result.Objects != null)
                {
                    List<ChatHistory> filtered = new List<ChatHistory>();
                    foreach (ChatHistory ch in result.Objects)
                    {
                        Assistant assistant = await Database.Assistant.ReadAsync(ch.AssistantId).ConfigureAwait(false);
                        if (assistant != null && assistant.UserId == user.Id)
                        {
                            filtered.Add(ch);
                        }
                    }
                    result.Objects = filtered;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetHistoryListAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/history/{historyId} - Get history entry by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetHistoryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string historyId = ctx.Request.Url.Parameters["historyId"];
                if (String.IsNullOrEmpty(historyId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                ChatHistory history = await Database.ChatHistory.ReadAsync(historyId).ConfigureAwait(false);
                if (history == null || !EnforceTenantOwnership(auth, history.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(history.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != auth.UserId)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(history)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetHistoryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/history/{historyId} - Delete history entry by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteHistoryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string historyId = ctx.Request.Url.Parameters["historyId"];
                if (String.IsNullOrEmpty(historyId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                ChatHistory history = await Database.ChatHistory.ReadAsync(historyId).ConfigureAwait(false);
                if (history == null || !EnforceTenantOwnership(auth, history.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(history.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != auth.UserId)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
                }

                await Database.ChatHistory.DeleteAsync(historyId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteHistoryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/threads - List distinct threads with summary info.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetThreadsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                EnumerationQuery query = BuildEnumerationQuery(ctx);

                // Fetch all history records (up to maxResults)
                EnumerationResult<ChatHistory> result = await Database.ChatHistory.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);

                // Group by thread_id
                Dictionary<string, List<ChatHistory>> threadGroups = new Dictionary<string, List<ChatHistory>>();
                if (result != null && result.Objects != null)
                {
                    foreach (ChatHistory ch in result.Objects)
                    {
                        // Ownership check for non-admins
                        if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin)
                        {
                            Assistant assistant = await Database.Assistant.ReadAsync(ch.AssistantId).ConfigureAwait(false);
                            if (assistant == null || assistant.UserId != auth.UserId) continue;
                        }

                        if (!threadGroups.ContainsKey(ch.ThreadId))
                            threadGroups[ch.ThreadId] = new List<ChatHistory>();
                        threadGroups[ch.ThreadId].Add(ch);
                    }
                }

                List<object> threads = new List<object>();
                foreach (var kvp in threadGroups)
                {
                    DateTime firstMsg = DateTime.MaxValue;
                    DateTime lastMsg = DateTime.MinValue;
                    string asstId = null;

                    foreach (ChatHistory ch in kvp.Value)
                    {
                        if (ch.UserMessageUtc < firstMsg) firstMsg = ch.UserMessageUtc;
                        if (ch.UserMessageUtc > lastMsg) lastMsg = ch.UserMessageUtc;
                        if (asstId == null) asstId = ch.AssistantId;
                    }

                    threads.Add(new
                    {
                        ThreadId = kvp.Key,
                        AssistantId = asstId,
                        FirstMessageUtc = firstMsg,
                        LastMessageUtc = lastMsg,
                        TurnCount = kvp.Value.Count
                    });
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(threads)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetThreadsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
