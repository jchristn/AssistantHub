namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    /// <summary>
    /// Download response envelope.
    /// </summary>
    public class BinaryResponse
    {
        /// <summary>
        /// Bytes.
        /// </summary>
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Content type.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Content length.
        /// </summary>
        public long? ContentLength { get; set; }

        /// <summary>
        /// File name.
        /// </summary>
        public string? FileName { get; set; }
    }
}
