namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    internal sealed class RestResponse
    {
        public RestResponse(HttpStatusCode statusCode, string reasonPhrase, string body, bool isSuccess)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Body = body;
            IsSuccess = isSuccess;
        }

        public HttpStatusCode StatusCode { get; }
        public string ReasonPhrase { get; }
        public string Body { get; }
        public bool IsSuccess { get; }
    }
}
