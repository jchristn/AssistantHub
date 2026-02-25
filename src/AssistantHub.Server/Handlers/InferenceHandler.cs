namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Web;
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
    /// Handles inference model listing and pull routes.
    /// </summary>
    public class InferenceHandler : HandlerBase
    {
        private static readonly string _Header = "[InferenceHandler] ";
        private readonly object _PullLock = new object();
        private PullProgress _CurrentPullProgress = null;

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
        public InferenceHandler(
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
        /// GET /v1.0/models - List available models on the configured inference provider.
        /// Accepts optional query parameter ?assistantId={id} to list models from an assistant's endpoint.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetModelsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                InferenceService inference = await ResolveInferenceServiceAsync(ctx).ConfigureAwait(false);
                if (inference == null) return; // response already sent

                List<InferenceModel> models = await inference.ListModelsAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(models)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetModelsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/models/pull - Start pulling a model in the background (admin only).
        /// Returns 202 Accepted immediately. Poll GET /v1.0/models/pull/status for progress.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostPullModelAsync(HttpContextBase ctx)
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

                InferenceService inference = await ResolveInferenceServiceAsync(ctx).ConfigureAwait(false);
                if (inference == null) return; // response already sent

                if (!inference.IsPullSupported)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, description: "Pull is not supported by the configured inference provider."))).ConfigureAwait(false);
                    return;
                }

                string requestBody = null;
                using (StreamReader reader = new StreamReader(ctx.Request.Data))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (String.IsNullOrEmpty(requestBody))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                PullModelRequest pullRequest = Serializer.DeserializeJson<PullModelRequest>(requestBody);
                if (pullRequest == null || String.IsNullOrEmpty(pullRequest.Name))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, description: "Model name is required."))).ConfigureAwait(false);
                    return;
                }

                lock (_PullLock)
                {
                    if (_CurrentPullProgress != null && !_CurrentPullProgress.IsComplete && !_CurrentPullProgress.HasError)
                    {
                        ctx.Response.StatusCode = 409;
                        ctx.Response.ContentType = "application/json";
                        ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, description: "A pull operation is already in progress."))).Wait();
                        return;
                    }

                    _CurrentPullProgress = new PullProgress
                    {
                        ModelName = pullRequest.Name,
                        Status = "starting",
                        StartedUtc = DateTime.UtcNow
                    };
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await inference.PullModelWithProgressAsync(pullRequest.Name, (progress) =>
                        {
                            lock (_PullLock)
                            {
                                _CurrentPullProgress = progress;
                            }
                            return Task.CompletedTask;
                        }).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logging.Warn(_Header + "background pull exception: " + e.Message);
                        lock (_PullLock)
                        {
                            if (_CurrentPullProgress != null)
                            {
                                _CurrentPullProgress.HasError = true;
                                _CurrentPullProgress.ErrorMessage = e.Message;
                                _CurrentPullProgress.IsComplete = true;
                            }
                        }
                    }
                });

                ctx.Response.StatusCode = 202;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { ModelName = pullRequest.Name, Status = "starting" })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostPullModelAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/models/pull/status - Get the current pull operation status (admin only).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetPullStatusAsync(HttpContextBase ctx)
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

                PullProgress progress;
                lock (_PullLock)
                {
                    progress = _CurrentPullProgress;
                }

                if (progress == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, description: "No pull operation in progress."))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(progress)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetPullStatusAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/models/{modelName} - Delete a model (admin only).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteModelAsync(HttpContextBase ctx)
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

                InferenceService inference = await ResolveInferenceServiceAsync(ctx).ConfigureAwait(false);
                if (inference == null) return; // response already sent

                string modelName = ctx.Request.Url.Parameters["modelName"];
                if (!String.IsNullOrEmpty(modelName))
                    modelName = HttpUtility.UrlDecode(modelName);

                if (String.IsNullOrEmpty(modelName))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, description: "Model name is required."))).ConfigureAwait(false);
                    return;
                }

                bool success = await inference.DeleteModelAsync(modelName).ConfigureAwait(false);

                if (success)
                {
                    ctx.Response.StatusCode = 204;
                    await ctx.Response.Send().ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteModelAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #region Private-Methods

        /// <summary>
        /// Resolve the inference service to use. If an assistantId query parameter is present,
        /// loads the assistant's settings and creates a temporary InferenceService targeting
        /// that assistant's endpoint. Otherwise returns the global Inference service.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>InferenceService instance, or null if an error response was sent.</returns>
        private async Task<InferenceService> ResolveInferenceServiceAsync(HttpContextBase ctx)
        {
            string assistantId = ctx.Request.Query.Elements.Get("assistantId");
            if (String.IsNullOrEmpty(assistantId))
                return Inference;

            AssistantSettings assistantSettings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
            if (assistantSettings == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, description: "Assistant or assistant settings not found."))).ConfigureAwait(false);
                return null;
            }

            InferenceSettings tempSettings = new InferenceSettings
            {
                Provider = Settings.Inference.Provider,
                Endpoint = Settings.Inference.Endpoint,
                ApiKey = Settings.Inference.ApiKey,
                DefaultModel = !String.IsNullOrEmpty(assistantSettings.Model) ? assistantSettings.Model : Settings.Inference.DefaultModel
            };

            if (!String.IsNullOrEmpty(assistantSettings.InferenceEndpointId))
            {
                try
                {
                    string url = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/completion/" + assistantSettings.InferenceEndpointId;
                    using (HttpClient client = new HttpClient())
                    {
                        if (!String.IsNullOrEmpty(Settings.Chunking.AccessKey))
                            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

                        HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                            JsonElement ep = JsonSerializer.Deserialize<JsonElement>(body, jsonOpts);

                            string apiFormat = ep.TryGetProperty("ApiFormat", out JsonElement af) ? af.GetString() : null;
                            if (!String.IsNullOrEmpty(apiFormat) && apiFormat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                                tempSettings.Provider = Enums.InferenceProviderEnum.OpenAI;

                            string epUrl = ep.TryGetProperty("Endpoint", out JsonElement eu) ? eu.GetString() : null;
                            if (!String.IsNullOrEmpty(epUrl)) tempSettings.Endpoint = epUrl;

                            string apiKey = ep.TryGetProperty("ApiKey", out JsonElement ak) ? ak.GetString() : null;
                            if (!String.IsNullOrEmpty(apiKey)) tempSettings.ApiKey = apiKey;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Warn(_Header + "failed to resolve completion endpoint: " + ex.Message);
                }
            }

            return new InferenceService(tempSettings, Logging);
        }

        #endregion

        #region Private-Classes

        private class PullModelRequest
        {
            public string Name { get; set; } = null;
        }

        #endregion
    }
}
