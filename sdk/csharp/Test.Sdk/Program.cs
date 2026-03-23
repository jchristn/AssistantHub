namespace Test.Sdk
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub C# SDK Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine();

            TestConfig config = TestConfig.Load(args);
            Console.WriteLine($"  Base URL:  {config.BaseUrl}");
            Console.WriteLine($"  Tenant:    {config.TenantId}");
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            try
            {
                // Test suite classes will be added here as they are implemented.
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
