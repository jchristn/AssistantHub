namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Chat history record representing a single turn in a conversation thread.
    /// </summary>
    public class ChatHistory
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix chist_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Thread identifier with prefix thr_.
        /// </summary>
        public string ThreadId
        {
            get => _ThreadId;
            set => _ThreadId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(ThreadId));
        }

        /// <summary>
        /// Assistant identifier to which this history belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Collection identifier from assistant settings.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// UTC timestamp of user message receipt.
        /// </summary>
        public DateTime UserMessageUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user message content.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// UTC timestamp when retrieval started.
        /// </summary>
        public DateTime? RetrievalStartUtc { get; set; } = null;

        /// <summary>
        /// Duration of retrieval in milliseconds.
        /// </summary>
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>
        /// Text retrieved from the vector database.
        /// </summary>
        public string RetrievalContext { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the prompt was sent to the model.
        /// </summary>
        public DateTime? PromptSentUtc { get; set; } = null;

        /// <summary>
        /// Estimated prompt token count sent to the model.
        /// </summary>
        public int PromptTokens { get; set; } = 0;

        /// <summary>
        /// Time to first token from the model in milliseconds.
        /// </summary>
        public double TimeToFirstTokenMs { get; set; } = 0;

        /// <summary>
        /// Time to last token from the model in milliseconds.
        /// </summary>
        public double TimeToLastTokenMs { get; set; } = 0;

        /// <summary>
        /// The assistant response content.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewChatHistoryId();
        private string _ThreadId = "thr_placeholder";
        private string _AssistantId = "asst_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatHistory()
        {
        }

        /// <summary>
        /// Create a ChatHistory from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>ChatHistory instance or null.</returns>
        public static ChatHistory FromDataRow(DataRow row)
        {
            if (row == null) return null;
            ChatHistory obj = new ChatHistory();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.ThreadId = DataTableHelper.GetStringValue(row, "thread_id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.CollectionId = DataTableHelper.GetStringValue(row, "collection_id");
            obj.UserMessageUtc = DataTableHelper.GetDateTimeValue(row, "user_message_utc");
            obj.UserMessage = DataTableHelper.GetStringValue(row, "user_message");
            obj.RetrievalStartUtc = DataTableHelper.GetNullableDateTimeValue(row, "retrieval_start_utc");
            obj.RetrievalDurationMs = DataTableHelper.GetDoubleValue(row, "retrieval_duration_ms");
            obj.RetrievalContext = DataTableHelper.GetStringValue(row, "retrieval_context");
            obj.PromptSentUtc = DataTableHelper.GetNullableDateTimeValue(row, "prompt_sent_utc");
            obj.PromptTokens = DataTableHelper.GetIntValue(row, "prompt_tokens");
            obj.TimeToFirstTokenMs = DataTableHelper.GetDoubleValue(row, "time_to_first_token_ms");
            obj.TimeToLastTokenMs = DataTableHelper.GetDoubleValue(row, "time_to_last_token_ms");
            obj.AssistantResponse = DataTableHelper.GetStringValue(row, "assistant_response");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
