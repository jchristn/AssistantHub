namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Assistant feedback record.
    /// </summary>
    public class AssistantFeedback
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix afb_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId
        {
            get => _TenantId;
            set => _TenantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(TenantId));
        }

        /// <summary>
        /// Assistant identifier to which this feedback belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// The user message that prompted the assistant response.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// The assistant response that was rated.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Feedback rating.
        /// </summary>
        public FeedbackRatingEnum Rating { get; set; } = FeedbackRatingEnum.ThumbsUp;

        /// <summary>
        /// Optional free-text feedback from the user.
        /// </summary>
        public string FeedbackText { get; set; } = null;

        /// <summary>
        /// Full message history as JSON string.
        /// </summary>
        public string MessageHistory { get; set; } = null;

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

        private string _Id = IdGenerator.NewAssistantFeedbackId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _AssistantId = "asst_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantFeedback()
        {
        }

        /// <summary>
        /// Create an AssistantFeedback from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>AssistantFeedback instance or null.</returns>
        public static AssistantFeedback FromDataRow(DataRow row)
        {
            if (row == null) return null;
            AssistantFeedback obj = new AssistantFeedback();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.UserMessage = DataTableHelper.GetStringValue(row, "user_message");
            obj.AssistantResponse = DataTableHelper.GetStringValue(row, "assistant_response");
            obj.Rating = DataTableHelper.GetEnumValue<FeedbackRatingEnum>(row, "rating", FeedbackRatingEnum.ThumbsUp);
            obj.FeedbackText = DataTableHelper.GetStringValue(row, "feedback_text");
            obj.MessageHistory = DataTableHelper.GetStringValue(row, "message_history");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
