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

    internal class StageDurationAggregate
    {
        public string Stage { get; set; } = null;
        public double TotalDuration { get; set; } = 0;
        public double AverageDuration { get; set; } = 0;
    }
}
