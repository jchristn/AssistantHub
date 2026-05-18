namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using AssistantHub.Core;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.Routing;

    /// <summary>
    /// Builds the runtime OpenAPI document from the actual registered route surface.
    /// </summary>
    public class OpenApiDocumentService
    {
        private readonly Func<Webserver> _ServerFactory;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serverFactory">Server factory.</param>
        public OpenApiDocumentService(Func<Webserver> serverFactory)
        {
            _ServerFactory = serverFactory ?? throw new ArgumentNullException(nameof(serverFactory));
        }

        /// <summary>
        /// Build the OpenAPI document for the current request origin.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>OpenAPI document JSON.</returns>
        public string BuildDocument(HttpContextBase ctx)
        {
            Webserver server = _ServerFactory();
            if (server == null) throw new InvalidOperationException("Webserver not initialized.");

            string scheme = ctx.Connection?.IsEncrypted ?? false ? "https" : "http";
            string host = ctx.Request?.Headers?.Get("Host");
            if (String.IsNullOrEmpty(host)) host = "localhost";

            JsonObject root = new JsonObject
            {
                ["openapi"] = "3.0.3",
                ["info"] = new JsonObject
                {
                    ["title"] = "AssistantHub REST API",
                    ["description"] = "Runtime route surface for AssistantHub.",
                    ["version"] = Constants.ProductVersion
                },
                ["servers"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["url"] = scheme + "://" + host,
                        ["description"] = "Runtime server"
                    }
                },
                ["tags"] = BuildTags(),
                ["paths"] = new JsonObject(),
                ["components"] = new JsonObject
                {
                    ["securitySchemes"] = new JsonObject
                    {
                        ["BearerAuth"] = new JsonObject
                        {
                            ["type"] = "http",
                            ["scheme"] = "bearer"
                        }
                    }
                }
            };

            JsonObject paths = root["paths"]?.AsObject() ?? new JsonObject();
            AddStaticRoutes(paths, server.Routes.PreAuthentication.Static.GetAll(), false);
            AddParameterRoutes(paths, server.Routes.PreAuthentication.Parameter.GetAll(), false);
            AddStaticRoutes(paths, server.Routes.PostAuthentication.Static.GetAll(), true);
            AddParameterRoutes(paths, server.Routes.PostAuthentication.Parameter.GetAll(), true);

            root["paths"] = paths;
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private void AddStaticRoutes(JsonObject paths, IReadOnlyList<StaticRoute> routes, bool authenticated)
        {
            if (routes == null) return;
            foreach (StaticRoute route in routes)
            {
                if (route == null) continue;
                AddOperation(paths, route.Path, route.Method.ToString(), authenticated);
            }
        }

        private void AddParameterRoutes(JsonObject paths, IReadOnlyList<ParameterRoute> routes, bool authenticated)
        {
            if (routes == null) return;
            foreach (ParameterRoute route in routes)
            {
                if (route == null) continue;
                AddOperation(paths, route.Path, route.Method.ToString(), authenticated);
            }
        }

        private void AddOperation(JsonObject paths, string path, string method, bool authenticated)
        {
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(method)) return;
            if (String.Equals(path, "/v1.0/openapi.json", StringComparison.OrdinalIgnoreCase)) return;

            if (!(paths[path] is JsonObject pathItem))
            {
                pathItem = new JsonObject();
                paths[path] = pathItem;
            }

            string normalizedMethod = method.ToLowerInvariant();
            pathItem[normalizedMethod] = new JsonObject
            {
                ["tags"] = new JsonArray(GetTagForPath(path)),
                ["summary"] = BuildSummary(method, path),
                ["operationId"] = BuildOperationId(method, path),
                ["parameters"] = BuildPathParameters(path),
                ["security"] = authenticated
                    ? new JsonArray { new JsonObject { ["BearerAuth"] = new JsonArray() } }
                    : new JsonArray()
            };
        }

        private JsonArray BuildPathParameters(string path)
        {
            JsonArray parameters = new JsonArray();
            if (String.IsNullOrEmpty(path)) return parameters;

            int index = 0;
            while (index < path.Length)
            {
                int start = path.IndexOf('{', index);
                if (start < 0) break;
                int end = path.IndexOf('}', start + 1);
                if (end < 0) break;

                string name = path.Substring(start + 1, end - start - 1);
                parameters.Add(new JsonObject
                {
                    ["name"] = name,
                    ["in"] = "path",
                    ["required"] = true,
                    ["schema"] = new JsonObject
                    {
                        ["type"] = "string"
                    }
                });

                index = end + 1;
            }

            return parameters;
        }

        private JsonArray BuildTags()
        {
            return new JsonArray
            {
                BuildTag("Health", "Service health endpoints."),
                BuildTag("OpenAPI", "Runtime OpenAPI document."),
                BuildTag("Authentication", "Authentication routes."),
                BuildTag("Tenants", "Tenant management routes."),
                BuildTag("Users", "User management routes."),
                BuildTag("Credentials", "Credential management routes."),
                BuildTag("Buckets", "Bucket management routes."),
                BuildTag("Bucket Objects", "Bucket object routes."),
                BuildTag("Collections", "Collection management routes."),
                BuildTag("Collection Records", "Collection record routes."),
                BuildTag("Assistants", "Assistant management routes."),
                BuildTag("Assistant Settings", "Assistant settings routes."),
                BuildTag("Assistant Public APIs", "Public assistant API routes."),
                BuildTag("Ingestion Rules", "Ingestion rule routes."),
                BuildTag("Documents", "Document routes."),
                BuildTag("Feedback", "Feedback routes."),
                BuildTag("History", "Chat history routes."),
                BuildTag("Request History", "HTTP request-history routes."),
                BuildTag("Models", "Model management routes."),
                BuildTag("Configuration", "Configuration routes."),
                BuildTag("Crawlers", "Crawler and crawl operation routes."),
                BuildTag("Evaluation", "Evaluation routes."),
                BuildTag("Embedding Endpoints", "Embedding endpoint routes."),
                BuildTag("Completion Endpoints", "Completion endpoint routes."),
                BuildTag("Misc", "Miscellaneous routes.")
            };
        }

        private JsonObject BuildTag(string name, string description)
        {
            return new JsonObject
            {
                ["name"] = name,
                ["description"] = description
            };
        }

        private string GetTagForPath(string path)
        {
            if (path == "/") return "Health";
            if (String.Equals(path, "/openapi.json", StringComparison.OrdinalIgnoreCase)) return "OpenAPI";
            if (path.StartsWith("/v1.0/authenticate", StringComparison.OrdinalIgnoreCase)) return "Authentication";
            if (path.StartsWith("/v1.0/requesthistory", StringComparison.OrdinalIgnoreCase)) return "Request History";
            if (path.StartsWith("/v1.0/tenants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/users", StringComparison.OrdinalIgnoreCase)) return "Users";
            if (path.StartsWith("/v1.0/tenants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/credentials", StringComparison.OrdinalIgnoreCase)) return "Credentials";
            if (path.StartsWith("/v1.0/tenants", StringComparison.OrdinalIgnoreCase)) return "Tenants";
            if (path.StartsWith("/v1.0/buckets/", StringComparison.OrdinalIgnoreCase) && path.Contains("/objects", StringComparison.OrdinalIgnoreCase)) return "Bucket Objects";
            if (path.StartsWith("/v1.0/buckets", StringComparison.OrdinalIgnoreCase)) return "Buckets";
            if (path.StartsWith("/v1.0/collections/", StringComparison.OrdinalIgnoreCase) && path.Contains("/records", StringComparison.OrdinalIgnoreCase)) return "Collection Records";
            if (path.StartsWith("/v1.0/collections", StringComparison.OrdinalIgnoreCase)) return "Collections";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase)
                && (path.Contains("/public", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/chat", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/feedback", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/compact", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/generate", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/threads", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/labels/", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/tags/", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/documents/", StringComparison.OrdinalIgnoreCase)))
                return "Assistant Public APIs";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/settings", StringComparison.OrdinalIgnoreCase)) return "Assistant Settings";
            if (path.StartsWith("/v1.0/assistants", StringComparison.OrdinalIgnoreCase)) return "Assistants";
            if (path.StartsWith("/v1.0/ingestion-rules", StringComparison.OrdinalIgnoreCase)) return "Ingestion Rules";
            if (path.StartsWith("/v1.0/documents", StringComparison.OrdinalIgnoreCase)) return "Documents";
            if (path.StartsWith("/v1.0/feedback", StringComparison.OrdinalIgnoreCase)) return "Feedback";
            if (path.StartsWith("/v1.0/history", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/v1.0/threads", StringComparison.OrdinalIgnoreCase)) return "History";
            if (path.StartsWith("/v1.0/models", StringComparison.OrdinalIgnoreCase)) return "Models";
            if (path.StartsWith("/v1.0/configuration", StringComparison.OrdinalIgnoreCase)) return "Configuration";
            if (path.StartsWith("/v1.0/crawlplans", StringComparison.OrdinalIgnoreCase)) return "Crawlers";
            if (path.StartsWith("/v1.0/eval", StringComparison.OrdinalIgnoreCase)) return "Evaluation";
            if (path.StartsWith("/v1.0/endpoints/embedding", StringComparison.OrdinalIgnoreCase)) return "Embedding Endpoints";
            if (path.StartsWith("/v1.0/endpoints/completion", StringComparison.OrdinalIgnoreCase)) return "Completion Endpoints";
            return "Misc";
        }

        private string BuildSummary(string method, string path)
        {
            return method.ToUpperInvariant() + " " + path;
        }

        private string BuildOperationId(string method, string path)
        {
            string value = (method + "_" + path)
                .Replace("/", "_")
                .Replace("{", String.Empty)
                .Replace("}", String.Empty)
                .Replace("-", "_")
                .Replace(".", "_");

            while (value.Contains("__", StringComparison.Ordinal))
                value = value.Replace("__", "_", StringComparison.Ordinal);

            return value.Trim('_');
        }
    }
}
