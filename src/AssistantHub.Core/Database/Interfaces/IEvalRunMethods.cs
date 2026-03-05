namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Eval run database methods interface.
    /// </summary>
    public interface IEvalRunMethods
    {
        /// <summary>
        /// Create an eval run record.
        /// </summary>
        Task<EvalRun> CreateAsync(EvalRun run, CancellationToken token = default);

        /// <summary>
        /// Read an eval run record by identifier.
        /// </summary>
        Task<EvalRun> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update an eval run record.
        /// </summary>
        Task<EvalRun> UpdateAsync(EvalRun run, CancellationToken token = default);

        /// <summary>
        /// Delete an eval run record and its associated results.
        /// </summary>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate eval run records scoped to a tenant.
        /// </summary>
        Task<EnumerationResult<EvalRun>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all eval run records belonging to an assistant.
        /// </summary>
        Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default);
    }
}
