#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles crawl operation routes under /v1.0/crawlplans/{planId}/operations.
    /// </summary>
    public class CrawlOperationHandler : HandlerBase
    {
        private static readonly string _Header = "[CrawlOperationHandler] ";

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
        /// <param name="processingLog">Processing log service.</param>
        public CrawlOperationHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            ProcessingLogService processingLog)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference, processingLog)
        {
        }

        /// <summary>
        /// GET /v1.0/crawlplans/{planId}/operations - List operations for a crawl plan.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetOperationsAsync(HttpContextBase ctx)
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

                string planId = ctx.Request.Url.Parameters["planId"];
                if (String.IsNullOrEmpty(planId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<CrawlOperation> result = await Database.CrawlOperation.EnumerateByCrawlPlanAsync(planId, query).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetOperationsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/crawlplans/{planId}/operations/statistics - Get aggregate statistics for all operations.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetStatisticsAsync(HttpContextBase ctx)
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

                string planId = ctx.Request.Url.Parameters["planId"];
                if (String.IsNullOrEmpty(planId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                // Retrieve all operations for this plan
                EnumerationQuery query = new EnumerationQuery();
                query.MaxResults = 1000;
                EnumerationResult<CrawlOperation> result = await Database.CrawlOperation.EnumerateByCrawlPlanAsync(planId, query).ConfigureAwait(false);
                List<CrawlOperation> operations = result.Objects;

                DateTime? lastRun = null;
                DateTime? nextRun = null;
                int failedRunCount = 0;
                int successfulRunCount = 0;
                double minRuntimeMs = 0;
                double maxRuntimeMs = 0;
                double avgRuntimeMs = 0;
                long objectCount = 0;
                long bytesCrawled = 0;

                if (operations != null && operations.Count > 0)
                {
                    List<double> runtimes = new List<double>();

                    foreach (CrawlOperation op in operations)
                    {
                        if (op.StartUtc != null)
                        {
                            if (lastRun == null || op.StartUtc.Value > lastRun.Value)
                                lastRun = op.StartUtc;
                        }

                        if (op.State == Enums.CrawlOperationStateEnum.Success)
                            successfulRunCount++;
                        else if (op.State == Enums.CrawlOperationStateEnum.Failed)
                            failedRunCount++;

                        if (op.StartUtc != null && op.FinishUtc != null)
                        {
                            double runtimeMs = (op.FinishUtc.Value - op.StartUtc.Value).TotalMilliseconds;
                            runtimes.Add(runtimeMs);
                        }

                        objectCount += op.ObjectsEnumerated;
                        bytesCrawled += op.BytesEnumerated;
                    }

                    if (runtimes.Count > 0)
                    {
                        minRuntimeMs = runtimes.Min();
                        maxRuntimeMs = runtimes.Max();
                        avgRuntimeMs = runtimes.Average();
                    }

                    // Compute next run based on schedule
                    if (plan.Schedule != null && lastRun != null)
                    {
                        double intervalMinutes = plan.Schedule.IntervalType switch
                        {
                            Enums.ScheduleIntervalEnum.Minutes => plan.Schedule.IntervalValue,
                            Enums.ScheduleIntervalEnum.Hours => plan.Schedule.IntervalValue * 60.0,
                            Enums.ScheduleIntervalEnum.Days => plan.Schedule.IntervalValue * 1440.0,
                            Enums.ScheduleIntervalEnum.Weeks => plan.Schedule.IntervalValue * 10080.0,
                            _ => 0
                        };
                        if (intervalMinutes > 0) nextRun = lastRun.Value.AddMinutes(intervalMinutes);
                    }
                }

                var statistics = new
                {
                    LastRun = lastRun,
                    NextRun = nextRun,
                    FailedRunCount = failedRunCount,
                    SuccessfulRunCount = successfulRunCount,
                    MinRuntimeMs = minRuntimeMs,
                    MaxRuntimeMs = maxRuntimeMs,
                    AvgRuntimeMs = avgRuntimeMs,
                    ObjectCount = objectCount,
                    BytesCrawled = bytesCrawled
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(statistics)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetStatisticsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/crawlplans/{planId}/operations/{id} - Get a single operation.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetOperationAsync(HttpContextBase ctx)
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

                string planId = ctx.Request.Url.Parameters["planId"];
                string id = ctx.Request.Url.Parameters["id"];
                if (String.IsNullOrEmpty(planId) || String.IsNullOrEmpty(id))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                CrawlOperation operation = await Database.CrawlOperation.ReadAsync(id).ConfigureAwait(false);
                if (operation == null || !String.Equals(operation.CrawlPlanId, planId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(operation)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetOperationAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/crawlplans/{planId}/operations/{id}/statistics - Get statistics for a single operation.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetOperationStatisticsAsync(HttpContextBase ctx)
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

                string planId = ctx.Request.Url.Parameters["planId"];
                string id = ctx.Request.Url.Parameters["id"];
                if (String.IsNullOrEmpty(planId) || String.IsNullOrEmpty(id))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                CrawlOperation operation = await Database.CrawlOperation.ReadAsync(id).ConfigureAwait(false);
                if (operation == null || !String.Equals(operation.CrawlPlanId, planId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                double runtimeMs = 0;
                if (operation.StartUtc != null && operation.FinishUtc != null)
                    runtimeMs = (operation.FinishUtc.Value - operation.StartUtc.Value).TotalMilliseconds;

                var statistics = new
                {
                    LastRun = operation.StartUtc,
                    FailedRunCount = (operation.State == Enums.CrawlOperationStateEnum.Failed) ? 1 : 0,
                    SuccessfulRunCount = (operation.State == Enums.CrawlOperationStateEnum.Success) ? 1 : 0,
                    RuntimeMs = runtimeMs,
                    ObjectCount = operation.ObjectsEnumerated,
                    BytesCrawled = operation.BytesEnumerated
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(statistics)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetOperationStatisticsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/crawlplans/{planId}/operations/{id} - Delete a single operation.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteOperationAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string planId = ctx.Request.Url.Parameters["planId"];
                string id = ctx.Request.Url.Parameters["id"];
                if (String.IsNullOrEmpty(planId) || String.IsNullOrEmpty(id))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                CrawlOperation operation = await Database.CrawlOperation.ReadAsync(id).ConfigureAwait(false);
                if (operation == null || !String.Equals(operation.CrawlPlanId, planId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                await Database.CrawlOperation.DeleteAsync(id).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteOperationAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/crawlplans/{planId}/operations/{id}/enumeration - Get enumeration file contents.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetEnumerationAsync(HttpContextBase ctx)
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

                string planId = ctx.Request.Url.Parameters["planId"];
                string id = ctx.Request.Url.Parameters["id"];
                if (String.IsNullOrEmpty(planId) || String.IsNullOrEmpty(id))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                CrawlPlan plan = await Database.CrawlPlan.ReadAsync(planId).ConfigureAwait(false);
                if (plan == null || !EnforceTenantOwnership(auth, plan.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                CrawlOperation operation = await Database.CrawlOperation.ReadAsync(id).ConfigureAwait(false);
                if (operation == null || !String.Equals(operation.CrawlPlanId, planId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrEmpty(operation.EnumerationFile) || !File.Exists(operation.EnumerationFile))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Enumeration file not found."))).ConfigureAwait(false);
                    return;
                }

                string json = await File.ReadAllTextAsync(operation.EnumerationFile).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(json).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetEnumerationAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}

#pragma warning restore CS8625, CS8603, CS8600
