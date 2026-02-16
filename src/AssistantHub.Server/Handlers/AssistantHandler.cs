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
    /// Handles assistant CRUD routes with ownership checks.
    /// </summary>
    public class AssistantHandler : HandlerBase
    {
        private static readonly string _Header = "[AssistantHandler] ";

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
        public AssistantHandler(
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
        /// PUT /v1.0/assistants - Create a new assistant.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutAssistantAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);

                string body = ctx.Request.DataAsString;
                Assistant assistant = Serializer.DeserializeJson<Assistant>(body);
                if (assistant == null || String.IsNullOrEmpty(assistant.Name))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Name is required."))).ConfigureAwait(false);
                    return;
                }

                assistant.Id = IdGenerator.NewAssistantId();
                assistant.UserId = user.Id;
                assistant.CreatedUtc = DateTime.UtcNow;
                assistant.LastUpdateUtc = DateTime.UtcNow;

                assistant = await Database.Assistant.CreateAsync(assistant).ConfigureAwait(false);

                // Create default assistant settings
                AssistantSettings settings = new AssistantSettings();
                settings.Id = IdGenerator.NewAssistantSettingsId();
                settings.AssistantId = assistant.Id;
                settings.CreatedUtc = DateTime.UtcNow;
                settings.LastUpdateUtc = DateTime.UtcNow;
                await Database.AssistantSettings.CreateAsync(settings).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(assistant)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutAssistantAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/assistants - List assistants (non-admins see only their own).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                EnumerationQuery query = BuildEnumerationQuery(ctx);

                EnumerationResult<Assistant> result = await Database.Assistant.EnumerateAsync(query).ConfigureAwait(false);

                // Non-admin users can only see their own assistants
                if (!isAdmin && result != null && result.Objects != null)
                {
                    result.Objects = result.Objects.FindAll(a => a.UserId == user.Id);
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId} - Get assistant by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (assistant == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin && assistant.UserId != user.Id)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(assistant)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/assistants/{assistantId} - Update assistant by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutAssistantByIdAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                Assistant existing = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (existing == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin && existing.UserId != user.Id)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                Assistant updated = Serializer.DeserializeJson<Assistant>(body);
                if (updated == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                updated.Id = assistantId;
                updated.UserId = existing.UserId;
                updated.CreatedUtc = existing.CreatedUtc;
                updated.LastUpdateUtc = DateTime.UtcNow;

                updated = await Database.Assistant.UpdateAsync(updated).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(updated)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutAssistantByIdAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/assistants/{assistantId} - Delete assistant and cascade settings, documents, feedback.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteAssistantAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                Assistant existing = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (existing == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin && existing.UserId != user.Id)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                // Cascading deletes
                await Database.AssistantSettings.DeleteByAssistantIdAsync(assistantId).ConfigureAwait(false);
                await Database.AssistantFeedback.DeleteByAssistantIdAsync(assistantId).ConfigureAwait(false);

                await Database.Assistant.DeleteAsync(assistantId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteAssistantAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// HEAD /v1.0/assistants/{assistantId} - Check assistant existence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadAssistantAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (assistant == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                if (!isAdmin && assistant.UserId != user.Id)
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadAssistantAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }
    }
}
