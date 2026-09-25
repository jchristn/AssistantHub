namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for unauthenticated system routes: health, OpenAPI document, Swagger UI, and authentication.
    /// </summary>
    public static class SystemApiDocs
    {
        #region Private-Members

        private const string _HealthTag = "Health";
        private const string _OpenApiTag = "OpenAPI";
        private const string _AuthenticationTag = "Authentication";

        private const string _ExampleBearerToken = "example-bearer-token-0000000000000000";

        private const string _SwaggerHtmlExample =
            "<!DOCTYPE html>\n" +
            "<html lang=\"en\">\n" +
            "<head>\n" +
            "  <meta charset=\"utf-8\" />\n" +
            "  <title>AssistantHub Swagger</title>\n" +
            "  <link rel=\"stylesheet\" href=\"https://unpkg.com/swagger-ui-dist@5/swagger-ui.css\" />\n" +
            "</head>\n" +
            "<body>\n" +
            "  <div id=\"swagger-ui\"></div>\n" +
            "  <script src=\"https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js\"></script>\n" +
            "  <script>window.onload = function () { window.ui = SwaggerUIBundle({ url: \"/openapi.json\", dom_id: \"#swagger-ui\" }); };</script>\n" +
            "</body>\n" +
            "</html>";

        private static OpenApiSchemaMetadata OpenApiDocumentSchema()
        {
            return new OpenApiSchemaMetadata
            {
                Type = "object",
                Description = "OpenAPI 3.0.3 document describing every registered AssistantHub route."
            };
        }

        private static object ExampleOpenApiDocument()
        {
            return new Dictionary<string, object>
            {
                { "openapi", "3.0.3" },
                {
                    "info", new Dictionary<string, object>
                    {
                        { "title", "AssistantHub REST API" },
                        { "description", "Runtime route surface for AssistantHub." },
                        { "version", Constants.ProductVersion }
                    }
                },
                {
                    "servers", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "url", "http://localhost:8800" },
                            { "description", "Runtime server" }
                        }
                    }
                },
                { "paths", new Dictionary<string, object>() },
                { "components", new Dictionary<string, object>() }
            };
        }

        private static AuthenticateRequest ExampleAuthenticateRequest()
        {
            return new AuthenticateRequest
            {
                TenantId = ApiExamples.TenantId,
                Email = "admin@acme.example",
                Password = "password"
            };
        }

        private static AuthenticateResult ExampleAuthenticateSuccess()
        {
            return new AuthenticateResult
            {
                Success = true,
                User = new UserMaster
                {
                    Id = ApiExamples.UserId,
                    TenantId = ApiExamples.TenantId,
                    Email = "admin@acme.example",
                    PasswordSha256 = null,
                    FirstName = "Ada",
                    LastName = "Admin",
                    IsAdmin = false,
                    IsTenantAdmin = true,
                    Active = true,
                    IsProtected = false,
                    CreatedUtc = ApiExamples.Created,
                    LastUpdateUtc = ApiExamples.Updated
                },
                Credential = new Credential
                {
                    Id = ApiExamples.CredentialId,
                    TenantId = ApiExamples.TenantId,
                    UserId = ApiExamples.UserId,
                    Name = "Default credential",
                    BearerToken = _ExampleBearerToken,
                    Active = true,
                    IsProtected = false,
                    CreatedUtc = ApiExamples.Created,
                    LastUpdateUtc = ApiExamples.Updated
                },
                ErrorMessage = null,
                TenantId = ApiExamples.TenantId,
                TenantName = "Acme Corporation",
                IsGlobalAdmin = false,
                IsTenantAdmin = true
            };
        }

        private static AuthenticateResult ExampleAuthenticateFailure()
        {
            return new AuthenticateResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /.</summary>
        public static OpenApiRouteMetadata GetRoot => ApiDoc.Create("Get service information", _HealthTag)
            .Describe("Unauthenticated health check. Returns the product name, product version, and the current server timestamp in UTC.")
            .Returns(200, "Service information.", new
            {
                Product = Constants.ProductName,
                Version = Constants.ProductVersion,
                Timestamp = ApiExamples.Created
            });

        /// <summary>HEAD /.</summary>
        public static OpenApiRouteMetadata HeadRoot => ApiDoc.Create("Check service availability", _HealthTag)
            .Describe("Unauthenticated liveness probe. Returns 200 with no response body when the server is running.")
            .ReturnsNoContent(200, "Service is available.");

        /// <summary>GET /openapi.json.</summary>
        public static OpenApiRouteMetadata GetOpenApi => ApiDoc.Create("Get OpenAPI document", _OpenApiTag)
            .Describe("Unauthenticated. Returns the OpenAPI 3.0.3 document generated at runtime from the registered routes. The servers entry is built from the request scheme and Host header. Also available at /v1.0/openapi.json.")
            .ReturnsContent(200, "OpenAPI document.", ApiDoc.Json, OpenApiDocumentSchema(), ExampleOpenApiDocument())
            .Errors(500);

        /// <summary>GET /v1.0/openapi.json.</summary>
        public static OpenApiRouteMetadata GetOpenApiVersioned => ApiDoc.Create("Get OpenAPI document (versioned path)", _OpenApiTag)
            .Describe("Unauthenticated. Versioned alias of /openapi.json; returns the same OpenAPI 3.0.3 document generated at runtime from the registered routes.")
            .ReturnsContent(200, "OpenAPI document.", ApiDoc.Json, OpenApiDocumentSchema(), ExampleOpenApiDocument())
            .Errors(500);

        /// <summary>GET /swagger.</summary>
        public static OpenApiRouteMetadata GetSwagger => ApiDoc.Create("Get Swagger UI", _OpenApiTag)
            .Describe("Unauthenticated. Returns an HTML page that loads Swagger UI from the unpkg CDN and points it at /openapi.json.")
            .ReturnsContent(200, "Swagger UI HTML page.", "text/html", ApiSchema.String("Swagger UI HTML page."), _SwaggerHtmlExample);

        /// <summary>POST /v1.0/authenticate.</summary>
        public static OpenApiRouteMetadata Authenticate => ApiDoc.Create("Authenticate", _AuthenticationTag)
            .Describe("Unauthenticated. Authenticates with either a bearer token (BearerToken) or an email and password (Email and Password, optionally scoped by TenantId; the default tenant is used when TenantId is omitted). When BearerToken is present it takes precedence. "
                + "On success returns the user (password hash redacted), an active credential for the user, and the tenant and administrator flags. When authenticating with the admin API key, a synthesized global-admin user and credential are returned. "
                + "Authentication failures return 401 with an AuthenticateResult body whose Success is false and ErrorMessage describes the failure.")
            .Body("Credentials. Supply either BearerToken, or Email and Password (with optional TenantId).", ExampleAuthenticateRequest())
            .Returns(200, "Authentication succeeded.", ExampleAuthenticateSuccess())
            .Returns(401, "Authentication failed.", ExampleAuthenticateFailure())
            .Errors(400, 500);

        #endregion
    }
}
