namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for tenant and identity routes.
    /// </summary>
    public static class TenantApiDocs
    {
        #region Private-Members

        private const string _Tag = "Tenants";

        private static TenantMetadata ExampleTenant()
        {
            return new TenantMetadata
            {
                Id = ApiExamples.TenantId,
                Name = "Acme Corporation",
                Active = true,
                IsProtected = false,
                Labels = new List<string> { "production" },
                Tags = new Dictionary<string, string> { { "region", "us-west" } },
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static TenantMetadata ExampleTenantRequest()
        {
            return new TenantMetadata
            {
                Name = "Acme Corporation",
                Active = true,
                Labels = new List<string> { "production" },
                Tags = new Dictionary<string, string> { { "region", "us-west" } }
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/tenants.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create tenant", _Tag)
            .Describe("Creates a tenant and provisions its default resources (administrator user, credential, bucket, and collection). Global administrators only. A tenant name must be unique.")
            .Body("Tenant to create. Name is required; Id is generated when omitted.", ExampleTenantRequest())
            .Returns(201, "Tenant created and provisioned.", new
            {
                Tenant = ExampleTenant(),
                Provisioning = new TenantProvisioningResult
                {
                    TenantId = ApiExamples.TenantId,
                    TenantName = "Acme Corporation",
                    AdminUserId = ApiExamples.UserId,
                    AdminEmail = "admin@acme.example",
                    AdminPassword = "password",
                    BearerToken = "4f1c9d2e7a6b4c3d8e9f0a1b2c3d4e5f",
                    VerbexTenantId = ApiExamples.TenantId,
                    VerbexDefaultIndexId = "default"
                }
            })
            .Errors(400, 401, 403, 409, 500);

        /// <summary>GET /v1.0/tenants.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List tenants", _Tag)
            .Describe("Global administrators see every tenant with paging; other callers receive a single-item page containing their own tenant.")
            .Paged()
            .Returns(200, "Page of tenants.", ApiExamples.Page(ExampleTenant()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/tenants/{id}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get tenant", _Tag)
            .Describe("Returns one tenant. Non-global administrators can only read their own tenant.")
            .Returns(200, "Tenant.", ExampleTenant())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/tenants/{id}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update tenant", _Tag)
            .Describe("Replaces the mutable fields of a tenant. Global administrators only.")
            .Body("Updated tenant values.", ExampleTenantRequest())
            .Returns(200, "Updated tenant.", ExampleTenant())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/tenants/{id}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete tenant", _Tag)
            .Describe("Deletes a tenant and its records. Protected tenants cannot be deleted; set Active to false instead. Global administrators only.")
            .ReturnsNoContent(204, "Tenant deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/tenants/{id}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check tenant existence", _Tag)
            .Describe("Returns 200 when the tenant exists and the caller can see it, 404 otherwise. No response body.")
            .ReturnsNoContent(200, "Tenant exists.")
            .ReturnsNoContent(404, "Tenant not found.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(403, "Authorization failed.");

        /// <summary>GET /v1.0/whoami.</summary>
        public static OpenApiRouteMetadata WhoAmI => ApiDoc.Create("Get caller identity", _Tag)
            .Describe("Describes the authenticated caller: tenant, user, and administrator flags.")
            .Returns(200, "Caller identity.", new
            {
                IsAuthenticated = true,
                IsGlobalAdmin = false,
                IsTenantAdmin = true,
                TenantId = ApiExamples.TenantId,
                TenantName = "Acme Corporation",
                UserId = ApiExamples.UserId,
                Email = "admin@acme.example"
            })
            .Errors(401, 500);

        #endregion
    }
}
