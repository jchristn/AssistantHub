namespace Test.Database
{
    using System;
    using System.Diagnostics;

    public class TestResult
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public double RuntimeMs { get; set; }
        public string ErrorMessage { get; set; }

        public TestResult(string testName, bool passed, double runtimeMs, string errorMessage = null)
        {
            TestName = testName;
            Passed = passed;
            RuntimeMs = runtimeMs;
            ErrorMessage = errorMessage;
        }
    }
}
