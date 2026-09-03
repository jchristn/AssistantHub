# AssistantHub Telemetry

AssistantHub is instrumented for **metrics** and **distributed traces** using the .NET base class
library (`System.Diagnostics.Metrics.Meter` and `System.Diagnostics.ActivitySource`) and exports them
over **OpenTelemetry (OTLP)**. Logs are shipped to Loki by the OpenTelemetry Collector. A ready-to-run
observability stack (OpenTelemetry Collector, Prometheus, Tempo, Loki, Grafana) ships in
[`docker/compose.yaml`](docker/compose.yaml), and the product can just as easily be pointed at an
existing enterprise observability platform.

This document is written for two audiences: developers who want to understand or extend the
instrumentation, and DevOps/SRE teams integrating AssistantHub into a broader observability stack.

---

## 1. Architecture at a glance

```
  AssistantHub.Server ─┐                          ┌─► Prometheus  (metrics, :9090)
                       │  OTLP gRPC (:4317)        │
  AssistantHub.McpServer ─►  OpenTelemetry  ───────┼─► Tempo       (traces,  :3200)
                       │      Collector            │
  (app log files)  ────┘  (filelog receiver) ──────┴─► Loki        (logs,    :3100)
                                                          │
                                                          └─► Grafana (:3000, admin/admin)
```

- **Emit side** — application code emits through the BCL `Meter`/`ActivitySource` named `AssistantHub`.
  Emitting is a near-free no-op until a host subscribes, so there is no cost when telemetry is disabled.
  The emit surface has **no dependency on OpenTelemetry** — it lives in `AssistantHub.Core`
  (`AssistantHub.Core.Telemetry.AssistantHubTelemetry`) and the published `AssistantHub.Core` NuGet
  package stays exporter-free.
