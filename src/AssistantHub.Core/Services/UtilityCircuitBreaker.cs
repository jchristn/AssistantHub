namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Process-wide circuit breaker for optional model steps (retrieval gate, query rewrite, reranking, answerability).
    /// After a number of consecutive failures on one key (a step and an endpoint), the step is skipped for a while and
    /// falls back to its default behavior, so a slow or failing model does not add its timeout to every request.
    /// </summary>
    public static class UtilityCircuitBreaker
    {
        #region Private-Members

        private static readonly ConcurrentDictionary<string, State> _States = new ConcurrentDictionary<string, State>(StringComparer.Ordinal);

        private sealed class State
        {
            public int ConsecutiveFailures;
            public DateTime OpenUntilUtc = DateTime.MinValue;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Whether the breaker for a key is open (the step should be skipped).
        /// </summary>
        /// <param name="key">Step and endpoint key.</param>
        /// <returns>True when open.</returns>
        public static bool IsOpen(string key)
        {
            if (String.IsNullOrEmpty(key) || !_States.TryGetValue(key, out State state)) return false;
            lock (state) return DateTime.UtcNow < state.OpenUntilUtc;
        }

        /// <summary>
        /// Record a successful call, closing the breaker.
        /// </summary>
        /// <param name="key">Step and endpoint key.</param>
        public static void RecordSuccess(string key)
        {
            if (String.IsNullOrEmpty(key) || !_States.TryGetValue(key, out State state)) return;
            lock (state)
            {
                state.ConsecutiveFailures = 0;
                state.OpenUntilUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Record a failed call. The breaker opens once the failures reach the threshold.
        /// </summary>
        /// <param name="key">Step and endpoint key.</param>
        /// <param name="failureThreshold">Consecutive failures that open the breaker.</param>
        /// <param name="openMs">How long the breaker stays open, in milliseconds.</param>
        /// <returns>True when this failure opened the breaker.</returns>
        public static bool RecordFailure(string key, int failureThreshold, int openMs)
        {
            if (String.IsNullOrEmpty(key)) return false;
            State state = _States.GetOrAdd(key, _ => new State());
            lock (state)
            {
                state.ConsecutiveFailures++;
                if (state.ConsecutiveFailures < Math.Max(1, failureThreshold)) return false;
                state.ConsecutiveFailures = 0;
                state.OpenUntilUtc = DateTime.UtcNow.AddMilliseconds(Math.Max(1, openMs));
                return true;
            }
        }

        /// <summary>
        /// Reset every breaker (for tests).
        /// </summary>
        public static void Reset()
        {
            _States.Clear();
        }

        #endregion
    }
}
