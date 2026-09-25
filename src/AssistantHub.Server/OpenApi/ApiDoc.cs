namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// Fluent builder for the OpenAPI metadata attached to each AssistantHub route registration.
    /// Schemas are derived from the CLR types of the request and response models; examples are
    /// real model instances serialized with the AssistantHub serializer conventions.
    /// </summary>
    public sealed class ApiDoc
    {
        #region Public-Members

        /// <summary>
        /// JSON media type.
        /// </summary>
        public const string Json = "application/json";

        #endregion

        #region Private-Members

        private readonly OpenApiRouteMetadata _Metadata;

        #endregion

        #region Constructors-and-Factories

        private ApiDoc(string summary, string tag)
        {
            if (System.String.IsNullOrWhiteSpace(summary)) throw new ArgumentNullException(nameof(summary));
            if (System.String.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));

            _Metadata = new OpenApiRouteMetadata
            {
                Summary = summary,
                Tags = new List<string> { tag },
                Parameters = new List<OpenApiParameterMetadata>(),
                Responses = new Dictionary<string, OpenApiResponseMetadata>(StringComparer.Ordinal)
            };
        }

        /// <summary>
        /// Start documenting an operation.
        /// </summary>
        /// <param name="summary">Short operation summary.</param>
        /// <param name="tag">OpenAPI tag used to group the operation.</param>
        /// <returns>Builder.</returns>
        public static ApiDoc Create(string summary, string tag)
        {
            return new ApiDoc(summary, tag);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Set the long-form operation description.
        /// </summary>
        /// <param name="description">Description.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Describe(string description)
        {
            _Metadata.Description = description;
            return this;
        }

        /// <summary>
        /// Add a query parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">OpenAPI primitive type: string, integer, number, or boolean.</param>
        /// <param name="description">Description.</param>
        /// <param name="required">True if required.</param>
        /// <param name="example">Optional example value.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Query(string name, string type, string description, bool required = false, object example = null)
        {
            _Metadata.Parameters.Add(new OpenApiParameterMetadata
            {
                Name = name,
                In = ParameterLocation.Query,
                Description = description,
                Required = required,
                Schema = new OpenApiSchemaMetadata { Type = type },
                Example = example
            });
            return this;
        }

        /// <summary>
        /// Add the standard enumeration query parameters read by HandlerBase.BuildEnumerationQuery:
        /// maxResults, continuationToken, and ordering.
        /// </summary>
        /// <returns>Builder.</returns>
        public ApiDoc Paged()
        {
            Query("maxResults", "integer", "Maximum number of records to return (1 to 1000, default 100).", false, 100);
            Query("continuationToken", "string", "Continuation token returned by the previous page.");
            Query("ordering", "string", "Result ordering: CreatedAscending or CreatedDescending (default).", false, "CreatedDescending");
            return this;
        }

        /// <summary>
        /// Add a request header parameter.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="description">Description.</param>
        /// <param name="required">True if required.</param>
        /// <param name="example">Optional example value.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Header(string name, string description, bool required = false, object example = null)
        {
            _Metadata.Parameters.Add(new OpenApiParameterMetadata
            {
                Name = name,
                In = ParameterLocation.Header,
                Description = description,
                Required = required,
                Schema = new OpenApiSchemaMetadata { Type = "string" },
                Example = example
            });
            return this;
        }

        /// <summary>
        /// Document a JSON request body. The schema is derived from <typeparamref name="T"/> (or from the runtime
        /// type of <paramref name="example"/> when <typeparamref name="T"/> is <see cref="object"/>).
        /// </summary>
        /// <typeparam name="T">Request model type.</typeparam>
        /// <param name="description">Description.</param>
        /// <param name="example">Example request body.</param>
        /// <param name="required">True if the body is required.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Body<T>(string description, T example, bool required = true)
        {
            if (example == null) throw new ArgumentNullException(nameof(example));

            _Metadata.RequestBody = new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaTypeMetadata>(StringComparer.Ordinal)
                {
                    [Json] = new OpenApiMediaTypeMetadata
                    {
                        Schema = ApiSchema.FromType(ResolveType(example)),
                        Example = example
                    }
                }
            };
            return this;
        }

        /// <summary>
        /// Document a non-JSON request body, such as a binary upload or plain text.
        /// </summary>
        /// <param name="contentType">Media type.</param>
        /// <param name="description">Description.</param>
        /// <param name="schema">Schema; defaults to a binary string.</param>
        /// <param name="example">Optional example value.</param>
        /// <param name="required">True if the body is required.</param>
        /// <returns>Builder.</returns>
        public ApiDoc RawBody(string contentType, string description, OpenApiSchemaMetadata schema = null, object example = null, bool required = true)
        {
            _Metadata.RequestBody = new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaTypeMetadata>(StringComparer.Ordinal)
                {
                    [contentType] = new OpenApiMediaTypeMetadata
                    {
                        Schema = schema ?? ApiSchema.Binary(),
                        Example = example
                    }
                }
            };
            return this;
        }

        /// <summary>
        /// Document a JSON response. The schema is derived from <typeparamref name="T"/> (or from the runtime type of
        /// <paramref name="example"/> when <typeparamref name="T"/> is <see cref="object"/>, including anonymous types).
        /// </summary>
        /// <typeparam name="T">Response model type.</typeparam>
        /// <param name="status">HTTP status code.</param>
        /// <param name="description">Description.</param>
        /// <param name="example">Example response body.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Returns<T>(int status, string description, T example)
        {
            if (example == null) throw new ArgumentNullException(nameof(example));

            _Metadata.Responses[status.ToString()] = new OpenApiResponseMetadata
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaTypeMetadata>(StringComparer.Ordinal)
                {
                    [Json] = new OpenApiMediaTypeMetadata
                    {
                        Schema = ApiSchema.FromType(ResolveType(example)),
                        Example = example
                    }
                }
            };
            return this;
        }

        /// <summary>
        /// Document a response with a non-JSON body, such as a file download, HTML page, or server-sent event stream.
        /// </summary>
        /// <param name="status">HTTP status code.</param>
        /// <param name="description">Description.</param>
        /// <param name="contentType">Media type.</param>
        /// <param name="schema">Schema; defaults to a binary string.</param>
        /// <param name="example">Optional example value.</param>
        /// <returns>Builder.</returns>
        public ApiDoc ReturnsContent(int status, string description, string contentType, OpenApiSchemaMetadata schema = null, object example = null)
        {
            string key = status.ToString();
            if (!_Metadata.Responses.TryGetValue(key, out OpenApiResponseMetadata response) || response.Content == null)
            {
                response = new OpenApiResponseMetadata
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaTypeMetadata>(StringComparer.Ordinal)
                };
                _Metadata.Responses[key] = response;
            }

            response.Content[contentType] = new OpenApiMediaTypeMetadata
            {
                Schema = schema ?? ApiSchema.Binary(),
                Example = example
            };
            return this;
        }

        /// <summary>
        /// Document a response without a body, such as 204 No Content or a HEAD existence check.
        /// </summary>
        /// <param name="status">HTTP status code.</param>
        /// <param name="description">Description.</param>
        /// <returns>Builder.</returns>
        public ApiDoc ReturnsNoContent(int status, string description)
        {
            _Metadata.Responses[status.ToString()] = new OpenApiResponseMetadata { Description = description };
            return this;
        }

        /// <summary>
        /// Document standard AssistantHub error responses (<see cref="ApiErrorResponse"/>) for the given status codes.
        /// Supported codes: 400, 401, 403, 404, 409, and 500.
        /// </summary>
        /// <param name="statuses">HTTP status codes.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Errors(params int[] statuses)
        {
            foreach (int status in statuses)
            {
                ApiErrorEnum error = status switch
                {
                    400 => ApiErrorEnum.BadRequest,
                    401 => ApiErrorEnum.AuthenticationFailed,
                    403 => ApiErrorEnum.AuthorizationFailed,
                    404 => ApiErrorEnum.NotFound,
                    409 => ApiErrorEnum.Conflict,
                    500 => ApiErrorEnum.InternalError,
                    _ => throw new ArgumentOutOfRangeException(nameof(statuses), "Use Error(status, description) for status " + status + ".")
                };

                ApiErrorResponse example = new ApiErrorResponse(error);
                Returns(status, example.Message, example);
            }

            return this;
        }

        /// <summary>
        /// Document a non-standard error response with an <see cref="ApiErrorResponse"/> body.
        /// </summary>
        /// <param name="status">HTTP status code.</param>
        /// <param name="description">Description.</param>
        /// <param name="example">Example error body; defaults to an internal error.</param>
        /// <returns>Builder.</returns>
        public ApiDoc Error(int status, string description, ApiErrorResponse example = null)
        {
            return Returns(status, description, example ?? new ApiErrorResponse(ApiErrorEnum.InternalError, null, description));
        }

        /// <summary>
        /// Mark the operation deprecated.
        /// </summary>
        /// <returns>Builder.</returns>
        public ApiDoc Deprecated()
        {
            _Metadata.Deprecated = true;
            return this;
        }

        /// <summary>
        /// Finish building.
        /// </summary>
        /// <returns>Route metadata.</returns>
        public OpenApiRouteMetadata Build()
        {
            bool hasSuccess = false;
            foreach (string status in _Metadata.Responses.Keys)
            {
                if (status.StartsWith("2", StringComparison.Ordinal)) hasSuccess = true;
            }

            if (!hasSuccess)
                throw new InvalidOperationException("Operation '" + _Metadata.Summary + "' must document at least one 2xx response.");

            return _Metadata;
        }

        /// <summary>
        /// Implicitly finish building so docs can be passed directly as route metadata.
        /// </summary>
        /// <param name="doc">Builder.</param>
        public static implicit operator OpenApiRouteMetadata(ApiDoc doc)
        {
            return doc?.Build();
        }

        #endregion

        #region Private-Methods

        private static Type ResolveType<T>(T example)
        {
            if (typeof(T) == typeof(object) && example != null) return example.GetType();
            return typeof(T);
        }

        #endregion
    }
}
