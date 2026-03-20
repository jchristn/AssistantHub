namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Generic identifier response payload for create operations that may return either Id or GUID.
    /// </summary>
    public class IdentifierResponse
    {
        /// <summary>
        /// String identifier.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Legacy GUID identifier.
        /// </summary>
        public string GUID { get; set; } = null;
    }
}
