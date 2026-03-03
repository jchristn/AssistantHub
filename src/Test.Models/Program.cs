namespace Test.Models
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;
    using Test.Models.Tests;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub Models Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            try
            {
                await EnumTests.RunAllAsync(runner, token);
                await CoreModelTests.RunAllAsync(runner, token);
                await RerankingModelTests.RunAllAsync(runner, token);
                await ApiContractModelTests.RunAllAsync(runner, token);
                await SettingsModelTests.RunAllAsync(runner, token);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception during test execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            totalStopwatch.Stop();
            runner.PrintSummary(totalStopwatch.Elapsed.TotalMilliseconds);

            foreach (TestResult r in runner.Results)
            {
                if (!r.Passed) return 1;
            }

            return 0;
        }
    }
}
