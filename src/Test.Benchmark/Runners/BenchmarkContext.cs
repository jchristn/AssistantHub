namespace Test.Benchmark.Runners
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shared state for a benchmark command: arguments, HTTP client, AssistantHub client, metrics endpoint,
    /// results directory and captured environment.
    /// </summary>
    public class BenchmarkContext : IDisposable
    {
        #region Public-Members

        /// <summary>Arguments.</summary>
        public BenchmarkArguments Arguments { get; }

        /// <summary>Shared HTTP client.</summary>
        public HttpClient Http { get; }

        /// <summary>AssistantHub client.</summary>
        public AssistantHubClient Client { get; }

        /// <summary>Prometheus URL of the benchmark collector, or null.</summary>
        public string? MetricsUrl { get; }

        /// <summary>Ollama URL (embedding model digests, default judge).</summary>
        public string OllamaUrl { get; }

        /// <summary>Directory reports are written to.</summary>
        public string ResultsDirectory { get; }

        /// <summary>Captured environment.</summary>
        public BenchmarkEnvironment Environment { get; private set; } = new BenchmarkEnvironment();

        /// <summary>
        /// Milliseconds to wait before the closing metrics scrape (AssistantHub exports metrics on an interval).
        /// </summary>
        public int MetricsSettleMs { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="arguments">Arguments.</param>
        public BenchmarkContext(BenchmarkArguments arguments)
        {
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            SocketsHttpHandler handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 256,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AutomaticDecompression = DecompressionMethods.All
            };
            Http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(arguments.GetInt("http-timeout-minutes", 15)) };

            // 127.0.0.1 rather than localhost: on Windows a refused IPv6 connect to ::1 costs about two seconds
            // before falling back to IPv4, which would dominate every measured latency.
            string url = arguments.Get("url", "http://127.0.0.1:38800");
            Client = new AssistantHubClient(Http, url, arguments.Get("token", "default"));
            string metrics = arguments.Get("metrics-url", "http://127.0.0.1:38889/metrics");
            MetricsUrl = string.Equals(metrics, "none", StringComparison.OrdinalIgnoreCase) ? null : metrics;
            OllamaUrl = arguments.Get("ollama-url", "http://127.0.0.1:11434");
            MetricsSettleMs = arguments.GetInt("metrics-settle-ms", 2500);
            ResultsDirectory = arguments.Get("output-dir", Path.Combine(FindRepositoryRoot(), "benchmarks", "results"));
            Directory.CreateDirectory(ResultsDirectory);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Capture the environment (call once before running).
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(CancellationToken token)
        {
            Environment = await BenchmarkEnvironment.CaptureAsync(Http, Client.BaseUrl, OllamaUrl, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Base path (without extension) for a report file.
        /// </summary>
        /// <param name="kind">Report kind.</param>
        /// <param name="name">Dataset or scenario name.</param>
        /// <returns>Base path.</returns>
        public string ReportBasePath(string kind, string name)
        {
            string label = Arguments.GetOptional("label") is string l && l.Length > 0 ? "-" + l : string.Empty;
            return Path.Combine(ResultsDirectory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + kind + "-" + name + label);
        }

        /// <summary>
        /// Dispose the HTTP client.
        /// </summary>
        public void Dispose()
        {
            Http.Dispose();
        }

        #endregion

        #region Private-Methods

        private static string FindRepositoryRoot()
        {
            string? directory = System.IO.Directory.GetCurrentDirectory();
            while (directory != null)
            {
                if (System.IO.Directory.Exists(Path.Combine(directory, "benchmarks")) && System.IO.Directory.Exists(Path.Combine(directory, "src"))) return directory;
                directory = Path.GetDirectoryName(directory);
            }

            return System.IO.Directory.GetCurrentDirectory();
        }

        #endregion
    }
}
