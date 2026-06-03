namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Automated;
    using Touchstone.Core;

    public static class TouchstoneSuiteCatalog
    {
        public static IReadOnlyList<TestSuiteDescriptor> GetSuites()
        {
            List<TestSuiteDescriptor> suites = new List<TestSuiteDescriptor>
            {
                CreateSuite("model", "Model Suite", () => new ModelSuite().RunAsync()),
                CreateSuite("service", "Service Suite", () => new ServiceSuite().RunAsync()),
                CreateSuite("api", "API Suite", () => new ApiSuite().RunAsync()),
                CreateSuite("integration", "Integration Suite", () => new IntegrationSuite().RunAsync()),
                CreateSuite("mcp", "MCP Suite", () => new McpSuite().RunAsync())
            };

            HashSet<string> requestedSuites = GetRequestedSuites();
            if (requestedSuites.Count < 1) return suites;

            return suites
                .Where(suite => requestedSuites.Contains(suite.SuiteId))
                .ToList();
        }

        private static TestSuiteDescriptor CreateSuite(
            string suiteId,
            string displayName,
            Func<Task<IReadOnlyList<AutomatedTestResult>>> executeSuiteAsync)
        {
            return new TestSuiteDescriptor(
                suiteId,
                displayName,
                new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId,
                        "all",
                        displayName,
                        token => RunLegacySuiteAsync(executeSuiteAsync, token),
                        new[] { suiteId })
                });
        }

        private static async Task RunLegacySuiteAsync(
            Func<Task<IReadOnlyList<AutomatedTestResult>>> executeSuiteAsync,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            IReadOnlyList<AutomatedTestResult> results = await executeSuiteAsync().ConfigureAwait(false);
            List<AutomatedTestResult> failures = results
                .Where(result => result != null && !result.Passed)
                .ToList();

            if (failures.Count < 1) return;

            throw new InvalidOperationException(
                failures.Count.ToString() +
                " shared test(s) failed: " +
                String.Join("; ", failures.Select(failure => failure.TestName + ": " + failure.ErrorMessage)));
        }

        private static HashSet<string> GetRequestedSuites()
        {
            string raw = Environment.GetEnvironmentVariable("ASSISTANTHUB_TEST_SUITES");
            if (String.IsNullOrWhiteSpace(raw))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return raw
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
