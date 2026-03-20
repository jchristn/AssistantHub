namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed representation of a Partio enumeration response envelope.
    /// </summary>
    /// <typeparam name="T">Envelope data type.</typeparam>
    public class PartioEnumerationEnvelope<T>
    {
        /// <summary>
        /// Enumerated objects.
        /// </summary>
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Total count.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Whether more results remain.
        /// </summary>
        public bool HasMore { get; set; } = false;
    }
}
