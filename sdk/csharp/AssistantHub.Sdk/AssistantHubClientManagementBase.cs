namespace AssistantHub.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Models;

    /// <summary>
    /// Adds crawl, configuration, health, tenant, user, credential, and search APIs to the SDK client.
    /// </summary>
    public abstract class AssistantHubClientManagementBase : AssistantHubClientDocumentBase
    {

        private protected AssistantHubClientManagementBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        private protected AssistantHubClientManagementBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        #region Crawl-Plans

        /// <summary>
        /// List crawl plans.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl plans.</returns>
        public async Task<EnumerationResult<CrawlPlan>> ListCrawlPlansAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<CrawlPlan>>(HttpMethod.Get, "/v1.0/crawlplans", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a crawl plan by identifier.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The crawl plan.</returns>
        public async Task<CrawlPlan> GetCrawlPlanAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<CrawlPlan>(HttpMethod.Get, "/v1.0/crawlplans/" + planId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new crawl plan.
        /// </summary>
        /// <param name="plan">Crawl plan to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created crawl plan.</returns>
        public async Task<CrawlPlan> CreateCrawlPlanAsync(CrawlPlan plan, CancellationToken cancellationToken = default)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return await SendAsync<CrawlPlan>(HttpMethod.Put, "/v1.0/crawlplans", plan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="plan">Updated crawl plan data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated crawl plan.</returns>
        public async Task<CrawlPlan> UpdateCrawlPlanAsync(string planId, CrawlPlan plan, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return await SendAsync<CrawlPlan>(HttpMethod.Put, "/v1.0/crawlplans/" + planId, plan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCrawlPlanAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Delete, "/v1.0/crawlplans/" + planId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Start a crawl for the specified plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Post, "/v1.0/crawlplans/" + planId + "/start", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop a running crawl for the specified plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StopCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Post, "/v1.0/crawlplans/" + planId + "/stop", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Crawl-Operations

        /// <summary>
        /// List crawl operations for a plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl operations.</returns>
        public async Task<EnumerationResult<CrawlOperation>> ListCrawlOperationsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<EnumerationResult<CrawlOperation>>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a crawl operation by identifier.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The crawl operation.</returns>
        public async Task<CrawlOperation> GetCrawlOperationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<CrawlOperation>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/" + operationId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a crawl operation.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCrawlOperationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            await SendAsync(HttpMethod.Delete, "/v1.0/crawlplans/" + planId + "/operations/" + operationId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get statistics for all operations under a crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics as a JSON element.</returns>
        public async Task<JsonElement> GetCrawlStatisticsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/statistics", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get statistics for a specific crawl operation.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics as a JSON element.</returns>
        public async Task<JsonElement> GetCrawlOperationStatisticsAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/" + operationId + "/statistics", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get the current server configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Configuration as a JSON element.</returns>
        public async Task<JsonElement> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/configuration", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get redacted external-search configuration status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>External-search configuration status.</returns>
        public async Task<ExternalSearchConfigurationStatus> GetExternalSearchStatusAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<ExternalSearchConfigurationStatus>(HttpMethod.Get, "/v1.0/configuration/external-search/status", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update the server configuration.
        /// </summary>
        /// <param name="configuration">Full configuration object to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated configuration as a JSON element.</returns>
        public async Task<JsonElement> UpdateConfigAsync(JsonElement configuration, CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/configuration", configuration, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Health

        /// <summary>
        /// Check server health by requesting the root endpoint.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the server is healthy.</returns>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, BaseUrl + "/"))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        /// <summary>
        /// Get the identity of the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>User identity as a JSON element.</returns>
        public async Task<JsonElement> WhoAmIAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/whoami", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Tenants

        /// <summary>
        /// List all tenants.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing tenants.</returns>
        public async Task<EnumerationResult<TenantMetadata>> ListTenantsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<TenantMetadata>>(HttpMethod.Get, "/v1.0/tenants", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a tenant by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The tenant.</returns>
        public async Task<TenantMetadata> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<TenantMetadata>(HttpMethod.Get, "/v1.0/tenants/" + tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new tenant.
        /// </summary>
        /// <param name="tenant">Tenant to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Composite response containing Tenant and Provisioning data.</returns>
        public async Task<JsonElement> CreateTenantAsync(TenantMetadata tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/tenants", tenant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="tenant">Updated tenant data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated tenant.</returns>
        public async Task<TenantMetadata> UpdateTenantAsync(string tenantId, TenantMetadata tenant, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            return await SendAsync<TenantMetadata>(HttpMethod.Put, "/v1.0/tenants/" + tenantId, tenant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Users

        /// <summary>
        /// List users for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing users.</returns>
        public async Task<EnumerationResult<UserMaster>> ListUsersAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<UserMaster>>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/users", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a user by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user.</returns>
        public async Task<UserMaster> GetUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            return await SendAsync<UserMaster>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/users/" + userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new user for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="user">User to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created user.</returns>
        public async Task<UserMaster> CreateUserAsync(string tenantId, UserMaster user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return await SendAsync<UserMaster>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/users", user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="user">Updated user data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user.</returns>
        public async Task<UserMaster> UpdateUserAsync(string tenantId, string userId, UserMaster user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return await SendAsync<UserMaster>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/users/" + userId, user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId + "/users/" + userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Credentials

        /// <summary>
        /// List credentials for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing credentials.</returns>
        public async Task<EnumerationResult<Credential>> ListCredentialsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<Credential>>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/credentials", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a credential by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The credential.</returns>
        public async Task<Credential> GetCredentialAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            return await SendAsync<Credential>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new credential for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credential">Credential to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created credential.</returns>
        public async Task<Credential> CreateCredentialAsync(string tenantId, Credential credential, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            return await SendAsync<Credential>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/credentials", credential, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing credential.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="credential">Updated credential data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated credential.</returns>
        public async Task<Credential> UpdateCredentialAsync(string tenantId, string credentialId, Credential credential, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            return await SendAsync<Credential>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, credential, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a credential.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCredentialAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Search

        /// <summary>
        /// Search documents via RAG retrieval through the chat endpoint.
        /// Sends a single user message to the assistant and returns the full response
        /// including retrieval results and citations.
        /// </summary>
        /// <param name="assistantId">Assistant identifier to search through.</param>
        /// <param name="query">Search query text.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="maxTokens">Optional maximum tokens for generation.</param>
        /// <param name="temperature">Optional temperature for generation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chat completion response containing retrieval results and citations.</returns>
        public async Task<ChatCompletionResponse> SearchAsync(
            string assistantId,
            string query,
            string threadId = null,
            int? maxTokens = null,
            double? temperature = null,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            ChatCompletionRequest request = new ChatCompletionRequest
            {
                Messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage { Role = "user", Content = query }
                }
            };

            if (maxTokens.HasValue)
                request.MaxTokens = maxTokens.Value;
            if (temperature.HasValue)
                request.Temperature = temperature.Value;

            return await SendMessageAsync(assistantId, request, threadId, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
