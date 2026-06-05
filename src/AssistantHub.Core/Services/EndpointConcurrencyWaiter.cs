namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class EndpointConcurrencyWaiter
    {
        private readonly TaskCompletionSource<IDisposable> _Tcs =
            new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);

        private CancellationTokenRegistration? _Registration;

        public bool Canceled { get; private set; } = false;
        public Task<IDisposable> Task => _Tcs.Task;

        public EndpointConcurrencyWaiter(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _Registration = token.Register(static state => ((EndpointConcurrencyWaiter)state).Cancel(), this);
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
}
