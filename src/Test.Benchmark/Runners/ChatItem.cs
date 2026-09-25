namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// One chat question, answer and its grading.
    /// </summary>
    public class ChatItem
    {
        #region Public-Members

        /// <summary>Corpus id.</summary>
        public string Corpus { get; set; } = string.Empty;

        /// <summary>Query id.</summary>
        public string QueryId { get; set; } = string.Empty;

        /// <summary>Repeat index (0-based).</summary>
        public int Repeat { get; set; } = 0;

        /// <summary>Query type.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Query category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Question text.</summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>Gold answer.</summary>
        public string Gold { get; set; } = string.Empty;

        /// <summary>Model answer.</summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>HTTP status.</summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>Error, if any.</summary>
        public string? Error { get; set; } = null;

        /// <summary>Client latency.</summary>
        public double LatencyMs { get; set; } = 0;

        /// <summary>Prompt tokens.</summary>
        public int PromptTokens { get; set; } = 0;

        /// <summary>Completion tokens.</summary>
        public int CompletionTokens { get; set; } = 0;

        /// <summary>True when the gold answer marks the question unanswerable.</summary>
        public bool Unanswerable { get; set; } = false;

        /// <summary>Relevant dataset document ids.</summary>
        public List<string> Relevant { get; set; } = new List<string>();

        /// <summary>Dataset document ids of the chunks injected into the prompt.</summary>
        public List<string> Retrieved { get; set; } = new List<string>();

        /// <summary>Dataset document ids the answer cited.</summary>
        public List<string> Cited { get; set; } = new List<string>();

        /// <summary>Whether the response carried citations.</summary>
        public bool CitationsPresent { get; set; } = false;

        /// <summary>Share of evidence passages present in the injected context.</summary>
        public double? ContextEvidence { get; set; } = null;

        /// <summary>Judge verdict: correct (answerable) or correctly declined (unanswerable). Null when unparseable.</summary>
        public bool? Correct { get; set; } = null;

        /// <summary>Judge verdict: every claim supported by the injected context. Null when not judged.</summary>
        public bool? Faithful { get; set; } = null;

        /// <summary>Answerability decision.</summary>
        public string? AnswerabilityDecision { get; set; } = null;

        #endregion
    }
}
