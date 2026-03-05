namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Evaluation run record — tracks progress and results of an evaluation execution.
    /// </summary>
    public class EvalRun
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix erun_.
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
        /// Assistant identifier to which this run belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Run status.
        /// </summary>
        public EvalStatusEnum Status { get; set; } = EvalStatusEnum.Pending;

        /// <summary>
        /// Total number of facts to evaluate.
        /// </summary>
        public int TotalFacts { get; set; } = 0;

        /// <summary>
        /// Number of facts evaluated so far.
        /// </summary>
        public int FactsEvaluated { get; set; } = 0;

        /// <summary>
        /// Number of facts that passed.
        /// </summary>
        public int FactsPassed { get; set; } = 0;

        /// <summary>
        /// Number of facts that failed.
        /// </summary>
        public int FactsFailed { get; set; } = 0;

        /// <summary>
        /// Pass rate as a percentage (0-100).
        /// </summary>
        public double PassRate { get; set; } = 0;

        /// <summary>
        /// Judge prompt override used for this run (null means default was used).
        /// </summary>
        public string JudgePrompt { get; set; } = null;

        /// <summary>
        /// Timestamp when the run started.
        /// </summary>
        public DateTime? StartedUtc { get; set; } = null;

        /// <summary>
        /// Timestamp when the run completed.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewEvalRunId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _AssistantId = "asst_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalRun()
        {
        }

        /// <summary>
        /// Create an EvalRun from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>EvalRun instance or null.</returns>
        public static EvalRun FromDataRow(DataRow row)
        {
            if (row == null) return null;
            EvalRun obj = new EvalRun();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.Status = DataTableHelper.GetEnumValue<EvalStatusEnum>(row, "status", EvalStatusEnum.Pending);
            obj.TotalFacts = DataTableHelper.GetIntValue(row, "total_facts", 0);
            obj.FactsEvaluated = DataTableHelper.GetIntValue(row, "facts_evaluated", 0);
            obj.FactsPassed = DataTableHelper.GetIntValue(row, "facts_passed", 0);
            obj.FactsFailed = DataTableHelper.GetIntValue(row, "facts_failed", 0);
            obj.PassRate = DataTableHelper.GetDoubleValue(row, "pass_rate", 0);
            obj.JudgePrompt = DataTableHelper.GetStringValue(row, "judge_prompt");
            obj.StartedUtc = DataTableHelper.GetNullableDateTimeValue(row, "started_utc");
            obj.CompletedUtc = DataTableHelper.GetNullableDateTimeValue(row, "completed_utc");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            return obj;
        }

        #endregion
    }
}
