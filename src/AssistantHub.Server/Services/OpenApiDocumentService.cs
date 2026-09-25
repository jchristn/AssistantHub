namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using AssistantHub.Core;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Server.OpenApi;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;
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
                            ["scheme"] = "bearer",
                            ["bearerFormat"] = "Admin API key or bearer token",
                            ["description"] = "Enter an AssistantHub admin API key or bearer token. Swagger sends it as Authorization: Bearer <token>."
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

            // Named models referenced by route metadata are emitted once and referenced with $ref.
            if (ApiSchema.Components.Count > 0)
            {
                JsonObject schemas = new JsonObject();
                foreach (KeyValuePair<string, OpenApiSchemaMetadata> component in ApiSchema.Components.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    schemas[component.Key] = SchemaToJson(component.Value);

                root["components"]!["schemas"] = schemas;
            }

            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private void AddStaticRoutes(JsonObject paths, IReadOnlyList<StaticRoute> routes, bool authenticated)
        {
            if (routes == null) return;
            foreach (StaticRoute route in routes)
            {
                if (route == null) continue;
                AddOperation(paths, route.Path, route.Method.ToString(), authenticated, route.OpenApiMetadata);
            }
        }

        private void AddParameterRoutes(JsonObject paths, IReadOnlyList<ParameterRoute> routes, bool authenticated)
        {
            if (routes == null) return;
            foreach (ParameterRoute route in routes)
            {
                if (route == null) continue;
                AddOperation(paths, route.Path, route.Method.ToString(), authenticated, route.OpenApiMetadata);
            }
        }

        private void AddOperation(JsonObject paths, string path, string method, bool authenticated, OpenApiRouteMetadata metadata)
        {
            if (metadata != null)
            {
                AddDocumentedOperation(paths, path, method, authenticated, metadata);
                return;
            }

            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(method)) return;
            string normalizedPath = NormalizeRoutePath(path);

            if (!(paths[normalizedPath] is JsonObject pathItem))
            {
                pathItem = new JsonObject();
                paths[normalizedPath] = pathItem;
            }

            string normalizedMethod = method.ToLowerInvariant();
            JsonObject operation = new JsonObject
            {
                ["tags"] = new JsonArray(GetTagForPath(normalizedPath)),
                ["summary"] = BuildSummary(method, normalizedPath),
                ["operationId"] = BuildOperationId(method, normalizedPath),
                ["parameters"] = BuildOperationParameters(normalizedPath),
                ["security"] = authenticated
                    ? new JsonArray { new JsonObject { ["BearerAuth"] = new JsonArray() } }
                    : new JsonArray(),
                ["responses"] = String.Equals(normalizedPath, "/swagger", StringComparison.OrdinalIgnoreCase)
                    ? BuildSwaggerResponses()
                    : BuildGenericResponses(method, normalizedPath)
            };

            if (MethodUsuallyHasJsonBody(normalizedMethod))
                operation["requestBody"] = BuildGenericJsonRequestBody(normalizedPath);

            pathItem[normalizedMethod] = operation;
        }

        private void AddDocumentedOperation(JsonObject paths, string path, string method, bool authenticated, OpenApiRouteMetadata metadata)
        {
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(method)) return;
            string normalizedPath = NormalizeRoutePath(path);

            if (!(paths[normalizedPath] is JsonObject pathItem))
            {
                pathItem = new JsonObject();
                paths[normalizedPath] = pathItem;
            }

            JsonArray tags = new JsonArray();
            if (metadata.Tags != null && metadata.Tags.Count > 0)
            {
                foreach (string tag in metadata.Tags) tags.Add(tag);
            }
            else
            {
                tags.Add(GetTagForPath(normalizedPath));
            }

            JsonObject operation = new JsonObject
            {
                ["tags"] = tags,
                ["summary"] = String.IsNullOrEmpty(metadata.Summary) ? BuildSummary(method, normalizedPath) : metadata.Summary
            };

            if (!String.IsNullOrEmpty(metadata.Description)) operation["description"] = metadata.Description;
            operation["operationId"] = String.IsNullOrEmpty(metadata.OperationId) ? BuildOperationId(method, normalizedPath) : metadata.OperationId;
            if (metadata.Deprecated) operation["deprecated"] = true;

            // Path parameters come from the route template; documented parameters replace generated ones with the same name and location.
            JsonArray generated = BuildOperationParameters(normalizedPath);
            JsonArray parameters = new JsonArray();
            HashSet<string> documented = new HashSet<string>(StringComparer.Ordinal);
            if (metadata.Parameters != null)
            {
                foreach (OpenApiParameterMetadata parameter in metadata.Parameters)
                    documented.Add(parameter.Name + "|" + ParameterLocationName(parameter.In));
            }

            foreach (JsonNode node in generated)
            {
                if (!(node is JsonObject parameter)) continue;
                string key = parameter["name"]?.GetValue<string>() + "|" + parameter["in"]?.GetValue<string>();
                if (documented.Contains(key)) continue;
                parameters.Add(parameter.DeepClone());
            }

            if (metadata.Parameters != null)
            {
                foreach (OpenApiParameterMetadata parameter in metadata.Parameters)
                    parameters.Add(ParameterToJson(parameter));
            }

            operation["parameters"] = parameters;
            operation["security"] = authenticated
                ? new JsonArray { new JsonObject { ["BearerAuth"] = new JsonArray() } }
                : new JsonArray();

            if (metadata.RequestBody != null)
                operation["requestBody"] = RequestBodyToJson(metadata.RequestBody);

            JsonObject responses = new JsonObject();
            if (metadata.Responses != null)
            {
                foreach (KeyValuePair<string, OpenApiResponseMetadata> response in metadata.Responses.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    responses[response.Key] = ResponseToJson(response.Value);
            }

            if (authenticated && responses["401"] == null)
                responses["401"] = new JsonObject { ["description"] = "Authentication failed." };
            if (responses["500"] == null)
                responses["500"] = new JsonObject { ["description"] = "Internal server error." };

            operation["responses"] = responses;
            pathItem[method.ToLowerInvariant()] = operation;
        }

        private static string ParameterLocationName(ParameterLocation location)
        {
            return location.ToString().ToLowerInvariant();
        }

        private static JsonObject ParameterToJson(OpenApiParameterMetadata parameter)
        {
            JsonObject json = new JsonObject
            {
                ["name"] = parameter.Name,
                ["in"] = ParameterLocationName(parameter.In),
                ["required"] = parameter.In == ParameterLocation.Path || parameter.Required
            };

            if (!String.IsNullOrEmpty(parameter.Description)) json["description"] = parameter.Description;
            if (parameter.Deprecated) json["deprecated"] = true;
            json["schema"] = SchemaToJson(parameter.Schema ?? new OpenApiSchemaMetadata { Type = "string" });
            if (parameter.Example != null) json["example"] = ValueToJson(parameter.Example);
            return json;
        }

        private static JsonObject RequestBodyToJson(OpenApiRequestBodyMetadata body)
        {
            JsonObject json = new JsonObject { ["required"] = body.Required };
            if (!String.IsNullOrEmpty(body.Description)) json["description"] = body.Description;
            json["content"] = ContentToJson(body.Content);
            return json;
        }

        private static JsonObject ResponseToJson(OpenApiResponseMetadata response)
        {
            JsonObject json = new JsonObject { ["description"] = response.Description ?? String.Empty };
            if (response.Content != null && response.Content.Count > 0) json["content"] = ContentToJson(response.Content);

            if (response.Headers != null && response.Headers.Count > 0)
            {
                JsonObject headers = new JsonObject();
                foreach (KeyValuePair<string, OpenApiHeaderMetadata> header in response.Headers)
                {
                    JsonObject headerJson = new JsonObject();
                    if (!String.IsNullOrEmpty(header.Value.Description)) headerJson["description"] = header.Value.Description;
                    if (header.Value.Required) headerJson["required"] = true;
                    headerJson["schema"] = SchemaToJson(header.Value.Schema ?? new OpenApiSchemaMetadata { Type = "string" });
                    headers[header.Key] = headerJson;
                }

                json["headers"] = headers;
            }

            return json;
        }

        private static JsonObject ContentToJson(Dictionary<string, OpenApiMediaTypeMetadata> content)
        {
            JsonObject json = new JsonObject();
            if (content == null) return json;

            foreach (KeyValuePair<string, OpenApiMediaTypeMetadata> media in content)
            {
                JsonObject mediaJson = new JsonObject();
                if (media.Value.Schema != null) mediaJson["schema"] = SchemaToJson(media.Value.Schema);
                if (media.Value.Example != null) mediaJson["example"] = ValueToJson(media.Value.Example);

                if (media.Value.Examples != null && media.Value.Examples.Count > 0)
                {
                    JsonObject examples = new JsonObject();
                    foreach (KeyValuePair<string, OpenApiExampleMetadata> example in media.Value.Examples)
                    {
                        JsonObject exampleJson = new JsonObject();
                        if (!String.IsNullOrEmpty(example.Value.Summary)) exampleJson["summary"] = example.Value.Summary;
                        if (!String.IsNullOrEmpty(example.Value.Description)) exampleJson["description"] = example.Value.Description;
                        if (example.Value.Value != null) exampleJson["value"] = ValueToJson(example.Value.Value);
                        examples[example.Key] = exampleJson;
                    }

                    mediaJson["examples"] = examples;
                }

                json[media.Key] = mediaJson;
            }

            return json;
        }

        private static JsonObject SchemaToJson(OpenApiSchemaMetadata schema)
        {
            JsonObject json = new JsonObject();
            if (schema == null) return json;

            if (!String.IsNullOrEmpty(schema.Ref))
            {
                json["$ref"] = schema.Ref;
                return json;
            }

            if (!String.IsNullOrEmpty(schema.Type)) json["type"] = schema.Type;
            if (!String.IsNullOrEmpty(schema.Format)) json["format"] = schema.Format;
            if (!String.IsNullOrEmpty(schema.Description)) json["description"] = schema.Description;
            if (schema.Nullable) json["nullable"] = true;

            if (schema.Enum != null && schema.Enum.Count > 0)
            {
                JsonArray values = new JsonArray();
                foreach (object value in schema.Enum) values.Add(ValueToJson(value));
                json["enum"] = values;
            }

            if (schema.Items != null) json["items"] = SchemaToJson(schema.Items);

            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                JsonObject properties = new JsonObject();
                foreach (KeyValuePair<string, OpenApiSchemaMetadata> property in schema.Properties)
                    properties[property.Key] = SchemaToJson(property.Value);
                json["properties"] = properties;
            }

            if (schema.Required != null && schema.Required.Count > 0)
            {
                JsonArray required = new JsonArray();
                foreach (string name in schema.Required) required.Add(name);
                json["required"] = required;
            }

            if (schema.OneOf != null && schema.OneOf.Count > 0)
            {
                JsonArray oneOf = new JsonArray();
                foreach (OpenApiSchemaMetadata option in schema.OneOf) oneOf.Add(SchemaToJson(option));
                json["oneOf"] = oneOf;
            }

            if (schema.Minimum.HasValue) json["minimum"] = schema.Minimum.Value;
            if (schema.Maximum.HasValue) json["maximum"] = schema.Maximum.Value;
            if (schema.MinLength.HasValue) json["minLength"] = schema.MinLength.Value;
            if (schema.MaxLength.HasValue) json["maxLength"] = schema.MaxLength.Value;
            if (!String.IsNullOrEmpty(schema.Pattern)) json["pattern"] = schema.Pattern;
            if (schema.Default != null) json["default"] = ValueToJson(schema.Default);
            if (schema.Example != null) json["example"] = ValueToJson(schema.Example);
            return json;
        }

        private static JsonNode ValueToJson(object value)
        {
            if (value == null) return null;
            if (value is JsonNode node) return node.DeepClone();
            if (value is string text) return JsonValue.Create(text);
            return JsonNode.Parse(Serializer.SerializeJson(value, false));
        }

        private string NormalizeRoutePath(string path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            string normalized = path;
            while (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal))
                normalized = normalized.Substring(0, normalized.Length - 1);

            return normalized;
        }

        private JsonObject BuildSwaggerResponses()
        {
            JsonObject responses = new JsonObject
            {
                ["200"] = new JsonObject
                {
                    ["description"] = "Swagger UI HTML page.",
                    ["content"] = new JsonObject
                    {
                        ["text/html"] = new JsonObject
                        {
                            ["schema"] = new JsonObject
                            {
                                ["type"] = "string"
                            }
                        }
                    }
                }
            };

            responses["500"] = new JsonObject { ["description"] = "Internal server error." };
            return responses;
        }

        private JsonObject BuildGenericResponses(string method, string path)
        {
            string normalizedMethod = method?.ToUpperInvariant() ?? "GET";
            JsonObject responses = new JsonObject();
            if (normalizedMethod == "DELETE" && String.Equals(path, "/v1.0/assistants/{assistantId}/tool-calls", StringComparison.OrdinalIgnoreCase))
                responses["200"] = new JsonObject
                {
                    ["description"] = "Deleted matching assistant tool-call traces.",
                    ["content"] = new JsonObject
                    {
                        ["application/json"] = new JsonObject
                        {
                            ["schema"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject
                                {
                                    ["DeletedCount"] = new JsonObject
                                    {
                                        ["type"] = "integer",
                                        ["description"] = "Number of deleted trace records."
                                    }
                                }
                            }
                        }
                    }
                };
            else if (normalizedMethod == "DELETE")
                responses["204"] = new JsonObject { ["description"] = "Deleted or completed with no response body." };
            else if (normalizedMethod == "HEAD")
                responses["200"] = new JsonObject { ["description"] = "Resource exists." };
            else
                responses["200"] = new JsonObject
                {
                    ["description"] = "Successful response.",
                    ["content"] = new JsonObject
                    {
                        ["application/json"] = new JsonObject
                        {
                            ["schema"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = true
                            }
                        }
                    }
                };

            responses["400"] = new JsonObject { ["description"] = "Bad request." };
            responses["401"] = new JsonObject { ["description"] = "Authentication failed." };
            responses["403"] = new JsonObject { ["description"] = "Authorization failed." };
            responses["404"] = new JsonObject { ["description"] = "Resource not found." };
            responses["500"] = new JsonObject { ["description"] = "Internal server error." };
            return responses;
        }

        private bool MethodUsuallyHasJsonBody(string normalizedMethod)
        {
            return String.Equals(normalizedMethod, "post", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalizedMethod, "put", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalizedMethod, "patch", StringComparison.OrdinalIgnoreCase);
        }

        private JsonObject BuildGenericJsonRequestBody(string path)
        {
            bool required = !path.EndsWith("/reindex", StringComparison.OrdinalIgnoreCase);

            if (String.Equals(path, "/v1.0/indices/{indexId}/search", StringComparison.OrdinalIgnoreCase))
                return BuildIndexSearchRequestBody(required);

            return new JsonObject
            {
                ["required"] = required,
                ["description"] = "JSON request payload for " + path + ". Pass-through proxy routes preserve the subordinate service schema.",
                ["content"] = new JsonObject
                {
                    ["application/json"] = new JsonObject
                    {
                        ["schema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = true
                        }
                    }
                }
            };
        }

        private JsonObject BuildIndexSearchRequestBody(bool required)
        {
            return new JsonObject
            {
                ["required"] = required,
                ["description"] = "Verbex index search request. AssistantHub proxies the request to Verbex and supports opt-in enriched result fields.",
                ["content"] = new JsonObject
                {
                    ["application/json"] = new JsonObject
                    {
                        ["schema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = true,
                            ["required"] = new JsonArray { "Query" },
                            ["properties"] = new JsonObject
                            {
                                ["Query"] = new JsonObject { ["type"] = "string", ["description"] = "TF-IDF query text. Use * to browse all records." },
                                ["MaxResults"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum number of results to return.", ["default"] = 100 },
                                ["UseAndLogic"] = new JsonObject { ["type"] = "boolean", ["description"] = "When true, all query terms must match.", ["default"] = false },
                                ["Labels"] = new JsonObject
                                {
                                    ["type"] = "array",
                                    ["description"] = "Required labels. Verbex applies AND logic.",
                                    ["items"] = new JsonObject { ["type"] = "string" }
                                },
                                ["Tags"] = new JsonObject { ["type"] = "object", ["description"] = "Required key/value tag filters. Verbex applies AND logic." },
                                ["IncludeMatchedTerms"] = new JsonObject { ["type"] = "boolean", ["description"] = "Include MatchedTerms on each result.", ["default"] = false },
                                ["IncludeTermDetails"] = new JsonObject { ["type"] = "boolean", ["description"] = "Include per-term score/frequency details and MatchedTerms on each result.", ["default"] = false },
                                ["IncludeDocumentTermStats"] = new JsonObject { ["type"] = "boolean", ["description"] = "Include whole-document UniqueTermCount and TotalTermOccurrences. Adds one grouped Verbex query.", ["default"] = false }
                            }
                        }
                    }
                }
            };
        }

        private JsonArray BuildOperationParameters(string path)
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

            if (path.Contains("/analytics/", StringComparison.OrdinalIgnoreCase))
                AddAnalyticsQueryParameters(parameters, path);

            if (String.Equals(path, "/v1.0/documents", StringComparison.OrdinalIgnoreCase)
                || String.Equals(path, "/v1.0/documents/reindex", StringComparison.OrdinalIgnoreCase))
                AddDocumentEnumerationQueryParameters(parameters);

            if (String.Equals(path, "/v1.0/assistants/{assistantId}/documents", StringComparison.OrdinalIgnoreCase))
                AddAssistantDocumentEnumerationQueryParameters(parameters);

            if (String.Equals(path, "/v1.0/assistants/{assistantId}/tool-calls", StringComparison.OrdinalIgnoreCase))
                AddAssistantToolCallQueryParameters(parameters);

            return parameters;
        }

        private void AddDocumentEnumerationQueryParameters(JsonArray parameters)
        {
            AddQueryParameter(parameters, "maxResults", "integer", "Maximum number of documents to process or return.");
            AddQueryParameter(parameters, "continuationToken", "string", "Continuation token for the next page.");
            AddQueryParameter(parameters, "bucketName", "string", "Optional bucket-name filter.");
            AddQueryParameter(parameters, "collectionId", "string", "Optional collection identifier filter.");
        }

        private void AddAssistantDocumentEnumerationQueryParameters(JsonArray parameters)
        {
            AddQueryParameter(parameters, "maxResults", "integer", "Maximum number of selectable documents to return.");
            AddQueryParameter(parameters, "continuationToken", "string", "Continuation token from a previous page.");
            AddQueryParameter(parameters, "query", "string", "Optional case-insensitive name or filename filter.");
            AddQueryParameter(parameters, "contentType", "string", "Optional MIME content type filter, such as application/pdf or text/*.");
        }

        private void AddAssistantToolCallQueryParameters(JsonArray parameters)
        {
            AddQueryParameter(parameters, "maxResults", "integer", "Maximum number of trace records to return.");
            AddQueryParameter(parameters, "continuationToken", "string", "Continuation token from a previous page.");
            AddQueryParameter(parameters, "ordering", "string", "CreatedAscending or CreatedDescending.");
            AddQueryParameter(parameters, "toolName", "string", "Filter by tool name.");
            AddQueryParameter(parameters, "traceId", "string", "Filter by trace identifier.");
            AddQueryParameter(parameters, "requestHistoryId", "string", "Filter by request-history identifier.");
            AddQueryParameter(parameters, "chatHistoryId", "string", "Filter by chat-history identifier.");
            AddQueryParameter(parameters, "threadId", "string", "Filter by assistant thread identifier.");
            AddQueryParameter(parameters, "success", "boolean", "Filter by success flag.");
            AddQueryParameter(parameters, "denied", "boolean", "Filter by denial flag.");
            AddQueryParameter(parameters, "startUtc", "string", "Filter to records created at or after this UTC timestamp.");
            AddQueryParameter(parameters, "endUtc", "string", "Filter to records created at or before this UTC timestamp.");
        }

        private void AddAnalyticsQueryParameters(JsonArray parameters, string path)
        {
            AddQueryParameter(parameters, "range", "string", "Range ID: lastHour, lastDay, lastWeek, or lastMonth.");
            AddQueryParameter(parameters, "startUtc", "string", "Explicit UTC start time.");
            AddQueryParameter(parameters, "endUtc", "string", "Explicit UTC end time.");
            AddQueryParameter(parameters, "bucketSeconds", "integer", "Explicit bucket width in seconds.");

            if (path.Contains("/timeseries", StringComparison.OrdinalIgnoreCase))
            {
                AddQueryParameter(parameters, "metrics", "string", "Comma-separated metric names.");
                AddQueryParameter(parameters, "stage", "string", "Optional stage filter.");
                AddQueryParameter(parameters, "endpointId", "string", "Optional endpoint ID filter.");
                AddQueryParameter(parameters, "model", "string", "Optional model filter.");
            }

            if (path.Contains("/stages", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/endpoints", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/slowest", StringComparison.OrdinalIgnoreCase))
            {
                AddQueryParameter(parameters, "stage", "string", "Optional stage filter.");
                AddQueryParameter(parameters, "endpointId", "string", "Optional endpoint ID filter.");
                AddQueryParameter(parameters, "endpointType", "string", "Optional endpoint type filter.");
                AddQueryParameter(parameters, "model", "string", "Optional model filter.");
            }

            if (path.Contains("/endpoints", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/slowest", StringComparison.OrdinalIgnoreCase))
                AddQueryParameter(parameters, "limit", "integer", "Maximum ranked rows to return.");
        }

        private void AddQueryParameter(JsonArray parameters, string name, string type, string description)
        {
            parameters.Add(new JsonObject
            {
                ["name"] = name,
                ["in"] = "query",
                ["required"] = false,
                ["description"] = description,
                ["schema"] = new JsonObject
                {
                    ["type"] = type
                }
            });
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
                BuildTag("Collection Search", "RecallDB collection search routes."),
                BuildTag("Indices", "Verbex inverted index routes."),
                BuildTag("Index Records", "Verbex inverted index record routes."),
                BuildTag("Index Search", "Verbex inverted index search routes."),
                BuildTag("Assistants", "Assistant management routes."),
                BuildTag("Assistant Settings", "Assistant settings routes."),
                BuildTag("Assistant Analytics", "Assistant performance analytics routes."),
                BuildTag("Assistant Tool Calls", "Assistant tool-call trace routes."),
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
            if (String.Equals(path, "/openapi.json", StringComparison.OrdinalIgnoreCase)
                || String.Equals(path, "/v1.0/openapi.json", StringComparison.OrdinalIgnoreCase)
                || String.Equals(path, "/swagger", StringComparison.OrdinalIgnoreCase)) return "OpenAPI";
            if (path.StartsWith("/v1.0/authenticate", StringComparison.OrdinalIgnoreCase)) return "Authentication";
            if (path.StartsWith("/v1.0/requesthistory", StringComparison.OrdinalIgnoreCase)) return "Request History";
            if (path.StartsWith("/v1.0/tenants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/users", StringComparison.OrdinalIgnoreCase)) return "Users";
            if (path.StartsWith("/v1.0/tenants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/credentials", StringComparison.OrdinalIgnoreCase)) return "Credentials";
            if (path.StartsWith("/v1.0/tenants", StringComparison.OrdinalIgnoreCase)) return "Tenants";
            if (path.StartsWith("/v1.0/buckets/", StringComparison.OrdinalIgnoreCase) && path.Contains("/objects", StringComparison.OrdinalIgnoreCase)) return "Bucket Objects";
            if (path.StartsWith("/v1.0/buckets", StringComparison.OrdinalIgnoreCase)) return "Buckets";
            if (path.StartsWith("/v1.0/collections/", StringComparison.OrdinalIgnoreCase) && path.Contains("/search", StringComparison.OrdinalIgnoreCase)) return "Collection Search";
            if (path.StartsWith("/v1.0/collections/", StringComparison.OrdinalIgnoreCase) && path.Contains("/records", StringComparison.OrdinalIgnoreCase)) return "Collection Records";
            if (path.StartsWith("/v1.0/collections", StringComparison.OrdinalIgnoreCase)) return "Collections";
            if (path.StartsWith("/v1.0/indices/", StringComparison.OrdinalIgnoreCase) && path.Contains("/records", StringComparison.OrdinalIgnoreCase)) return "Index Records";
            if (path.StartsWith("/v1.0/indices/", StringComparison.OrdinalIgnoreCase) && path.Contains("/search", StringComparison.OrdinalIgnoreCase)) return "Index Search";
            if (path.StartsWith("/v1.0/indices", StringComparison.OrdinalIgnoreCase)) return "Indices";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase)
                && (path.Contains("/public", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/chat", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/feedback", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/compact", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/generate", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/threads", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/labels/", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/tags/", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("/documents", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/documents/", StringComparison.OrdinalIgnoreCase)))
                return "Assistant Public APIs";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/settings", StringComparison.OrdinalIgnoreCase)) return "Assistant Settings";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/analytics", StringComparison.OrdinalIgnoreCase)) return "Assistant Analytics";
            if (path.StartsWith("/v1.0/assistants/", StringComparison.OrdinalIgnoreCase) && path.Contains("/tool-calls", StringComparison.OrdinalIgnoreCase)) return "Assistant Tool Calls";
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
            if (String.Equals(path, "/v1.0/configuration/external-search/status", StringComparison.OrdinalIgnoreCase))
                return "Get external-search configuration status";
            if (String.Equals(path, "/v1.0/assistants/{assistantId}/documents", StringComparison.OrdinalIgnoreCase))
                return "List public assistant documents";
            if (String.Equals(path, "/v1.0/assistants/{assistantId}/tool-calls", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
                    return "Delete assistant tool-call traces";

                return "List assistant tool-call traces";
            }

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
