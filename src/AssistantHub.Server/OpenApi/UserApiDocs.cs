namespace AssistantHub.Server.OpenApi
{
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for tenant-scoped user routes.
    /// </summary>
    public static class UserApiDocs
    {
        #region Private-Members

        private const string _Tag = "Users";

        private const string _ExamplePasswordSha256 = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8";

        private static UserMaster ExampleUser()
        {
            return new UserMaster
            {
                Id = ApiExamples.UserId,
                TenantId = ApiExamples.TenantId,
                Email = "jane.doe@acme.example",
                PasswordSha256 = _ExamplePasswordSha256,
                FirstName = "Jane",
                LastName = "Doe",
                IsAdmin = false,
                IsTenantAdmin = false,
                Active = true,
                IsProtected = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static UserMaster ExampleUserRequest()
        {
            return new UserMaster
            {
                Id = ApiExamples.UserId,
                TenantId = ApiExamples.TenantId,
                Email = "jane.doe@acme.example",
                PasswordSha256 = _ExamplePasswordSha256,
                FirstName = "Jane",
                LastName = "Doe",
                IsAdmin = false,
                IsTenantAdmin = false,
                Active = true,
                IsProtected = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/tenants/{tenantId}/users.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create user", _Tag)
            .Describe("Creates a user in the tenant. Requires a global administrator or a tenant administrator of {tenantId}; tenant administrators cannot create users in other tenants. "
                + "Email is required and must be unique within the tenant. PasswordSha256 is the lowercase hex SHA-256 hash of the password. "
                + "Id, TenantId, CreatedUtc, and LastUpdateUtc in the body are ignored: the server generates the Id and stamps the tenant and timestamps.")
            .Body("User to create. Email is required.", ExampleUserRequest())
            .Returns(201, "User created.", ExampleUser())
            .Errors(400, 401, 403, 409, 500);

        /// <summary>GET /v1.0/tenants/{tenantId}/users.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List users", _Tag)
            .Describe("Returns a page of users in the tenant. Requires a global administrator or a tenant administrator of {tenantId}.")
            .Paged()
            .Returns(200, "Page of users.", ApiExamples.Page(ExampleUser()))
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/tenants/{tenantId}/users/{userId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get user", _Tag)
            .Describe("Returns one user. Requires a global administrator or a tenant administrator of {tenantId}. Returns 404 when the user does not exist or belongs to a different tenant.")
            .Returns(200, "User.", ExampleUser())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/tenants/{tenantId}/users/{userId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update user", _Tag)
            .Describe("Replaces a user. Requires a global administrator or a tenant administrator of {tenantId}. "
                + "Id, TenantId, and CreatedUtc are preserved from the existing record and LastUpdateUtc is set by the server. "
                + "When PasswordSha256 is omitted or empty the existing password hash is kept. Send the full user object: other omitted fields revert to their defaults.")
            .Body("Updated user values.", ExampleUserRequest())
            .Returns(200, "Updated user.", ExampleUser())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/tenants/{tenantId}/users/{userId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete user", _Tag)
            .Describe("Deletes a user and all credentials belonging to that user (cascading delete). Requires a global administrator or a tenant administrator of {tenantId}. "
                + "Protected users cannot be deleted (403); set Active to false instead.")
            .ReturnsNoContent(204, "User and its credentials deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/tenants/{tenantId}/users/{userId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check user existence", _Tag)
            .Describe("Returns 200 when the user exists in the tenant, 404 otherwise. Requires a global administrator or a tenant administrator of {tenantId}. No response body.")
            .ReturnsNoContent(200, "User exists.")
            .ReturnsNoContent(404, "User not found.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(500, "Internal server error.");

        #endregion
    }
}
