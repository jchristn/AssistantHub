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

    internal class FeedbackAnalyticsRow
    {
        public string Id { get; set; } = null;
        public string Rating { get; set; } = null;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
