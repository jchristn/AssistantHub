namespace Test.XUnit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Test.Automated;

    public class ModelFixture
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

                    ModelSuite suite = new ModelSuite();
                    IReadOnlyList<AutomatedTestResult> results = suite.RunAsync()
                        .GetAwaiter()
                        .GetResult();

                    Dictionary<string, AutomatedTestResult> cachedResults =
                        new Dictionary<string, AutomatedTestResult>(StringComparer.Ordinal);

                    foreach (var result in results)
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
