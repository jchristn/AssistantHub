namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string resultsPath = ParseResultsPath(args);
            return await ConsoleRunner.RunAsync(
                TouchstoneSuiteCatalog.GetSuites(),
                resultsPath: resultsPath).ConfigureAwait(false);
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
