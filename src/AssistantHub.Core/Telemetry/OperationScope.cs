namespace AssistantHub.Core.Telemetry
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Times a single application-layer operation and emits a span plus a duration measurement on disposal.
    /// Create via <see cref="AssistantHubTelemetry.StartOperation(string, string)"/>. The scope defaults to an
    /// "ok" outcome; call <see cref="Fail(Exception)"/> (or <see cref="SetOutcome(string)"/>) to mark failure.
    /// Safe to use even when no telemetry host is listening (the span is then inert).
    /// </summary>
    public sealed class OperationScope : IDisposable
    {
        #region Private-Members

        private readonly string _Domain;
        private readonly string _Operation;
        private readonly long _StartTicks;
        private readonly Activity _Activity;
        private string _Outcome = "ok";
        private bool _Disposed;

        #endregion

        #region Constructors-and-Factories

        internal OperationScope(string domain, string operation)
        {
            _Domain = domain;
            _Operation = operation;
            _StartTicks = Stopwatch.GetTimestamp();
            _Activity = AssistantHubTelemetry.Activity.StartActivity(
                (domain ?? "operation") + "." + (operation ?? "run"), ActivityKind.Internal);
            if (_Activity != null)
            {
                _Activity.SetTag("domain", domain);
                _Activity.SetTag("operation", operation);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Attach a tag to the underlying span (a no-op when nothing is sampling). Tags set here land on the
        /// span only, never on the low-cardinality duration metric.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>This scope, for chaining.</returns>
        public OperationScope SetTag(string key, object value)
        {
            if (_Activity != null && !String.IsNullOrEmpty(key)) _Activity.SetTag(key, value);
            return this;
        }

        /// <summary>
        /// Set the outcome label recorded for this operation (default "ok").
        /// </summary>
        /// <param name="outcome">Outcome label (low cardinality).</param>
        public void SetOutcome(string outcome)
        {
            if (!String.IsNullOrEmpty(outcome)) _Outcome = outcome;
        }

        /// <summary>
        /// Mark this operation as failed, record the exception on the span, and set the "error" outcome.
        /// </summary>
        /// <param name="exception">The failure.</param>
        public void Fail(Exception exception)
        {
            _Outcome = "error";
            if (_Activity != null)
            {
                if (exception != null)
                {
                    _Activity.SetTag("error.type", exception.GetType().FullName);
                    _Activity.SetTag("error.message", exception.Message);
                }
                _Activity.SetStatus(ActivityStatusCode.Error, exception?.Message);
            }
        }

        /// <summary>
        /// Record the duration measurement and stop the span.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            double seconds = (Stopwatch.GetTimestamp() - _StartTicks) / (double)Stopwatch.Frequency;
            AssistantHubTelemetry.RecordOperation(_Domain, _Operation, _Outcome, seconds);

            if (_Activity != null)
            {
                if (_Outcome == "ok") _Activity.SetStatus(ActivityStatusCode.Ok);
                _Activity.Dispose();
            }
        }

        #endregion
    }
}
