namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Record of a request sent through this handler.
    /// </summary>
    public class RequestRecord
    {
        public HttpMethod Method { get; }
        public string Url { get; }
        public string Body { get; }
        public System.Net.Http.Headers.HttpRequestHeaders Headers { get; }

        public RequestRecord(HttpMethod method, string url, string body, System.Net.Http.Headers.HttpRequestHeaders headers)
        {
            Method = method;
            Url = url;
            Body = body;
            Headers = headers;
        }
    }
}
