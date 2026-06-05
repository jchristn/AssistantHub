namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class EndpointConcurrencyLease : IDisposable
    {
        private readonly AsyncEndpointLimiter _Owner;
        private bool _Disposed = false;

        public EndpointConcurrencyLease(AsyncEndpointLimiter owner)
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
