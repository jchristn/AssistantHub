namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Slack assistant connection manager.
    /// </summary>
    public class SlackAssistantConnectionManager : ISlackAssistantConnectionManager
    {
        private static readonly string _Header = "[SlackAssistantConnectionManager] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly RetrievalService _Retrieval;
        private readonly InferenceService _Inference;
        private readonly IObjectStorageService _Storage;
        private readonly ConcurrentDictionary<string, SlackAssistantWorker> _Workers = new ConcurrentDictionary<string, SlackAssistantWorker>();
        private CancellationTokenSource _ReconcilerTokenSource;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SlackAssistantConnectionManager(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            InferenceService inference,
            IObjectStorageService storage = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
            _Storage = storage;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken token = default)
        {
            await ReconcileAsync(token).ConfigureAwait(false);
            _ReconcilerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = Task.Run(() => ReconcilerLoopAsync(_ReconcilerTokenSource.Token));
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken token = default)
        {
            if (_ReconcilerTokenSource != null)
            {
                _ReconcilerTokenSource.Cancel();
                _ReconcilerTokenSource.Dispose();
                _ReconcilerTokenSource = null;
            }

            foreach (KeyValuePair<string, SlackAssistantWorker> kvp in _Workers)
            {
                try
                {
                    await kvp.Value.StopAsync(token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "failed stopping Slack worker for assistant " + kvp.Key + ": " + e.Message);
                }
            }

            _Workers.Clear();
        }

        /// <inheritdoc />
        public async Task RefreshAssistantAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) return;

            Assistant assistant = await _Database.Assistant.ReadAsync(assistantId, token).ConfigureAwait(false);
            AssistantSettings settings = await _Database.AssistantSettings.ReadByAssistantIdAsync(assistantId, token).ConfigureAwait(false);
            bool shouldRun = assistant != null && assistant.Active && settings != null && settings.EnableSlack;

            if (_Workers.TryRemove(assistantId, out SlackAssistantWorker existing))
            {
                try
                {
                    await existing.StopAsync(token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "failed restarting Slack worker for assistant " + assistantId + ": " + e.Message);
                }
            }

            if (shouldRun)
            {
                SlackAssistantWorker worker = new SlackAssistantWorker(_Database, _Logging, _Settings, _Retrieval, _Inference, assistantId, _Storage);
                _Workers[assistantId] = worker;
                await worker.StartAsync(token).ConfigureAwait(false);
            }
        }

        private async Task ReconcilerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
                    await ReconcileAsync(token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "Slack reconciler error: " + e.Message);
                }
            }
        }

        private async Task ReconcileAsync(CancellationToken token)
        {
            HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
            EnumerationQuery tenantQuery = new EnumerationQuery { MaxResults = 1000 };
            EnumerationResult<TenantMetadata> tenants = await _Database.Tenant.EnumerateAsync(tenantQuery, token).ConfigureAwait(false);
            if (tenants?.Objects == null) return;

            foreach (TenantMetadata tenant in tenants.Objects)
            {
                EnumerationQuery assistantQuery = new EnumerationQuery { MaxResults = 1000 };
                EnumerationResult<Assistant> assistants = await _Database.Assistant.EnumerateAsync(tenant.Id, assistantQuery, token).ConfigureAwait(false);
                if (assistants?.Objects == null) continue;

                foreach (Assistant assistant in assistants.Objects)
                {
                    AssistantSettings settings = await _Database.AssistantSettings.ReadByAssistantIdAsync(assistant.Id, token).ConfigureAwait(false);
                    if (assistant.Active && settings != null && settings.EnableSlack)
                    {
                        expected.Add(assistant.Id);
                        if (!_Workers.ContainsKey(assistant.Id))
                        {
                            _Logging.Info(_Header + "reconciler starting missing Slack worker for assistant " + assistant.Id);
                            await RefreshAssistantAsync(assistant.Id, token).ConfigureAwait(false);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, SlackAssistantWorker> kvp in _Workers)
            {
                if (!expected.Contains(kvp.Key))
                {
                    _Logging.Info(_Header + "reconciler stopping stale Slack worker for assistant " + kvp.Key);
                    await RefreshAssistantAsync(kvp.Key, token).ConfigureAwait(false);
                }
            }
        }
    }
}
