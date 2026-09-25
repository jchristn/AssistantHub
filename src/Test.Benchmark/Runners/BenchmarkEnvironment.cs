namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provenance recorded in every report, so a number can always be traced to the code, containers, models and
    /// data that produced it.
    /// </summary>
    public class BenchmarkEnvironment
    {
        #region Public-Members

        /// <summary>Start time.</summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Harness version.</summary>
        public string HarnessVersion { get; set; } = "0.1.0";

        /// <summary>AssistantHub URL.</summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>Git commit of the working tree, with -dirty for uncommitted changes.</summary>
        public string GitCommit { get; set; } = string.Empty;

        /// <summary>Machine description.</summary>
        public string Machine { get; set; } = string.Empty;

        /// <summary>Container image digests of the benchmark stack, by container name.</summary>
        public Dictionary<string, string> Images { get; set; } = new Dictionary<string, string>();

        /// <summary>Ollama model digests, by model name.</summary>
        public Dictionary<string, string> Models { get; set; } = new Dictionary<string, string>();

        /// <summary>Dataset file hash.</summary>
        public string? DatasetHash { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Capture the environment.
        /// </summary>
        /// <param name="http">HTTP client.</param>
        /// <param name="serverUrl">AssistantHub URL.</param>
        /// <param name="ollamaUrl">Ollama URL for model digests (optional).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Environment.</returns>
        public static async Task<BenchmarkEnvironment> CaptureAsync(HttpClient http, string serverUrl, string? ollamaUrl, CancellationToken token)
        {
            BenchmarkEnvironment environment = new BenchmarkEnvironment
            {
                ServerUrl = serverUrl,
                GitCommit = GitCommitOrUnknown(),
                Machine = Environment.MachineName + " / " + RuntimeInformation.OSDescription + " / " + Environment.ProcessorCount + " logical CPUs / .NET " + Environment.Version
            };

            try
            {
                string ps = Run("docker", "ps --filter name=assistanthub-bench --format {{.Names}}|{{.Image}}");
                foreach (string line in ps.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length != 2) continue;
                    string digest = Run("docker", "inspect --format {{.Image}} " + parts[0]).Trim();
                    environment.Images[parts[0]] = parts[1] + (digest.Length > 19 ? " (" + digest.Substring(0, 19) + ")" : string.Empty);
                }
            }
            catch (Exception)
            {
            }

            if (!string.IsNullOrEmpty(ollamaUrl))
            {
                try
                {
                    JsonNode? tags = JsonNode.Parse(await http.GetStringAsync(ollamaUrl.TrimEnd('/') + "/api/tags", token).ConfigureAwait(false));
                    if (tags?["models"] is JsonArray models)
                    {
                        foreach (JsonNode? model in models)
                        {
                            string? name = model?["name"]?.GetValue<string>();
                            string? digest = model?["digest"]?.GetValue<string>();
                            if (name != null && digest != null) environment.Models[name] = digest.Length > 12 ? digest.Substring(0, 12) : digest;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return environment;
        }

        #endregion

        #region Private-Methods

        private static string GitCommitOrUnknown()
        {
            try
            {
                string commit = Run("git", "rev-parse --short HEAD").Trim();
                string status = Run("git", "status --porcelain --untracked-files=no").Trim();
                return string.IsNullOrEmpty(commit) ? "unknown" : commit + (status.Length > 0 ? "-dirty" : string.Empty);
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static string Run(string file, string arguments)
        {
            ProcessStartInfo info = new ProcessStartInfo(file, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(info);
            if (process == null) return string.Empty;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(10000);
            return output;
        }

        #endregion
    }
}
