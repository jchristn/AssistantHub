namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Eval fact database methods interface.
    /// </summary>
    public interface IEvalFactMethods
    {
        /// <summary>
        /// Create an eval fact record.
        /// </summary>
        Task<EvalFact> CreateAsync(EvalFact fact, CancellationToken token = default);

        /// <summary>
        /// Read an eval fact record by identifier.
        /// </summary>
        Task<EvalFact> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update an eval fact record.
        /// </summary>
        Task<EvalFact> UpdateAsync(EvalFact fact, CancellationToken token = default);

        /// <summary>
        /// Delete an eval fact record.
        /// </summary>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate eval fact records scoped to a tenant.
        /// </summary>
        Task<EnumerationResult<EvalFact>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all eval fact records belonging to an assistant.
        /// </summary>
        Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default);
    }
}
