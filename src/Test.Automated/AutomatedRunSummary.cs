namespace Test.Automated
{
    using System;
    using System.Collections.Generic;

    public class AutomatedRunSummary
    {
        public int TotalCount { get; set; } = 0;
        public int PassedCount { get; set; } = 0;
        public int FailedCount { get; set; } = 0;
        public TimeSpan TotalRuntime { get; set; } = TimeSpan.Zero;
        public IReadOnlyList<AutomatedTestResult> Results { get; set; } = Array.Empty<AutomatedTestResult>();
    }
}
