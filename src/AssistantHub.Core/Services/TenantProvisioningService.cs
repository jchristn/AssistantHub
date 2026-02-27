namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
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
        private static readonly HttpClient _HttpClient = new HttpClient();
        private readonly AmazonS3Client _S3Client;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TenantProvisioningService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));

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

            // Delete RecallDB tenant
            await DeleteRecallDbTenantAsync(tenantId).ConfigureAwait(false);

            // Delete tenant row
            await _Database.Tenant.DeleteByIdAsync(tenantId).ConfigureAwait(false);

            _Logging.Info(_Header + "deprovisioned tenant " + tenantId);
        }

        private async Task DeleteRecallDbTenantAsync(string tenantId)
        {
            try
            {
                string url = _Settings.RecallDb.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "?force=true";

                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Add("Authorization", "Bearer " + _Settings.RecallDb.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                string url = _Settings.RecallDb.Endpoint.TrimEnd('/') + "/v1.0/tenants";
                string body = "{\"Id\":\"" + tenantId + "\",\"Name\":\"" + tenantName.Replace("\"", "\\\"") + "\"}";

                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, url);
                req.Headers.Add("Authorization", "Bearer " + _Settings.RecallDb.AccessKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                string url = _Settings.RecallDb.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections";
                string body = "{\"Name\":\"default\"}";

                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, url);
                req.Headers.Add("Authorization", "Bearer " + _Settings.RecallDb.AccessKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                {
                    _Logging.Info(_Header + "created default RecallDB collection for tenant " + tenantId);
                    // Try to extract Id from response
                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(respBody);
                        if (doc.RootElement.TryGetProperty("Id", out var idElem))
                            return idElem.GetString();
                        if (doc.RootElement.TryGetProperty("GUID", out var guidElem))
                            return guidElem.GetString();
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
    }

    /// <summary>
    /// Result of tenant provisioning.
    /// </summary>
    public class TenantProvisioningResult
    {
        /// <summary>Tenant ID.</summary>
        public string TenantId { get; set; }

        /// <summary>Tenant name.</summary>
        public string TenantName { get; set; }

        /// <summary>Admin user ID.</summary>
        public string AdminUserId { get; set; }

        /// <summary>Admin email.</summary>
        public string AdminEmail { get; set; }

        /// <summary>Admin password (plaintext, for initial provisioning only).</summary>
        public string AdminPassword { get; set; }

        /// <summary>Default bearer token.</summary>
        public string BearerToken { get; set; }

        /// <summary>Admin user created during provisioning.</summary>
        public UserMaster User { get; set; }

        /// <summary>Credential created during provisioning.</summary>
        public Credential Credential { get; set; }
    }
}
