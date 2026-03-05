namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Evaluation result record — the outcome of evaluating a single fact within a run.
    /// </summary>
    public class EvalResult
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix eres_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Run identifier this result belongs to.
        /// </summary>
        public string RunId
        {
            get => _RunId;
            set => _RunId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(RunId));
        }

        /// <summary>
        /// Fact identifier that was evaluated.
        /// </summary>
        public string FactId
        {
            get => _FactId;
            set => _FactId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(FactId));
        }

        /// <summary>
        /// The question that was sent to the assistant.
        /// </summary>
        public string Question { get; set; } = null;

        /// <summary>
        /// JSON array of expected facts.
        /// </summary>
        public string ExpectedFacts { get; set; } = null;

        /// <summary>
        /// The full LLM response text.
        /// </summary>
        public string LlmResponse { get; set; } = null;

        /// <summary>
        /// JSON array of FactVerdict objects.
        /// </summary>
        public string FactVerdicts { get; set; } = null;

        /// <summary>
        /// Whether all expected facts passed.
        /// </summary>
        public bool OverallPass { get; set; } = false;

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long DurationMs { get; set; } = 0;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewEvalResultId();
        private string _RunId = "erun_placeholder";
        private string _FactId = "ef_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalResult()
        {
        }

        /// <summary>
        /// Create an EvalResult from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>EvalResult instance or null.</returns>
        public static EvalResult FromDataRow(DataRow row)
        {
            if (row == null) return null;
            EvalResult obj = new EvalResult();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.RunId = DataTableHelper.GetStringValue(row, "run_id");
            obj.FactId = DataTableHelper.GetStringValue(row, "fact_id");
            obj.Question = DataTableHelper.GetStringValue(row, "question");
            obj.ExpectedFacts = DataTableHelper.GetStringValue(row, "expected_facts");
            obj.LlmResponse = DataTableHelper.GetStringValue(row, "llm_response");
            obj.FactVerdicts = DataTableHelper.GetStringValue(row, "fact_verdicts");
            obj.OverallPass = DataTableHelper.GetBooleanValue(row, "overall_pass", false);
            obj.DurationMs = DataTableHelper.GetLongValue(row, "duration_ms");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            return obj;
        }

        #endregion
    }
}
