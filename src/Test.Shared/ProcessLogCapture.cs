namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Settings;
    using Voltaic;

    internal sealed class ProcessLogCapture : IDisposable
    {
        private readonly object _sync = new object();
        private readonly Queue<string> _recentLines = new Queue<string>();
        private readonly StreamWriter _writer;

        public ProcessLogCapture(string logFilePath)
        {
            LogFilePath = logFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            _writer = new StreamWriter(
                new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };
        }

        public string LogFilePath { get; }

        public void Append(string streamName, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            string entry = "[" + DateTime.UtcNow.ToString("O") + "] " + streamName + ": " + line;

            lock (_sync)
            {
                _writer.WriteLine(entry);
                _recentLines.Enqueue(entry);

                while (_recentLines.Count > 80)
                {
                    _recentLines.Dequeue();
                }
            }
        }

        public string GetRecentOutput()
        {
            lock (_sync)
            {
                return string.Join(Environment.NewLine, _recentLines);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _writer.Dispose();
            }
        }
    }
}
