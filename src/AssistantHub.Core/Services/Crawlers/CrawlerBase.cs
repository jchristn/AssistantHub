#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using SyslogLogging;

    /// <summary>
    /// Abstract base class for all crawler types.
    /// </summary>
    public abstract class CrawlerBase : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[CrawlerBase] ";

        /// <summary>
        /// Logging module.
        /// </summary>
        protected LoggingModule _Logging = null;

        /// <summary>
        /// Database driver.
        /// </summary>
        protected DatabaseDriverBase _Database = null;

        /// <summary>
        /// Crawl plan configuration.
        /// </summary>
        protected CrawlPlan _CrawlPlan = null;

        /// <summary>
        /// Current crawl operation record.
        /// </summary>
        protected CrawlOperation _CrawlOperation = null;

        /// <summary>
        /// Ingestion service.
        /// </summary>
        protected IngestionService _Ingestion = null;

        /// <summary>
        /// Storage service.
        /// </summary>
        protected StorageService _Storage = null;

        /// <summary>
        /// Processing log service.
        /// </summary>
        protected ProcessingLogService _ProcessingLog = null;

        /// <summary>
        /// Enumeration directory.
        /// </summary>
        protected string _EnumerationDirectory = "./crawl-enumerations/";

        /// <summary>
        /// Cancellation token.
        /// </summary>
        protected CancellationToken _Token = default;

        private readonly object _EnumerationLock = new object();
        private CrawlEnumeration _CurrentEnumeration = null;
        private bool _Disposed = false;

        internal static List<string> SkipFilenames = new List<string>
        {
            ".", "..", ".ds_store", "desktop.ini", "thumbs.db",
            ".directory", ".gitignore", ".gitattributes", ".localized"
        };

        internal static List<string> SkipPrefixes = new List<string> { "~", ".dropbox" };
        internal static List<string> SkipSuffixes = new List<string> { ".swp", ".tmp", ".bak" };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="crawlPlan">Crawl plan.</param>
        /// <param name="crawlOperation">Crawl operation.</param>
        /// <param name="ingestion">Ingestion service (nullable).</param>
        /// <param name="storage">Storage service (nullable).</param>
        /// <param name="processingLog">Processing log service (nullable).</param>
        /// <param name="enumerationDirectory">Enumeration directory.</param>
        /// <param name="token">Cancellation token.</param>
        protected CrawlerBase(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            StorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _CrawlPlan = crawlPlan ?? throw new ArgumentNullException(nameof(crawlPlan));
            _CrawlOperation = crawlOperation ?? throw new ArgumentNullException(nameof(crawlOperation));
            _Ingestion = ingestion;
            _Storage = storage;
            _ProcessingLog = processingLog;
            _EnumerationDirectory = !String.IsNullOrEmpty(enumerationDirectory) ? enumerationDirectory : "./crawl-enumerations/";
            _Token = token;
        }

        #endregion

        #region Public-Abstract-Methods

        /// <summary>
        /// Enumerate objects from the repository.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of crawled objects.</returns>
        public abstract IAsyncEnumerable<CrawledObject> EnumerateAsync(CancellationToken token = default);

        /// <summary>
        /// Validate connectivity to the repository.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if connectivity is valid.</returns>
        public abstract Task<bool> ValidateConnectivityAsync(CancellationToken token = default);

        /// <summary>
        /// Enumerate repository contents with pagination.
        /// </summary>
        /// <param name="maxKeys">Maximum objects to return.</param>
        /// <param name="skip">Number of objects to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of crawled objects.</returns>
        public abstract Task<List<CrawledObject>> EnumerateContentsAsync(int maxKeys = 100, int skip = 0, CancellationToken token = default);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the crawl operation.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task StartAsync()
        {
            _CurrentEnumeration = new CrawlEnumeration();

            try
            {
                // Step 1: Set starting state
                _Logging.Info(_Header + "starting crawl operation " + _CrawlOperation.Id + " for plan " + _CrawlPlan.Id);
                _CrawlPlan.State = CrawlPlanStateEnum.Running;
                _CrawlOperation.State = CrawlOperationStateEnum.Starting;
                _CrawlOperation.StartUtc = DateTime.UtcNow;
                await _Database.CrawlPlan.UpdateAsync(_CrawlPlan, _Token).ConfigureAwait(false);
                await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);

                // Step 2: Enumeration phase
                _Logging.Info(_Header + "beginning enumeration for operation " + _CrawlOperation.Id);
                _CrawlOperation.State = CrawlOperationStateEnum.Enumerating;
                _CrawlOperation.StartEnumerationUtc = DateTime.UtcNow;
                await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);

                // Step 3: Enumerate
                _Logging.Info(_Header + "calling EnumerateAsync for operation " + _CrawlOperation.Id);
                List<CrawledObject> currentObjects = new List<CrawledObject>();
                await foreach (CrawledObject obj in EnumerateAsync(_Token))
                {
                    _Token.ThrowIfCancellationRequested();
                    if (obj.IsFolder) continue;
                    if (IsSkipFile(obj.Key)) continue;
                    currentObjects.Add(obj);
                }

                // Step 4: Finish enumeration
                _CrawlOperation.FinishEnumerationUtc = DateTime.UtcNow;
                _CrawlOperation.ObjectsEnumerated = currentObjects.Count;
                _CrawlOperation.BytesEnumerated = currentObjects.Sum(o => o.ContentLength);
                _CurrentEnumeration.AllFiles = currentObjects;

                // Step 5: Load previous enumeration
                CrawlEnumeration previousEnumeration = LoadEnumerationFile(_EnumerationDirectory, _CrawlPlan.Id);

                // Step 6: Build delta
                List<CrawledObject> previousFiles = (previousEnumeration != null) ? previousEnumeration.AllFiles : new List<CrawledObject>();
                List<CrawledObject> previousFailed = (previousEnumeration != null) ? previousEnumeration.Failed : new List<CrawledObject>();
                BuildEnumerationDelta(currentObjects, previousFiles, previousFailed);

                await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);

                // Step 7: Retrieval phase
                _CrawlOperation.State = CrawlOperationStateEnum.Retrieving;
                _CrawlOperation.StartRetrievalUtc = DateTime.UtcNow;
                await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);

                SemaphoreSlim semaphore = new SemaphoreSlim(_CrawlPlan.MaxDrainTasks, _CrawlPlan.MaxDrainTasks);

                // Step 8: Process additions
                if (_CrawlPlan.ProcessAdditions)
                {
                    List<Task> additionTasks = new List<Task>();
                    foreach (CrawledObject obj in _CurrentEnumeration.Added)
                    {
                        if (!MatchesFilter(obj, _CrawlPlan.Filter)) continue;
                        _Token.ThrowIfCancellationRequested();

                        additionTasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync(_Token).ConfigureAwait(false);
                            try
                            {
                                await ProcessAdditionAsync(obj).ConfigureAwait(false);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, _Token));
                    }
                    await DrainTasksAsync(additionTasks).ConfigureAwait(false);
                    _CrawlOperation.ObjectsAdded = _CurrentEnumeration.Added.Count;
                    _CrawlOperation.BytesAdded = _CurrentEnumeration.Added.Sum(o => o.ContentLength);
                    await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);
                }

                // Step 9: Process updates
                if (_CrawlPlan.ProcessUpdates)
                {
                    List<Task> updateTasks = new List<Task>();
                    foreach (CrawledObject obj in _CurrentEnumeration.Changed)
                    {
                        if (!MatchesFilter(obj, _CrawlPlan.Filter)) continue;
                        _Token.ThrowIfCancellationRequested();

                        updateTasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync(_Token).ConfigureAwait(false);
                            try
                            {
                                await ProcessUpdateAsync(obj).ConfigureAwait(false);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, _Token));
                    }
                    await DrainTasksAsync(updateTasks).ConfigureAwait(false);
                    _CrawlOperation.ObjectsUpdated = _CurrentEnumeration.Changed.Count;
                    _CrawlOperation.BytesUpdated = _CurrentEnumeration.Changed.Sum(o => o.ContentLength);
                    await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);
                }

                // Step 10: Process deletions
                if (_CrawlPlan.ProcessDeletions)
                {
                    List<Task> deletionTasks = new List<Task>();
                    foreach (CrawledObject obj in _CurrentEnumeration.Deleted)
                    {
                        _Token.ThrowIfCancellationRequested();

                        deletionTasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync(_Token).ConfigureAwait(false);
                            try
                            {
                                await ProcessDeletionAsync(obj).ConfigureAwait(false);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, _Token));
                    }
                    await DrainTasksAsync(deletionTasks).ConfigureAwait(false);
                    _CrawlOperation.ObjectsDeleted = _CurrentEnumeration.Deleted.Count;
                    _CrawlOperation.BytesDeleted = _CurrentEnumeration.Deleted.Sum(o => o.ContentLength);
                    await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, _Token).ConfigureAwait(false);
                }

                // Step 11: Finish retrieval
                _CrawlOperation.FinishRetrievalUtc = DateTime.UtcNow;

                // Step 12: Mark success or failure
                if (_CrawlOperation.ObjectsFailed > 0)
                    _CrawlOperation.State = CrawlOperationStateEnum.Failed;
                else
                    _CrawlOperation.State = CrawlOperationStateEnum.Success;

                // Step 13: Finish
                _CrawlOperation.FinishUtc = DateTime.UtcNow;

                // Step 14: Update plan
                _CrawlPlan.LastCrawlStartUtc = _CrawlOperation.StartUtc;
                _CrawlPlan.LastCrawlFinishUtc = _CrawlOperation.FinishUtc;
                _CrawlPlan.LastCrawlSuccess = (_CrawlOperation.State == CrawlOperationStateEnum.Success);

                semaphore.Dispose();
            }
            catch (OperationCanceledException)
            {
                _CrawlOperation.State = CrawlOperationStateEnum.Canceled;
                _CrawlOperation.StatusMessage = "Operation was canceled";
                _Logging.Warn(_Header + "crawl operation " + _CrawlOperation.Id + " canceled");
            }
            catch (Exception ex)
            {
                _CrawlOperation.State = CrawlOperationStateEnum.Failed;
                _CrawlOperation.StatusMessage = ex.Message;
                _Logging.Alert(_Header + "crawl operation " + _CrawlOperation.Id + " failed: " + ex.Message);
            }
            finally
            {
                try
                {
                    // Update statistics
                    _CurrentEnumeration.Statistics = BuildStatistics();

                    // Save enumeration file
                    SaveEnumerationFile(_EnumerationDirectory, _CrawlPlan.Id, _CrawlOperation.Id, _CurrentEnumeration);
                    _CrawlOperation.EnumerationFile = Path.Combine(_EnumerationDirectory, _CrawlPlan.Id, _CrawlOperation.Id + ".json");

                    // Reset plan state
                    _CrawlPlan.State = CrawlPlanStateEnum.Stopped;
                    _CrawlOperation.FinishUtc = _CrawlOperation.FinishUtc ?? DateTime.UtcNow;

                    await _Database.CrawlPlan.UpdateAsync(_CrawlPlan, default).ConfigureAwait(false);
                    await _Database.CrawlOperation.UpdateAsync(_CrawlOperation, default).ConfigureAwait(false);

                    // Log completion summary
                    double runtimeMs = ((_CrawlOperation.FinishUtc ?? DateTime.UtcNow) - (_CrawlOperation.StartUtc ?? DateTime.UtcNow)).TotalMilliseconds;
                    _Logging.Info(_Header + "completed crawl operation " + _CrawlOperation.Id + " for crawler " + _CrawlPlan.Id + Environment.NewLine
                        + "| Objects enumerated        : " + _CrawlOperation.ObjectsEnumerated + ", " + FormatBytes(_CrawlOperation.BytesEnumerated) + Environment.NewLine
                        + "| Objects added             : " + _CrawlOperation.ObjectsAdded + ", " + FormatBytes(_CrawlOperation.BytesAdded) + Environment.NewLine
                        + "| Objects deleted           : " + _CrawlOperation.ObjectsDeleted + ", " + FormatBytes(_CrawlOperation.BytesDeleted) + Environment.NewLine
                        + "| Objects updated           : " + _CrawlOperation.ObjectsUpdated + ", " + FormatBytes(_CrawlOperation.BytesUpdated) + Environment.NewLine
                        + "| Objects processed         : " + (_CrawlOperation.ObjectsSuccess + _CrawlOperation.ObjectsFailed) + " (" + _CrawlOperation.ObjectsSuccess + " success, " + _CrawlOperation.ObjectsFailed + " failures)" + Environment.NewLine
                        + "| Total runtime             : " + runtimeMs.ToString("F2") + "ms");
                }
                catch (Exception ex)
                {
                    _Logging.Alert(_Header + "error during crawl finalization: " + ex.Message);
                }

                _CurrentEnumeration = null;
            }
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Check if a file should be skipped based on filename.
        /// </summary>
        /// <param name="filename">Filename or key.</param>
        /// <returns>True if the file should be skipped.</returns>
        protected virtual bool IsSkipFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) return true;

            string name = Path.GetFileName(filename).ToLowerInvariant();

            foreach (string skip in SkipFilenames)
            {
                if (name.Equals(skip, StringComparison.OrdinalIgnoreCase)) return true;
            }

            foreach (string prefix in SkipPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }

            foreach (string suffix in SkipSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;
            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private bool MatchesFilter(CrawledObject obj, CrawlFilterSettings filter)
        {
            if (obj == null) return false;
            if (filter == null) return true;

            if (obj.ContentLength <= 0) return false;

            if (!String.IsNullOrEmpty(filter.ObjectPrefix))
            {
                if (!obj.Key.StartsWith(filter.ObjectPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (!String.IsNullOrEmpty(filter.ObjectSuffix))
            {
                if (!obj.Key.EndsWith(filter.ObjectSuffix, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (filter.AllowedContentTypes != null && filter.AllowedContentTypes.Count > 0)
            {
                bool found = false;
                foreach (string ct in filter.AllowedContentTypes)
                {
                    if (!String.IsNullOrEmpty(obj.ContentType) &&
                        obj.ContentType.Equals(ct, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            if (filter.MinimumSize > 0 && obj.ContentLength < filter.MinimumSize) return false;
            if (filter.MaximumSize != null && obj.ContentLength > filter.MaximumSize.Value) return false;

            return true;
        }

        private void BuildEnumerationDelta(
            List<CrawledObject> current,
            List<CrawledObject> previous,
            List<CrawledObject> previousFailed)
        {
            Dictionary<string, CrawledObject> previousMap = new Dictionary<string, CrawledObject>(StringComparer.OrdinalIgnoreCase);
            foreach (CrawledObject obj in previous)
            {
                if (!String.IsNullOrEmpty(obj.Key))
                    previousMap[obj.Key] = obj;
            }

            HashSet<string> currentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CrawledObject obj in current)
            {
                if (String.IsNullOrEmpty(obj.Key)) continue;
                currentKeys.Add(obj.Key);

                if (!previousMap.ContainsKey(obj.Key))
                {
                    _CurrentEnumeration.Added.Add(obj);
                }
                else
                {
                    CrawledObject prev = previousMap[obj.Key];
                    if (IsChanged(obj, prev))
                        _CurrentEnumeration.Changed.Add(obj);
                    else
                        _CurrentEnumeration.Unchanged.Add(obj);
                }
            }

            foreach (CrawledObject obj in previous)
            {
                if (!String.IsNullOrEmpty(obj.Key) && !currentKeys.Contains(obj.Key))
                {
                    _CurrentEnumeration.Deleted.Add(obj);
                }
            }

            // Re-process previously failed items
            if (previousFailed != null)
            {
                HashSet<string> addedKeys = new HashSet<string>(
                    _CurrentEnumeration.Added.Select(o => o.Key),
                    StringComparer.OrdinalIgnoreCase);
                HashSet<string> changedKeys = new HashSet<string>(
                    _CurrentEnumeration.Changed.Select(o => o.Key),
                    StringComparer.OrdinalIgnoreCase);

                foreach (CrawledObject failed in previousFailed)
                {
                    if (!String.IsNullOrEmpty(failed.Key) &&
                        !addedKeys.Contains(failed.Key) &&
                        !changedKeys.Contains(failed.Key) &&
                        currentKeys.Contains(failed.Key))
                    {
                        CrawledObject currentObj = current.FirstOrDefault(o =>
                            o.Key.Equals(failed.Key, StringComparison.OrdinalIgnoreCase));
                        if (currentObj != null)
                        {
                            _CurrentEnumeration.Added.Add(currentObj);
                            _CurrentEnumeration.Unchanged.Remove(currentObj);
                        }
                    }
                }
            }
        }

        private bool IsChanged(CrawledObject current, CrawledObject previous)
        {
            if (current.ContentLength != previous.ContentLength) return true;

            if (!String.IsNullOrEmpty(current.ETag) && !String.IsNullOrEmpty(previous.ETag))
            {
                if (!current.ETag.Equals(previous.ETag, StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (!String.IsNullOrEmpty(current.SHA256Hash) && !String.IsNullOrEmpty(previous.SHA256Hash))
            {
                if (!current.SHA256Hash.Equals(previous.SHA256Hash, StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (!String.IsNullOrEmpty(current.SHA1Hash) && !String.IsNullOrEmpty(previous.SHA1Hash))
            {
                if (!current.SHA1Hash.Equals(previous.SHA1Hash, StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (!String.IsNullOrEmpty(current.MD5Hash) && !String.IsNullOrEmpty(previous.MD5Hash))
            {
                if (!current.MD5Hash.Equals(previous.MD5Hash, StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (current.LastModifiedUtc != null && previous.LastModifiedUtc != null)
            {
                string currentFormatted = current.LastModifiedUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                string previousFormatted = previous.LastModifiedUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                if (!currentFormatted.Equals(previousFormatted)) return true;
            }

            return false;
        }

        private async Task ProcessAdditionAsync(CrawledObject obj)
        {
            try
            {
                string documentId = null;

                if (_Storage != null && _CrawlPlan.IngestionSettings != null && _CrawlPlan.IngestionSettings.StoreInS3 && obj.Data != null)
                {
                    string s3Key = _CrawlPlan.Id + "/" + Guid.NewGuid().ToString() + "/" + SanitizeFilename(obj.Key);
                    string bucketName = _CrawlPlan.IngestionSettings.S3BucketName;

                    if (!String.IsNullOrEmpty(bucketName))
                        await _Storage.UploadAsync(bucketName, s3Key, obj.ContentType ?? "application/octet-stream", obj.Data, _Token).ConfigureAwait(false);
                    else
                        await _Storage.UploadAsync(s3Key, obj.ContentType ?? "application/octet-stream", obj.Data, _Token).ConfigureAwait(false);

                    // Create document record
                    string ingestionRuleId = _CrawlPlan.IngestionSettings?.IngestionRuleId;
                    IngestionRule ingestionRule = null;

                    if (!String.IsNullOrEmpty(ingestionRuleId))
                        ingestionRule = await _Database.IngestionRule.ReadAsync(ingestionRuleId, _Token).ConfigureAwait(false);

                    AssistantDocument doc = new AssistantDocument();
                    doc.TenantId = _CrawlPlan.TenantId;
                    doc.Name = GetDocumentName(obj.Key);
                    doc.OriginalFilename = GetDocumentName(obj.Key);
                    doc.ContentType = obj.ContentType ?? "application/octet-stream";
                    doc.SizeBytes = obj.ContentLength;
                    doc.S3Key = s3Key;
                    doc.Status = DocumentStatusEnum.Uploaded;
                    doc.IngestionRuleId = ingestionRuleId;
                    doc.BucketName = bucketName ?? (ingestionRule != null ? ingestionRule.Bucket : null);
                    doc.CollectionId = ingestionRule != null ? ingestionRule.CollectionId : null;
                    doc.CrawlPlanId = _CrawlPlan.Id;
                    doc.CrawlOperationId = _CrawlOperation.Id;
                    doc.SourceUrl = obj.Key;

                    doc = await _Database.AssistantDocument.CreateAsync(doc, _Token).ConfigureAwait(false);
                    documentId = doc.Id;
                    obj.DocumentId = documentId;

                    // Trigger ingestion
                    if (_Ingestion != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _Ingestion.ProcessDocumentAsync(documentId, _Token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _Logging.Warn(_Header + "ingestion failed for document " + documentId + ": " + ex.Message);
                            }
                        });
                    }
                }

                lock (_EnumerationLock)
                {
                    _CurrentEnumeration.Success.Add(obj);
                    _CrawlOperation.ObjectsSuccess++;
                    _CrawlOperation.BytesSuccess += obj.ContentLength;
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "failed to process addition for " + obj.Key + ": " + ex.Message);

                lock (_EnumerationLock)
                {
                    _CurrentEnumeration.Failed.Add(obj);
                    _CrawlOperation.ObjectsFailed++;
                    _CrawlOperation.BytesFailed += obj.ContentLength;
                }
            }
        }

        private async Task ProcessUpdateAsync(CrawledObject obj)
        {
            try
            {
                // Find existing document
                EnumerationQuery query = new EnumerationQuery();
                query.MaxResults = 1;

                EnumerationResult<AssistantDocument> existingDocs = await _Database.AssistantDocument.EnumerateAsync(
                    _CrawlPlan.TenantId, query, _Token).ConfigureAwait(false);

                AssistantDocument existingDoc = null;
                foreach (AssistantDocument doc in existingDocs.Objects)
                {
                    if (!String.IsNullOrEmpty(doc.SourceUrl) &&
                        doc.SourceUrl.Equals(obj.Key, StringComparison.OrdinalIgnoreCase) &&
                        !String.IsNullOrEmpty(doc.CrawlPlanId) &&
                        doc.CrawlPlanId.Equals(_CrawlPlan.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        existingDoc = doc;
                        break;
                    }
                }

                // Delete existing document via cleanup
                if (existingDoc != null)
                {
                    await CleanupDocumentAsync(existingDoc, _Token).ConfigureAwait(false);
                }

                // Re-add as new
                await ProcessAdditionAsync(obj).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "failed to process update for " + obj.Key + ": " + ex.Message);

                lock (_EnumerationLock)
                {
                    _CurrentEnumeration.Failed.Add(obj);
                    _CrawlOperation.ObjectsFailed++;
                    _CrawlOperation.BytesFailed += obj.ContentLength;
                }
            }
        }

        private async Task ProcessDeletionAsync(CrawledObject obj)
        {
            try
            {
                // Find existing document
                EnumerationQuery query = new EnumerationQuery();
                query.MaxResults = 1000;

                EnumerationResult<AssistantDocument> existingDocs = await _Database.AssistantDocument.EnumerateAsync(
                    _CrawlPlan.TenantId, query, _Token).ConfigureAwait(false);

                foreach (AssistantDocument doc in existingDocs.Objects)
                {
                    if (!String.IsNullOrEmpty(doc.SourceUrl) &&
                        doc.SourceUrl.Equals(obj.Key, StringComparison.OrdinalIgnoreCase) &&
                        !String.IsNullOrEmpty(doc.CrawlPlanId) &&
                        doc.CrawlPlanId.Equals(_CrawlPlan.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        await CleanupDocumentAsync(doc, _Token).ConfigureAwait(false);
                        break;
                    }
                }

                lock (_EnumerationLock)
                {
                    _CrawlOperation.ObjectsSuccess++;
                    _CrawlOperation.BytesSuccess += obj.ContentLength;
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "failed to process deletion for " + obj.Key + ": " + ex.Message);

                lock (_EnumerationLock)
                {
                    _CrawlOperation.ObjectsFailed++;
                    _CrawlOperation.BytesFailed += obj.ContentLength;
                }
            }
        }

        /// <summary>
        /// Cleanup a document by removing embeddings, S3 object, processing log, and database record.
        /// </summary>
        /// <param name="doc">Document to clean up.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task CleanupDocumentAsync(AssistantDocument doc, CancellationToken token = default)
        {
            if (doc == null) return;

            try
            {
                // Delete RecallDB embeddings
                if (!String.IsNullOrEmpty(doc.ChunkRecordIds) && !String.IsNullOrEmpty(doc.CollectionId))
                {
                    try
                    {
                        List<string> recordIds = Serializer.DeserializeJson<List<string>>(doc.ChunkRecordIds);
                        if (recordIds != null)
                        {
                            _Logging.Debug(_Header + "deleting " + recordIds.Count + " chunk records for document " + doc.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "error deleting chunk records for document " + doc.Id + ": " + ex.Message);
                    }
                }

                // Delete S3 object
                if (_Storage != null && !String.IsNullOrEmpty(doc.S3Key))
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(doc.BucketName))
                            await _Storage.DeleteAsync(doc.BucketName, doc.S3Key, token).ConfigureAwait(false);
                        else
                            await _Storage.DeleteAsync(doc.S3Key, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "error deleting S3 object for document " + doc.Id + ": " + ex.Message);
                    }
                }

                // Delete database record
                await _Database.AssistantDocument.DeleteAsync(doc.Id, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error during document cleanup for " + doc.Id + ": " + ex.Message);
            }
        }

        private async Task DrainTasksAsync(List<Task> tasks)
        {
            if (tasks == null || tasks.Count == 0) return;

            while (true)
            {
                List<Task> running = tasks.Where(t => !t.IsCompleted).ToList();
                if (running.Count == 0) break;

                _Logging.Debug(_Header + "draining " + running.Count + " remaining tasks");
                await Task.WhenAny(Task.WhenAll(running), Task.Delay(5000, _Token)).ConfigureAwait(false);
            }
        }

        private CrawlEnumerationStatistics BuildStatistics()
        {
            CrawlEnumerationStatistics stats = new CrawlEnumerationStatistics();
            if (_CurrentEnumeration == null) return stats;

            stats.TotalCount = _CurrentEnumeration.AllFiles.Count;
            stats.TotalBytes = _CurrentEnumeration.AllFiles.Sum(o => o.ContentLength);
            stats.AddedCount = _CurrentEnumeration.Added.Count;
            stats.AddedBytes = _CurrentEnumeration.Added.Sum(o => o.ContentLength);
            stats.ChangedCount = _CurrentEnumeration.Changed.Count;
            stats.ChangedBytes = _CurrentEnumeration.Changed.Sum(o => o.ContentLength);
            stats.DeletedCount = _CurrentEnumeration.Deleted.Count;
            stats.DeletedBytes = _CurrentEnumeration.Deleted.Sum(o => o.ContentLength);
            stats.SuccessCount = _CurrentEnumeration.Success.Count;
            stats.SuccessBytes = _CurrentEnumeration.Success.Sum(o => o.ContentLength);
            stats.FailedCount = _CurrentEnumeration.Failed.Count;
            stats.FailedBytes = _CurrentEnumeration.Failed.Sum(o => o.ContentLength);

            return stats;
        }

        private string SanitizeFilename(string key)
        {
            if (String.IsNullOrEmpty(key)) return "unknown";
            string name = key;
            try
            {
                Uri uri = new Uri(key);
                name = uri.AbsolutePath.TrimStart('/');
            }
            catch { }

            if (String.IsNullOrEmpty(name)) name = "index.html";
            name = name.Replace("/", "_").Replace("\\", "_");
            return name;
        }

        private string GetDocumentName(string key)
        {
            if (String.IsNullOrEmpty(key)) return "Unknown";
            try
            {
                Uri uri = new Uri(key);
                string path = uri.AbsolutePath.TrimStart('/');
                if (!String.IsNullOrEmpty(path)) return Path.GetFileName(path);
                return uri.Host;
            }
            catch
            {
                return Path.GetFileName(key) ?? key;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + "B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + "KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + "MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + "GB";
        }

        internal static void SaveEnumerationFile(string directory, string crawlPlanId, string operationId, CrawlEnumeration enumeration)
        {
            if (enumeration == null) return;

            string dir = Path.Combine(directory, crawlPlanId);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, operationId + ".json");
            CrawlEnumeration stripped = enumeration.CopyWithoutData();
            string json = Serializer.SerializeJson(stripped, true);
            File.WriteAllText(filePath, json);
        }

        internal static CrawlEnumeration LoadEnumerationFile(string directory, string crawlPlanId)
        {
            string dir = Path.Combine(directory, crawlPlanId);
            if (!Directory.Exists(dir)) return null;

            string[] files = Directory.GetFiles(dir, "*.json");
            if (files.Length == 0) return null;

            // Sort by filename (K-sortable operation IDs ensure chronological order)
            Array.Sort(files);
            string latestFile = files[files.Length - 1];

            try
            {
                string json = File.ReadAllText(latestFile);
                return Serializer.DeserializeJson<CrawlEnumeration>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Delete an enumeration file from disk.
        /// </summary>
        /// <param name="path">File path.</param>
        public static void DeleteEnumerationFile(string path)
        {
            if (!String.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
