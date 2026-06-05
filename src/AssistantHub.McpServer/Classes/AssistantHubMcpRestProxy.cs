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
    /// Minimal authenticated REST bridge for MCP operations.
    /// </summary>
    internal static class AssistantHubMcpRestProxy
    {
        private static readonly HttpClient _Http = new HttpClient();

        /// <summary>
        /// Send JSON and return the body.
        /// </summary>
        public static string SendJson(AssistantHubMcpContext context, HttpMethod method, string pathAndQuery, string? jsonBody = null)
        {
            return Send(context, method, pathAndQuery, CreateJsonContent(jsonBody), throwOnError: true).Body;
        }

        /// <summary>
        /// Send JSON and return the body or null on not found.
        /// </summary>
        public static string SendJsonOrNullOnNotFound(AssistantHubMcpContext context, HttpMethod method, string pathAndQuery, string? jsonBody = null)
        {
            RestResponse response = Send(context, method, pathAndQuery, CreateJsonContent(jsonBody), throwOnError: false);
            if (response.IsSuccess)
                return response.Body;
            if (response.StatusCode == HttpStatusCode.NotFound)
                return "null";

            throw BuildException("AssistantHub", response);
        }

        /// <summary>
        /// Send a binary request.
        /// </summary>
        public static string SendBinary(AssistantHubMcpContext context, HttpMethod method, string pathAndQuery, byte[] bytes, string contentType)
        {
            using ByteArrayContent content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            return Send(context, method, pathAndQuery, content, throwOnError: true).Body;
        }

        /// <summary>
        /// Download binary content.
        /// </summary>
        public static BinaryResponse Download(AssistantHubMcpContext context, string pathAndQuery)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(pathAndQuery))
                throw new ArgumentNullException(nameof(pathAndQuery));

            string url = BuildUrl(context, pathAndQuery);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyHeaders(context, request);

            using HttpResponseMessage response = _Http.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw BuildException("AssistantHub", new RestResponse(response.StatusCode, response.ReasonPhrase ?? string.Empty, errorBody, false));
            }

            byte[] data = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            string? contentType = response.Content.Headers.ContentType?.MediaType;
            long? contentLength = response.Content.Headers.ContentLength;
            string? filename = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName;

            return new BinaryResponse
            {
                Bytes = data,
                ContentType = contentType,
                ContentLength = contentLength,
                FileName = filename?.Trim('"')
            };
        }

        /// <summary>
        /// Check whether a resource exists using HEAD.
        /// </summary>
        public static bool HeadExists(AssistantHubMcpContext context, string pathAndQuery)
        {
            RestResponse response = Send(context, HttpMethod.Head, pathAndQuery, null, throwOnError: false);
            if (response.IsSuccess)
                return true;
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            throw BuildException("AssistantHub", response);
        }

        /// <summary>
        /// URL escape a string.
        /// </summary>
        public static string Escape(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return Uri.EscapeDataString(value);
        }

        private static StringContent? CreateJsonContent(string? jsonBody)
        {
            if (jsonBody == null)
                return null;
            return new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        private static RestResponse Send(AssistantHubMcpContext context, HttpMethod method, string pathAndQuery, HttpContent? content, bool throwOnError)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(pathAndQuery))
                throw new ArgumentNullException(nameof(pathAndQuery));

            string url = BuildUrl(context, pathAndQuery);

            using HttpRequestMessage request = new HttpRequestMessage(method, url);
            ApplyHeaders(context, request);
            if (content != null)
            {
                request.Content = content;
            }

            using HttpResponseMessage response = _Http.Send(request);
            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode && throwOnError)
                throw BuildException("AssistantHub", new RestResponse(response.StatusCode, response.ReasonPhrase ?? string.Empty, body, false));

            return new RestResponse(response.StatusCode, response.ReasonPhrase ?? string.Empty, body, response.IsSuccessStatusCode);
        }

        private static string BuildUrl(AssistantHubMcpContext context, string pathAndQuery)
        {
            return context.Settings.AssistantHub.Endpoint.TrimEnd('/') + "/" + pathAndQuery.TrimStart('/');
        }

        private static void ApplyHeaders(AssistantHubMcpContext context, HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(context.Settings.AssistantHub.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.Settings.AssistantHub.ApiKey);
            }
        }

        private static InvalidOperationException BuildException(string name, RestResponse response)
        {
            return new InvalidOperationException(
                name
                + " endpoint returned "
                + (int)response.StatusCode
                + " "
                + response.ReasonPhrase
                + ": "
                + response.Body);
        }
    }
}
