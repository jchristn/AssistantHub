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

    internal class EndpointUsageAggregate
    {
        public ChatHistoryPerformanceEvent Event { get; set; } = null;
        public int Calls { get; set; } = 0;
    }
}
