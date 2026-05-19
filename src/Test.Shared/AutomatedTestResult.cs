namespace Test.Automated
{
    using System;

    public class AutomatedTestResult
    {
        public string SuiteName { get; set; } = String.Empty;
        public string TestName { get; set; } = String.Empty;
        public bool Passed { get; set; } = false;
        public long ElapsedMilliseconds { get; set; } = 0;
        public string ErrorMessage { get; set; } = null;
        public string DisplayName
        {
            get { return TestName; }
        }
    }
}
