namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Evaluation fact record — a question with expected facts for RAG evaluation.
    /// </summary>
    public class EvalFact
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix ef_.
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
        /// Assistant identifier to which this fact belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Category for organizing facts.
        /// </summary>
        public string Category { get; set; } = null;

        /// <summary>
        /// The question to send to the assistant.
        /// </summary>
        public string Question { get; set; } = null;

        /// <summary>
        /// JSON array of expected facts that the response should contain.
        /// </summary>
        public string ExpectedFacts { get; set; } = null;

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

        private string _Id = IdGenerator.NewEvalFactId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _AssistantId = "asst_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalFact()
        {
        }

        /// <summary>
        /// Create an EvalFact from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>EvalFact instance or null.</returns>
        public static EvalFact FromDataRow(DataRow row)
        {
            if (row == null) return null;
            EvalFact obj = new EvalFact();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.Category = DataTableHelper.GetStringValue(row, "category");
            obj.Question = DataTableHelper.GetStringValue(row, "question");
            obj.ExpectedFacts = DataTableHelper.GetStringValue(row, "expected_facts");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
