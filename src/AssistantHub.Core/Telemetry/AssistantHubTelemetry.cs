namespace AssistantHub.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// AssistantHub telemetry emit surface. Instruments and spans are created through the .NET base class
    /// library (<see cref="Meter"/> and <see cref="ActivitySource"/>) so this type carries no dependency on
    /// OpenTelemetry or any exporter. Emitting is a near-free no-op until a host (for example the Radiant
    /// telemetry host in AssistantHub.Server or AssistantHub.McpServer) subscribes to the
    /// <see cref="SourceName"/> meter and activity source by name.
    /// <para>
    /// Instrument names, units, and label keys follow the OpenTelemetry semantic conventions so the signals
    /// render in stock Prometheus/Grafana dashboards. Durations are always seconds. Labels are kept
    /// low-cardinality (methods, routes, domains, outcomes); identifiers belong on spans, not metrics.
    /// </para>
    /// </summary>
    public static class AssistantHubTelemetry
    {
        #region Public-Members

        /// <summary>
        /// The meter and activity source name a telemetry host subscribes to. Stable public contract.
        /// </summary>
        public const string SourceName = "AssistantHub";

        /// <summary>
        /// The activity source used for all AssistantHub spans.
        /// </summary>
        public static readonly ActivitySource Activity = new ActivitySource(SourceName);

        #endregion

        #region Private-Members

        private static readonly Meter _Meter = new Meter(SourceName);

        // HTTP server (inbound REST API)
        private static readonly Histogram<double> _HttpDuration = _Meter.CreateHistogram<double>(
            "http.server.request.duration", "s", "Duration of inbound HTTP server requests.");
        private static readonly UpDownCounter<long> _HttpActive = _Meter.CreateUpDownCounter<long>(
            "http.server.active_requests", "{request}", "Concurrent HTTP server requests in flight.");

        // MCP server (inbound tool invocations)
        private static readonly Histogram<double> _McpDuration = _Meter.CreateHistogram<double>(
            "mcp.server.tool.duration", "s", "Duration of MCP tool invocations.");
        private static readonly Counter<long> _McpCalls = _Meter.CreateCounter<long>(
            "mcp.server.tool.calls", "{call}", "MCP tool invocations.");
        private static readonly UpDownCounter<long> _McpActive = _Meter.CreateUpDownCounter<long>(
            "mcp.server.active_requests", "{request}", "Concurrent MCP tool invocations in flight.");

        // Application-layer operations (inference, retrieval, ingestion, storage, crawl, eval, auth, chat...)
        private static readonly Histogram<double> _OperationDuration = _Meter.CreateHistogram<double>(
            "assistanthub.operation.duration", "s", "Duration of application-layer operations.");

        // Domain-specific instruments
        private static readonly Counter<long> _InferenceTokens = _Meter.CreateCounter<long>(
            "inference.tokens", "{token}", "Tokens processed by inference operations.");
        private static readonly Histogram<int> _RetrievalResults = _Meter.CreateHistogram<int>(
            "retrieval.results", "{result}", "Results returned per retrieval query.");
        private static readonly Counter<long> _IngestionDocuments = _Meter.CreateCounter<long>(
            "ingestion.documents", "{document}", "Documents processed by ingestion.");
        private static readonly Counter<long> _IngestionChunks = _Meter.CreateCounter<long>(
            "ingestion.chunks", "{chunk}", "Chunks produced during ingestion.");

        #endregion

        #region HTTP

        /// <summary>
        /// Increment the in-flight HTTP request gauge. Pair with <see cref="DecrementHttpActive"/>.
        /// </summary>
        public static void IncrementHttpActive()
        {
            _HttpActive.Add(1);
        }

        /// <summary>
        /// Decrement the in-flight HTTP request gauge.
        /// </summary>
        public static void DecrementHttpActive()
        {
            _HttpActive.Add(-1);
        }

        /// <summary>
        /// Record a completed HTTP server request.
        /// </summary>
        /// <param name="method">HTTP method (for example GET).</param>
        /// <param name="route">Matched route template (low cardinality), for example /v1.0/tenants/{id}.</param>
        /// <param name="statusCode">HTTP response status code.</param>
        /// <param name="seconds">Request duration in seconds.</param>
        public static void RecordHttpRequest(string method, string route, int statusCode, double seconds)
        {
            TagList tags = new TagList
            {
                { "http.request.method", method ?? "UNKNOWN" },
                { "http.route", String.IsNullOrEmpty(route) ? "unmatched" : route },
                { "http.response.status_code", statusCode }
            };
            _HttpDuration.Record(seconds, tags);
        }

        /// <summary>
        /// Emit an HTTP server span with explicit start and end timestamps.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="route">Matched route template.</param>
        /// <param name="statusCode">HTTP response status code.</param>
        /// <param name="startUtc">Request start time (UTC).</param>
        /// <param name="endUtc">Request end time (UTC).</param>
        public static void EmitHttpSpan(string method, string route, int statusCode, DateTime startUtc, DateTime endUtc)
        {
            string name = (method ?? "HTTP") + " " + (String.IsNullOrEmpty(route) ? "unmatched" : route);
            Activity activity = Activity.StartActivity(name, ActivityKind.Server, default(ActivityContext), startTime: startUtc);
            if (activity == null) return;

            try
            {
                activity.SetTag("http.request.method", method);
                activity.SetTag("http.route", route);
                activity.SetTag("http.response.status_code", statusCode);
                activity.SetStatus(statusCode >= 500 ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
                activity.SetEndTime(endUtc);
            }
            finally
            {
                activity.Dispose();
            }
        }

        #endregion

        #region MCP

        /// <summary>
        /// Begin timing an MCP tool invocation. Dispose the returned scope when the invocation completes;
        /// call <see cref="McpToolScope.Fail"/> on failure.
        /// </summary>
        /// <param name="tool">The MCP tool name (for example assistant/list).</param>
        /// <param name="transport">The transport the invocation arrived on (http, tcp, ws).</param>
        /// <returns>A disposable MCP tool scope.</returns>
        public static McpToolScope StartMcpTool(string tool, string transport)
        {
            return new McpToolScope(tool, transport);
        }

        internal static void RecordMcpTool(string tool, string transport, string outcome, double seconds)
        {
            TagList tags = new TagList
            {
                { "mcp.tool", tool ?? "unknown" },
                { "mcp.transport", transport ?? "unknown" },
                { "outcome", outcome ?? "ok" }
            };
            _McpDuration.Record(seconds, tags);
            _McpCalls.Add(1, tags);
        }

        internal static void IncrementMcpActive(string transport)
        {
            _McpActive.Add(1, new KeyValuePair<string, object?>("mcp.transport", transport));
        }

        internal static void DecrementMcpActive(string transport)
        {
            _McpActive.Add(-1, new KeyValuePair<string, object?>("mcp.transport", transport));
        }

        #endregion

        #region Application-Operations

        /// <summary>
        /// Begin timing an application-layer operation. Dispose the returned scope when the operation
        /// completes; call <see cref="OperationScope.Fail"/> on failure.
        /// </summary>
        /// <param name="domain">The operation domain (for example inference, retrieval, ingestion, storage).</param>
        /// <param name="operation">The operation name (for example completion, search, ingest-document).</param>
        /// <returns>A disposable operation scope.</returns>
        public static OperationScope StartOperation(string domain, string operation)
        {
            return new OperationScope(domain, operation);
        }

        internal static void RecordOperation(string domain, string operation, string outcome, double seconds)
        {
            TagList tags = new TagList
            {
                { "domain", domain ?? "unknown" },
                { "operation", operation ?? "unknown" },
                { "outcome", outcome ?? "ok" }
            };
            _OperationDuration.Record(seconds, tags);
        }

        #endregion

        #region Domain-Metrics

        /// <summary>
        /// Record tokens consumed or produced by an inference operation.
        /// </summary>
        /// <param name="provider">Inference provider (for example ollama, openai, gemini).</param>
        /// <param name="model">Model name.</param>
        /// <param name="tokenType">Token type: input or output.</param>
        /// <param name="tokens">Token count.</param>
        public static void RecordInferenceTokens(string provider, string model, string tokenType, long tokens)
        {
            if (tokens <= 0) return;
            TagList tags = new TagList
            {
                { "provider", provider ?? "unknown" },
                { "model", model ?? "unknown" },
                { "token.type", tokenType ?? "unknown" }
            };
            _InferenceTokens.Add(tokens, tags);
        }

        /// <summary>
        /// Record the number of results returned by a retrieval query.
        /// </summary>
        /// <param name="mode">Retrieval mode (for example vector, keyword, hybrid).</param>
        /// <param name="count">Result count.</param>
        public static void RecordRetrievalResults(string mode, int count)
        {
            _RetrievalResults.Record(count, new KeyValuePair<string, object?>("mode", mode ?? "unknown"));
        }

        /// <summary>
        /// Record a document processed by ingestion.
        /// </summary>
        /// <param name="outcome">Outcome (ok or error).</param>
        public static void RecordIngestedDocument(string outcome)
        {
            _IngestionDocuments.Add(1, new KeyValuePair<string, object?>("outcome", outcome ?? "ok"));
        }

        /// <summary>
        /// Record chunks produced during ingestion.
        /// </summary>
        /// <param name="count">Chunk count.</param>
        public static void RecordIngestedChunks(long count)
        {
            if (count <= 0) return;
            _IngestionChunks.Add(count);
        }

        #endregion
    }
}
