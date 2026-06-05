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

    internal class MetricAggregate
    {
        public double? Value { get; set; } = null;
        public int SampleCount { get; set; } = 0;
        public int NullCount { get; set; } = 0;
    }
}
