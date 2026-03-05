namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
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
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles RAG evaluation routes: facts CRUD, run management, results, and SSE streaming.
    /// </summary>
    public class EvalHandler : HandlerBase
    {
        private static readonly string _Header = "[EvalHandler] ";
        private readonly EvalService _EvalService;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            EvalService evalService)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _EvalService = evalService ?? throw new ArgumentNullException(nameof(evalService));
        }

        #region Facts

        /// <summary>
        /// PUT /v1.0/eval/facts - Create a new eval fact.
        /// </summary>
        public async Task PutFactAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string body = await ctx.Request.ReadPayloadAsStringAsync().ConfigureAwait(false);
                if (String.IsNullOrEmpty(body))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalFact fact = Serializer.DeserializeJson<EvalFact>(body);
                fact.TenantId = auth.TenantId;

                if (String.IsNullOrEmpty(fact.AssistantId) || fact.AssistantId == "asst_placeholder")
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                fact = await Database.EvalFact.CreateAsync(fact).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(fact)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutFactAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/facts - List eval facts.
        /// </summary>
        public async Task GetFactsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<EvalFact> result = await Database.EvalFact.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetFactsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/facts/{factId} - Get a single eval fact.
        /// </summary>
        public async Task GetFactAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string factId = ctx.Request.Url.Parameters["factId"];
                if (String.IsNullOrEmpty(factId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalFact fact = await Database.EvalFact.ReadAsync(factId).ConfigureAwait(false);
                if (fact == null || !EnforceTenantOwnership(auth, fact.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(fact)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetFactAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/eval/facts/{factId} - Update an eval fact.
        /// </summary>
        public async Task PutFactByIdAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string factId = ctx.Request.Url.Parameters["factId"];
                if (String.IsNullOrEmpty(factId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalFact existing = await Database.EvalFact.ReadAsync(factId).ConfigureAwait(false);
                if (existing == null || !EnforceTenantOwnership(auth, existing.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string body = await ctx.Request.ReadPayloadAsStringAsync().ConfigureAwait(false);
                EvalFact updated = Serializer.DeserializeJson<EvalFact>(body);

                existing.Category = updated.Category;
                existing.Question = updated.Question;
                existing.ExpectedFacts = updated.ExpectedFacts;

                existing = await Database.EvalFact.UpdateAsync(existing).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(existing)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutFactByIdAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/eval/facts/{factId} - Delete an eval fact.
        /// </summary>
        public async Task DeleteFactAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string factId = ctx.Request.Url.Parameters["factId"];
                if (String.IsNullOrEmpty(factId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalFact fact = await Database.EvalFact.ReadAsync(factId).ConfigureAwait(false);
                if (fact == null || !EnforceTenantOwnership(auth, fact.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                await Database.EvalFact.DeleteAsync(factId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteFactAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion

        #region Runs

        /// <summary>
        /// POST /v1.0/eval/runs - Start a new evaluation run.
        /// </summary>
        public async Task PostRunAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string body = await ctx.Request.ReadPayloadAsStringAsync().ConfigureAwait(false);

                string assistantId = null;
                string judgePromptOverride = null;

                if (!String.IsNullOrEmpty(body))
                {
                    try
                    {
                        JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
                        if (json.TryGetProperty("AssistantId", out JsonElement aiProp))
                            assistantId = aiProp.GetString();
                        if (json.TryGetProperty("JudgePrompt", out JsonElement jpProp))
                            judgePromptOverride = jpProp.GetString();
                    }
                    catch { }
                }

                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalRun run = await _EvalService.StartRunAsync(auth.TenantId, assistantId, judgePromptOverride).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(run)).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Message = ex.Message })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostRunAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/runs - List eval runs.
        /// </summary>
        public async Task GetRunsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<EvalRun> result = await Database.EvalRun.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRunsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/runs/{runId} - Get a single eval run.
        /// </summary>
        public async Task GetRunAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string runId = ctx.Request.Url.Parameters["runId"];
                if (String.IsNullOrEmpty(runId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalRun run = await Database.EvalRun.ReadAsync(runId).ConfigureAwait(false);
                if (run == null || !EnforceTenantOwnership(auth, run.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(run)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRunAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/eval/runs/{runId} - Delete an eval run and its results.
        /// </summary>
        public async Task DeleteRunAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string runId = ctx.Request.Url.Parameters["runId"];
                if (String.IsNullOrEmpty(runId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalRun run = await Database.EvalRun.ReadAsync(runId).ConfigureAwait(false);
                if (run == null || !EnforceTenantOwnership(auth, run.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                await Database.EvalRun.DeleteAsync(runId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteRunAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion

        #region Results

        /// <summary>
        /// GET /v1.0/eval/runs/{runId}/results - Get all results for a run.
        /// </summary>
        public async Task GetRunResultsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string runId = ctx.Request.Url.Parameters["runId"];
                if (String.IsNullOrEmpty(runId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalRun run = await Database.EvalRun.ReadAsync(runId).ConfigureAwait(false);
                if (run == null || !EnforceTenantOwnership(auth, run.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                List<EvalResult> results = await Database.EvalResult.GetByRunIdAsync(runId).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(results)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRunResultsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/results/{resultId} - Get a single eval result.
        /// </summary>
        public async Task GetResultAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string resultId = ctx.Request.Url.Parameters["resultId"];
                if (String.IsNullOrEmpty(resultId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalResult result = await Database.EvalResult.ReadAsync(resultId).ConfigureAwait(false);
                if (result == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetResultAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/runs/{runId}/stream - SSE stream of run progress.
        /// </summary>
        public async Task GetRunStreamAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = GetAuthContext(ctx);
                string runId = ctx.Request.Url.Parameters["runId"];
                if (String.IsNullOrEmpty(runId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EvalRun run = await Database.EvalRun.ReadAsync(runId).ConfigureAwait(false);
                if (run == null || !EnforceTenantOwnership(auth, run.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/event-stream";
                ctx.Response.Headers.Add("Cache-Control", "no-cache");
                ctx.Response.Headers.Add("Connection", "keep-alive");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                while (true)
                {
                    run = await Database.EvalRun.ReadAsync(runId).ConfigureAwait(false);
                    List<EvalResult> results = await Database.EvalResult.GetByRunIdAsync(runId).ConfigureAwait(false);

                    string data = JsonSerializer.Serialize(new { Run = run, Results = results }, jsonOptions);
                    string sseMessage = "data: " + data + "\n\n";
                    await ctx.Response.SendChunk(System.Text.Encoding.UTF8.GetBytes(sseMessage)).ConfigureAwait(false);

                    if (run.Status == Enums.EvalStatusEnum.Completed || run.Status == Enums.EvalStatusEnum.Failed)
                    {
                        await ctx.Response.SendChunk(System.Text.Encoding.UTF8.GetBytes("data: [DONE]\n\n")).ConfigureAwait(false);
                        break;
                    }

                    await Task.Delay(2000).ConfigureAwait(false);
                }

                await ctx.Response.SendFinalChunk(Array.Empty<byte>()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRunStreamAsync: " + e.Message);
            }
        }

        /// <summary>
        /// GET /v1.0/eval/judge-prompt/default - Get the default judge prompt.
        /// </summary>
        public async Task GetDefaultJudgePromptAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Prompt = EvalService.DefaultJudgePrompt })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDefaultJudgePromptAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
