namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Analytics query filter.
    /// </summary>
    public class AssistantAnalyticsFilter
    {
        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = Constants.DefaultTenantId;

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Range identifier.
        /// </summary>
        public string Range { get; set; } = "lastDay";

        /// <summary>
        /// Explicit UTC start time.
        /// </summary>
        public DateTime? StartUtc { get; set; } = null;

        /// <summary>
        /// Explicit UTC end time.
        /// </summary>
        public DateTime? EndUtc { get; set; } = null;

        /// <summary>
        /// Explicit bucket width in seconds.
        /// </summary>
        public int? BucketSeconds { get; set; } = null;

        /// <summary>
        /// Optional metric names to return.
        /// </summary>
        public List<string> Metrics { get; set; } = new List<string>();

        /// <summary>
        /// Optional stage filter.
        /// </summary>
        public string Stage { get; set; } = null;

        /// <summary>
        /// Optional endpoint identifier filter.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Optional endpoint type filter.
        /// </summary>
        public string EndpointType { get; set; } = null;

        /// <summary>
        /// Optional model filter.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Result limit for ranked tables.
        /// </summary>
        public int Limit { get; set; } = 25;
    }
}
