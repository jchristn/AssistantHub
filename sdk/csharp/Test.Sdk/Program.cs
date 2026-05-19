namespace Test.Sdk
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using Test.Shared;
    using Test.Sdk.Tests;

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
                using (AssistantHubClient client = new AssistantHubClient(config.BaseUrl, config.ApiKey))
                {
                    await HealthTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await TenantTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await AssistantTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await CollectionTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await DocumentTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await ThreadTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await EndpointTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await InferenceTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await EvalTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await CrawlPlanTests.RunAsync(runner, client, token).ConfigureAwait(false);
                    await ConfigTests.RunAsync(runner, client, token).ConfigureAwait(false);
                }
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
