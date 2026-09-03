namespace AssistantHub.Server
{
    using System;
    using AssistantHub.Core.Settings;
    using AssistantHub.Core.Telemetry;
    using Radiant;
    using SyslogLogging;

    /// <summary>
    /// Builds and starts the Radiant telemetry host that subscribes to the AssistantHub meter and activity
    /// source and exports metrics and traces over OTLP. Emit-side code never references this type; it only
    /// wires the exporter pipeline at the composition root.
    /// </summary>
    internal static class TelemetryBootstrap
    {
        /// <summary>
        /// Start the telemetry host, or return null when telemetry is disabled or fails to start. Never throws.
        /// </summary>
        /// <param name="settings">Telemetry settings from configuration (environment variables override).</param>
        /// <param name="defaultServiceName">The service.name to use when none is configured.</param>
        /// <param name="logging">Logging module for diagnostics.</param>
        /// <returns>A started <see cref="RadiantHost"/>, or null.</returns>
        public static RadiantHost Start(TelemetrySettings settings, string defaultServiceName, LoggingModule logging)
        {
            try
            {
                string envEnabled = Environment.GetEnvironmentVariable("ASSISTANTHUB_TELEMETRY_ENABLED");
                string envEndpoint = Environment.GetEnvironmentVariable("ASSISTANTHUB_OTLP_ENDPOINT");

                bool enabled = settings != null && settings.Enable;
                if (!String.IsNullOrEmpty(envEnabled))
                    enabled = envEnabled.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (!enabled)
                {
                    logging?.Info("[Telemetry] telemetry disabled (set ASSISTANTHUB_TELEMETRY_ENABLED=true to enable)");
                    return null;
                }

                string endpoint = !String.IsNullOrEmpty(envEndpoint)
                    ? envEndpoint
                    : (settings?.OtlpEndpoint ?? "http://localhost:4317");
                string serviceName = String.IsNullOrEmpty(settings?.ServiceName) ? defaultServiceName : settings.ServiceName;

                RadiantSettings radiant = new RadiantSettings(serviceName);
                radiant.Otlp.Endpoint = endpoint;
                radiant.Otlp.Protocol = ("HttpProtobuf".Equals(settings?.OtlpProtocol, StringComparison.OrdinalIgnoreCase))
                    ? OtlpProtocolEnum.HttpProtobuf
                    : OtlpProtocolEnum.Grpc;
                radiant.Metrics.IncludeRuntime = settings?.EnableRuntimeMetrics ?? true;
                radiant.Metrics.ExportIntervalMs = settings?.ExportIntervalMs ?? 15000;
                radiant.Traces.SamplingRatio = settings?.SamplingRatio ?? 1.0;

                // Application logs are shipped to Loki by the OpenTelemetry Collector's filelog receiver, so
                // the in-process logs pillar is left off.
                radiant.Logs.Enable = false;

                radiant.Sources.AddMeter(AssistantHubTelemetry.SourceName);
                radiant.Sources.AddActivitySource(AssistantHubTelemetry.SourceName);
                radiant.DiagnosticCallback = message => logging?.Debug("[Telemetry] " + message);

                RadiantHost host = RadiantHost.Start(radiant);
                logging?.Info("[Telemetry] started (service=" + serviceName + ", otlp=" + endpoint + ")");
                return host;
            }
            catch (Exception e)
            {
                logging?.Warn("[Telemetry] failed to start, continuing without telemetry: " + e.Message);
                return null;
            }
        }
    }
}
