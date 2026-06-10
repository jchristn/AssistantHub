namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using EasySlack;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles assistant settings GET and PUT routes.
    /// </summary>
    public class AssistantSettingsHandler : HandlerBase
    {
        private static readonly string _Header = "[AssistantSettingsHandler] ";

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
        public AssistantSettingsHandler(
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
        /// GET /v1.0/assistants/{assistantId}/settings - Get settings for an assistant.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetSettingsAsync(HttpContextBase ctx)
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

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin && assistant.UserId != auth.UserId)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Settings not found for this assistant."))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(settings)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetSettingsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/tools - Get effective tool availability for an assistant.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetToolsAsync(HttpContextBase ctx)
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

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin && assistant.UserId != auth.UserId)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Settings not found for this assistant."))).ConfigureAwait(false);
                    return;
                }

                AssistantToolPolicyResolver resolver = new AssistantToolPolicyResolver(Settings);
                List<AssistantToolDescriptor> tools = resolver.Resolve(assistant, settings, true);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(tools)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetToolsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/settings/tools/validate - Validate draft tool policy.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task ValidateToolsAsync(HttpContextBase ctx)
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

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin && assistant.UserId != auth.UserId)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Settings not found for this assistant."))).ConfigureAwait(false);
                    return;
                }

                AssistantToolPolicyValidationRequest request = null;
                if (!String.IsNullOrWhiteSpace(ctx.Request.DataAsString))
                    request = Serializer.DeserializeJson<AssistantToolPolicyValidationRequest>(ctx.Request.DataAsString);
                request ??= new AssistantToolPolicyValidationRequest();

                AssistantToolPolicyValidationResult result = ValidateDraftToolPolicy(assistant, settings, request);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Tool policy validation request must be valid JSON: " + e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in ValidateToolsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/settings/tools/test - Dry-run assistant tool diagnostics.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task TestToolsAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Settings not found for this assistant."))).ConfigureAwait(false);
                    return;
                }

                AssistantToolPolicyValidationRequest request = null;
                if (!String.IsNullOrWhiteSpace(ctx.Request.DataAsString))
                    request = Serializer.DeserializeJson<AssistantToolPolicyValidationRequest>(ctx.Request.DataAsString);
                request ??= new AssistantToolPolicyValidationRequest();

                AssistantToolPolicyTestResult result = await BuildToolPolicyTestResultAsync(assistant, settings, request).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Tool policy diagnostics request must be valid JSON: " + e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in TestToolsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/assistants/{assistantId}/settings - Create or update assistant settings.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutSettingsAsync(HttpContextBase ctx)
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

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin && assistant.UserId != auth.UserId)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                AssistantSettings updated = Serializer.DeserializeJson<AssistantSettings>(body);
                if (updated == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                // Validate search mode fields
                string[] validSearchModes = { "Vector", "FullText", "Hybrid" };
                if (!String.IsNullOrEmpty(updated.SearchMode) &&
                    !validSearchModes.Contains(updated.SearchMode, StringComparer.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "SearchMode must be Vector, FullText, or Hybrid."))).ConfigureAwait(false);
                    return;
                }

                NormalizeEndpointSettings(updated);
                NormalizeSlackSettings(updated);
                updated.DocumentAttachmentMaxCount = Math.Clamp(updated.DocumentAttachmentMaxCount, 1, 100);

                string slackValidationError = ValidateSlackSettings(updated);
                if (!String.IsNullOrEmpty(slackValidationError))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, slackValidationError))).ConfigureAwait(false);
                    return;
                }

                string toolPolicyValidationError = ValidateToolPolicyJson(updated);
                if (!String.IsNullOrEmpty(toolPolicyValidationError))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, toolPolicyValidationError))).ConfigureAwait(false);
                    return;
                }

                updated.TextWeight = Math.Clamp(updated.TextWeight, 0.0, 1.0);
                updated.RetrievalIncludeNeighbors = Math.Clamp(updated.RetrievalIncludeNeighbors, 0, 10);

                if (String.IsNullOrWhiteSpace(updated.InferenceEndpointId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "InferenceEndpointId is required for assistant settings."))).ConfigureAwait(false);
                    return;
                }

                // Validate query rewrite prompt placeholder
                if (!String.IsNullOrEmpty(updated.QueryRewritePrompt) && !updated.QueryRewritePrompt.Contains("{prompt}"))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "QueryRewritePrompt must contain the {prompt} placeholder."))).ConfigureAwait(false);
                    return;
                }

                // Validate re-rank prompt placeholders
                if (!String.IsNullOrEmpty(updated.RerankPrompt) &&
                    (!updated.RerankPrompt.Contains("{query}") || !updated.RerankPrompt.Contains("{chunks}")))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "RerankPrompt must contain the {query} and {chunks} placeholders."))).ConfigureAwait(false);
                    return;
                }

                string[] validSearchTypes = { "TsRank", "TsRankCd" };
                if (!String.IsNullOrEmpty(updated.FullTextSearchType) &&
                    !validSearchTypes.Contains(updated.FullTextSearchType, StringComparer.OrdinalIgnoreCase))
                {
                    updated.FullTextSearchType = "TsRank";
                }

                AssistantSettings existing = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);

                bool restartSlackWorker = SlackSettingsChanged(existing, updated);

                if (existing != null)
                {
                    updated.Id = existing.Id;
                    updated.AssistantId = assistantId;
                    updated.CreatedUtc = existing.CreatedUtc;
                    updated.LastUpdateUtc = DateTime.UtcNow;
                    updated = await Database.AssistantSettings.UpdateAsync(updated).ConfigureAwait(false);
                }
                else
                {
                    updated.Id = IdGenerator.NewAssistantSettingsId();
                    updated.AssistantId = assistantId;
                    updated.CreatedUtc = DateTime.UtcNow;
                    updated.LastUpdateUtc = DateTime.UtcNow;
                    updated = await Database.AssistantSettings.CreateAsync(updated).ConfigureAwait(false);
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(updated)).ConfigureAwait(false);

                if (restartSlackWorker && AssistantHubServer.SlackConnectionManager != null)
                {
                    _ = AssistantHubServer.SlackConnectionManager.RefreshAssistantAsync(assistantId, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutSettingsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/settings/slack/verify - Verify draft Slack settings.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task VerifySlackSettingsAsync(HttpContextBase ctx)
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

                if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin && assistant.UserId != auth.UserId)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                SlackVerificationRequest request = Serializer.DeserializeJson<SlackVerificationRequest>(ctx.Request.DataAsString);
                if (request == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                NormalizeSlackSettings(request);

                string validationError = ValidateSlackSettings(request, true);
                if (!String.IsNullOrEmpty(validationError))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, validationError))).ConfigureAwait(false);
                    return;
                }

                SlackVerificationResponse response = await VerifySlackDraftAsync(request).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(response)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in VerifySlackSettingsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private static void NormalizeSlackSettings(AssistantSettings settings)
        {
            if (settings == null) return;
            settings.SlackAppToken = settings.SlackAppToken?.Trim();
            settings.SlackBotToken = settings.SlackBotToken?.Trim();
            settings.SlackChannelId = settings.SlackChannelId?.Trim();
            settings.SlackMessagePrefix = settings.SlackMessagePrefix?.Trim();
        }

        private static void NormalizeEndpointSettings(AssistantSettings settings)
        {
            if (settings == null) return;
            settings.InferenceEndpointId = NormalizeRequiredEndpointId(settings.InferenceEndpointId);
            settings.EmbeddingEndpointId = NormalizeOptionalEndpointId(settings.EmbeddingEndpointId);
            settings.RetrievalGateInferenceEndpointId = NormalizeOptionalEndpointId(settings.RetrievalGateInferenceEndpointId);
            settings.QueryRewriteInferenceEndpointId = NormalizeOptionalEndpointId(settings.QueryRewriteInferenceEndpointId);
            settings.RerankInferenceEndpointId = NormalizeOptionalEndpointId(settings.RerankInferenceEndpointId);
        }

        private static string NormalizeRequiredEndpointId(string endpointId)
        {
            return endpointId?.Trim();
        }

        private static string NormalizeOptionalEndpointId(string endpointId)
        {
            string trimmed = endpointId?.Trim();
            return String.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static void NormalizeSlackSettings(SlackVerificationRequest request)
        {
            if (request == null) return;
            request.SlackAppToken = request.SlackAppToken?.Trim();
            request.SlackBotToken = request.SlackBotToken?.Trim();
            request.SlackChannelId = request.SlackChannelId?.Trim();
            request.SlackMessagePrefix = request.SlackMessagePrefix?.Trim();
        }

        private static string ValidateSlackSettings(AssistantSettings settings)
        {
            if (settings == null) return "Settings payload is required.";

            if (!String.IsNullOrEmpty(settings.SlackAppToken) && !settings.SlackAppToken.StartsWith("xapp-", StringComparison.OrdinalIgnoreCase))
                return "SlackAppToken must start with xapp-.";
            if (!String.IsNullOrEmpty(settings.SlackBotToken) && !settings.SlackBotToken.StartsWith("xoxb-", StringComparison.OrdinalIgnoreCase))
                return "SlackBotToken must start with xoxb-.";

            if (settings.EnableSlack)
            {
                if (String.IsNullOrEmpty(settings.SlackAppToken))
                    return "SlackAppToken is required when Slack is enabled.";
                if (String.IsNullOrEmpty(settings.SlackBotToken))
                    return "SlackBotToken is required when Slack is enabled.";
                if (String.IsNullOrEmpty(settings.SlackChannelId))
                    return "SlackChannelId is required when Slack is enabled.";
                if (String.IsNullOrWhiteSpace(settings.SlackMessagePrefix))
                    return "SlackMessagePrefix is required when Slack is enabled.";
            }

            return null;
        }

        private static string ValidateToolPolicyJson(AssistantSettings settings)
        {
            if (settings == null) return "Settings payload is required.";

            string policyJson = settings.ToolPolicyJson?.Trim();
            if (String.IsNullOrEmpty(policyJson))
            {
                settings.ToolPolicyJson = null;
                return null;
            }

            try
            {
                AssistantToolPolicy policy = JsonSerializer.Deserialize<AssistantToolPolicy>(policyJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (policy == null)
                {
                    settings.ToolPolicyJson = null;
                    return null;
                }

                settings.ToolPolicy = policy;
                return null;
            }
            catch (JsonException e)
            {
                return "ToolPolicyJson must be valid AssistantToolPolicy JSON: " + e.Message;
            }
        }

        private AssistantToolPolicyValidationResult ValidateDraftToolPolicy(
            Assistant assistant,
            AssistantSettings settings,
            AssistantToolPolicyValidationRequest request)
        {
            AssistantToolPolicyValidationResult result = new AssistantToolPolicyValidationResult();
            AssistantToolPolicy policy = request?.ToolPolicy;
            string policyJson = request?.ToolPolicyJson?.Trim();

            if (policy == null && !String.IsNullOrWhiteSpace(policyJson))
            {
                try
                {
                    policy = JsonSerializer.Deserialize<AssistantToolPolicy>(policyJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException e)
                {
                    result.Success = false;
                    result.Message = "Tool policy JSON is invalid.";
                    AddToolPolicyValidationError(result, "invalid_tool_policy_json", "ToolPolicyJson must be valid AssistantToolPolicy JSON: " + e.Message);
                    return result;
                }
            }

            policy ??= new AssistantToolPolicy();
            settings.ToolPolicy = policy;

            AssistantToolPolicyResolver resolver = new AssistantToolPolicyResolver(Settings);
            List<AssistantToolDescriptor> allTools = resolver.Resolve(assistant, settings, true);
            List<AssistantToolDescriptor> availableTools = resolver.Resolve(assistant, settings, false);

            foreach (string toolName in policy.AllowedToolNames ?? new List<string>())
            {
                if (!allTools.Any(tool => String.Equals(tool.ToolName, toolName, StringComparison.OrdinalIgnoreCase)))
                    AddToolPolicyValidationError(result, "unknown_allowed_tool", "AllowedToolNames contains an unknown tool: " + toolName + ".");
            }

            if (policy.EnableToolCalls && !String.Equals(policy.ToolChoiceMode, "None", StringComparison.OrdinalIgnoreCase))
            {
                bool anyToolSwitchEnabled =
                    policy.EnableCollectionSearchTool
                    || policy.EnableCollectionReadChunksTool
                    || policy.EnableCollectionEnumerateDocumentsTool
                    || policy.EnableVerbexFullTextSearchTool
                    || policy.EnableIndexEnumerateRecordsTool
                    || policy.EnableS3ObjectReadTool
                    || policy.EnableBucketEnumerateObjectsTool
                    || policy.EnableWebSearchTool;

                if (!anyToolSwitchEnabled)
                    AddToolPolicyValidationError(result, "no_tool_enabled", "EnableToolCalls is true but no tool switch is enabled.");
                else if (availableTools.Count == 0)
                    AddToolPolicyValidationError(result, "no_available_tools", "EnableToolCalls is true but no enabled tool is currently executable after prerequisites and allow-lists are applied.");
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success ? "Tool policy is valid." : "Tool policy is invalid.";
            result.ToolPolicy = settings.ToolPolicy;
            result.ToolPolicyJson = Serializer.SerializeJson(settings.ToolPolicy);
            result.Tools = allTools;
            return result;
        }

        private static void AddToolPolicyValidationError(AssistantToolPolicyValidationResult result, string code, string message)
        {
            if (result == null) return;
            if (!String.IsNullOrWhiteSpace(code) && !result.ErrorCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                result.ErrorCodes.Add(code);
            if (!String.IsNullOrWhiteSpace(message))
                result.Errors.Add(message);
        }

        private async Task<AssistantToolPolicyTestResult> BuildToolPolicyTestResultAsync(
            Assistant assistant,
            AssistantSettings settings,
            AssistantToolPolicyValidationRequest request)
        {
            AssistantToolPolicyValidationResult validation = ValidateDraftToolPolicy(assistant, settings, request);
            AssistantToolPolicyTestResult result = new AssistantToolPolicyTestResult
            {
                AssistantId = assistant?.Id,
                InferenceEndpointId = settings?.InferenceEndpointId,
                Validation = validation,
                Tools = validation?.Tools ?? new List<AssistantToolDescriptor>()
            };

            foreach (string error in validation?.Errors ?? new List<string>())
                AddToolPolicyTestError(result, null, error);
            foreach (string code in validation?.ErrorCodes ?? new List<string>())
                AddToolPolicyTestError(result, code, null);

            AssistantToolPolicy policy = validation?.ToolPolicy ?? settings?.ToolPolicy ?? new AssistantToolPolicy();
            bool toolCallsRequested = policy.EnableToolCalls && !String.Equals(policy.ToolChoiceMode, "None", StringComparison.OrdinalIgnoreCase);
            if (toolCallsRequested)
            {
                if (String.IsNullOrWhiteSpace(settings?.InferenceEndpointId))
                {
                    AddToolPolicyTestError(result, "completion_endpoint_missing", "InferenceEndpointId is required before tool calls can run.");
                }
                else
                {
                    PartioEndpointConfig endpoint = await ResolveCompletionEndpointConfigForDiagnosticsAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (endpoint == null)
                    {
                        AddToolPolicyTestError(result, "completion_endpoint_unresolved", "The selected completion endpoint could not be resolved.");
                    }
                    else
                    {
                        result.EndpointResolved = true;
                        result.EndpointModel = endpoint.Model;
                        result.EndpointApiFormat = endpoint.ApiFormat;
                        result.EndpointActive = endpoint.Active;
                        result.EndpointSupportsToolCalling = endpoint.SupportsToolCalling;
                        result.EndpointToolCallingApiFormat = endpoint.ToolCallingApiFormat;
                        result.EndpointSupportsParallelToolCalls = endpoint.SupportsParallelToolCalls;
                        result.EndpointSupportsStreamingToolCalls = endpoint.SupportsStreamingToolCalls;

                        if (!endpoint.Active)
                            AddToolPolicyTestError(result, "completion_endpoint_inactive", "The selected completion endpoint is inactive.");
                        if (!endpoint.SupportsToolCalling)
                            AddToolPolicyTestError(result, "completion_endpoint_not_tool_capable", "The selected completion endpoint does not explicitly support tool calling.");
                        else if (!IsSupportedToolCallingEndpoint(endpoint))
                            AddToolPolicyTestError(result, "unsupported_tool_call_format", "The selected completion endpoint tool-call format is not supported for its provider.");

                        if (policy.AllowParallelToolCalls && !endpoint.SupportsParallelToolCalls)
                            result.Warnings.Add("The policy allows parallel tool calls, but the selected endpoint does not advertise parallel tool-call support.");
                        if (settings.Streaming && !endpoint.SupportsStreamingToolCalls)
                            result.Warnings.Add("Streaming chat is enabled, but the selected endpoint does not advertise streaming tool-call support; validate this path manually before production use.");
                    }
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? "Tool diagnostics passed without blocking issues."
                : "Tool diagnostics found blocking issues.";
            return result;
        }

        private async Task<PartioEndpointConfig> ResolveCompletionEndpointConfigForDiagnosticsAsync(string endpointId)
        {
            if (String.IsNullOrWhiteSpace(endpointId)) return null;

            try
            {
                using HttpResponseMessage response = await InferenceEndpoints.SendAsync(
                    System.Net.Http.HttpMethod.Get,
                    "/v1.0/endpoints/completion/" + endpointId).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Logging.Warn(_Header + "tool diagnostics failed to resolve completion endpoint " + endpointId + ": " + (int)response.StatusCode);
                    return null;
                }

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                PartioEndpointConfig endpoint = JsonSerializer.Deserialize<PartioEndpointConfig>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                PartioEndpointToolMetadata.ReadTagsToToolFields(endpoint);
                return endpoint;
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "tool diagnostics exception resolving completion endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        private static bool IsSupportedToolCallingEndpoint(PartioEndpointConfig endpoint)
        {
            if (endpoint == null || !endpoint.SupportsToolCalling) return false;

            Enums.InferenceProviderEnum provider = InferenceProviderHelper.FromApiFormat(endpoint.ApiFormat, Enums.InferenceProviderEnum.Ollama);
            string format = NormalizeToolCallingApiFormat(endpoint.ToolCallingApiFormat);
            return provider switch
            {
                Enums.InferenceProviderEnum.OpenAI => format == "openaichatcompletions" || format == "openai",
                Enums.InferenceProviderEnum.Ollama => format == "ollamachat" || format == "ollama",
                _ => false
            };
        }

        private static string NormalizeToolCallingApiFormat(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return String.Empty;
            return new string(value.Trim().Where(Char.IsLetterOrDigit).Select(Char.ToLowerInvariant).ToArray());
        }

        private static void AddToolPolicyTestError(AssistantToolPolicyTestResult result, string code, string message)
        {
            if (result == null) return;
            if (!String.IsNullOrWhiteSpace(code) && !result.ErrorCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                result.ErrorCodes.Add(code);
            if (!String.IsNullOrWhiteSpace(message))
                result.Errors.Add(message);
        }

        private static string ValidateSlackSettings(SlackVerificationRequest request, bool requireAll)
        {
            if (request == null) return "Verification payload is required.";

            if (!String.IsNullOrEmpty(request.SlackAppToken) && !request.SlackAppToken.StartsWith("xapp-", StringComparison.OrdinalIgnoreCase))
                return "SlackAppToken must start with xapp-.";
            if (!String.IsNullOrEmpty(request.SlackBotToken) && !request.SlackBotToken.StartsWith("xoxb-", StringComparison.OrdinalIgnoreCase))
                return "SlackBotToken must start with xoxb-.";

            if (requireAll || request.EnableSlack)
            {
                if (String.IsNullOrEmpty(request.SlackAppToken))
                    return "SlackAppToken is required for verification.";
                if (String.IsNullOrEmpty(request.SlackBotToken))
                    return "SlackBotToken is required for verification.";
                if (String.IsNullOrEmpty(request.SlackChannelId))
                    return "SlackChannelId is required for verification.";
                if (String.IsNullOrWhiteSpace(request.SlackMessagePrefix))
                    return "SlackMessagePrefix is required for verification.";
            }

            return null;
        }

        private static bool SlackSettingsChanged(AssistantSettings existing, AssistantSettings updated)
        {
            if (existing == null) return updated != null && updated.EnableSlack;
            if (updated == null) return false;

            return existing.EnableSlack != updated.EnableSlack
                || !String.Equals(existing.SlackAppToken, updated.SlackAppToken, StringComparison.Ordinal)
                || !String.Equals(existing.SlackBotToken, updated.SlackBotToken, StringComparison.Ordinal)
                || !String.Equals(existing.SlackChannelId, updated.SlackChannelId, StringComparison.Ordinal)
                || !String.Equals(existing.SlackMessagePrefix, updated.SlackMessagePrefix, StringComparison.Ordinal);
        }

        private async Task<SlackVerificationResponse> VerifySlackDraftAsync(SlackVerificationRequest request)
        {
            SlackVerificationResponse response = new SlackVerificationResponse();

            SlackConnector connector = null;
            try
            {
                connector = new SlackConnector(new SlackConnectorOptions(new SlackAuthMaterial(request.SlackBotToken, request.SlackAppToken)));

                SlackValidationResult? validation = await connector.ValidateConnectionAsync(CancellationToken.None).ConfigureAwait(false);
                response.BotToken.Success = validation != null && validation.Ok;
                response.BotToken.Message = response.BotToken.Success ? "Bot token is valid." : "Bot token validation failed.";
                response.BotToken.Details = validation == null ? null : new
                {
                    validation.TeamId,
                    validation.TeamName,
                    validation.UserId,
                    validation.UserName,
                    validation.BotId,
                    validation.Error
                };

                if (!response.BotToken.Success)
                    Logging.Warn(_Header + "Slack bot token verification failed for assistant settings verification.");

                SlackChannelInfoResult? channel = await connector.GetChannelInfoAsync(request.SlackChannelId, CancellationToken.None).ConfigureAwait(false);
                response.Channel.Success = channel != null && channel.Ok;
                response.Channel.Message = response.Channel.Success ? "Channel lookup succeeded." : "Channel lookup failed.";
                response.Channel.Details = channel == null ? null : new
                {
                    channel.ChannelId,
                    channel.Name,
                    channel.IsChannel,
                    channel.IsPrivate,
                    channel.Error
                };

                if (!response.Channel.Success)
                    Logging.Warn(_Header + "Slack channel verification failed for assistant settings verification.");

                try
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        await connector.StartAsync(cts.Token).ConfigureAwait(false);
                        response.SocketMode.Success = true;
                        response.SocketMode.Message = "Socket Mode connection succeeded.";
                        await connector.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception socketEx)
                {
                    response.SocketMode.Success = false;
                    response.SocketMode.Message = socketEx.Message;
                    Logging.Warn(_Header + "Slack Socket Mode verification failed: " + socketEx.Message);
                }

                response.Success = response.BotToken.Success && response.Channel.Success && response.SocketMode.Success;
                return response;
            }
            finally
            {
                if (connector != null)
                {
                    try
                    {
                        await connector.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
