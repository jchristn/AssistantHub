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

    internal sealed class ManagedProcess
    {
        public ManagedProcess(string displayName, Process process, ProcessLogCapture capture)
        {
            DisplayName = displayName;
            Process = process;
            Capture = capture;
        }

        public string DisplayName { get; }
        public Process Process { get; }
        public ProcessLogCapture Capture { get; }
    }
}
