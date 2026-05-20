namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class AutomatedTestExecution
    {
        public static async Task<IReadOnlyList<AutomatedTestResult>> RunAllAsync()
        {
            List<AutomatedTestResult> results = new List<AutomatedTestResult>();
            HashSet<string> requestedSuites = GetRequestedSuites();

            if (ShouldRun(requestedSuites, "model"))
            {
                ModelSuite modelSuite = new ModelSuite();
                results.AddRange(await modelSuite.RunAsync().ConfigureAwait(false));
            }

            if (ShouldRun(requestedSuites, "service"))
            {
                ServiceSuite serviceSuite = new ServiceSuite();
                results.AddRange(await serviceSuite.RunAsync().ConfigureAwait(false));
            }

            if (ShouldRun(requestedSuites, "api"))
            {
                ApiSuite apiSuite = new ApiSuite();
                results.AddRange(await apiSuite.RunAsync().ConfigureAwait(false));
            }

            if (ShouldRun(requestedSuites, "integration"))
            {
                IntegrationSuite integrationSuite = new IntegrationSuite();
                results.AddRange(await integrationSuite.RunAsync().ConfigureAwait(false));
            }

            if (ShouldRun(requestedSuites, "mcp"))
            {
                McpSuite mcpSuite = new McpSuite();
                results.AddRange(await mcpSuite.RunAsync().ConfigureAwait(false));
            }

            return results.ToArray();
        }

        private static HashSet<string> GetRequestedSuites()
        {
            string? raw = Environment.GetEnvironmentVariable("ASSISTANTHUB_TEST_SUITES");
            if (string.IsNullOrWhiteSpace(raw))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return raw
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool ShouldRun(HashSet<string> requestedSuites, string suiteName)
        {
            return requestedSuites.Count < 1 || requestedSuites.Contains(suiteName);
        }
    }
}
