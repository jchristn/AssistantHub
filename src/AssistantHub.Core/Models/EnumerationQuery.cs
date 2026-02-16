namespace AssistantHub.Core.Models
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Enumeration query.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentException("MaxResults must be between 1 and 1000.");
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Continuation token for pagination.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Ordering for results.
        /// </summary>
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Filter by assistant identifier.
        /// </summary>
        public string AssistantIdFilter { get; set; } = null;

        /// <summary>
        /// Filter by bucket name.
        /// </summary>
        public string BucketNameFilter { get; set; } = null;

        /// <summary>
        /// Filter by collection identifier.
        /// </summary>
        public string CollectionIdFilter { get; set; } = null;

        /// <summary>
        /// Filter by thread identifier.
        /// </summary>
        public string ThreadIdFilter { get; set; } = null;

        #endregion

        #region Private-Members

        private int _MaxResults = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EnumerationQuery()
        {
        }

        #endregion
    }
}