- **Host side** — each executable (`AssistantHub.Server`, `AssistantHub.McpServer`) starts a telemetry
  host (the [Radiant](https://www.nuget.org/packages/Radiant) SDK) that subscribes to the `AssistantHub`
  meter/activity source by name and exports OTLP to the collector.
- **Collection** — the OpenTelemetry Collector fans out: metrics → Prometheus, traces → Tempo, logs → Loki.
- **Visualization** — Grafana, pre-provisioned with datasources and an `AssistantHub` dashboard folder.

### Service identity

| Executable | `service.name` |
|---|---|
| REST API server | `assistanthub-server` |
| MCP server | `assistanthub-mcp` |

Both stamp a `service.instance.id` resource attribute (auto-generated GUID per process unless configured).

---

## 2. What is measured

Coverage is achieved at the framework boundaries plus the critical application workflows:

- **HTTP** — every REST route is measured and traced via a single Watson Webserver pre/post-routing hook.
- **MCP** — every MCP tool, across all three transports (HTTP, TCP, WebSocket), is measured and traced via
  a single registration wrapper.
- **Application layer** — inference, retrieval, ingestion, vector/index/object storage, chat, crawl, eval,
  and auth workflows emit operation spans and a duration histogram, plus domain-specific counters.
- **Runtime/process** — .NET GC, heap, thread pool, working set, CPU and uptime (via the telemetry host).

### Metric catalog

All instruments are emitted by the meter named `AssistantHub`. Durations are in **seconds** (UCUM `s`).
The Prometheus-exported name (after the collector applies unit suffixes and converts dots to underscores)
is shown in the right column.

| Instrument | Type | Unit | Labels | Prometheus name |
|---|---|---|---|---|
| `http.server.request.duration` | Histogram | s | `http.request.method`, `http.route`, `http.response.status_code` | `http_server_request_duration_seconds_{bucket,count,sum}` |
| `http.server.active_requests` | UpDownCounter | {request} | — | `http_server_active_requests` |
| `mcp.server.tool.duration` | Histogram | s | `mcp.tool`, `mcp.transport`, `outcome` | `mcp_server_tool_duration_seconds_*` |
| `mcp.server.tool.calls` | Counter | {call} | `mcp.tool`, `mcp.transport`, `outcome` | `mcp_server_tool_calls_total` |
| `mcp.server.active_requests` | UpDownCounter | {request} | `mcp.transport` | `mcp_server_active_requests` |
| `assistanthub.operation.duration` | Histogram | s | `domain`, `operation`, `outcome` | `assistanthub_operation_duration_seconds_*` |
| `inference.tokens` | Counter | {token} | `provider`, `model`, `token.type` | `inference_tokens_total` |
| `retrieval.results` | Histogram | {result} | `mode` | `retrieval_results_{bucket,count,sum}` |
| `ingestion.documents` | Counter | {document} | `outcome` | `ingestion_documents_total` |
| `ingestion.chunks` | Counter | {chunk} | — | `ingestion_chunks_total` |

`assistanthub.operation.duration` carries the whole application layer through a low-cardinality `domain`
label. Domains: `inference`, `retrieval`, `ingestion`, `storage`, `vector`, `index`, `object`, `crawl`,
`eval`, `auth`, `chat`, `embedding`, `chunking`. Filter a domain in PromQL with, e.g.,
`assistanthub_operation_duration_seconds_bucket{domain="inference"}`.

**Cardinality rule:** metric labels are kept low-cardinality (methods, routes, domains, outcomes, models).
High-cardinality identifiers (tenant id, document id, request id) are attached to **spans**, never to
metrics.

### Traces

The `AssistantHub` activity source emits spans for:

- **HTTP requests** (span kind *server*) — tagged with method, route, and status code.
- **MCP tool invocations** (span kind *server*) — tagged with tool, transport, and outcome.
- **Application operations** (span kind *internal*) — named `<domain>.<operation>`, e.g.
  `inference.completion`, `retrieval.search`, `ingestion.ingest-document`. Nested service calls nest in the
  trace, so an ingestion trace shows its atomize → chunk → embed → store child spans.

Head-based sampling is configurable (`SamplingRatio`, default `1.0` = sample everything) via a parent-based
sampler, so a sampled parent keeps its children.

### Logs

The apps log via `SyslogLogging` to console and to per-service log files
(`docker/assistanthub/logs`, `docker/assistanthub-mcp/logs`). The OpenTelemetry Collector's `filelog`
receiver tails those files and forwards them to Loki, stamped with a `service_name`/`job` label so logs are
filterable per app. No application code change is required to ship logs.

---

## 3. Enabling and configuring telemetry

Telemetry is **off by default** and turns on when enabled. Configuration comes from the settings file with
environment variables overriding.

### Configuration (settings file)

Both `assistanthub.json` (server) and `assistanthub-mcp.json` (MCP) accept a `Telemetry` section:

```json
"Telemetry": {
  "Enable": false,
  "ServiceName": null,
  "OtlpEndpoint": "http://localhost:4317",
  "OtlpProtocol": "Grpc",
  "SamplingRatio": 1.0,
  "EnableRuntimeMetrics": true,
  "ExportIntervalMs": 15000
}
```

| Field | Meaning | Default |
|---|---|---|
| `Enable` | Master switch for OTLP export | `false` |
| `ServiceName` | Overrides the default `service.name` | `null` (host default) |
| `OtlpEndpoint` | Collector endpoint | `http://localhost:4317` |
| `OtlpProtocol` | `Grpc` (:4317) or `HttpProtobuf` (:4318) | `Grpc` |
| `SamplingRatio` | Head-based trace sampling ratio 0.0–1.0 | `1.0` |
| `EnableRuntimeMetrics` | Include .NET runtime metrics | `true` |
| `ExportIntervalMs` | Periodic metric export interval | `15000` |

### Configuration (environment variables — override the file)

| Variable | Overrides | Example |
|---|---|---|
| `ASSISTANTHUB_TELEMETRY_ENABLED` | `Enable` | `true` |
| `ASSISTANTHUB_OTLP_ENDPOINT` | `OtlpEndpoint` | `http://assistanthub-otel-collector:4317` |

The docker compose file sets both variables on the two app services, so telemetry is on automatically when
you run the stack.

---

## 4. Running the bundled stack

```bash
cd docker
docker compose up -d
```

New services and host ports (chosen to avoid conflicts with the existing AssistantHub ports):

| Service | Container | Host port(s) | Default credentials |
|---|---|---|---|
| Grafana | `assistanthub-grafana` | 3000 | `admin` / `admin` |
| Prometheus | `assistanthub-prometheus` | 9090 | none |
| Tempo | `assistanthub-tempo` | 3200 | none (query via Grafana) |
| Loki | `assistanthub-loki` | 3100 | none (query via Grafana) |
| OpenTelemetry Collector | `assistanthub-otel-collector` | 4317 (OTLP gRPC), 4318 (OTLP HTTP), 8889 (Prometheus scrape) | n/a |

These links are also surfaced in the product dashboard under **Configuration → Observability**, each card
showing the service name, URL, and default credentials.

### Grafana dashboards

Grafana is pre-provisioned with datasources (Prometheus, Tempo, Loki, cross-linked for
metric↔trace↔log correlation) and a single top-level folder named **`AssistantHub`** containing:

`AssistantHub - Overview`, `- HTTP`, `- MCP`, `- Inference`, `- Retrieval`, `- Ingestion`, `- Storage`,
`- Runtime`, `- Traces`, `- Logs`.

Open Grafana at `http://localhost:3000` (admin/admin) and browse the `AssistantHub` folder.

---

## 5. Integrating with an existing observability platform

You do **not** need the bundled stack. To ship to your own platform:

### Point at your own collector / OTLP endpoint

Set `ASSISTANTHUB_OTLP_ENDPOINT` (and `ASSISTANTHUB_TELEMETRY_ENABLED=true`) to your OpenTelemetry
Collector or any OTLP-compatible endpoint. Use `OtlpProtocol: "HttpProtobuf"` and port 4318 if your
endpoint speaks OTLP/HTTP instead of gRPC. If your collector requires auth headers, front it with your
gateway or run a local collector that adds them — the app sends standard OTLP.

### Prometheus

The bundled collector exposes a Prometheus endpoint at `:8889` with
`resource_to_telemetry_conversion` enabled (so `service.name` etc. become labels). Any Prometheus-compatible
scraper (Prometheus, Grafana Agent, VictoriaMetrics, Thanos, Datadog OpenMetrics, etc.) can scrape it. The
metric names in the catalog above are stable; build recording rules / alerts against them. Useful starting
points:

```promql
# HTTP error ratio
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))
  / sum(rate(http_server_request_duration_seconds_count[5m]))

# HTTP p95 latency by route
histogram_quantile(0.95,
  sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[$__rate_interval])))

# MCP tool call rate by outcome
sum by (mcp_tool, outcome) (rate(mcp_server_tool_calls_total[$__rate_interval]))

# Inference output tokens/sec by model
sum by (model) (rate(inference_tokens_total{token_type="output"}[$__rate_interval]))
```

### Tempo / Jaeger / any OTLP trace backend

Traces are exported as OTLP. Point the collector's trace exporter at your backend (Tempo, Jaeger, Grafana
Cloud Traces, Datadog, Honeycomb, etc.). Spans follow OpenTelemetry semantic conventions, and W3C
trace-context propagation is enabled, so AssistantHub traces stitch into upstream/downstream services that
also propagate context.

### Loki / your log pipeline

The bundled collector tails the app log files and forwards to Loki via OTLP. To use a different log
pipeline, either (a) repoint the collector's log exporter, or (b) tail the same log directories with your
existing agent (Promtail, Fluent Bit, Vector, the Datadog agent, etc.). Log files live at
`docker/assistanthub/logs` and `docker/assistanthub-mcp/logs`.

### Kubernetes / production notes

- Set `ASSISTANTHUB_OTLP_ENDPOINT` to your in-cluster collector service (e.g. the OpenTelemetry Operator's
  collector) and `ASSISTANTHUB_TELEMETRY_ENABLED=true`.
- Lower `SamplingRatio` (e.g. `0.1`) under high traffic to control trace volume; metrics are unaffected by
  sampling.
- Scrape `service.instance.id` to distinguish replicas.
- The two OTLP env vars are the only wiring you need; everything else has safe defaults.

---

## 6. Extending the instrumentation (developers)

Add telemetry to new code with the helper in `AssistantHub.Core.Telemetry.AssistantHubTelemetry`:

```csharp
using AssistantHub.Core.Telemetry;

using (OperationScope op = AssistantHubTelemetry.StartOperation("inference", "completion"))
{
    try
    {
        op.SetTag("model", model);           // high-cardinality context → span only
        // ... do the work (awaits are timed) ...
        AssistantHubTelemetry.RecordInferenceTokens(provider, model, "output", outputTokens);
    }
    catch (Exception e)
    {
        op.Fail(e);                          // marks outcome=error + records the exception on the span
        throw;
    }
}
```

- The scope records `assistanthub.operation.duration{domain,operation,outcome}` and emits a
  `<domain>.<operation>` span; nested scopes nest in the trace automatically.
- Keep metric labels low-cardinality; put identifiers on spans via `op.SetTag(...)`.
- Durations are always seconds. New instrument names should follow the OpenTelemetry semantic-convention
  style (dotted, lowercase).
- The emit surface never references OpenTelemetry, so `AssistantHub.Core` stays exporter-free.
