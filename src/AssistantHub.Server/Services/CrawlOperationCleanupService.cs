#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Background service that performs periodic cleanup of expired crawl operations and their associated enumeration files.
    /// </summary>
    public class CrawlOperationCleanupService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[CrawlOperationCleanup] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;

        private CancellationTokenSource _ServiceCts = null;
        private Task _CleanupLoop = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        public CrawlOperationCleanupService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the cleanup service.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task StartAsync(CancellationToken token = default)
        {
            _Logging.Info(_Header + "starting crawl operation cleanup service");

            _ServiceCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Run immediately on startup
            try
            {
                await PerformCleanupAsync(_ServiceCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "initial cleanup failed: " + ex.Message);
            }

            _CleanupLoop = Task.Run(() => CleanupLoopAsync(_ServiceCts.Token));

            _Logging.Info(_Header + "crawl operation cleanup service started");
        }

        /// <summary>
        /// Stop the cleanup service.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task StopAsync()
        {
            _Logging.Info(_Header + "stopping crawl operation cleanup service");

            if (_ServiceCts != null)
            {
                _ServiceCts.Cancel();
                _ServiceCts.Dispose();
                _ServiceCts = null;
            }

            if (_CleanupLoop != null)
            {
                try
                {
                    await _CleanupLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "cleanup loop exited with error: " + ex.Message);
                }

                _CleanupLoop = null;
            }

            _Logging.Info(_Header + "crawl operation cleanup service stopped");
        }

        #endregion

        #region Private-Methods

        private async Task CleanupLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await PerformCleanupAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "error during cleanup cycle: " + ex.Message);
                }
            }
        }

        private async Task PerformCleanupAsync(CancellationToken token)
        {
            _Logging.Info(_Header + "performing crawl operation cleanup");

            string enumerationDirectory = _Settings.Crawl.EnumerationDirectory;
            List<CrawlPlan> allPlans = await GetAllCrawlPlansAsync(token).ConfigureAwait(false);

            foreach (CrawlPlan plan in allPlans)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    // Retrieve expired operations before deleting them so we can clean up enumeration files
                    List<CrawlOperation> expiredOperations = await GetExpiredOperationsAsync(plan, token).ConfigureAwait(false);

                    // Delete enumeration files for expired operations
                    foreach (CrawlOperation op in expiredOperations)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(op.EnumerationFile))
                            {
                                CrawlerBase.DeleteEnumerationFile(op.EnumerationFile);
                                _Logging.Debug(_Header + "deleted enumeration file for operation " + op.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _Logging.Warn(_Header + "error deleting enumeration file for operation " + op.Id + ": " + ex.Message);
                        }
                    }

                    // Delete expired operations from database
                    await _Database.CrawlOperation.DeleteExpiredAsync(plan.Id, plan.RetentionDays, token).ConfigureAwait(false);

                    if (expiredOperations.Count > 0)
                        _Logging.Info(_Header + "cleaned up " + expiredOperations.Count + " expired operations for plan " + plan.Id);
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + "error cleaning up operations for plan " + plan.Id + ": " + ex.Message);
                }
            }

            _Logging.Info(_Header + "crawl operation cleanup complete");
        }

        private async Task<List<CrawlOperation>> GetExpiredOperationsAsync(CrawlPlan plan, CancellationToken token)
        {
            List<CrawlOperation> expired = new List<CrawlOperation>();
            DateTime cutoff = DateTime.UtcNow.AddDays(-plan.RetentionDays);

            string continuationToken = null;

            do
            {
                EnumerationQuery query = new EnumerationQuery();
                query.MaxResults = 1000;
                query.ContinuationToken = continuationToken;

                EnumerationResult<CrawlOperation> result =
                    await _Database.CrawlOperation.EnumerateByCrawlPlanAsync(plan.Id, query, token).ConfigureAwait(false);

                if (result != null && result.Objects != null)
                {
                    foreach (CrawlOperation op in result.Objects)
                    {
                        if (op.CreatedUtc < cutoff)
                            expired.Add(op);
                    }
                }

                continuationToken = (result != null) ? result.ContinuationToken : null;
            }
            while (!String.IsNullOrEmpty(continuationToken));

            return expired;
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
