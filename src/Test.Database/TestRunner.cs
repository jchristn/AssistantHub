namespace Test.Database
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestRunner
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        public IReadOnlyList<TestResult> Results => _results;

        public async Task<TestResult> RunTestAsync(string testName, Func<CancellationToken, Task> testAction, CancellationToken token = default)
        {
            Stopwatch sw = Stopwatch.StartNew();
            TestResult result;

            try
            {
                await testAction(token).ConfigureAwait(false);
                sw.Stop();
                result = new TestResult(testName, true, sw.Elapsed.TotalMilliseconds);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  PASS ");
            }
            catch (Exception ex)
            {
                sw.Stop();
                result = new TestResult(testName, false, sw.Elapsed.TotalMilliseconds, ex.Message);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  FAIL ");
            }

            Console.ResetColor();
            Console.WriteLine($" {testName} ({result.RuntimeMs:F1}ms)");

            if (!result.Passed)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"         {result.ErrorMessage}");
                Console.ResetColor();
            }

            _results.Add(result);
            return result;
        }

        public void PrintSummary(double totalRuntimeMs)
        {
            int passed = 0;
            int failed = 0;
            List<TestResult> failures = new List<TestResult>();

            foreach (TestResult r in _results)
            {
                if (r.Passed) passed++;
                else
                {
                    failed++;
                    failures.Add(r);
                }
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("TEST SUMMARY");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"  Total:   {_results.Count}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Passed:  {passed}");
            Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"  Failed:  {failed}");
            Console.ResetColor();
            Console.WriteLine($"  Runtime: {totalRuntimeMs:F1}ms");
            Console.WriteLine();

            if (failures.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED TESTS:");
                Console.ResetColor();

                foreach (TestResult f in failures)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("  FAIL ");
                    Console.ResetColor();
                    Console.WriteLine($" {f.TestName}");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"         {f.ErrorMessage}");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OVERALL: FAIL");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OVERALL: PASS");
            }

            Console.ResetColor();
        }
    }
}
