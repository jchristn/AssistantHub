namespace Test.Xunit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Test.Automated;

    public class IntegrationFixture
    {
        private static readonly object _Sync = new object();
        private static IReadOnlyDictionary<string, AutomatedTestResult> _CachedResults = null;

        public IReadOnlyDictionary<string, AutomatedTestResult> Results
        {
            get { return EnsureResults(); }
        }

        private static IReadOnlyDictionary<string, AutomatedTestResult> EnsureResults()
        {
            lock (_Sync)
            {
                if (_CachedResults != null)
                    return _CachedResults;

                TextWriter standardOutput = Console.Out;
                TextWriter standardError = Console.Error;

                try
                {
                    Console.SetOut(TextWriter.Null);
                    Console.SetError(TextWriter.Null);

                    IntegrationSuite suite = new IntegrationSuite();
                    McpSuite mcpSuite = new McpSuite();
                    IReadOnlyList<AutomatedTestResult> integrationResults = suite.RunAsync()
                        .GetAwaiter()
                        .GetResult();
                    IReadOnlyList<AutomatedTestResult> mcpResults = mcpSuite.RunAsync()
                        .GetAwaiter()
                        .GetResult();

                    Dictionary<string, AutomatedTestResult> cachedResults =
                        new Dictionary<string, AutomatedTestResult>(StringComparer.Ordinal);

                    foreach (var result in integrationResults)
                        cachedResults[result.TestName] = result;

                    foreach (var result in mcpResults)
                        cachedResults[result.TestName] = result;

                    _CachedResults = cachedResults;
                    return _CachedResults;
                }
                finally
                {
                    Console.SetOut(standardOutput);
                    Console.SetError(standardError);
                }
            }
        }
    }
}
