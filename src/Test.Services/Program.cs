namespace Test.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;
    using Test.Services.Tests;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub Services Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            try
            {
                await AuthenticationServiceTests.RunAllAsync(runner, token);
                await ProcessingLogServiceTests.RunAllAsync(runner, token);
                await InferenceServiceTests.RunAllAsync(runner, token);
                await TenantProvisioningServiceTests.RunAllAsync(runner, token);
                await CrawlerFactoryTests.RunAllAsync(runner, token);
                await CrawlOperationCleanupServiceTests.RunAllAsync(runner, token);
                await CrawlSchedulerServiceTests.RunAllAsync(runner, token);
                await EndpointHealthCheckServiceTests.RunAllAsync(runner, token);
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
