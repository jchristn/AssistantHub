namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Service for provisioning resources when a new tenant is created.
    /// Creates RecallDB tenant, default collection, admin user, credential, and ingestion rule.
    /// </summary>
    public class TenantProvisioningService
    {
        private static readonly string _Header = "[TenantProvisioningService] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly IVectorStoreService _VectorStore;
        private readonly IInvertedIndexService _InvertedIndex;
        private readonly AmazonS3Client _S3Client;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TenantProvisioningService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings)
            : this(database, logging, settings, null, null)
        {
        }

        /// <summary>
        /// Instantiate with explicit subordinate service implementations.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">AssistantHub settings.</param>
        /// <param name="vectorStore">Vector store implementation.</param>
        /// <param name="invertedIndex">Inverted index implementation.</param>
        public TenantProvisioningService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            IVectorStoreService vectorStore,
            IInvertedIndexService invertedIndex)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _VectorStore = vectorStore ?? new RecallDbVectorStoreService(_Settings.RecallDb, _Logging);
            _InvertedIndex = invertedIndex ?? new VerbexInvertedIndexService(_Settings.Verbex, _Logging);

            if (_Settings.S3 != null && !String.IsNullOrEmpty(_Settings.S3.EndpointUrl))
            {
                BasicAWSCredentials credentials = new BasicAWSCredentials(_Settings.S3.AccessKey, _Settings.S3.SecretKey);
                AmazonS3Config config = new AmazonS3Config
                {
                    ServiceURL = _Settings.S3.EndpointUrl,
                    ForcePathStyle = true,
                    UseHttp = !_Settings.S3.UseSsl
                };
                _S3Client = new AmazonS3Client(credentials, config);
            }
        }

        /// <summary>
        /// Provision all default resources for a new tenant.
        /// </summary>
        /// <param name="tenant">The tenant metadata (already persisted).</param>
        /// <returns>Provisioning result with created credentials.</returns>
        public async Task<TenantProvisioningResult> ProvisionAsync(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            TenantProvisioningResult result = new TenantProvisioningResult();
            result.TenantId = tenant.Id;
            result.TenantName = tenant.Name;

            // Step 1: Provision RecallDB tenant
            await ProvisionRecallDbTenantAsync(tenant.Id, tenant.Name).ConfigureAwait(false);

            // Step 2: Create default RecallDB collection
            string collectionId = await ProvisionRecallDbCollectionAsync(tenant.Id).ConfigureAwait(false);

            // Step 2b: Create Verbex tenant and default index
            VerbexProvisioningInfo verbex = await ProvisionVerbexAsync(tenant).ConfigureAwait(false);
            if (verbex != null)
            {
                result.VerbexTenantId = verbex.TenantId;
                result.VerbexDefaultIndexId = verbex.IndexId;

                tenant.Tags ??= new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(verbex.TenantId))
                    tenant.Tags[Constants.VerbexTenantIdTag] = verbex.TenantId;
                if (!String.IsNullOrEmpty(verbex.IndexId))
                    tenant.Tags[Constants.VerbexDefaultIndexIdTag] = verbex.IndexId;
                tenant.LastUpdateUtc = DateTime.UtcNow;
                await _Database.Tenant.UpdateAsync(tenant).ConfigureAwait(false);
            }

            // Step 3: Create default admin user
            string sanitizedName = tenant.Name.ToLower().Replace(" ", "");
            string adminEmail = "admin@" + sanitizedName;
            string adminPassword = "password";
            string passwordHash = ComputeSha256(adminPassword);

            UserMaster adminUser = new UserMaster();
            adminUser.Id = IdGenerator.NewUserId();
            adminUser.TenantId = tenant.Id;
            adminUser.Email = adminEmail;
            adminUser.PasswordSha256 = passwordHash;
            adminUser.FirstName = "Admin";
            adminUser.LastName = "User";
            adminUser.IsAdmin = true;
            adminUser.IsTenantAdmin = true;
            adminUser.Active = true;
            adminUser.IsProtected = true;
            adminUser.CreatedUtc = DateTime.UtcNow;
            adminUser.LastUpdateUtc = DateTime.UtcNow;

            adminUser = await _Database.User.CreateAsync(adminUser).ConfigureAwait(false);
            result.AdminUserId = adminUser.Id;
            result.AdminEmail = adminEmail;
            result.AdminPassword = adminPassword;

            // Step 4: Create default credential
            string bearerToken = IdGenerator.NewBearerToken();

            Credential credential = new Credential();
            credential.Id = IdGenerator.NewCredentialId();
            credential.TenantId = tenant.Id;
            credential.UserId = adminUser.Id;
            credential.Name = "Default admin credential";
            credential.BearerToken = bearerToken;
            credential.Active = true;
            credential.IsProtected = true;
            credential.CreatedUtc = DateTime.UtcNow;
            credential.LastUpdateUtc = DateTime.UtcNow;

            credential = await _Database.Credential.CreateAsync(credential).ConfigureAwait(false);
            result.BearerToken = bearerToken;
            result.User = adminUser;
            result.Credential = credential;

            // Step 5: Create default S3 bucket for tenant
            string tenantBucket = tenant.Id + "_default";
            await ProvisionS3BucketAsync(tenantBucket).ConfigureAwait(false);

            // Step 6: Create default ingestion rule
            IngestionRule rule = new IngestionRule();
            rule.Id = IdGenerator.NewIngestionRuleId();
            rule.TenantId = tenant.Id;
            rule.Name = "Default";
            rule.Description = "Default ingestion rule";
            rule.Bucket = tenantBucket;
            rule.CollectionName = "default";
            rule.CollectionId = collectionId ?? "default";
            rule.VerbexIndexId = _Settings.Verbex.DefaultIndexId;
            rule.Chunking = new IngestionChunkingConfig();
            rule.Embedding = new IngestionEmbeddingConfig
            {
                EmbeddingEndpointId = "default",
                L2Normalization = true
            };
            rule.CreatedUtc = DateTime.UtcNow;
            rule.LastUpdateUtc = DateTime.UtcNow;

            rule = await _Database.IngestionRule.CreateAsync(rule).ConfigureAwait(false);

            _Logging.Info(_Header + "provisioned tenant " + tenant.Id + " (" + tenant.Name + ")");

            return result;
        }

        /// <summary>
        /// Deprovision all resources for a tenant being deleted.
        /// Deletes child rows in dependency order, RecallDB tenant, and tenant row.
        /// </summary>
        /// <param name="tenantId">The tenant ID to deprovision.</param>
        /// <returns>Task.</returns>
        public async Task DeprovisionAsync(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(tenantId).ConfigureAwait(false);
            if (tenant != null && tenant.IsProtected)
                throw new InvalidOperationException("Cannot deprovision a protected tenant.");

            _Logging.Info(_Header + "deprovisioning tenant " + tenantId);

            // Delete child rows in dependency order
            string[] tables = new string[]
            {
                "chat_history",
                "assistant_feedback",
                "assistant_settings",
                "assistant_documents",
                "ingestion_rules",
                "credentials",
                "assistants",
                "users"
            };

            foreach (string table in tables)
            {
                try
                {
                    await _Database.ExecuteQueryAsync(
                        "DELETE FROM " + table + " WHERE tenant_id = '" + tenantId.Replace("'", "''") + "'",
                        true).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "failed to clean up " + table + " for tenant " + tenantId + ": " + e.Message);
                }
            }

            // Delete tenant S3 buckets
            await DeleteTenantS3BucketsAsync(tenantId).ConfigureAwait(false);

            string verbexTenantId = null;
            if (tenant?.Tags != null && tenant.Tags.TryGetValue(Constants.VerbexTenantIdTag, out string mappedVerbexTenantId))
                verbexTenantId = mappedVerbexTenantId;

            // Delete RecallDB tenant
            await DeleteRecallDbTenantAsync(tenantId).ConfigureAwait(false);

            // Delete Verbex tenant and all associated index data
            await DeleteVerbexTenantAsync(String.IsNullOrEmpty(verbexTenantId) ? tenantId : verbexTenantId).ConfigureAwait(false);

            // Delete tenant row
            await _Database.Tenant.DeleteByIdAsync(tenantId).ConfigureAwait(false);

            _Logging.Info(_Header + "deprovisioned tenant " + tenantId);
        }

        private async Task<VerbexProvisioningInfo> ProvisionVerbexAsync(TenantMetadata tenant)
        {
            try
            {
                string verbexTenantId = await ProvisionVerbexTenantAsync(tenant).ConfigureAwait(false);
                if (String.IsNullOrEmpty(verbexTenantId))
                    return null;

                string indexId = await ProvisionVerbexDefaultIndexAsync(tenant.Id, verbexTenantId).ConfigureAwait(false);
                return new VerbexProvisioningInfo
                {
                    TenantId = verbexTenantId,
                    IndexId = indexId
                };
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception provisioning Verbex for tenant " + tenant.Id + ": " + e.Message);
                return null;
            }
        }

        private async Task<string> ProvisionVerbexTenantAsync(TenantMetadata tenant)
        {
            try
            {
                object body = new
                {
                    name = tenant.Name,
                    description = "AssistantHub tenant " + tenant.Id
                };

                HttpResponseMessage resp = await _InvertedIndex.SendAsync(HttpMethod.Post, "/v1.0/tenants", JsonSerializer.Serialize(body)).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "failed to create Verbex tenant for AssistantHub tenant " + tenant.Id + ": HTTP " + (int)resp.StatusCode + " " + respBody);
                    return null;
                }

                string verbexTenantId = ExtractString(respBody, "Data", "Tenant", "Identifier");
                if (String.IsNullOrEmpty(verbexTenantId))
                    verbexTenantId = ExtractString(respBody, "Data", "Tenant", "TenantId");

                if (String.IsNullOrEmpty(verbexTenantId))
                {
                    _Logging.Warn(_Header + "Verbex tenant response did not include a tenant identifier for AssistantHub tenant " + tenant.Id);
                    return null;
                }

                _Logging.Info(_Header + "created Verbex tenant " + verbexTenantId + " for AssistantHub tenant " + tenant.Id);
                return verbexTenantId;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception creating Verbex tenant for AssistantHub tenant " + tenant.Id + ": " + e.Message);
                return null;
            }
        }

        private async Task<string> ProvisionVerbexDefaultIndexAsync(string assistantHubTenantId, string verbexTenantId)
        {
            string indexId = BuildVerbexDefaultIndexId(assistantHubTenantId);

            try
            {
                object body = new
                {
                    Identifier = indexId,
                    TenantId = verbexTenantId,
                    Name = _Settings.Verbex.DefaultIndexId,
                    Description = "Default AssistantHub text search index for tenant " + assistantHubTenantId,
                    Labels = new[] { "assistanthub", "default" },
                    Tags = new Dictionary<string, string>
                    {
                        { "AssistantHubTenantId", assistantHubTenantId },
                        { "Purpose", "DocumentTextSearch" }
                    }
                };

                HttpResponseMessage resp = await _InvertedIndex.SendAsync(HttpMethod.Post, "/v1.0/indices", JsonSerializer.Serialize(body)).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _Logging.Info(_Header + "ensured Verbex default index " + indexId + " for AssistantHub tenant " + assistantHubTenantId);
                    return indexId;
                }

                _Logging.Warn(_Header + "failed to create Verbex default index " + indexId + " for AssistantHub tenant " + assistantHubTenantId + ": HTTP " + (int)resp.StatusCode + " " + respBody);
                return null;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception creating Verbex default index " + indexId + " for AssistantHub tenant " + assistantHubTenantId + ": " + e.Message);
                return null;
            }
        }

        private async Task DeleteVerbexTenantAsync(string verbexTenantId)
        {
            if (String.IsNullOrEmpty(verbexTenantId)) return;

            try
            {
                HttpResponseMessage resp = await _InvertedIndex.SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + Uri.EscapeDataString(verbexTenantId)).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    _Logging.Info(_Header + "deleted Verbex tenant " + verbexTenantId);
                else
                    _Logging.Warn(_Header + "failed to delete Verbex tenant " + verbexTenantId + ": HTTP " + (int)resp.StatusCode);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception deleting Verbex tenant " + verbexTenantId + ": " + e.Message);
            }
        }

        private async Task DeleteRecallDbTenantAsync(string tenantId)
        {
            try
            {
                HttpResponseMessage resp = await _VectorStore.SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId + "?force=true").ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                    _Logging.Info(_Header + "deleted RecallDB tenant " + tenantId);
                else
                    _Logging.Warn(_Header + "failed to delete RecallDB tenant " + tenantId + ": HTTP " + (int)resp.StatusCode);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception deleting RecallDB tenant " + tenantId + ": " + e.Message);
            }
        }

        private async Task ProvisionRecallDbTenantAsync(string tenantId, string tenantName)
        {
            try
            {
                string body = "{\"Id\":\"" + tenantId + "\",\"Name\":\"" + tenantName.Replace("\"", "\\\"") + "\"}";

                HttpResponseMessage resp = await _VectorStore.SendAsync(HttpMethod.Put, "/v1.0/tenants", body).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _Logging.Warn(_Header + "failed to create RecallDB tenant " + tenantId + ": " + (int)resp.StatusCode + " " + respBody);
                }
                else
                {
                    _Logging.Info(_Header + "created RecallDB tenant " + tenantId);
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception creating RecallDB tenant " + tenantId + ": " + e.Message);
            }
        }

        private async Task<string> ProvisionRecallDbCollectionAsync(string tenantId)
        {
            try
            {
                string body = "{\"Name\":\"default\"}";

                HttpResponseMessage resp = await _VectorStore.SendAsync(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/collections", body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                {
                    _Logging.Info(_Header + "created default RecallDB collection for tenant " + tenantId);
                    // Try to extract Id from response
                    try
                    {
                        IdentifierResponse result = System.Text.Json.JsonSerializer.Deserialize<IdentifierResponse>(respBody);
                        if (!String.IsNullOrEmpty(result?.Id))
                            return result.Id;
                        if (!String.IsNullOrEmpty(result?.GUID))
                            return result.GUID;
                    }
                    catch { }
                    return "default";
                }
                else
                {
                    _Logging.Warn(_Header + "failed to create RecallDB collection for tenant " + tenantId + ": " + (int)resp.StatusCode + " " + respBody);
                    return null;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception creating RecallDB collection for tenant " + tenantId + ": " + e.Message);
                return null;
            }
        }

        private string BuildVerbexDefaultIndexId(string assistantHubTenantId)
        {
            string configured = String.IsNullOrEmpty(_Settings.Verbex.DefaultIndexId) ? "default" : _Settings.Verbex.DefaultIndexId;
            if (String.Equals(assistantHubTenantId, Constants.DefaultTenantId, StringComparison.OrdinalIgnoreCase))
                return configured;

            return assistantHubTenantId + "_" + configured;
        }

        private static string ExtractString(string json, params string[] path)
        {
            if (String.IsNullOrEmpty(json) || path == null || path.Length < 1)
                return null;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement current = doc.RootElement;
                foreach (string segment in path)
                {
                    if (!TryGetPropertyCaseInsensitive(current, segment, out JsonElement next))
                        return null;
                    current = next;
                }

                return current.ValueKind == JsonValueKind.String ? current.GetString() : current.GetRawText();
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (String.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private async Task ProvisionS3BucketAsync(string bucketName)
        {
            if (_S3Client == null) return;

            try
            {
                await _S3Client.PutBucketAsync(bucketName).ConfigureAwait(false);
                _Logging.Info(_Header + "created S3 bucket " + bucketName);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _Logging.Info(_Header + "S3 bucket " + bucketName + " already exists");
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception creating S3 bucket " + bucketName + ": " + e.Message);
            }
        }

        private async Task DeleteTenantS3BucketsAsync(string tenantId)
        {
            if (_S3Client == null) return;

            try
            {
                ListBucketsResponse listResponse = await _S3Client.ListBucketsAsync().ConfigureAwait(false);
                string prefix = tenantId + "_";

                if (listResponse.Buckets != null)
                {
                    foreach (S3Bucket bucket in listResponse.Buckets.Where(b => b.BucketName.StartsWith(prefix)))
                    {
                        try
                        {
                            // Delete all objects in the bucket first
                            string continuationToken = null;
                            do
                            {
                                ListObjectsV2Request listReq = new ListObjectsV2Request
                                {
                                    BucketName = bucket.BucketName,
                                    ContinuationToken = continuationToken,
                                    MaxKeys = 1000
                                };

                                ListObjectsV2Response objResponse = await _S3Client.ListObjectsV2Async(listReq).ConfigureAwait(false);
                                if (objResponse.S3Objects != null)
                                {
                                    foreach (S3Object obj in objResponse.S3Objects)
                                    {
                                        await _S3Client.DeleteObjectAsync(bucket.BucketName, obj.Key).ConfigureAwait(false);
                                    }
                                }

                                continuationToken = (objResponse.IsTruncated == true) ? objResponse.NextContinuationToken : null;
                            } while (continuationToken != null);

                            await _S3Client.DeleteBucketAsync(bucket.BucketName).ConfigureAwait(false);
                            _Logging.Info(_Header + "deleted S3 bucket " + bucket.BucketName);
                        }
                        catch (Exception e)
                        {
                            _Logging.Warn(_Header + "failed to delete S3 bucket " + bucket.BucketName + ": " + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception deleting tenant S3 buckets for " + tenantId + ": " + e.Message);
            }
        }

        private static string ComputeSha256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private class VerbexProvisioningInfo
        {
            public string TenantId { get; set; }
            public string IndexId { get; set; }
        }
    }
}
