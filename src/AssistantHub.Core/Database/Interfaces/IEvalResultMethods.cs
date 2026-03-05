namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Eval result database methods interface.
    /// </summary>
    public interface IEvalResultMethods
    {
        /// <summary>
        /// Create an eval result record.
        /// </summary>
        Task<EvalResult> CreateAsync(EvalResult result, CancellationToken token = default);

        /// <summary>
        /// Read an eval result record by identifier.
        /// </summary>
        Task<EvalResult> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Get all results for a given run.
        /// </summary>
        Task<List<EvalResult>> GetByRunIdAsync(string runId, CancellationToken token = default);

        /// <summary>
        /// Delete an eval result record.
        /// </summary>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete all eval result records belonging to a run.
        /// </summary>
        Task DeleteByRunIdAsync(string runId, CancellationToken token = default);
    }
}
