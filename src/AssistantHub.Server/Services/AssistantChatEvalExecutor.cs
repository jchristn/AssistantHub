namespace AssistantHub.Server.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Executes eval facts through the production assistant chat rail.
    /// </summary>
    public class AssistantChatEvalExecutor : IEvalChatExecutor
    {
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly RetrievalService _Retrieval;
        private readonly InferenceService _Inference;
        private readonly IObjectStorageService _Storage;
        private readonly IInvertedIndexService _InvertedIndex;
        private readonly IInferenceEndpointService _InferenceEndpoints;

        /// <summary>
        /// Instantiate the eval chat executor.
        /// </summary>
        public AssistantChatEvalExecutor(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            InferenceService inference,
            IObjectStorageService storage,
            IInvertedIndexService invertedIndex,
            IInferenceEndpointService inferenceEndpoints)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
            _Storage = storage;
            _InvertedIndex = invertedIndex;
            _InferenceEndpoints = inferenceEndpoints;
        }

        /// <inheritdoc />
        public async Task<EvalChatExecutionResult> ExecuteAsync(EvalChatExecutionRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.AssistantId)) throw new ArgumentNullException(nameof(request.AssistantId));

            string traceId = IdGenerator.NewTraceId();
            AssistantChatService chatService = new AssistantChatService(
                _Database,
                _Logging,
                _Settings,
                _Retrieval,
                _Inference,
                _Storage,
                _InvertedIndex,
                null,
                null,
                _InferenceEndpoints);

            AssistantChatExecutionResult result = await chatService.ExecuteNonStreamingAsync(
                new AssistantChatExecutionRequest
                {
                    AssistantId = request.AssistantId,
                    Messages = request.Messages,
                    ThreadId = IdGenerator.NewThreadId(),
                    TraceId = traceId,
                    Origin = String.IsNullOrWhiteSpace(request.Origin) ? "eval" : request.Origin
                },
                token).ConfigureAwait(false);

            if (result == null || !result.Success)
            {
                return new EvalChatExecutionResult
                {
                    Success = false,
                    ErrorMessage = result?.ErrorMessage ?? "Eval chat execution failed.",
                    TraceId = traceId
                };
            }

            return new EvalChatExecutionResult
            {
                Success = true,
                ResponseText = result.CanonicalResponseText,
                ChatHistoryId = result.ChatHistoryId,
                TraceId = traceId,
                Retrieval = result.Response?.Retrieval,
                Citations = result.Response?.Citations,
                ToolCalls = result.ToolCalls
            };
        }
    }
}
