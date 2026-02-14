namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Assistant settings database methods interface.
    /// </summary>
    public interface IAssistantSettingsMethods
    {
        /// <summary>
        /// Create an assistant settings record.
        /// </summary>
        /// <param name="settings">Assistant settings record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created assistant settings record.</returns>
        Task<AssistantSettings> CreateAsync(AssistantSettings settings, CancellationToken token = default);

        /// <summary>
        /// Read an assistant settings record by identifier.
        /// </summary>
        /// <param name="id">Assistant settings identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant settings record.</returns>
        Task<AssistantSettings> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read an assistant settings record by assistant identifier.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant settings record.</returns>
        Task<AssistantSettings> ReadByAssistantIdAsync(string assistantId, CancellationToken token = default);

        /// <summary>
        /// Update an assistant settings record.
        /// </summary>
        /// <param name="settings">Assistant settings record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated assistant settings record.</returns>
        Task<AssistantSettings> UpdateAsync(AssistantSettings settings, CancellationToken token = default);

        /// <summary>
        /// Delete an assistant settings record.
        /// </summary>
        /// <param name="id">Assistant settings identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete an assistant settings record by assistant identifier.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default);
    }
}
