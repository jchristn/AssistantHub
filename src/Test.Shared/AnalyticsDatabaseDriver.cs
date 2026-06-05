namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Server.Services;
    using Test.Shared;

    internal sealed class AnalyticsDatabaseDriver : MockDatabaseDriver
    {
        private readonly DateTime _StartUtc;
        private readonly string _TrueLiteral;
        private readonly string _FalseLiteral;
        private readonly List<string> _Queries = new List<string>();

        public AnalyticsDatabaseDriver(DateTime startUtc)
            : this(startUtc, "1", "0")
        {
        }

        public AnalyticsDatabaseDriver(DateTime startUtc, string trueLiteral, string falseLiteral)
        {
            _StartUtc = startUtc;
            _TrueLiteral = trueLiteral;
            _FalseLiteral = falseLiteral;
        }

        public IReadOnlyList<string> Queries => _Queries;

        public override string FormatBoolean(bool value)
        {
            return value ? _TrueLiteral : _FalseLiteral;
        }

        public override Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            _Queries.Add(query);

            if (query.Contains("FROM chat_history h", StringComparison.OrdinalIgnoreCase)
                && query.Contains("LEFT JOIN request_history r", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(BuildRequestHistoryTable());
            if (query.Contains("FROM request_history", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assistant analytics request metrics must be anchored to chat_history.");
            if (query.Contains("FROM chat_history_performance_events e", StringComparison.OrdinalIgnoreCase)
                && query.Contains("INNER JOIN chat_history h", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(BuildPerformanceEventsTable());
            if (query.Contains("FROM chat_history_performance_events", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assistant analytics event metrics must be anchored to chat_history.");
            if (query.Contains("FROM assistant_feedback", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(BuildFeedbackTable());

            return Task.FromResult(new DataTable());
        }

        private DataTable BuildRequestHistoryTable()
        {
            DataTable table = CreateTable(
                "id",
                "trace_id",
                "chat_history_id",
                "tenant_id",
                "assistant_id",
                "thread_id",
                "request_type",
                "source_type",
                "http_method",
                "request_path",
                "status_code",
                "success",
                "duration_ms",
                "created_utc");

            AddRow(table, "req_1", "trace_1", "chist_1", "ten_test", "asst_test", "thr_1", "AssistantChat", "api", "POST", "/v1.0/assistants/asst_test/chat", "200", "true", "1000", _StartUtc.AddMinutes(1).ToString("o"));
            AddRow(table, "req_2", "trace_2", "chist_2", "ten_test", "asst_test", "thr_1", "AssistantChat", "api", "POST", "/v1.0/assistants/asst_test/chat", "500", "false", "2000", _StartUtc.AddMinutes(6).ToString("o"));
            return table;
        }

        private DataTable BuildPerformanceEventsTable()
        {
            DataTable table = CreateTable(
                "id",
                "tenant_id",
                "assistant_id",
                "chat_history_id",
                "request_history_id",
                "trace_id",
                "sequence_number",
                "stage",
                "phase",
                "kind",
                "endpoint_id",
                "endpoint_name",
                "endpoint_type",
                "provider",
                "api_format",
                "model",
                "started_utc",
                "finished_utc",
                "duration_ms",
                "success",
                "http_status_code",
                "error_type",
                "error_message",
                "input_tokens",
                "output_tokens",
                "total_tokens",
                "chunks_input",
                "chunks_output",
                "retrieval_query_count",
                "endpoint_limiter_wait_ms",
                "request_to_headers_ms",
                "headers_to_first_token_ms",
                "first_token_to_last_token_ms",
                "client_total_ms",
                "provider_queue_ms",
                "provider_load_ms",
                "provider_prompt_eval_ms",
                "provider_generation_ms",
                "provider_total_ms",
                "provider_tokens_per_second",
                "provider_request_id",
                "metadata_json",
                "provider_metrics_json",
                "provider_raw_json",
                "created_utc");

            AddEventRow(table, "evt_1", "chist_1", "req_1", "trace_1", "retrieval", "retrieval", "", "", "", "", "", 100, true, null, null, null, null, null, 2, _StartUtc.AddMinutes(1));
            AddEventRow(table, "evt_2", "chist_1", "req_1", "trace_1", "final_inference", "inference", "cep_final", "local", "completion", "Ollama", "gemma3:4b", 900, true, 10, 100, 50, 700, 20, null, _StartUtc.AddMinutes(1).AddSeconds(10));
            AddEventRow(table, "evt_3", "chist_2", "req_2", "trace_2", "retrieval", "retrieval", "", "", "", "", "", 200, true, null, null, null, null, null, 3, _StartUtc.AddMinutes(6));
            AddEventRow(table, "evt_4", "chist_2", "req_2", "trace_2", "final_inference", "inference", "cep_final", "local", "completion", "Ollama", "gemma3:4b", 1700, false, 20, 200, 150, 1200, 30, null, _StartUtc.AddMinutes(6).AddSeconds(10));
            return table;
        }

        private DataTable BuildFeedbackTable()
        {
            DataTable table = CreateTable("id", "tenant_id", "assistant_id", "rating", "created_utc");
            AddRow(table, "afb_1", "ten_test", "asst_test", "ThumbsUp", _StartUtc.AddMinutes(2).ToString("o"));
            AddRow(table, "afb_2", "ten_test", "asst_test", "ThumbsDown", _StartUtc.AddMinutes(7).ToString("o"));
            return table;
        }

        private static DataTable CreateTable(params string[] columns)
        {
            DataTable table = new DataTable();
            foreach (string column in columns) table.Columns.Add(column, typeof(string));
            return table;
        }

        private static void AddRow(DataTable table, params string[] values)
        {
            DataRow row = table.NewRow();
            for (int i = 0; i < values.Length; i++) row[i] = values[i] ?? String.Empty;
            table.Rows.Add(row);
        }

        private static void AddEventRow(
            DataTable table,
            string id,
            string chatHistoryId,
            string requestHistoryId,
            string traceId,
            string stage,
            string kind,
            string endpointId,
            string endpointName,
            string endpointType,
            string provider,
            string model,
            double durationMs,
            bool success,
            double? limiterWaitMs,
            double? requestToHeadersMs,
            double? providerLoadMs,
            double? providerGenerationMs,
            double? tokensPerSecond,
            int? retrievalQueryCount,
            DateTime createdUtc)
        {
            AddRow(
                table,
                id,
                "ten_test",
                "asst_test",
                chatHistoryId,
                requestHistoryId,
                traceId,
                stage == "retrieval" ? "40" : "70",
                stage,
                "",
                kind,
                endpointId,
                endpointName,
                endpointType,
                provider,
                endpointId == "" ? "" : "Ollama",
                model,
                createdUtc.ToString("o"),
                createdUtc.AddMilliseconds(durationMs).ToString("o"),
                durationMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                success ? "true" : "false",
                success ? "200" : "500",
                success ? "" : "UpstreamError",
                success ? "" : "Provider failed",
                endpointId == "" ? "" : (success ? "10" : "20"),
                endpointId == "" ? "" : (success ? "5" : "10"),
                endpointId == "" ? "" : (success ? "15" : "30"),
                endpointId == "" ? "" : "0",
                endpointId == "" ? "8" : "",
                retrievalQueryCount?.ToString() ?? "",
                limiterWaitMs?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                requestToHeadersMs?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                "",
                "",
                durationMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "",
                providerLoadMs?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                "",
                providerGenerationMs?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                durationMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                tokensPerSecond?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                "",
                "",
                "",
                "",
                createdUtc.ToString("o"));
        }
    }
}
