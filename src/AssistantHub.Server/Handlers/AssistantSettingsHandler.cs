namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using EasySlack;
    using SyslogLogging;
    using WatsonWebserver.Core;

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
            StorageService storage,
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

                NormalizeSlackSettings(updated);

                string slackValidationError = ValidateSlackSettings(updated);
                if (!String.IsNullOrEmpty(slackValidationError))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, slackValidationError))).ConfigureAwait(false);
                    return;
                }

                updated.TextWeight = Math.Clamp(updated.TextWeight, 0.0, 1.0);
                updated.RetrievalIncludeNeighbors = Math.Clamp(updated.RetrievalIncludeNeighbors, 0, 10);

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

                var validation = await connector.ValidateConnectionAsync(CancellationToken.None).ConfigureAwait(false);
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

                var channel = await connector.GetChannelInfoAsync(request.SlackChannelId, CancellationToken.None).ConfigureAwait(false);
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
