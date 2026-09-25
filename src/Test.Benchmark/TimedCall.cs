namespace Test.Benchmark
{
    /// <summary>
    /// The outcome of one timed HTTP call.
    /// </summary>
    public class TimedCall
    {
        #region Public-Members

        /// <summary>HTTP status (0 on a transport failure).</summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>Response body.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Client-side elapsed milliseconds.</summary>
        public double ElapsedMs { get; set; } = 0.0;

        /// <summary>Transport error, if any.</summary>
        public string? Error { get; set; } = null;

        /// <summary>True for a 2xx status.</summary>
        public bool IsSuccess
        {
            get
            {
                return StatusCode >= 200 && StatusCode < 300;
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Short description for error messages.
        /// </summary>
        /// <returns>Status and a body excerpt.</returns>
        public string Describe()
        {
            string body = Body ?? string.Empty;
            if (body.Length > 300) body = body.Substring(0, 300) + "…";
            return StatusCode + " " + (Error ?? body).Replace("\n", " ").Replace("\r", " ");
        }

        #endregion
    }
}
