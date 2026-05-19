namespace Test.Automated
{
    using System;

    public static class AutomatedTestReporter
    {
        public static Action<AutomatedTestResult> ResultRecorded { get; set; } = null;
    }
}
