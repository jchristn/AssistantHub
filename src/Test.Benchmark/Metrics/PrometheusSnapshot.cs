namespace Test.Benchmark.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A point-in-time scrape of the benchmark collector's Prometheus endpoint. Two snapshots bracket a phase; their
    /// difference gives server-side time per <c>domain.operation</c> from AssistantHub's existing
    /// <c>assistanthub.operation.duration</c> histogram, without extra instrumentation. AssistantHub exports metrics
    /// on an interval, so callers wait one export interval before the closing scrape.
    /// </summary>
    public class PrometheusSnapshot
    {
        #region Public-Members

        /// <summary>
        /// True when the scrape succeeded.
        /// </summary>
        public bool Available { get; private set; } = false;

        #endregion

        #region Private-Members

        private static readonly Regex _Line = new Regex(
            "^assistanthub_operation_duration_seconds_(sum|count)\\{(?<labels>[^}]*)\\}\\s+(?<value>\\S+)",
            RegexOptions.Compiled);

        private static readonly Regex _Label = new Regex("(?<name>[a-zA-Z_]+)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled);

        private readonly Dictionary<string, double> _Sums = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _Counts = new Dictionary<string, double>(StringComparer.Ordinal);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Scrape a Prometheus text endpoint. A failed scrape yields an unavailable snapshot, because metrics are
        /// optional context.
        /// </summary>
        /// <param name="http">HTTP client.</param>
        /// <param name="url">Metrics URL, or null to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The snapshot.</returns>
        public static async Task<PrometheusSnapshot> CaptureAsync(HttpClient http, string? url, CancellationToken token)
        {
            PrometheusSnapshot snapshot = new PrometheusSnapshot();
            if (string.IsNullOrEmpty(url)) return snapshot;

            try
            {
                string text = await http.GetStringAsync(url, token).ConfigureAwait(false);
                snapshot.Parse(text);
                snapshot.Available = true;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
            }

            return snapshot;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Per-operation mean latency and call count between an earlier snapshot and this one.
        /// </summary>
        /// <param name="before">Earlier snapshot.</param>
        /// <returns>Operation (<c>domain.operation</c>) to breakdown; empty when unavailable.</returns>
        public Dictionary<string, StageBreakdown> Since(PrometheusSnapshot before)
        {
            Dictionary<string, StageBreakdown> stages = new Dictionary<string, StageBreakdown>(StringComparer.Ordinal);
            if (!Available || before == null || !before.Available) return stages;

            foreach (KeyValuePair<string, double> count in _Counts)
            {
                double calls = count.Value - (before._Counts.TryGetValue(count.Key, out double c) ? c : 0.0);
                double seconds = (_Sums.TryGetValue(count.Key, out double s) ? s : 0.0) - (before._Sums.TryGetValue(count.Key, out double bs) ? bs : 0.0);
                if (calls <= 0) continue;
                stages[count.Key] = new StageBreakdown
                {
                    Count = (long)Math.Round(calls),
                    MeanMs = Math.Round(seconds * 1000.0 / calls, 2),
                    TotalMs = Math.Round(seconds * 1000.0, 1)
                };
            }

            return stages;
        }

        #endregion

        #region Private-Methods

        private void Parse(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                Match match = _Line.Match(raw.Trim());
                if (!match.Success) continue;
                if (!double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) continue;

                string domain = "unknown";
                string operation = "unknown";
                foreach (Match label in _Label.Matches(match.Groups["labels"].Value))
                {
                    if (label.Groups["name"].Value == "domain") domain = label.Groups["value"].Value;
                    else if (label.Groups["name"].Value == "operation") operation = label.Groups["value"].Value;
                }

                string key = domain + "." + operation;
                Dictionary<string, double> target = match.Groups[1].Value == "sum" ? _Sums : _Counts;
                target[key] = (target.TryGetValue(key, out double existing) ? existing : 0.0) + value;
            }
        }

        #endregion
    }
}
