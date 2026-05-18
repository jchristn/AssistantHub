namespace AssistantHub.Core.Models
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Request-history search filter.
    /// </summary>
    public class RequestHistorySearchFilter
    {
        #region Public-Members

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set => _MaxResults = Math.Clamp(value, 1, 1000);
        }

        /// <summary>
        /// Continuation token.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Ordering.
        /// </summary>
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Request type filter.
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// HTTP method filter.
        /// </summary>
        public string HttpMethod { get; set; } = null;

        /// <summary>
        /// Path substring filter.
        /// </summary>
        public string PathContains { get; set; } = null;

        /// <summary>
        /// Status code filter.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Success filter.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Tenant identifier filter.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// User identifier filter.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Credential identifier filter.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Assistant identifier filter.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Thread identifier filter.
        /// </summary>
        public string ThreadId { get; set; } = null;

        /// <summary>
        /// Source-type filter.
        /// </summary>
        public string SourceType { get; set; } = null;

        /// <summary>
        /// Free-text search.
        /// </summary>
        public string SearchText { get; set; } = null;

        /// <summary>
        /// Start time bound.
        /// </summary>
        public DateTime? StartUtc { get; set; } = null;

        /// <summary>
        /// End time bound.
        /// </summary>
        public DateTime? EndUtc { get; set; } = null;

        /// <summary>
        /// Summary bucket width in minutes.
        /// </summary>
        public int BucketMinutes
        {
            get => _BucketMinutes;
            set => _BucketMinutes = Math.Clamp(value, 1, 1440);
        }

        #endregion

        #region Private-Members

        private int _MaxResults = 100;
        private int _BucketMinutes = 15;

        #endregion
    }
}
