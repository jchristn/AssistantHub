namespace Test.Common
{
    using System;
    using System.Threading;
    using SyslogLogging;

    /// <summary>
    /// Base class for test fixtures with common setup utilities.
    /// </summary>
    public class TestFixture
    {
        /// <summary>
        /// Logging module with console output suppressed.
        /// </summary>
        public LoggingModule Logging { get; }

        /// <summary>
        /// Cancellation token source for test timeout control.
        /// </summary>
        public CancellationTokenSource TokenSource { get; }

        /// <summary>
        /// Cancellation token derived from the token source.
        /// </summary>
        public CancellationToken Token => TokenSource.Token;

        /// <summary>
        /// Instantiate with default settings.
        /// </summary>
        /// <param name="timeoutMs">Test timeout in milliseconds (default 30 seconds).</param>
        public TestFixture(int timeoutMs = 30000)
        {
            Logging = new LoggingModule();
            Logging.Settings.EnableConsole = false;

            TokenSource = new CancellationTokenSource(timeoutMs);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            TokenSource?.Dispose();
        }
    }
}
