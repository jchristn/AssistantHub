namespace AssistantHub.Server.Handlers
{
    using System;
    using System.IO;
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
    /// Handles server configuration routes (admin only).
    /// </summary>
    public class ConfigurationHandler : HandlerBase
    {
        private static readonly string _Header = "[ConfigurationHandler] ";

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ConfigurationHandler(
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
        /// GET /v1.0/configuration - Return current settings.
        /// </summary>
        public async Task GetConfigurationAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(Settings)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Exception(e, _Header + "GetConfigurationAsync");
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/configuration - Update settings and persist to disk.
        /// </summary>
        public async Task PutConfigurationAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                AssistantHubSettings updated = Serializer.DeserializeJson<AssistantHubSettings>(body);
                if (updated == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Invalid settings payload."))).ConfigureAwait(false);
                    return;
                }

                Settings.Webserver = updated.Webserver;
                Settings.Database = updated.Database;
                Settings.S3 = updated.S3;
                Settings.DocumentAtom = updated.DocumentAtom;
                Settings.Chunking = updated.Chunking;
                Settings.Inference = updated.Inference;
                Settings.RecallDb = updated.RecallDb;
                Settings.Logging = updated.Logging;

                string json = Serializer.SerializeJson(Settings, true);
                File.WriteAllText(Constants.SettingsFile, json);

                Logging.Info(_Header + "configuration updated and saved to " + Constants.SettingsFile);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(Settings)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Exception(e, _Header + "PutConfigurationAsync");
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
