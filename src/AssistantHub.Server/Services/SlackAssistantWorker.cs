namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using EasySlack;
    using SyslogLogging;

    /// <summary>
    /// Assistant-scoped Slack worker.
    /// </summary>
    public class SlackAssistantWorker
    {
        private static readonly string _Header = "[SlackAssistantWorker] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly RetrievalService _Retrieval;
        private readonly InferenceService _Inference;
        private readonly IObjectStorageService _Storage;
        private readonly string _AssistantId;
        private readonly ConcurrentDictionary<string, string> _ThreadAliases = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private ISlackConnector _Connector;
        private string _BotUserId;
        private Assistant _Assistant;
        private AssistantSettings _AssistantSettings;

        /// <summary>
        /// Instantiate the worker.
        /// </summary>
        public SlackAssistantWorker(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            InferenceService inference,
            string assistantId,
            IObjectStorageService storage = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
            _AssistantId = assistantId ?? throw new ArgumentNullException(nameof(assistantId));
            _Storage = storage;
        }

        /// <summary>
        /// Start the worker.
        /// </summary>
        public async Task StartAsync(CancellationToken token = default)
        {
            _Assistant = await _Database.Assistant.ReadAsync(_AssistantId, token).ConfigureAwait(false);
            _AssistantSettings = await _Database.AssistantSettings.ReadByAssistantIdAsync(_AssistantId, token).ConfigureAwait(false);

            if (_Assistant == null || _AssistantSettings == null || !_Assistant.Active || !_AssistantSettings.EnableSlack)
                return;

            SlackAuthMaterial auth = new SlackAuthMaterial(_AssistantSettings.SlackBotToken, _AssistantSettings.SlackAppToken);
            SlackConnectorOptions options = new SlackConnectorOptions(auth)
            {
                AutoReconnect = true
            };

            _Connector = new SlackConnector(options);
            _Connector.MessageReceived += OnMessageReceived;
            _Connector.Disconnected += OnDisconnected;
            _Connector.ActionRequired += OnActionRequired;

            SlackValidationResult validation = await _Connector.ValidateConnectionAsync(token).ConfigureAwait(false);
            _BotUserId = validation?.UserId;

            _Logging.Info(_Header + "starting Slack worker for assistant " + _AssistantId);
            await _Connector.StartAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop the worker.
        /// </summary>
        public async Task StopAsync(CancellationToken token = default)
        {
            if (_Connector == null) return;

            _Logging.Info(_Header + "stopping Slack worker for assistant " + _AssistantId);

            _Connector.MessageReceived -= OnMessageReceived;
            _Connector.Disconnected -= OnDisconnected;
            _Connector.ActionRequired -= OnActionRequired;
            await _Connector.StopAsync(token).ConfigureAwait(false);
            _Connector = null;
        }

        private async Task OnMessageReceived(object sender, SlackMessageReceivedEventArgs e)
        {
            try
            {
                if (_AssistantSettings == null || !_AssistantSettings.EnableSlack) return;
                if (e == null) return;
                if (!String.IsNullOrEmpty(e.Subtype)) return;
                if (String.IsNullOrEmpty(e.Text) || String.IsNullOrEmpty(e.ChannelId)) return;
                if (!String.IsNullOrEmpty(_BotUserId) && String.Equals(e.UserId, _BotUserId, StringComparison.Ordinal))
                    return;

                bool isDirectMessage = e.ChannelId.StartsWith("D", StringComparison.OrdinalIgnoreCase);
                if (!isDirectMessage && !String.Equals(e.ChannelId, _AssistantSettings.SlackChannelId, StringComparison.Ordinal))
                    return;

                string conversationThreadId = ResolveConversationThreadId(e, isDirectMessage);
                if (String.IsNullOrWhiteSpace(conversationThreadId))
                    return;

                bool allowImplicitThreadReply = !isDirectMessage
                    && !String.IsNullOrWhiteSpace(e.ThreadTimestamp)
                    && await ThreadHasHistoryAsync(conversationThreadId).ConfigureAwait(false);

                string normalizedText = e.Text.Trim();
                bool mentionsBot = !String.IsNullOrEmpty(_BotUserId) && normalizedText.Contains("<@" + _BotUserId + ">", StringComparison.Ordinal);
                bool matchesPrefix = !String.IsNullOrWhiteSpace(_AssistantSettings.SlackMessagePrefix)
                    && normalizedText.StartsWith(_AssistantSettings.SlackMessagePrefix, StringComparison.OrdinalIgnoreCase);

                if (!isDirectMessage && !allowImplicitThreadReply && !matchesPrefix && !mentionsBot)
                    return;

                string cleanedUserMessage = SlackAssistantUtilities.StripSlackTrigger(normalizedText, _AssistantSettings.SlackMessagePrefix, _BotUserId);
                if (String.IsNullOrWhiteSpace(cleanedUserMessage))
                    return;

                List<ChatCompletionMessage> messages = await BuildConversationMessagesAsync(conversationThreadId, cleanedUserMessage).ConfigureAwait(false);
                bool emitSlackToolProgress = ShouldEmitSlackToolProgress(_AssistantSettings);
                string responseThreadTimestamp = e.ThreadTimestamp;
                if (emitSlackToolProgress && !isDirectMessage && String.IsNullOrWhiteSpace(responseThreadTimestamp))
                    responseThreadTimestamp = e.Timestamp;

                AssistantChatService chatService = new AssistantChatService(_Database, _Logging, _Settings, _Retrieval, _Inference, _Storage);
                AssistantChatExecutionResult result = await chatService.ExecuteNonStreamingAsync(
                    new AssistantChatExecutionRequest
                    {
                        AssistantId = _AssistantId,
                        Assistant = _Assistant,
                        AssistantSettings = _AssistantSettings,
                        Messages = messages,
                        ThreadId = conversationThreadId,
                        Origin = "slack",
                        UserMessageUtc = DateTime.UtcNow,
                        ToolProgress = emitSlackToolProgress
                            ? evt => SendSlackToolProgressAsync(e.ChannelId, responseThreadTimestamp, evt)
                            : null
                    }).ConfigureAwait(false);

                if (!result.Success || String.IsNullOrWhiteSpace(result.CanonicalResponseText))
                    return;

                foreach (string chunk in SlackAssistantUtilities.ChunkSlackMessage(SlackAssistantUtilities.ShapeSlackText(result.CanonicalResponseText)))
                {
                    SlackSendMessageResult sendResult = await _Connector.SendMessageToChannelAsync(e.ChannelId, chunk, responseThreadTimestamp, CancellationToken.None).ConfigureAwait(false);
                    if (sendResult != null
                        && !isDirectMessage
                        && String.IsNullOrWhiteSpace(responseThreadTimestamp)
                        && !String.IsNullOrWhiteSpace(sendResult.Timestamp))
                    {
                        RememberThreadAlias(e.ChannelId, sendResult.Timestamp, conversationThreadId);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "Slack message handling failed for assistant " + _AssistantId + ": " + ex.Message);
            }
        }

        private Task OnDisconnected(object sender, SlackDisconnectedEventArgs e)
        {
            _Logging.Warn(_Header + "Slack worker disconnected for assistant " + _AssistantId + ": " + e?.Reason);
            return Task.CompletedTask;
        }

        private Task OnActionRequired(object sender, SlackActionRequiredEventArgs e)
        {
            _Logging.Warn(_Header + "Slack worker action required for assistant " + _AssistantId + ": " + e?.Code + " " + e?.Description);
            return Task.CompletedTask;
        }

        private async Task<List<ChatCompletionMessage>> BuildConversationMessagesAsync(string threadId, string currentUserMessage)
        {
            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>();
            EnumerationQuery query = new EnumerationQuery
            {
                ThreadIdFilter = threadId,
                AssistantIdFilter = _AssistantId,
                Ordering = EnumerationOrderEnum.CreatedAscending,
                MaxResults = 1000
            };

            EnumerationResult<ChatHistory> history = await _Database.ChatHistory.EnumerateAsync(_Assistant.TenantId, query).ConfigureAwait(false);
            if (history?.Objects != null)
            {
                foreach (ChatHistory item in history.Objects.OrderBy(x => x.CreatedUtc))
                {
                    if (!String.IsNullOrWhiteSpace(item.UserMessage))
                        messages.Add(new ChatCompletionMessage { Role = "user", Content = item.UserMessage });
                    if (!String.IsNullOrWhiteSpace(item.AssistantResponse))
                        messages.Add(new ChatCompletionMessage { Role = "assistant", Content = item.AssistantResponse });
                }
            }

            messages.Add(new ChatCompletionMessage { Role = "user", Content = currentUserMessage });
            return messages;
        }

        private async Task SendSlackToolProgressAsync(string channelId, string threadTimestamp, AssistantToolProgressEvent evt)
        {
            if (_Connector == null) return;
            if (String.IsNullOrWhiteSpace(channelId)) return;

            string message = SlackAssistantUtilities.ShapeSlackToolProgressMessage(evt);
            if (String.IsNullOrWhiteSpace(message)) return;

            foreach (string chunk in SlackAssistantUtilities.ChunkSlackMessage(message))
            {
                await _Connector.SendMessageToChannelAsync(channelId, chunk, threadTimestamp, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private async Task<bool> ThreadHasHistoryAsync(string threadId)
        {
            if (String.IsNullOrWhiteSpace(threadId)) return false;

            EnumerationQuery query = new EnumerationQuery
            {
                ThreadIdFilter = threadId,
                AssistantIdFilter = _AssistantId,
                Ordering = EnumerationOrderEnum.CreatedAscending,
                MaxResults = 1
            };

            EnumerationResult<ChatHistory> history = await _Database.ChatHistory.EnumerateAsync(_Assistant.TenantId, query).ConfigureAwait(false);
            return history?.Objects != null && history.Objects.Count > 0;
        }

        private string ResolveConversationThreadId(SlackMessageReceivedEventArgs e, bool isDirectMessage)
        {
            if (e == null) return null;
            if (String.IsNullOrWhiteSpace(e.ChannelId)) return null;

            if (isDirectMessage)
                return SlackAssistantUtilities.BuildThreadId(_AssistantId, e.ChannelId, "dm");

            string rootTimestamp = e.ThreadTimestamp ?? e.Timestamp;
            if (String.IsNullOrWhiteSpace(rootTimestamp)) return null;

            if (_ThreadAliases.TryGetValue(BuildAliasKey(e.ChannelId, rootTimestamp), out string aliasedThreadId))
                return aliasedThreadId;

            return SlackAssistantUtilities.BuildThreadId(_AssistantId, e.ChannelId, rootTimestamp);
        }

        private void RememberThreadAlias(string channelId, string rootTimestamp, string conversationThreadId)
        {
            if (String.IsNullOrWhiteSpace(channelId)) return;
            if (String.IsNullOrWhiteSpace(rootTimestamp)) return;
            if (String.IsNullOrWhiteSpace(conversationThreadId)) return;

            _ThreadAliases[BuildAliasKey(channelId, rootTimestamp)] = conversationThreadId;
        }

        private static string BuildAliasKey(string channelId, string rootTimestamp)
        {
            return channelId.Trim() + ":" + rootTimestamp.Trim();
        }

        private static bool ShouldEmitSlackToolProgress(AssistantSettings settings)
        {
            AssistantToolPolicy policy = settings?.ToolPolicy;
            if (policy == null) return false;

            policy.Normalize();
            return policy.EnableToolCalls && policy.EnableSlackToolProgressMessages;
        }

    }
}
