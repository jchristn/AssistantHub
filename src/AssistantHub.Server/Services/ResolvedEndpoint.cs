namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    internal struct ResolvedEndpoint
    {
        public string EndpointId;
        public Enums.InferenceProviderEnum Provider;
        public string Endpoint;
        public string ApiKey;
        public string Model;
        public int MaxConcurrentRequests;
        public bool SupportsToolCalling;
        public string ToolCallingApiFormat;
        public bool SupportsParallelToolCalls;
        public bool SupportsStreamingToolCalls;
    }
}
