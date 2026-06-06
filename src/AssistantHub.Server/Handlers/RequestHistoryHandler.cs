namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Globalization;
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
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles request-history routes.
    /// </summary>
    public class RequestHistoryHandler : HandlerBase
    {
        private static readonly string _Header = "[RequestHistoryHandler] ";

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryHandler(
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
        /// GET /v1.0/requesthistory - enumerate request-history entries.
        /// </summary>
        public async Task GetRequestHistoryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                RequestHistorySearchFilter filter = BuildFilter(ctx, auth);
                EnumerationResult<RequestHistoryEntry> result = Settings.RequestHistory.Enabled
                    ? await Database.RequestHistory.EnumerateAsync(filter).ConfigureAwait(false)
                    : new EnumerationResult<RequestHistoryEntry> { MaxResults = filter.MaxResults };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRequestHistoryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/requesthistory/summary - summarize request-history entries.
        /// </summary>
        public async Task GetRequestHistorySummaryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                RequestHistorySearchFilter filter = BuildFilter(ctx, auth);
                RequestHistorySummaryResult result = Settings.RequestHistory.Enabled
                    ? await Database.RequestHistory.SummarizeAsync(filter).ConfigureAwait(false)
                    : BuildEmptySummary(filter);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRequestHistorySummaryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/requesthistory/{requestId} - get request-history detail.
        /// </summary>
        public async Task GetRequestHistoryEntryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                string requestId = ctx.Request.Url.Parameters["requestId"];
                if (String.IsNullOrEmpty(requestId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!Settings.RequestHistory.Enabled)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                RequestHistoryEntry entry = await Database.RequestHistory.ReadAsync(requestId, true).ConfigureAwait(false);
                if (entry == null || !EnforceTenantOwnership(auth, entry.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(entry)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRequestHistoryEntryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/requesthistory/{requestId}/detail - get request-history detail.
        /// </summary>
        public Task GetRequestHistoryEntryDetailAsync(HttpContextBase ctx)
        {
            return GetRequestHistoryEntryAsync(ctx);
        }

        /// <summary>
        /// DELETE /v1.0/requesthistory/{requestId} - delete one request-history entry.
        /// </summary>
        public async Task DeleteRequestHistoryEntryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                string requestId = ctx.Request.Url.Parameters["requestId"];
                if (String.IsNullOrEmpty(requestId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!Settings.RequestHistory.Enabled)
                {
                    ctx.Response.StatusCode = 204;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                RequestHistoryEntry entry = await Database.RequestHistory.ReadAsync(requestId, false).ConfigureAwait(false);
                if (entry == null || !EnforceTenantOwnership(auth, entry.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                await Database.RequestHistory.DeleteAsync(requestId).ConfigureAwait(false);
                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteRequestHistoryEntryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/requesthistory/bulk - delete request-history entries matching the current filter.
        /// </summary>
        public async Task DeleteRequestHistoryBulkAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    await SendAuthzFailure(ctx).ConfigureAwait(false);
                    return;
                }

                RequestHistorySearchFilter filter = BuildFilter(ctx, auth);
                int deletedCount = Settings.RequestHistory.Enabled
                    ? await Database.RequestHistory.DeleteByFilterAsync(filter).ConfigureAwait(false)
                    : 0;

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { DeletedCount = deletedCount })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteRequestHistoryBulkAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private RequestHistorySearchFilter BuildFilter(HttpContextBase ctx, AuthContext auth)
        {
            RequestHistorySearchFilter filter = new RequestHistorySearchFilter();

            string maxResults = GetDecodedQueryValue(ctx, "maxResults");
            if (!String.IsNullOrEmpty(maxResults) && Int32.TryParse(maxResults, out int parsedMaxResults))
                filter.MaxResults = parsedMaxResults;

            filter.ContinuationToken = GetDecodedQueryValue(ctx, "continuationToken");

            string ordering = GetDecodedQueryValue(ctx, "ordering");
            if (!String.IsNullOrEmpty(ordering)
                && Enum.TryParse<Enums.EnumerationOrderEnum>(ordering, true, out Enums.EnumerationOrderEnum parsedOrdering))
                filter.Ordering = parsedOrdering;

            filter.RequestType = GetDecodedQueryValue(ctx, "requestType");
            filter.HttpMethod = GetDecodedQueryValue(ctx, "method");
            filter.PathContains = GetDecodedQueryValue(ctx, "path");
            filter.TenantId = auth.IsGlobalAdmin ? GetDecodedQueryValue(ctx, "tenantId") : auth.TenantId;
            filter.UserId = GetDecodedQueryValue(ctx, "userId");
            filter.CredentialId = GetDecodedQueryValue(ctx, "credentialId");
            filter.AssistantId = GetDecodedQueryValue(ctx, "assistantId");
            filter.ThreadId = GetDecodedQueryValue(ctx, "threadId");
            filter.SourceType = GetDecodedQueryValue(ctx, "sourceType");
            filter.SearchText = GetDecodedQueryValue(ctx, "search");

            string statusCode = GetDecodedQueryValue(ctx, "statusCode");
            if (!String.IsNullOrEmpty(statusCode) && Int32.TryParse(statusCode, out int parsedStatusCode))
                filter.StatusCode = parsedStatusCode;

            string success = GetDecodedQueryValue(ctx, "success");
            if (!String.IsNullOrEmpty(success))
            {
                if (Boolean.TryParse(success, out bool parsedSuccess))
                    filter.Success = parsedSuccess;
                else if (success == "1") filter.Success = true;
                else if (success == "0") filter.Success = false;
            }

            string startUtc = GetDecodedQueryValue(ctx, "startUtc");
            if (!String.IsNullOrEmpty(startUtc)
                && DateTime.TryParse(startUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedStartUtc))
                filter.StartUtc = parsedStartUtc;

            string endUtc = GetDecodedQueryValue(ctx, "endUtc");
            if (!String.IsNullOrEmpty(endUtc)
                && DateTime.TryParse(endUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedEndUtc))
                filter.EndUtc = parsedEndUtc;

            string bucketSeconds = GetDecodedQueryValue(ctx, "bucketSeconds");
            if (!String.IsNullOrEmpty(bucketSeconds) && Int32.TryParse(bucketSeconds, out int parsedBucketSeconds))
            {
                filter.BucketSeconds = parsedBucketSeconds;
            }
            else
            {
                string bucketMinutes = GetDecodedQueryValue(ctx, "bucketMinutes");
                if (!String.IsNullOrEmpty(bucketMinutes) && Int32.TryParse(bucketMinutes, out int parsedBucketMinutes))
                    filter.BucketMinutes = parsedBucketMinutes;
            }

            return filter;
        }

        private static string? GetDecodedQueryValue(HttpContextBase ctx, string key)
        {
            string? value = ctx.Request.Query.Elements.Get(key);
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

        private RequestHistorySummaryResult BuildEmptySummary(RequestHistorySearchFilter filter)
        {
            RequestHistorySummaryResult ret = new RequestHistorySummaryResult();

            DateTime startUtc = filter.StartUtc ?? DateTime.UtcNow.AddHours(-24);
            DateTime endUtc = filter.EndUtc ?? DateTime.UtcNow;
            if (endUtc <= startUtc)
                endUtc = startUtc.AddSeconds(filter.BucketSeconds);

            DateTime cursor = startUtc;
            while (cursor < endUtc)
            {
                DateTime next = cursor.AddSeconds(filter.BucketSeconds);
                ret.Buckets.Add(new RequestHistorySummaryBucket
                {
                    BucketStartUtc = cursor,
                    BucketEndUtc = next
                });
                cursor = next;
            }

            return ret;
        }

        private async Task SendAuthzFailure(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
        }
    }
}
