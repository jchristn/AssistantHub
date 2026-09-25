namespace Test.Shared
{
    /// <summary>
    /// A route registration parsed from AssistantHubServer.cs, with the OpenAPI metadata member it passes (if any).
    /// </summary>
    public sealed class RouteAnnotation
    {
        /// <summary>
        /// HTTP method, upper case.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Route path template.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Docs class name in AssistantHub.Server.OpenApi, or null when the route passes no metadata.
        /// </summary>
        public string DocsClass { get; set; }

        /// <summary>
        /// Docs member name, or null when the route passes no metadata.
        /// </summary>
        public string DocsMember { get; set; }

        /// <summary>
        /// Route key in the form "METHOD /path".
        /// </summary>
        public string Route => Method + " " + Path;
    }
}
