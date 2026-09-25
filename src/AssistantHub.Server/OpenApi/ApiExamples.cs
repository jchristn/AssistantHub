namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Shared, deterministic values for OpenAPI examples.
    /// </summary>
    public static class ApiExamples
    {
        /// <summary>
        /// Example creation timestamp.
        /// </summary>
        public static readonly DateTime Created = new DateTime(2026, 1, 15, 17, 30, 0, DateTimeKind.Utc);

        /// <summary>
        /// Example last-update timestamp.
        /// </summary>
        public static readonly DateTime Updated = new DateTime(2026, 1, 16, 9, 45, 0, DateTimeKind.Utc);

        /// <summary>Example tenant identifier.</summary>
        public const string TenantId = "ten_01JH3Z6Q9V4T8K2M5N7P1R3S6W";

        /// <summary>Example user identifier.</summary>
        public const string UserId = "usr_01JH3Z7A2B4C6D8E0F1G3H5J7K";

        /// <summary>Example credential identifier.</summary>
        public const string CredentialId = "cred_01JH3Z7M4N6P8Q0R2S4T6V8W0X";

        /// <summary>Example assistant identifier.</summary>
        public const string AssistantId = "asst_01JH3Z8B1C3D5E7F9G1H3J5K7M";

        /// <summary>Example document identifier.</summary>
        public const string DocumentId = "adoc_01JH3Z8Q2R4S6T8V0W2X4Y6Z8A";

        /// <summary>Example collection identifier.</summary>
        public const string CollectionId = "col_01JH3Z9C3D5E7F9G1H3J5K7M9N";

        /// <summary>Example thread identifier.</summary>
        public const string ThreadId = "thr_01JH3Z9R4S6T8V0W2X4Y6Z8A0B";

        /// <summary>
        /// Wrap objects in a single-page enumeration result.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="objects">Objects.</param>
        /// <returns>Enumeration result.</returns>
        public static EnumerationResult<T> Page<T>(params T[] objects)
        {
            List<T> list = new List<T>(objects ?? Array.Empty<T>());
            return new EnumerationResult<T>
            {
                Success = true,
                MaxResults = 100,
                TotalRecords = list.Count,
                RecordsRemaining = 0,
                EndOfResults = true,
                Objects = list,
                TotalMs = 4.21
            };
        }
    }
}
