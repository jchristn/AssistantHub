namespace AssistantHub.Core.Settings
{
    /// <summary>
    /// OpenTelemetry / observability settings. These describe where metrics and traces are exported; the
    /// emit surface itself (<see cref="AssistantHub.Core.Telemetry.AssistantHubTelemetry"/>) has no dependency
    /// on any exporter. A telemetry host in the server/MCP executable reads these values (with environment
    /// variables overriding, see below) and wires the OTLP pipeline.
    /// <para>
    /// Environment overrides: <c>ASSISTANTHUB_TELEMETRY_ENABLED</c> (true/false) overrides <see cref="Enable"/>,
    /// and <c>ASSISTANTHUB_OTLP_ENDPOINT</c> overrides <see cref="OtlpEndpoint"/>.
    /// </para>
    /// </summary>
    public class TelemetrySettings
    {
        #region Public-Members

        /// <summary>
        /// Whether telemetry export is enabled. Default false; the docker observability stack sets the
        /// ASSISTANTHUB_TELEMETRY_ENABLED environment variable to true.
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// The logical service name stamped as the service.name resource attribute. Null uses the host
        /// default (assistanthub-server or assistanthub-mcp).
        /// </summary>
        public string ServiceName { get; set; } = null;

        /// <summary>
        /// The OTLP collector endpoint. Default http://localhost:4317 (gRPC). In docker this is set via the
        /// ASSISTANTHUB_OTLP_ENDPOINT environment variable to the collector service.
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// The OTLP wire protocol: Grpc (port 4317) or HttpProtobuf (port 4318). Default Grpc.
        /// </summary>
        public string OtlpProtocol { get; set; } = "Grpc";

        /// <summary>
        /// Head-based trace sampling ratio in the range 0.0 to 1.0. Default 1.0 (sample everything).
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;

        /// <summary>
        /// Whether to include .NET runtime instrumentation (GC, heap, threads). Default true.
        /// </summary>
        public bool EnableRuntimeMetrics { get; set; } = true;

        /// <summary>
        /// Metric export interval in milliseconds for the periodic OTLP reader. Default 15000.
        /// </summary>
        public int ExportIntervalMs { get; set; } = 15000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TelemetrySettings()
        {
        }

        #endregion
    }
}
