namespace AssistantHub.Core.Models
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Crawl schedule settings sub-object.
    /// </summary>
    public class CrawlScheduleSettings
    {
        #region Public-Members

        /// <summary>
        /// Schedule interval type.
        /// Default: Hours.
        /// </summary>
        public ScheduleIntervalEnum IntervalType { get; set; } = ScheduleIntervalEnum.Hours;

        /// <summary>
        /// Interval value.
        /// Default: 24. Clamped 1-10080.
        /// </summary>
        public int IntervalValue
        {
            get => _IntervalValue;
            set => _IntervalValue = Math.Clamp(value, 1, 10080);
        }

        #endregion

        #region Private-Members

        private int _IntervalValue = 24;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlScheduleSettings()
        {
        }

        #endregion
    }
}
