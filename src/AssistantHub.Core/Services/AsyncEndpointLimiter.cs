namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class AsyncEndpointLimiter
    {
        private readonly object _Lock = new object();
        private readonly Queue<EndpointConcurrencyWaiter> _Queue = new Queue<EndpointConcurrencyWaiter>();
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
                    return Task.FromResult<IDisposable>(new EndpointConcurrencyLease(this));
                }

                EndpointConcurrencyWaiter waiter = new EndpointConcurrencyWaiter(token);
                _Queue.Enqueue(waiter);
                return waiter.Task;
            }
        }

        internal void Release()
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
                EndpointConcurrencyWaiter waiter = _Queue.Dequeue();
                if (waiter.Canceled || waiter.Task.IsCanceled)
                {
                    waiter.DisposeRegistration();
                    continue;
                }

                _Active++;
                if (!waiter.TrySetResult(new EndpointConcurrencyLease(this)))
                    _Active--;
                waiter.DisposeRegistration();
            }
        }
    }
}
