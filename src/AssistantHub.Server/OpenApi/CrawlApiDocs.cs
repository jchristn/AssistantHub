namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services.Crawlers;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for crawl plan and crawl operation routes.
    /// </summary>
    public static class CrawlApiDocs
    {
        #region Private-Members

        private const string _Tag = "Crawlers";

        private const string _PlanId = "cplan_01JH4D5E6F7G8H9J0K1M2N3P4Q";
        private const string _OperationId = "cop_01JH4D6F7G8H9J0K1M2N3P4Q5R";
        private const string _IngestionRuleId = "irule_01JH4D7G8H9J0K1M2N3P4Q5R6S";

        private const string _RepositorySettingsNote =
            " RepositorySettings is polymorphic and selected by RepositoryType (or by its own RepositoryType property):" +
            " Web uses AuthenticationType (None, Basic, ApiKey, BearerToken), Username, Password, ApiKeyHeader, ApiKeyValue, BearerToken," +
            " UserAgent, StartUrl (required), UseHeadlessBrowser, FollowLinks, FollowRedirects, ExtractSitemapLinks, RestrictToChildUrls," +
            " RestrictToSubdomain, RestrictToRootDomain, IgnoreRobotsTxt, MaxDepth (1-100), MaxParallelTasks (1-64), and CrawlDelayMs (0-60000);" +
            " CIFS uses CifsHostname, CifsUsername, CifsPassword, CifsShareName, and IncludeSubdirectories;" +
            " NFS uses NfsHostname, NfsUserId, NfsGroupId, NfsShareName, NfsVersion, and IncludeSubdirectories." +
            " Repository credentials are stored and returned in plain text, so treat crawl plan responses as sensitive.";

        private static WebCrawlRepositorySettings ExampleWebRepository()
        {
            return new WebCrawlRepositorySettings
            {
                RepositoryType = RepositoryTypeEnum.Web,
                AuthenticationType = WebAuthTypeEnum.None,
                UserAgent = "assistanthub-crawler",
                StartUrl = "https://docs.example.com",
                UseHeadlessBrowser = false,
                FollowLinks = true,
                FollowRedirects = true,
                ExtractSitemapLinks = true,
                RestrictToChildUrls = true,
                RestrictToSubdomain = false,
                RestrictToRootDomain = true,
                IgnoreRobotsTxt = false,
                MaxDepth = 5,
                MaxParallelTasks = 8,
                CrawlDelayMs = 100
            };
        }

        private static CrawlPlan ExamplePlanRequest()
        {
            return new CrawlPlan
            {
                Name = "Documentation Crawler",
                RepositoryType = RepositoryTypeEnum.Web,
                IngestionSettings = new CrawlIngestionSettings
                {
                    IngestionRuleId = _IngestionRuleId,
                    StoreInS3 = true,
                    S3BucketName = "default"
                },
                RepositorySettings = ExampleWebRepository(),
                Schedule = new CrawlScheduleSettings
                {
                    IntervalType = ScheduleIntervalEnum.Days,
                    IntervalValue = 1
                },
                Filter = new CrawlFilterSettings
                {
                    ObjectPrefix = "https://docs.example.com/guides/",
                    AllowedContentTypes = new List<string> { "text/html", "application/pdf" },
                    MinimumSize = 0,
                    MaximumSize = 10485760
                },
                ProcessAdditions = true,
                ProcessUpdates = true,
                ProcessDeletions = false,
                MaxDrainTasks = 8,
                RetentionDays = 7
            };
        }

        private static CrawlPlan ExamplePlan()
        {
            CrawlPlan plan = ExamplePlanRequest();
            plan.Id = _PlanId;
            plan.TenantId = ApiExamples.TenantId;
            plan.State = CrawlPlanStateEnum.Stopped;
            plan.LastCrawlStartUtc = ApiExamples.Updated;
            plan.LastCrawlFinishUtc = ApiExamples.Updated.AddMinutes(4);
            plan.LastCrawlSuccess = true;
            plan.CreatedUtc = ApiExamples.Created;
            plan.LastUpdateUtc = ApiExamples.Updated;
            return plan;
        }

        private static CrawlOperation ExampleOperation()
        {
            DateTime start = ApiExamples.Updated;

            return new CrawlOperation
            {
                Id = _OperationId,
                TenantId = ApiExamples.TenantId,
                CrawlPlanId = _PlanId,
                State = CrawlOperationStateEnum.Success,
                ObjectsEnumerated = 128,
                BytesEnumerated = 5242880,
                ObjectsAdded = 12,
                BytesAdded = 491520,
                ObjectsUpdated = 3,
                BytesUpdated = 122880,
                ObjectsDeleted = 0,
                BytesDeleted = 0,
                ObjectsSuccess = 15,
                BytesSuccess = 614400,
                ObjectsFailed = 0,
                BytesFailed = 0,
                EnumerationFile = "./crawl-enumerations/" + _PlanId + "/" + _OperationId + ".json",
                StartUtc = start,
                StartEnumerationUtc = start.AddSeconds(1),
                FinishEnumerationUtc = start.AddSeconds(95),
                StartRetrievalUtc = start.AddSeconds(96),
                FinishRetrievalUtc = start.AddSeconds(238),
                FinishUtc = start.AddSeconds(240),
                CreatedUtc = start,
                LastUpdateUtc = start.AddSeconds(240)
            };
        }

        private static CrawledObject ExampleCrawledObject(string key, long length)
        {
            return new CrawledObject
            {
                Key = key,
                ContentType = "text/html",
                ContentLength = length,
                MD5Hash = "9E107D9D372BB6826BD81D3542A419D6",
                SHA1Hash = "2FD4E1C67A2D28FCED849EE1BB76E7391B93EB12",
                SHA256Hash = "D7A8FBB307D7809469CA9ABCB0082E4F8D5651E46D3CDB762D02D0BF37C9E592",
                ETag = "\"5f3a-1c2b\"",
                LastModifiedUtc = ApiExamples.Created,
                IsFolder = false
            };
        }

        private static CrawlEnumeration ExampleEnumeration()
        {
            CrawledObject added = ExampleCrawledObject("https://docs.example.com/guides/getting-started", 40960);
            added.DocumentId = ApiExamples.DocumentId;
            CrawledObject unchanged = ExampleCrawledObject("https://docs.example.com/guides/faq", 20480);

            return new CrawlEnumeration
            {
                AllFiles = new List<CrawledObject> { added, unchanged },
                Added = new List<CrawledObject> { added },
                Changed = new List<CrawledObject>(),
                Deleted = new List<CrawledObject>(),
                Unchanged = new List<CrawledObject> { unchanged },
                Success = new List<CrawledObject> { added },
                Failed = new List<CrawledObject>(),
                Statistics = new CrawlEnumerationStatistics
                {
                    TotalCount = 2,
                    TotalBytes = 61440,
                    AddedCount = 1,
                    AddedBytes = 40960,
                    ChangedCount = 0,
                    ChangedBytes = 0,
                    DeletedCount = 0,
                    DeletedBytes = 0,
                    SuccessCount = 1,
                    SuccessBytes = 40960,
                    FailedCount = 0,
                    FailedBytes = 0
                }
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/crawlplans.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create crawl plan", _Tag)
            .Describe("Creates a crawl plan in the caller's tenant. A crawl plan defines a content repository (Web, CIFS, or NFS), a schedule, filters, and the ingestion rule used for discovered content. TenantId is always set to the caller's tenant; Id is generated when omitted. Repository settings are validated for the selected repository type." + _RepositorySettingsNote)
            .Body("Crawl plan to create.", ExamplePlanRequest())
            .Returns(201, "Crawl plan created.", ExamplePlan())
            .Errors(400, 401, 500);

        /// <summary>GET /v1.0/crawlplans.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List crawl plans", _Tag)
            .Describe("Enumerates crawl plans in the caller's tenant.")
            .Paged()
            .Returns(200, "Page of crawl plans.", ApiExamples.Page(ExamplePlan()))
            .Errors(401, 500);

        /// <summary>POST /v1.0/crawlplans/connectivity.</summary>
        public static OpenApiRouteMetadata TestDraftConnectivity => ApiDoc.Create("Test draft crawl plan connectivity", _Tag)
            .Describe("Validates the repository settings of an unsaved crawl plan and attempts to connect to the repository without persisting anything. Connection failures are reported in the 200 response body with Success set to false." + _RepositorySettingsNote)
            .Body("Draft crawl plan to test.", ExamplePlanRequest())
            .Returns(200, "Connectivity test result.", new CrawlConnectivityResult
            {
                Success = true,
                Message = "Repository connectivity verified."
            })
            .Errors(400, 401, 500);

        /// <summary>GET /v1.0/crawlplans/{id}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get crawl plan", _Tag)
            .Describe("Returns one crawl plan. The plan must belong to the caller's tenant (global administrators can read any tenant).")
            .Returns(200, "Crawl plan.", ExamplePlan())
            .Errors(400, 401, 404, 500);

        /// <summary>PUT /v1.0/crawlplans/{id}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update crawl plan", _Tag)
            .Describe("Replaces a crawl plan with the supplied values. Id, TenantId, and CreatedUtc are preserved from the stored plan and LastUpdateUtc is set by the server; every other field is taken from the request body, so send the complete plan. Repository settings are revalidated." + _RepositorySettingsNote)
            .Body("Complete updated crawl plan.", ExamplePlanRequest())
            .Returns(200, "Updated crawl plan.", ExamplePlan())
            .Errors(400, 401, 404, 500);

        /// <summary>DELETE /v1.0/crawlplans/{id}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete crawl plan", _Tag)
            .Describe("Deletes a crawl plan and all of its crawl operations. Global administrators or tenant administrators only; the plan must belong to the caller's tenant unless the caller is a global administrator. Documents already ingested from the plan are not deleted.")
            .ReturnsNoContent(204, "Crawl plan and its operations deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/crawlplans/{id}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check crawl plan existence", _Tag)
            .Describe("Returns 200 when the crawl plan exists and belongs to the caller's tenant, 404 otherwise. No response body.")
            .ReturnsNoContent(200, "Crawl plan exists.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(401, "Authentication failed.")
            .ReturnsNoContent(404, "Crawl plan not found.")
            .ReturnsNoContent(500, "Internal server error.");

        /// <summary>POST /v1.0/crawlplans/{id}/start.</summary>
        public static OpenApiRouteMetadata Start => ApiDoc.Create("Start crawl", _Tag)
            .Describe("Starts a crawl operation for the plan in the background. Global administrators or tenant administrators only. If the plan is already running the request is a no-op. The response is the plan as read before the crawl was launched, so its State may not yet reflect Running; poll the plan or its operations for progress.")
            .Returns(200, "Crawl plan.", ExamplePlan())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/crawlplans/{id}/stop.</summary>
        public static OpenApiRouteMetadata Stop => ApiDoc.Create("Stop crawl", _Tag)
            .Describe("Cancels the running crawl for the plan, if any, and resets the plan state to Stopped. Global administrators or tenant administrators only. The response is the plan as read before the crawl was stopped.")
            .Returns(200, "Crawl plan.", ExamplePlan())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/crawlplans/{id}/connectivity.</summary>
        public static OpenApiRouteMetadata TestConnectivity => ApiDoc.Create("Test crawl plan connectivity", _Tag)
            .Describe("Attempts to connect to the repository configured on a saved crawl plan. No request body. Connection failures are reported in the 200 response body with Success set to false.")
            .Returns(200, "Connectivity test result.", new CrawlConnectivityResult
            {
                Success = true,
                Message = "Repository connectivity verified."
            })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{id}/enumerate.</summary>
        public static OpenApiRouteMetadata Enumerate => ApiDoc.Create("Enumerate repository contents", _Tag)
            .Describe("Connects to the plan's repository and returns a preview of up to 100 objects a crawl would discover, without ingesting anything or creating a crawl operation. Object Data is not included.")
            .Returns(200, "Objects discovered in the repository.", new List<CrawledObject>
            {
                ExampleCrawledObject("https://docs.example.com/guides/getting-started", 40960),
                ExampleCrawledObject("https://docs.example.com/guides/faq", 20480)
            })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{planId}/operations.</summary>
        public static OpenApiRouteMetadata ListOperations => ApiDoc.Create("List crawl operations", _Tag)
            .Describe("Enumerates crawl operations (individual crawl executions) for a crawl plan. The plan must belong to the caller's tenant.")
            .Paged()
            .Returns(200, "Page of crawl operations.", ApiExamples.Page(ExampleOperation()))
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{planId}/operations/statistics.</summary>
        public static OpenApiRouteMetadata PlanStatistics => ApiDoc.Create("Get crawl plan statistics", _Tag)
            .Describe("Aggregates statistics across up to 1000 of the plan's crawl operations: most recent start time, the next scheduled run (last run plus the schedule interval), success and failure counts, runtime minimum, maximum, and average, and totals of objects and bytes enumerated. LastRun and NextRun are null when the plan has never run.")
            .Returns(200, "Aggregate crawl statistics.", new
            {
                LastRun = (DateTime?)ApiExamples.Updated,
                NextRun = (DateTime?)ApiExamples.Updated.AddDays(1),
                FailedRunCount = 1,
                SuccessfulRunCount = 14,
                MinRuntimeMs = 182340.5,
                MaxRuntimeMs = 312880.0,
                AvgRuntimeMs = 240115.3,
                ObjectCount = 1920L,
                BytesCrawled = 78643200L
            })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{planId}/operations/{id}.</summary>
        public static OpenApiRouteMetadata ReadOperation => ApiDoc.Create("Get crawl operation", _Tag)
            .Describe("Returns one crawl operation, including its state, per-phase timestamps, and object and byte counters. The plan must belong to the caller's tenant and the operation must belong to the plan.")
            .Returns(200, "Crawl operation.", ExampleOperation())
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{planId}/operations/{id}/statistics.</summary>
        public static OpenApiRouteMetadata OperationStatistics => ApiDoc.Create("Get crawl operation statistics", _Tag)
            .Describe("Returns summary statistics for a single crawl operation. FailedRunCount and SuccessfulRunCount are 0 or 1 based on the operation state; RuntimeMs is 0 until the operation finishes.")
            .Returns(200, "Crawl operation statistics.", new
            {
                LastRun = (DateTime?)ApiExamples.Updated,
                FailedRunCount = 0,
                SuccessfulRunCount = 1,
                RuntimeMs = 240000.0,
                ObjectCount = 128L,
                BytesCrawled = 5242880L
            })
            .Errors(400, 401, 404, 500);

        /// <summary>DELETE /v1.0/crawlplans/{planId}/operations/{id}.</summary>
        public static OpenApiRouteMetadata DeleteOperation => ApiDoc.Create("Delete crawl operation", _Tag)
            .Describe("Deletes one crawl operation record. Global administrators or tenant administrators only; the plan must belong to the caller's tenant and the operation must belong to the plan.")
            .ReturnsNoContent(204, "Crawl operation deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/crawlplans/{planId}/operations/{id}/enumeration.</summary>
        public static OpenApiRouteMetadata ReadEnumeration => ApiDoc.Create("Get crawl operation enumeration", _Tag)
            .Describe("Returns the enumeration file saved by a crawl operation: every object discovered, the objects classified as added, changed, deleted, or unchanged since the previous crawl, which objects were processed successfully or failed, and aggregate statistics. Object data is not included. Returns 404 when the operation has no enumeration file on disk.")
            .Returns(200, "Crawl enumeration.", ExampleEnumeration())
            .Errors(400, 401, 404, 500);

        #endregion
    }
}
