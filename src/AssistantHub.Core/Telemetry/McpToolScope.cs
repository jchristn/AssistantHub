namespace AssistantHub.Core.Telemetry
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Times a single MCP tool invocation and emits a span plus duration/count measurements on disposal.
    /// Create via <see cref="AssistantHubTelemetry.StartMcpTool(string, string)"/>. Defaults to an "ok"
    /// outcome; call <see cref="Fail(Exception)"/> to mark failure. Safe to use even when no telemetry host
    /// is listening.
    /// </summary>
    public sealed class McpToolScope : IDisposable
    {
        #region Private-Members

        private readonly string _Tool;
        private readonly string _Transport;
        private readonly long _StartTicks;
        private readonly Activity _Activity;
        private string _Outcome = "ok";
        private bool _Disposed;

        #endregion

        #region Constructors-and-Factories

        internal McpToolScope(string tool, string transport)
        {
            _Tool = tool;
            _Transport = transport;
            _StartTicks = Stopwatch.GetTimestamp();
            _Activity = AssistantHubTelemetry.Activity.StartActivity("mcp " + (tool ?? "tool"), ActivityKind.Server);
            if (_Activity != null)
            {
                _Activity.SetTag("mcp.tool", tool);
                _Activity.SetTag("mcp.transport", transport);
            }
            AssistantHubTelemetry.IncrementMcpActive(transport);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Mark this invocation as failed, record the exception on the span, and set the "error" outcome.
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
        /// Record the duration and call-count measurements, decrement the in-flight gauge, and stop the span.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            double seconds = (Stopwatch.GetTimestamp() - _StartTicks) / (double)Stopwatch.Frequency;
            AssistantHubTelemetry.RecordMcpTool(_Tool, _Transport, _Outcome, seconds);
            AssistantHubTelemetry.DecrementMcpActive(_Transport);

            if (_Activity != null)
            {
                if (_Outcome == "ok") _Activity.SetStatus(ActivityStatusCode.Ok);
                _Activity.Dispose();
            }
        }

        #endregion
    }
}
