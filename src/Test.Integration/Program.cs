namespace Test.Integration
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;
    using Test.Integration.Tests;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub Integration Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            TestServer server = null;

            try
            {
                Console.WriteLine("Starting test server...");
                server = await TestServer.CreateAsync();
                Console.WriteLine($"Test server running at {server.BaseUrl}");
                Console.WriteLine();

                await ServerLifecycleTests.RunAllAsync(server, runner, token);
                await AuthenticationFlowTests.RunAllAsync(server, runner, token);
                await CrudLifecycleTests.RunAllAsync(server, runner, token);
                await PaginationTests.RunAllAsync(server, runner, token);
                await MultiTenantIsolationTests.RunAllAsync(server, runner, token);
                await ErrorHandlingTests.RunAllAsync(server, runner, token);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception during test execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
            finally
            {
                server?.Dispose();
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
