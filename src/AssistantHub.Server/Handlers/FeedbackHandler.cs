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
    /// Handles feedback list, read, and delete routes.
    /// </summary>
    public class FeedbackHandler : HandlerBase
    {
        private static readonly string _Header = "[FeedbackHandler] ";

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
        public FeedbackHandler(
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
        /// GET /v1.0/feedback - List feedback (non-admins see only their assistants' feedback).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetFeedbackListAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<AssistantFeedback> result = await Database.AssistantFeedback.EnumerateAsync(query).ConfigureAwait(false);

                // Non-admin users: filter to only their assistants' feedback
                if (!isAdmin && result != null && result.Objects != null)
                {
                    List<AssistantFeedback> filtered = new List<AssistantFeedback>();
                    foreach (AssistantFeedback fb in result.Objects)
                    {
                        Assistant assistant = await Database.Assistant.ReadAsync(fb.AssistantId).ConfigureAwait(false);
                        if (assistant != null && assistant.UserId == user.Id)
                        {
                            filtered.Add(fb);
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
                Logging.Warn(_Header + "exception in GetFeedbackListAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/feedback/{feedbackId} - Get feedback by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetFeedbackAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string feedbackId = ctx.Request.Url.Parameters["feedbackId"];
                if (String.IsNullOrEmpty(feedbackId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantFeedback feedback = await Database.AssistantFeedback.ReadAsync(feedbackId).ConfigureAwait(false);
                if (feedback == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(feedback.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != user.Id)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(feedback)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetFeedbackAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/feedback/{feedbackId} - Delete feedback by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteFeedbackAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string feedbackId = ctx.Request.Url.Parameters["feedbackId"];
                if (String.IsNullOrEmpty(feedbackId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantFeedback feedback = await Database.AssistantFeedback.ReadAsync(feedbackId).ConfigureAwait(false);
                if (feedback == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(feedback.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != user.Id)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
                }

                await Database.AssistantFeedback.DeleteAsync(feedbackId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteFeedbackAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
