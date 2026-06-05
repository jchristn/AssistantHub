namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Internal config holder for parsed endpoint JSON.
    /// </summary>
    internal class EndpointConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string TenantId { get; set; } = "default";
        public string Endpoint { get; set; } = string.Empty;
        public string ApiFormat { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public bool Active { get; set; }
        public bool HealthCheckEnabled { get; set; }
        public string? HealthCheckUrl { get; set; }
        public string HealthCheckMethod { get; set; } = "GET";
        public int HealthCheckIntervalMs { get; set; } = 30000;
        public int HealthCheckTimeoutMs { get; set; } = 10000;
        public int HealthCheckExpectedStatusCode { get; set; } = 200;
        public int HealthyThreshold { get; set; } = 2;
        public int UnhealthyThreshold { get; set; } = 2;
        public bool HealthCheckUseAuth { get; set; }
    }
}
