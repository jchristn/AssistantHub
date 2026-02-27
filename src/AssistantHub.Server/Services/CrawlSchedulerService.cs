#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Background service that manages crawl scheduling, startup recovery, and on-demand crawl execution.
    /// </summary>
    public class CrawlSchedulerService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[CrawlScheduler] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly IngestionService _Ingestion;
        private readonly StorageService _Storage;
        private readonly ProcessingLogService _ProcessingLog;
        private readonly ConcurrentDictionary<string, (CrawlerBase Crawler, CancellationTokenSource Cts)> _RunningCrawlers
            = new ConcurrentDictionary<string, (CrawlerBase Crawler, CancellationTokenSource Cts)>();

        private CancellationTokenSource _ServiceCts = null;
        private Task _SchedulerLoop = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="processingLog">Processing log service.</param>
        public CrawlSchedulerService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            IngestionService ingestion,
            StorageService storage,
            ProcessingLogService processingLog)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Ingestion = ingestion;
            _Storage = storage;
            _ProcessingLog = processingLog;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the crawl scheduler service.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task StartAsync(CancellationToken token = default)
        {
            _Logging.Info(_Header + "starting crawl scheduler service");

            await PerformStartupRecoveryAsync(token).ConfigureAwait(false);

            _ServiceCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _SchedulerLoop = Task.Run(() => SchedulerLoopAsync(_ServiceCts.Token));

            _Logging.Info(_Header + "crawl scheduler service started");
        }

        /// <summary>
        /// Stop the crawl scheduler service.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task StopAsync()
        {
            _Logging.Info(_Header + "stopping crawl scheduler service");

            if (_ServiceCts != null)
            {
                _ServiceCts.Cancel();
                _ServiceCts.Dispose();
                _ServiceCts = null;
            }

            // Cancel all running crawlers
            foreach (string key in _RunningCrawlers.Keys.ToList())
            {
                if (_RunningCrawlers.TryRemove(key, out var entry))
                {
                    try
                    {
                        entry.Cts.Cancel();
                        entry.Cts.Dispose();
                        entry.Crawler.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "error stopping crawler for plan " + key + ": " + ex.Message);
                    }
                }
            }

            if (_SchedulerLoop != null)
            {
                try
                {
                    await _SchedulerLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "scheduler loop exited with error: " + ex.Message);
                }

                _SchedulerLoop = null;
            }

            _Logging.Info(_Header + "crawl scheduler service stopped");
        }

        /// <summary>
        /// Start a crawl for a specific crawl plan on demand.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <returns>The created crawl operation, or null on failure.</returns>
        public async Task<CrawlOperation> StartCrawlAsync(string crawlPlanId)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) throw new ArgumentNullException(nameof(crawlPlanId));

            if (_RunningCrawlers.ContainsKey(crawlPlanId))
            {
                _Logging.Warn(_Header + "crawl plan " + crawlPlanId + " is already running");
                return null;
            }

            CrawlPlan plan = await _Database.CrawlPlan.ReadAsync(crawlPlanId).ConfigureAwait(false);
            if (plan == null)
            {
                _Logging.Warn(_Header + "crawl plan " + crawlPlanId + " not found");
                return null;
            }

            return await LaunchCrawlAsync(plan).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop a running crawl for a specific crawl plan.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <returns>Task.</returns>
        public async Task StopCrawlAsync(string crawlPlanId)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) throw new ArgumentNullException(nameof(crawlPlanId));

            if (_RunningCrawlers.TryRemove(crawlPlanId, out var entry))
            {
                _Logging.Info(_Header + "stopping crawl for plan " + crawlPlanId);

                try
                {
                    entry.Cts.Cancel();
                    entry.Cts.Dispose();
                    entry.Crawler.Dispose();
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "error stopping crawler for plan " + crawlPlanId + ": " + ex.Message);
                }
            }

            // Reset plan state
            try
            {
                CrawlPlan plan = await _Database.CrawlPlan.ReadAsync(crawlPlanId).ConfigureAwait(false);
                if (plan != null && plan.State == CrawlPlanStateEnum.Running)
                {
                    plan.State = CrawlPlanStateEnum.Stopped;
                    await _Database.CrawlPlan.UpdateAsync(plan).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error resetting plan state for " + crawlPlanId + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Check if a crawl plan is currently running.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <returns>True if the crawl plan is running.</returns>
        public bool IsRunning(string crawlPlanId)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) return false;
            return _RunningCrawlers.ContainsKey(crawlPlanId);
        }

        #endregion

        #region Private-Methods

        private async Task PerformStartupRecoveryAsync(CancellationToken token)
        {
            _Logging.Info(_Header + "performing startup recovery");

            try
            {
                List<CrawlPlan> allPlans = await GetAllCrawlPlansAsync(token).ConfigureAwait(false);

                foreach (CrawlPlan plan in allPlans)
                {
                    if (plan.State == CrawlPlanStateEnum.Running)
                    {
                        _Logging.Warn(_Header + "recovering plan " + plan.Id + " from running state");

                        plan.State = CrawlPlanStateEnum.Stopped;
                        plan.LastCrawlSuccess = false;
                        await _Database.CrawlPlan.UpdateAsync(plan, token).ConfigureAwait(false);

                        // Mark the last operation as failed
                        try
                        {
                            EnumerationQuery query = new EnumerationQuery();
                            query.MaxResults = 1;
                            query.Ordering = EnumerationOrderEnum.CreatedDescending;

                            EnumerationResult<CrawlOperation> ops =
                                await _Database.CrawlOperation.EnumerateByCrawlPlanAsync(plan.Id, query, token).ConfigureAwait(false);

                            if (ops != null && ops.Objects != null)
                            {
                                foreach (CrawlOperation op in ops.Objects)
                                {
                                    if (op.State != CrawlOperationStateEnum.Success &&
                                        op.State != CrawlOperationStateEnum.Failed &&
                                        op.State != CrawlOperationStateEnum.Canceled)
                                    {
                                        op.State = CrawlOperationStateEnum.Failed;
                                        op.StatusMessage = "Recovered during startup";
                                        op.FinishUtc = op.FinishUtc ?? DateTime.UtcNow;
                                        await _Database.CrawlOperation.UpdateAsync(op, token).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _Logging.Warn(_Header + "error recovering operations for plan " + plan.Id + ": " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "startup recovery failed: " + ex.Message);
            }
        }

        private async Task SchedulerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(60000, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await CheckScheduledCrawlsAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "error checking scheduled crawls: " + ex.Message);
                }
            }
        }

        private async Task CheckScheduledCrawlsAsync(CancellationToken token)
        {
            List<CrawlPlan> allPlans = await GetAllCrawlPlansAsync(token).ConfigureAwait(false);

            foreach (CrawlPlan plan in allPlans)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    if (plan.State != CrawlPlanStateEnum.Stopped) continue;
                    if (_RunningCrawlers.ContainsKey(plan.Id)) continue;

                    // Skip OneTime plans that already ran
                    if (plan.Schedule.IntervalType == ScheduleIntervalEnum.OneTime &&
                        plan.LastCrawlFinishUtc != null)
                    {
                        continue;
                    }

                    if (!IsDue(plan)) continue;

                    _Logging.Info(_Header + "crawl plan " + plan.Id + " is due, launching crawl");
                    await LaunchCrawlAsync(plan).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "error evaluating plan " + plan.Id + ": " + ex.Message);
                }
            }
        }

        private bool IsDue(CrawlPlan plan)
        {
            if (plan.LastCrawlStartUtc == null) return true;

            TimeSpan interval = CalculateInterval(plan.Schedule.IntervalType, plan.Schedule.IntervalValue);
            if (interval == TimeSpan.Zero) return true;

            DateTime nextDue = plan.LastCrawlStartUtc.Value + interval;
            return DateTime.UtcNow >= nextDue;
        }

        private TimeSpan CalculateInterval(ScheduleIntervalEnum intervalType, int intervalValue)
        {
            switch (intervalType)
            {
                case ScheduleIntervalEnum.OneTime:
                    return TimeSpan.Zero;
                case ScheduleIntervalEnum.Minutes:
                    return TimeSpan.FromMinutes(intervalValue);
                case ScheduleIntervalEnum.Hours:
                    return TimeSpan.FromHours(intervalValue);
                case ScheduleIntervalEnum.Days:
                    return TimeSpan.FromDays(intervalValue);
                case ScheduleIntervalEnum.Weeks:
                    return TimeSpan.FromDays(intervalValue * 7);
                default:
                    return TimeSpan.FromHours(intervalValue);
            }
        }

        private async Task<CrawlOperation> LaunchCrawlAsync(CrawlPlan plan)
        {
            CrawlOperation operation = new CrawlOperation();
            operation.TenantId = plan.TenantId;
            operation.CrawlPlanId = plan.Id;
            operation.State = CrawlOperationStateEnum.NotStarted;

            operation = await _Database.CrawlOperation.CreateAsync(operation).ConfigureAwait(false);

            _Logging.Info(_Header + "created crawl operation " + operation.Id + " for plan " + plan.Id);

            CancellationTokenSource cts = new CancellationTokenSource();

            string enumerationDirectory = _Settings.Crawl.EnumerationDirectory;

            CrawlerBase crawler = CrawlerFactory.Create(
                plan.RepositoryType,
                _Logging,
                _Database,
                plan,
                operation,
                _Ingestion,
                _Storage,
                _ProcessingLog,
                enumerationDirectory,
                cts.Token);

            _RunningCrawlers[plan.Id] = (crawler, cts);

            // Set plan state to Running immediately so the dashboard reflects it
            plan.State = CrawlPlanStateEnum.Running;
            await _Database.CrawlPlan.UpdateAsync(plan).ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                try
                {
                    await crawler.StartAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _Logging.Alert(_Header + "crawl operation " + operation.Id + " for plan " + plan.Id + " failed: " + ex.Message);

                    // Ensure plan state is reset on failure
                    try
                    {
                        plan.State = CrawlPlanStateEnum.Stopped;
                        operation.State = CrawlOperationStateEnum.Failed;
                        operation.StatusMessage = ex.Message;
                        operation.FinishUtc = DateTime.UtcNow;
                        await _Database.CrawlPlan.UpdateAsync(plan).ConfigureAwait(false);
                        await _Database.CrawlOperation.UpdateAsync(operation).ConfigureAwait(false);
                    }
                    catch (Exception dbEx)
                    {
                        _Logging.Warn(_Header + "error updating state after failure for plan " + plan.Id + ": " + dbEx.Message);
                    }
                }
                finally
                {
                    _RunningCrawlers.TryRemove(plan.Id, out _);

                    try
                    {
                        crawler.Dispose();
                    }
                    catch { }

                    try
                    {
                        cts.Dispose();
                    }
                    catch { }
                }
            });

            return operation;
        }

        private async Task<List<CrawlPlan>> GetAllCrawlPlansAsync(CancellationToken token)
        {
            List<CrawlPlan> allPlans = new List<CrawlPlan>();

            try
            {
                // Enumerate all tenants
                EnumerationQuery tenantQuery = new EnumerationQuery();
                tenantQuery.MaxResults = 1000;

                EnumerationResult<TenantMetadata> tenants =
                    await _Database.Tenant.EnumerateAsync(tenantQuery, token).ConfigureAwait(false);

                if (tenants == null || tenants.Objects == null) return allPlans;

                foreach (TenantMetadata tenant in tenants.Objects)
                {
                    try
                    {
                        string continuationToken = null;

                        do
                        {
                            EnumerationQuery planQuery = new EnumerationQuery();
                            planQuery.MaxResults = 1000;
                            planQuery.ContinuationToken = continuationToken;

                            EnumerationResult<CrawlPlan> plans =
                                await _Database.CrawlPlan.EnumerateAsync(tenant.Id, planQuery, token).ConfigureAwait(false);

                            if (plans != null && plans.Objects != null)
                                allPlans.AddRange(plans.Objects);

                            continuationToken = (plans != null) ? plans.ContinuationToken : null;
                        }
                        while (!String.IsNullOrEmpty(continuationToken));
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "error enumerating crawl plans for tenant " + tenant.Id + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error enumerating tenants: " + ex.Message);
            }

            return allPlans;
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
