namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shared in-process concurrency limiter for Partio-managed upstream endpoints.
    /// </summary>
    public static class EndpointConcurrencyLimiter
    {
        private static readonly ConcurrentDictionary<string, AsyncEndpointLimiter> _Limiters =
            new ConcurrentDictionary<string, AsyncEndpointLimiter>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Build the canonical limiter key for an endpoint.
        /// </summary>
        /// <param name="endpointType">Endpoint type, for example completion or embedding.</param>
        /// <param name="endpointId">Endpoint identifier.</param>
        /// <returns>Canonical limiter key.</returns>
        public static string BuildKey(string endpointType, string endpointId)
        {
            if (String.IsNullOrWhiteSpace(endpointType) || String.IsNullOrWhiteSpace(endpointId))
                return String.Empty;

            return endpointType.Trim() + ":" + endpointId.Trim();
        }

        /// <summary>
        /// Acquire a concurrency slot for an endpoint.
        /// </summary>
        /// <param name="endpointType">Endpoint type, for example completion or embedding.</param>
        /// <param name="endpointId">Endpoint identifier.</param>
        /// <param name="maxConcurrentRequests">Maximum concurrent requests for the endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Disposable lease that releases the slot.</returns>
        public static Task<IDisposable> AcquireAsync(
            string endpointType,
            string endpointId,
            int maxConcurrentRequests,
            CancellationToken token = default)
        {
            return AcquireAsync(BuildKey(endpointType, endpointId), maxConcurrentRequests, token);
        }

        /// <summary>
        /// Acquire a concurrency slot for a canonical endpoint key.
        /// </summary>
        /// <param name="key">Canonical endpoint key.</param>
        /// <param name="maxConcurrentRequests">Maximum concurrent requests for the endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Disposable lease that releases the slot.</returns>
        public static Task<IDisposable> AcquireAsync(
            string key,
            int maxConcurrentRequests,
            CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(key))
                return Task.FromResult<IDisposable>(EndpointConcurrencyNoopLease.Instance);

            int max = Math.Max(1, maxConcurrentRequests);
            AsyncEndpointLimiter limiter = _Limiters.GetOrAdd(key, _ => new AsyncEndpointLimiter(max));
            limiter.UpdateMax(max);
            return limiter.AcquireAsync(token);
        }

        /// <summary>
        /// Create a lease that disposes a set of endpoint leases in reverse acquisition order.
        /// </summary>
        /// <param name="leases">Endpoint leases.</param>
        /// <returns>Composite lease.</returns>
        public static IDisposable CreateCompositeLease(IEnumerable<IDisposable> leases)
        {
            return new EndpointConcurrencyCompositeLease(leases);
        }
    }
}
