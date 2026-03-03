namespace Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Mock HTTP message handler for stubbing external HTTP calls in tests.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<RequestRecord> _requests = new List<RequestRecord>();
        private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _handlers = new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>();
        private Func<HttpRequestMessage, HttpResponseMessage> _defaultHandler;

        /// <summary>
        /// All requests sent through this handler.
        /// </summary>
        public IReadOnlyList<RequestRecord> Requests => _requests;

        /// <summary>
        /// Register a response for a specific URL pattern.
        /// </summary>
        public MockHttpMessageHandler When(string urlContains, HttpStatusCode statusCode, string responseBody, string contentType = "application/json")
        {
            _handlers[urlContains] = (req) => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, contentType)
            };
            return this;
        }

        /// <summary>
        /// Register a custom handler for a specific URL pattern.
        /// </summary>
        public MockHttpMessageHandler When(string urlContains, Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handlers[urlContains] = handler;
            return this;
        }

        /// <summary>
        /// Set a default response for any unmatched URL.
        /// </summary>
        public MockHttpMessageHandler Default(HttpStatusCode statusCode, string responseBody = "{}", string contentType = "application/json")
        {
            _defaultHandler = (req) => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, contentType)
            };
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri?.ToString() ?? "";
            string body = null;
            if (request.Content != null)
            {
                body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            _requests.Add(new RequestRecord(request.Method, url, body, request.Headers));

            foreach (var kvp in _handlers)
            {
                if (url.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(kvp.Value(request));
                }
            }

            if (_defaultHandler != null)
            {
                return Task.FromResult(_defaultHandler(request));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"{{\"error\":\"No mock handler for {url}\"}}", Encoding.UTF8, "application/json")
            });
        }

        /// <summary>
        /// Create an HttpClient backed by this mock handler.
        /// </summary>
        public HttpClient CreateClient()
        {
            return new HttpClient(this);
        }

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
}
