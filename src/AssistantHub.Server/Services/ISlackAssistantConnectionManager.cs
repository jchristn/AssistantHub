namespace AssistantHub.Server.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Slack assistant connection manager.
    /// </summary>
    public interface ISlackAssistantConnectionManager
    {
        /// <summary>
        /// Start manager state.
        /// </summary>
        Task StartAsync(CancellationToken token = default);

        /// <summary>
        /// Stop manager state.
        /// </summary>
        Task StopAsync(CancellationToken token = default);

        /// <summary>
        /// Refresh a single assistant worker.
        /// </summary>
        Task RefreshAssistantAsync(string assistantId, CancellationToken token = default);
    }
}
