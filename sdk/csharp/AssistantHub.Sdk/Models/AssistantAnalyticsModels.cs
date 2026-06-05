namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Query options for assistant analytics endpoints.
    /// </summary>
    public class AssistantAnalyticsQuery
    {
        /// <summary>
        /// Named range identifier, for example lastHour, lastDay, lastWeek, or lastMonth.
        /// </summary>
        public string Range { get; set; } = "lastDay";

        /// <summary>
        /// Explicit UTC start time for a custom range.
        /// </summary>
        public DateTime? StartUtc { get; set; }

        /// <summary>
        /// Explicit UTC end time for a custom range.
        /// </summary>
        public DateTime? EndUtc { get; set; }

        /// <summary>
        /// Optional bucket size in seconds.
        /// </summary>
        public int? BucketSeconds { get; set; }

        /// <summary>
        /// Optional metrics to include in a time-series response.
        /// </summary>
        public List<string> Metrics { get; set; } = new List<string>();

        /// <summary>
        /// Optional performance stage filter.
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Optional endpoint identifier filter.
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Optional endpoint type filter.
        /// </summary>
        public string EndpointType { get; set; }

        /// <summary>
        /// Optional model filter.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Optional result limit for slowest-request responses.
        /// </summary>
        public int? Limit { get; set; }
    }
}
