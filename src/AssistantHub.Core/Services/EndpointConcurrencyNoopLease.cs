namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class EndpointConcurrencyNoopLease : IDisposable
    {
        public static readonly EndpointConcurrencyNoopLease Instance = new EndpointConcurrencyNoopLease();

        public void Dispose()
        {
        }
    }
}
