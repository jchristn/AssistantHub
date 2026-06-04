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
                return Task.FromResult<IDisposable>(NoopLease.Instance);

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
            return new CompositeLease(leases);
        }

        private sealed class CompositeLease : IDisposable
        {
            private readonly List<IDisposable> _Leases;
            private bool _Disposed = false;

            public CompositeLease(IEnumerable<IDisposable> leases)
            {
                _Leases = leases?.Where(l => l != null).ToList() ?? new List<IDisposable>();
            }

            public void Dispose()
            {
                if (_Disposed) return;
                _Disposed = true;

                for (int i = _Leases.Count - 1; i >= 0; i--)
                    _Leases[i].Dispose();
            }
        }

        private sealed class NoopLease : IDisposable
        {
            public static readonly NoopLease Instance = new NoopLease();

            public void Dispose()
            {
            }
        }

        private sealed class AsyncEndpointLimiter
        {
            private readonly object _Lock = new object();
            private readonly Queue<Waiter> _Queue = new Queue<Waiter>();
            private int _Max;
            private int _Active = 0;

            public AsyncEndpointLimiter(int max)
            {
                _Max = Math.Max(1, max);
            }

            public void UpdateMax(int max)
            {
                lock (_Lock)
                {
                    _Max = Math.Max(1, max);
                    DrainQueue();
                }
            }

            public Task<IDisposable> AcquireAsync(CancellationToken token)
            {
                if (token.IsCancellationRequested)
                    return Task.FromCanceled<IDisposable>(token);

                lock (_Lock)
                {
                    if (_Active < _Max)
                    {
                        _Active++;
                        return Task.FromResult<IDisposable>(new Lease(this));
                    }

                    Waiter waiter = new Waiter(token);
                    _Queue.Enqueue(waiter);
                    return waiter.Task;
                }
            }

            private void Release()
            {
                lock (_Lock)
                {
                    if (_Active > 0)
                        _Active--;
                    DrainQueue();
                }
            }

            private void DrainQueue()
            {
                while (_Active < _Max && _Queue.Count > 0)
                {
                    Waiter waiter = _Queue.Dequeue();
                    if (waiter.Canceled || waiter.Task.IsCanceled)
                    {
                        waiter.DisposeRegistration();
                        continue;
                    }

                    _Active++;
                    if (!waiter.TrySetResult(new Lease(this)))
                        _Active--;
                    waiter.DisposeRegistration();
                }
            }

            private sealed class Waiter
            {
                private readonly TaskCompletionSource<IDisposable> _Tcs =
                    new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);

                private CancellationTokenRegistration? _Registration;

                public bool Canceled { get; private set; } = false;
                public Task<IDisposable> Task => _Tcs.Task;

                public Waiter(CancellationToken token)
                {
                    if (token.CanBeCanceled)
                        _Registration = token.Register(static state => ((Waiter)state).Cancel(), this);
                }

                public bool TrySetResult(IDisposable lease)
                {
                    return _Tcs.TrySetResult(lease);
                }

                public void DisposeRegistration()
                {
                    _Registration?.Dispose();
                    _Registration = null;
                }

                private void Cancel()
                {
                    Canceled = true;
                    _Tcs.TrySetCanceled();
                }
            }

            private sealed class Lease : IDisposable
            {
                private readonly AsyncEndpointLimiter _Owner;
                private bool _Disposed = false;

                public Lease(AsyncEndpointLimiter owner)
                {
                    _Owner = owner;
                }

                public void Dispose()
                {
                    if (_Disposed) return;
                    _Disposed = true;
                    _Owner.Release();
                }
            }
        }
    }
}
