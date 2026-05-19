namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;

    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            string resultsPath = ParseResultsPath(args);
            AutomatedConsoleRunner runner = new AutomatedConsoleRunner(resultsPath);
            AutomatedRunSummary summary = await runner.RunAsync().ConfigureAwait(false);
            Environment.Exit(summary.FailedCount > 0 ? 1 : 0);
        }

        private static string ParseResultsPath(string[] args)
        {
            if (args == null || args.Length < 2)
                return null;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (String.Equals(args[i], "--results", StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }
    }
}
