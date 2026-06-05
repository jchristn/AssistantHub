namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using Enums = AssistantHub.Core.Enums;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles assistant analytics routes.
    /// </summary>
    public class AssistantAnalyticsHandler : HandlerBase
    {
        private static readonly string _Header = "[AssistantAnalyticsHandler] ";
        private readonly AssistantAnalyticsService _Analytics;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantAnalyticsHandler(
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
            _Analytics = new AssistantAnalyticsService(database);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/overview.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetOverviewAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetOverviewAsync(filter).ConfigureAwait(false), "GetOverviewAsync").ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/timeseries.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetTimeSeriesAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetTimeSeriesAsync(filter).ConfigureAwait(false), "GetTimeSeriesAsync").ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/stages.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetStagesAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetStagesAsync(filter).ConfigureAwait(false), "GetStagesAsync").ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/endpoints.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetEndpointsAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetEndpointsAsync(filter).ConfigureAwait(false), "GetEndpointsAsync").ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/slowest.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetSlowestAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetSlowestAsync(filter).ConfigureAwait(false), "GetSlowestAsync").ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/analytics/feedback.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetFeedbackAsync(HttpContextBase ctx)
        {
            await ExecuteAnalyticsAsync(ctx, async filter => await _Analytics.GetFeedbackAsync(filter).ConfigureAwait(false), "GetFeedbackAsync").ConfigureAwait(false);
        }

        private async Task ExecuteAnalyticsAsync(HttpContextBase ctx, Func<AssistantAnalyticsFilter, Task<object>> executor, string operation)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                AssistantAnalyticsFilter filter = await BuildAuthorizedFilterAsync(ctx, auth).ConfigureAwait(false);
                if (filter == null) return;

                object result = await executor(filter).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (ArgumentException e)
            {
                Logging.Warn(_Header + "bad request in " + operation + ": " + e.Message);
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in " + operation + ": " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private async Task<AssistantAnalyticsFilter> BuildAuthorizedFilterAsync(HttpContextBase ctx, AuthContext auth)
        {
            string assistantId = ctx.Request.Url.Parameters["assistantId"];
            if (String.IsNullOrEmpty(assistantId))
            {
                await SendBadRequest(ctx).ConfigureAwait(false);
                return null;
            }

            Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
            if (assistant == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return null;
            }

            if (!auth.IsGlobalAdmin && !String.Equals(auth.TenantId, assistant.TenantId, StringComparison.Ordinal))
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return null;
            }

            if (!auth.IsGlobalAdmin
                && !auth.IsTenantAdmin
                && !String.Equals(auth.UserId, assistant.UserId, StringComparison.Ordinal))
            {
                await SendAuthzFailure(ctx).ConfigureAwait(false);
                return null;
            }

            AssistantAnalyticsFilter filter = new AssistantAnalyticsFilter
            {
                TenantId = assistant.TenantId,
                AssistantId = assistant.Id,
                Range = GetDecodedQueryValue(ctx, "range")
            };

            if (String.IsNullOrEmpty(filter.Range))
                filter.Range = HasAnyExplicitRange(ctx) ? null : "lastDay";

            string metrics = GetDecodedQueryValue(ctx, "metrics");
            if (!String.IsNullOrWhiteSpace(metrics))
            {
                foreach (string metric in metrics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    filter.Metrics.Add(metric);
            }

            filter.Stage = GetDecodedQueryValue(ctx, "stage");
            filter.EndpointId = GetDecodedQueryValue(ctx, "endpointId");
            filter.EndpointType = GetDecodedQueryValue(ctx, "endpointType");
            filter.Model = GetDecodedQueryValue(ctx, "model");

            string startUtc = GetDecodedQueryValue(ctx, "startUtc");
            if (!String.IsNullOrEmpty(startUtc)
                && DateTime.TryParse(startUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedStartUtc))
                filter.StartUtc = parsedStartUtc;
            else if (!String.IsNullOrEmpty(startUtc))
                throw new ArgumentException("Invalid startUtc.");

            string endUtc = GetDecodedQueryValue(ctx, "endUtc");
            if (!String.IsNullOrEmpty(endUtc)
                && DateTime.TryParse(endUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedEndUtc))
                filter.EndUtc = parsedEndUtc;
            else if (!String.IsNullOrEmpty(endUtc))
                throw new ArgumentException("Invalid endUtc.");

            string bucketSeconds = GetDecodedQueryValue(ctx, "bucketSeconds");
            if (!String.IsNullOrEmpty(bucketSeconds))
            {
                if (!Int32.TryParse(bucketSeconds, out int parsedBucketSeconds))
                    throw new ArgumentException("Invalid bucketSeconds.");
                filter.BucketSeconds = parsedBucketSeconds;
            }

            string limit = GetDecodedQueryValue(ctx, "limit");
            if (!String.IsNullOrEmpty(limit))
            {
                if (!Int32.TryParse(limit, out int parsedLimit))
                    throw new ArgumentException("Invalid limit.");
                filter.Limit = parsedLimit;
            }

            return filter;
        }

        private static bool HasAnyExplicitRange(HttpContextBase ctx)
        {
            return !String.IsNullOrEmpty(ctx.Request.Query.Elements.Get("startUtc"))
                || !String.IsNullOrEmpty(ctx.Request.Query.Elements.Get("endUtc"));
        }

        private static string GetDecodedQueryValue(HttpContextBase ctx, string key)
        {
            string value = ctx.Request.Query.Elements.Get(key);
            if (String.IsNullOrEmpty(value))
                return value;

            try
            {
                return Uri.UnescapeDataString(value);
            }
            catch
            {
                return value;
            }
        }

        private async Task SendAuthzFailure(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
        }

        private async Task SendBadRequest(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
        }

        private async Task SendNotFound(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
        }
    }
}
