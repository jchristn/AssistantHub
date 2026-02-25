namespace AssistantHub.Core.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Service for writing and reading per-document processing log files.
    /// </summary>
    public class ProcessingLogService
    {
        #region Private-Members

        private string _Header = "[ProcessingLogService] ";
        private ProcessingLogSettings _Settings = null;
        private LoggingModule _Logging = null;
        private string _Directory = null;
        private readonly object _WriteLock = new object();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Processing log settings.</param>
        /// <param name="logging">Logging module.</param>
        public ProcessingLogService(ProcessingLogSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Directory = settings.Directory;

            if (!Directory.Exists(_Directory))
            {
                Directory.CreateDirectory(_Directory);
                _Logging.Info(_Header + "created processing log directory: " + _Directory);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Write a log entry for a document.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="level">Log level (INFO, WARN, ERROR, DEBUG).</param>
        /// <param name="message">Log message.</param>
        /// <param name="tenantId">Optional tenant identifier for directory namespacing.</param>
        /// <returns>Task.</returns>
        public async Task LogAsync(string documentId, string level, string message, string tenantId = null)
        {
            if (String.IsNullOrEmpty(documentId)) return;

            string line = "[" + DateTime.UtcNow.ToString("o") + "] [" + level + "] " + message + Environment.NewLine;
            string filePath = GetLogFilePath(documentId, tenantId);

            try
            {
                await File.AppendAllTextAsync(filePath, line).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to write processing log for " + documentId + ": " + e.Message);
            }
        }

        /// <summary>
        /// Log the start of a processing step.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="stepName">Step name.</param>
        /// <returns>A Stopwatch started at call time for measuring elapsed time.</returns>
        public async Task<Stopwatch> LogStepStartAsync(string documentId, string stepName, string tenantId = null)
        {
            await LogAsync(documentId, "INFO", stepName + " started", tenantId).ConfigureAwait(false);
            return Stopwatch.StartNew();
        }

        /// <summary>
        /// Log the completion of a processing step.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="stepName">Step name.</param>
        /// <param name="result">Result description.</param>
        /// <param name="sw">Stopwatch from LogStepStartAsync.</param>
        /// <returns>Task.</returns>
        public async Task LogStepCompleteAsync(string documentId, string stepName, string result, Stopwatch sw, string tenantId = null)
        {
            sw?.Stop();
            string elapsed = sw != null ? sw.Elapsed.TotalMilliseconds.ToString("F2") + "ms" : "unknown";
            string message = stepName + " completed in " + elapsed;
            if (!String.IsNullOrEmpty(result))
                message += " — " + result;
            await LogAsync(documentId, "INFO", message, tenantId).ConfigureAwait(false);
        }

        /// <summary>
        /// Read the processing log for a document.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <returns>Log file contents, or null if no log file exists.</returns>
        public async Task<string> GetLogAsync(string documentId, string tenantId = null)
        {
            if (String.IsNullOrEmpty(documentId)) return null;

            string filePath = GetLogFilePath(documentId, tenantId);

            // Fallback: check old flat path if tenant-namespaced path doesn't exist
            if (!File.Exists(filePath) && !String.IsNullOrEmpty(tenantId))
            {
                string flatPath = GetLogFilePath(documentId, null);
                if (File.Exists(flatPath))
                    filePath = flatPath;
            }

            if (!File.Exists(filePath))
                return null;

            try
            {
                return await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to read processing log for " + documentId + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Delete processing log files older than the configured retention period.
        /// </summary>
        /// <returns>Task.</returns>
        public Task CleanupOldLogsAsync()
        {
            try
            {
                if (!Directory.Exists(_Directory)) return Task.CompletedTask;

                DateTime cutoff = DateTime.UtcNow.AddDays(-_Settings.RetentionDays);
                string[] files = Directory.GetFiles(_Directory, "*.log", SearchOption.AllDirectories);
                int deleted = 0;

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastWriteTimeUtc < cutoff)
                    {
                        fi.Delete();
                        deleted++;
                    }
                }

                if (deleted > 0)
                    _Logging.Info(_Header + "cleaned up " + deleted + " old processing log files");
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "error during log cleanup: " + e.Message);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Private-Methods

        private string GetLogFilePath(string documentId, string tenantId = null)
        {
            if (!String.IsNullOrEmpty(tenantId))
            {
                string tenantDir = Path.Combine(_Directory, tenantId);
                if (!Directory.Exists(tenantDir))
                    Directory.CreateDirectory(tenantDir);
                return Path.Combine(tenantDir, documentId + ".log");
            }
            return Path.Combine(_Directory, documentId + ".log");
        }

        #endregion
    }
}
