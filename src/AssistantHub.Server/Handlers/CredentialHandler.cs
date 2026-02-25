namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles credential CRUD routes under /v1.0/tenants/{tenantId}/credentials.
    /// </summary>
    public class CredentialHandler : HandlerBase
    {
        private static readonly string _Header = "[CredentialHandler] ";

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        public CredentialHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// PUT /v1.0/tenants/{tenantId}/credentials - Create a new credential.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutCredentialAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                if (String.IsNullOrEmpty(tenantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                Credential credential = Serializer.DeserializeJson<Credential>(body);
                if (credential == null || String.IsNullOrEmpty(credential.UserId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "UserId is required."))).ConfigureAwait(false);
                    return;
                }

                // Verify the user exists and belongs to this tenant
                UserMaster user = await Database.User.ReadAsync(credential.UserId).ConfigureAwait(false);
                if (user == null || !String.Equals(user.TenantId, tenantId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "User not found in this tenant."))).ConfigureAwait(false);
                    return;
                }

                credential.Id = IdGenerator.NewCredentialId();
                credential.TenantId = tenantId;
                credential.BearerToken = IdGenerator.NewBearerToken();
                credential.CreatedUtc = DateTime.UtcNow;
                credential.LastUpdateUtc = DateTime.UtcNow;

                credential = await Database.Credential.CreateAsync(credential).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(credential)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutCredentialAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/tenants/{tenantId}/credentials - List credentials.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetCredentialsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                if (String.IsNullOrEmpty(tenantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<Credential> result = await Database.Credential.EnumerateAsync(tenantId, query).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetCredentialsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/tenants/{tenantId}/credentials/{credentialId} - Get credential by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetCredentialAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                if (String.IsNullOrEmpty(tenantId) || String.IsNullOrEmpty(credentialId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                Credential credential = await Database.Credential.ReadAsync(credentialId).ConfigureAwait(false);
                if (credential == null || !String.Equals(credential.TenantId, tenantId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(credential)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetCredentialAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/tenants/{tenantId}/credentials/{credentialId} - Update credential by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutCredentialByIdAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                if (String.IsNullOrEmpty(tenantId) || String.IsNullOrEmpty(credentialId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                Credential existing = await Database.Credential.ReadAsync(credentialId).ConfigureAwait(false);
                if (existing == null || !String.Equals(existing.TenantId, tenantId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                Credential updated = Serializer.DeserializeJson<Credential>(body);
                if (updated == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                updated.Id = credentialId;
                updated.TenantId = tenantId;
                updated.UserId = existing.UserId;
                updated.BearerToken = existing.BearerToken;
                updated.CreatedUtc = existing.CreatedUtc;
                updated.LastUpdateUtc = DateTime.UtcNow;

                updated = await Database.Credential.UpdateAsync(updated).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(updated)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutCredentialByIdAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/tenants/{tenantId}/credentials/{credentialId} - Delete credential by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteCredentialAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                if (String.IsNullOrEmpty(tenantId) || String.IsNullOrEmpty(credentialId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                Credential existing = await Database.Credential.ReadAsync(credentialId).ConfigureAwait(false);
                if (existing == null || !String.Equals(existing.TenantId, tenantId, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (existing.IsProtected)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed, null, "Protected records cannot be deleted. Deactivate by setting Active to false instead."))).ConfigureAwait(false);
                    return;
                }

                await Database.Credential.DeleteAsync(credentialId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteCredentialAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// HEAD /v1.0/tenants/{tenantId}/credentials/{credentialId} - Check credential existence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadCredentialAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                if (String.IsNullOrEmpty(tenantId) || String.IsNullOrEmpty(credentialId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                if (!ValidateTenantAccess(auth, tenantId))
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                Credential credential = await Database.Credential.ReadAsync(credentialId).ConfigureAwait(false);
                bool exists = credential != null && String.Equals(credential.TenantId, tenantId, StringComparison.Ordinal);
                ctx.Response.StatusCode = exists ? 200 : 404;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadCredentialAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }
    }
}
