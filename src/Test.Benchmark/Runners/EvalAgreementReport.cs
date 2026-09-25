namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// Agreement between AssistantHub's in-product Eval verdicts and the harness's independent judge on the same
    /// answers.
    /// </summary>
    public class EvalAgreementReport
    {
        #region Public-Members

        /// <summary>Report kind.</summary>
        public string Kind { get; set; } = "eval";

        /// <summary>Dataset name.</summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>Environment.</summary>
        public BenchmarkEnvironment Environment { get; set; } = new BenchmarkEnvironment();

        /// <summary>Run configuration.</summary>
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

        /// <summary>Eval run id.</summary>
        public string RunId { get; set; } = string.Empty;

        /// <summary>Summary: facts, in-product pass rate, judge pass rate, agreement, kappa.</summary>
        public Dictionary<string, double> Summary { get; set; } = new Dictionary<string, double>();

        /// <summary>Per-fact verdict pairs.</summary>
        public List<Dictionary<string, string>> Items { get; set; } = new List<Dictionary<string, string>>();

        #endregion
    }
}
