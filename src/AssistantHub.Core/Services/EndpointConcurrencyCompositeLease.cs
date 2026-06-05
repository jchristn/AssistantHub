namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class EndpointConcurrencyCompositeLease : IDisposable
    {
        private readonly List<IDisposable> _Leases;
        private bool _Disposed = false;

        public EndpointConcurrencyCompositeLease(IEnumerable<IDisposable> leases)
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
}
