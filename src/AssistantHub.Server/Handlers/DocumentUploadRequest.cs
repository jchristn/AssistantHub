namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    internal class DocumentUploadRequest
    {
        public string IngestionRuleId { get; set; } = null;
        public string Name { get; set; } = null;
        public string OriginalFilename { get; set; } = null;
        public string ContentType { get; set; } = null;
        public List<string> Labels { get; set; } = null;
        public Dictionary<string, string> Tags { get; set; } = null;
        public string Base64Content { get; set; } = null;
    }
}
