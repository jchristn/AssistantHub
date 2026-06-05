namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;

    internal class RequestAnalyticsRow
    {
        public string Id { get; set; } = null;
        public string TraceId { get; set; } = null;
        public string ChatHistoryId { get; set; } = null;
        public string TenantId { get; set; } = null;
        public string AssistantId { get; set; } = null;
        public string ThreadId { get; set; } = null;
        public string RequestType { get; set; } = null;
        public string SourceType { get; set; } = null;
        public string HttpMethod { get; set; } = null;
        public string RequestPath { get; set; } = null;
        public int StatusCode { get; set; } = 0;
        public bool Success { get; set; } = false;
        public double DurationMs { get; set; } = 0;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
