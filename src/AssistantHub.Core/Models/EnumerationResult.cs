namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Enumeration result.
    /// </summary>
    /// <typeparam name="T">Type of object contained in the result.</typeparam>
    public class EnumerationResult<T>
    {
        #region Public-Members

        /// <summary>
        /// Indicates whether or not the enumeration was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Maximum number of results requested.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set
            {
                if (value < 1) throw new ArgumentException("MaxResults must be greater than or equal to 1.");
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Total number of records available.
        /// </summary>
        public long TotalRecords
        {
            get => _TotalRecords;
            set
            {
                if (value < 0) throw new ArgumentException("TotalRecords must be greater than or equal to 0.");
                _TotalRecords = value;
            }
        }

        /// <summary>
        /// Number of records remaining.
        /// </summary>
        public long RecordsRemaining
        {
            get => _RecordsRemaining;
            set
            {
                if (value < 0) throw new ArgumentException("RecordsRemaining must be greater than or equal to 0.");
                _RecordsRemaining = value;
            }
        }

        /// <summary>
        /// Continuation token for retrieving the next page of results.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Indicates whether or not the end of results has been reached.
        /// </summary>
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// List of objects.
        /// </summary>
        [JsonPropertyOrder(999)]
        public List<T> Objects
        {
            get => _Objects;
            set => _Objects = value ?? new List<T>();
        }

        /// <summary>
        /// Total milliseconds elapsed.
        /// </summary>
        public double TotalMs { get; set; } = 0;

        #endregion

        #region Private-Members

        private int _MaxResults = 100;
        private long _TotalRecords = 0;
        private long _RecordsRemaining = 0;
        private List<T> _Objects = new List<T>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EnumerationResult()
        {
        }

        #endregion
    }
}
