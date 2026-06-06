namespace AssistantHub.Server.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Aggregate health validation for subordinate external services.
    /// </summary>
    public interface IExternalServiceHealthService
    {
        /// <summary>
        /// Validate connectivity to all required external services.
        /// </summary>
        /// <returns>True if all required services are reachable.</returns>
        Task<bool> ValidateConnectivityAsync();
    }
}
