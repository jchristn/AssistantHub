namespace AssistantHub.Server.OpenApi
{
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for tenant-scoped credential (API key) routes.
    /// </summary>
    public static class CredentialApiDocs
    {
        #region Private-Members

        private const string _Tag = "Credentials";

        private const string _ExampleBearerToken = "example0bearer0token0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f6";

        private static Credential ExampleCredential()
        {
            return new Credential
            {
                Id = ApiExamples.CredentialId,
                TenantId = ApiExamples.TenantId,
                UserId = ApiExamples.UserId,
                Name = "Reporting integration",
                BearerToken = _ExampleBearerToken,
                Active = true,
                IsProtected = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static Credential ExampleCredentialRequest()
        {
            return new Credential
            {
                Id = ApiExamples.CredentialId,
                TenantId = ApiExamples.TenantId,
                UserId = ApiExamples.UserId,
                Name = "Reporting integration",
                BearerToken = _ExampleBearerToken,
                Active = true,
                IsProtected = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/tenants/{tenantId}/credentials.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create credential", _Tag)
            .Describe("Creates an API credential (bearer token) for a user in the tenant. Requires a global administrator or a tenant administrator of {tenantId}. "
                + "UserId is required and must reference a user in {tenantId} (404 otherwise). "
                + "The server generates the Id and BearerToken and stamps TenantId and timestamps; any values supplied for those fields are ignored. "
                + "The response contains the generated bearer token.")
            .Body("Credential to create. UserId is required.", ExampleCredentialRequest())
            .Returns(201, "Credential created, including the generated bearer token.", ExampleCredential())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/tenants/{tenantId}/credentials.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List credentials", _Tag)
            .Describe("Returns a page of credentials in the tenant, including their bearer tokens. Requires a global administrator or a tenant administrator of {tenantId}.")
            .Paged()
            .Returns(200, "Page of credentials.", ApiExamples.Page(ExampleCredential()))
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/tenants/{tenantId}/credentials/{credentialId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get credential", _Tag)
            .Describe("Returns one credential, including its bearer token. Requires a global administrator or a tenant administrator of {tenantId}. "
                + "Returns 404 when the credential does not exist or belongs to a different tenant.")
            .Returns(200, "Credential.", ExampleCredential())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/tenants/{tenantId}/credentials/{credentialId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update credential", _Tag)
            .Describe("Replaces a credential. Requires a global administrator or a tenant administrator of {tenantId}. "
                + "Id, TenantId, UserId, BearerToken, and CreatedUtc are preserved from the existing record and LastUpdateUtc is set by the server, so only Name, Active, and IsProtected can change. "
                + "Send the full object: omitted mutable fields revert to their defaults.")
            .Body("Updated credential values.", ExampleCredentialRequest())
            .Returns(200, "Updated credential.", ExampleCredential())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/tenants/{tenantId}/credentials/{credentialId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete credential", _Tag)
            .Describe("Deletes a credential, revoking its bearer token. Requires a global administrator or a tenant administrator of {tenantId}. "
                + "Protected credentials cannot be deleted (403); set Active to false instead.")
            .ReturnsNoContent(204, "Credential deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/tenants/{tenantId}/credentials/{credentialId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check credential existence", _Tag)
            .Describe("Returns 200 when the credential exists in the tenant, 404 otherwise. Requires a global administrator or a tenant administrator of {tenantId}. No response body.")
            .ReturnsNoContent(200, "Credential exists.")
            .ReturnsNoContent(404, "Credential not found.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(500, "Internal server error.");

        #endregion
    }
}
