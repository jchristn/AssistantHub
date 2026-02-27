namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
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
    /// Handles S3 bucket CRUD routes using AmazonS3Client (admin only).
    /// </summary>
    public class BucketHandler : HandlerBase
    {
        private static readonly string _Header = "[BucketHandler] ";
        private readonly AmazonS3Client _S3Client;

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
        public BucketHandler(
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
            BasicAWSCredentials credentials = new BasicAWSCredentials(Settings.S3.AccessKey, Settings.S3.SecretKey);
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = Settings.S3.EndpointUrl,
                ForcePathStyle = true,
                UseHttp = !Settings.S3.UseSsl
            };

            _S3Client = new AmazonS3Client(credentials, config);
        }

        /// <summary>
        /// PUT /v1.0/buckets - Create a new bucket.
        /// Non-global-admin users have their tenant ID automatically prefixed to the bucket name.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutBucketAsync(HttpContextBase ctx)
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

                string body = ctx.Request.DataAsString;
                if (String.IsNullOrEmpty(body))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Request body is required."))).ConfigureAwait(false);
                    return;
                }

                Dictionary<string, object> parsed = Serializer.DeserializeJson<Dictionary<string, object>>(body);
                if (parsed == null || !parsed.ContainsKey("Name") || String.IsNullOrEmpty(parsed["Name"]?.ToString()))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Name is required."))).ConfigureAwait(false);
                    return;
                }

                string bucketName = parsed["Name"].ToString();

                // Non-global-admin users: auto-prefix bucket name with tenant ID
                if (!auth.IsGlobalAdmin)
                {
                    string tenantPrefix = auth.TenantId + "_";
                    if (!bucketName.StartsWith(tenantPrefix))
                        bucketName = tenantPrefix + bucketName;
                }

                await _S3Client.PutBucketAsync(bucketName).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Name = bucketName })).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Logging.Warn(_Header + "conflict in PutBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.Conflict, null, "Bucket already exists."))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in PutBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutBucketAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/buckets - List buckets.
        /// Non-global-admin users only see buckets prefixed with their tenant ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetBucketsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                ListBucketsResponse listResponse = await _S3Client.ListBucketsAsync().ConfigureAwait(false);

                List<object> buckets = new List<object>();
                if (listResponse.Buckets != null)
                {
                    string tenantPrefix = auth.IsGlobalAdmin ? null : auth.TenantId + "_";

                    foreach (S3Bucket bucket in listResponse.Buckets)
                    {
                        // Non-global-admin users only see their tenant's buckets
                        if (tenantPrefix != null && !bucket.BucketName.StartsWith(tenantPrefix))
                            continue;

                        buckets.Add(new { Name = bucket.BucketName, CreationDate = bucket.CreationDate });
                    }
                }

                var envelope = new
                {
                    Objects = buckets,
                    TotalRecords = buckets.Count
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(envelope)).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in GetBucketsAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetBucketsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/buckets/{name} - Get a specific bucket by name.
        /// Non-global-admin users can only access buckets prefixed with their tenant ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetBucketAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string name = ctx.Request.Url.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !name.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                ListBucketsResponse listResponse = await _S3Client.ListBucketsAsync().ConfigureAwait(false);
                S3Bucket found = listResponse.Buckets?.FirstOrDefault(b => b.BucketName == name);

                if (found == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Name = found.BucketName, CreationDate = found.CreationDate })).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in GetBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetBucketAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/buckets/{name} - Delete a bucket by name.
        /// Non-global-admin users can only delete buckets prefixed with their tenant ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteBucketAsync(HttpContextBase ctx)
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

                string name = ctx.Request.Url.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !name.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                await _S3Client.DeleteBucketAsync(name).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Logging.Warn(_Header + "conflict in DeleteBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.Conflict, null, "Bucket is not empty. Remove all objects before deleting."))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in DeleteBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteBucketAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// HEAD /v1.0/buckets/{name} - Check bucket existence.
        /// Non-global-admin users can only check buckets prefixed with their tenant ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadBucketAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                string name = ctx.Request.Url.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                if (!auth.IsGlobalAdmin && !name.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                await _S3Client.GetBucketLocationAsync(name).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in HeadBucketAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadBucketAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }

        #region Object-Operations

        /// <summary>
        /// GET /v1.0/buckets/{name}/objects - List objects in a bucket.
        /// Query params: prefix, delimiter (default "/").
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetObjectsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                if (String.IsNullOrEmpty(bucketName))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string prefix = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("prefix") ?? "");
                string delimiter = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("delimiter") ?? "/");

                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    Delimiter = delimiter,
                    MaxKeys = 1000
                };

                ListObjectsV2Response response = await _S3Client.ListObjectsV2Async(request).ConfigureAwait(false);

                List<object> prefixes = new List<object>();
                if (response.CommonPrefixes != null)
                {
                    foreach (string cp in response.CommonPrefixes)
                    {
                        // Skip the prefix itself if it appears as a common prefix
                        if (cp == prefix) continue;
                        prefixes.Add(new { Prefix = cp });
                    }
                }

                List<object> objects = new List<object>();
                if (response.S3Objects != null)
                {
                    foreach (S3Object obj in response.S3Objects)
                    {
                        // Skip the prefix itself if it appears as an object
                        if (obj.Key == prefix) continue;
                        objects.Add(new
                        {
                            Key = obj.Key,
                            Size = obj.Size,
                            LastModified = obj.LastModified,
                            ETag = obj.ETag
                        });
                    }
                }

                var envelope = new
                {
                    Prefix = prefix,
                    Delimiter = delimiter,
                    CommonPrefixes = prefixes,
                    Objects = objects,
                    TotalRecords = objects.Count
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(envelope)).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Bucket not found."))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in GetObjectsAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetObjectsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/buckets/{name}/objects/metadata - Get object metadata.
        /// Query param: key (required).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetObjectMetadataAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                string key = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("key") ?? "");
                if (String.IsNullOrEmpty(bucketName) || String.IsNullOrEmpty(key))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Bucket name and key are required."))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                GetObjectMetadataRequest request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                GetObjectMetadataResponse response = await _S3Client.GetObjectMetadataAsync(request).ConfigureAwait(false);

                Dictionary<string, string> metadata = new Dictionary<string, string>();
                if (response.Metadata?.Keys != null)
                {
                    foreach (string mk in response.Metadata.Keys)
                    {
                        metadata[mk] = response.Metadata[mk];
                    }
                }

                var result = new
                {
                    Key = key,
                    ContentLength = response.ContentLength,
                    ContentType = response.Headers?.ContentType,
                    LastModified = response.LastModified,
                    ETag = response.ETag,
                    Metadata = metadata
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in GetObjectMetadataAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetObjectMetadataAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/buckets/{name}/objects - Delete an object.
        /// Query param: key (required).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteObjectAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                string key = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("key") ?? "");
                if (String.IsNullOrEmpty(bucketName) || String.IsNullOrEmpty(key))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Bucket name and key are required."))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                if (key.EndsWith("/"))
                {
                    // Folder deletion: list and delete all child objects under this prefix
                    string continuationToken = null;
                    do
                    {
                        ListObjectsV2Request listRequest = new ListObjectsV2Request
                        {
                            BucketName = bucketName,
                            Prefix = key,
                            ContinuationToken = continuationToken,
                            MaxKeys = 1000
                        };

                        ListObjectsV2Response listResponse = await _S3Client.ListObjectsV2Async(listRequest).ConfigureAwait(false);

                        if (listResponse.S3Objects != null)
                        {
                            foreach (S3Object obj in listResponse.S3Objects)
                            {
                                await _S3Client.DeleteObjectAsync(new DeleteObjectRequest
                                {
                                    BucketName = bucketName,
                                    Key = obj.Key
                                }).ConfigureAwait(false);
                            }
                        }

                        continuationToken = (listResponse.IsTruncated == true) ? listResponse.NextContinuationToken : null;
                    } while (continuationToken != null);

                    // Attempt to delete the folder marker object itself (ignore 404)
                    try
                    {
                        await _S3Client.DeleteObjectAsync(new DeleteObjectRequest
                        {
                            BucketName = bucketName,
                            Key = key
                        }).ConfigureAwait(false);
                    }
                    catch (AmazonS3Exception) { }
                }
                else
                {
                    DeleteObjectRequest request = new DeleteObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key
                    };

                    await _S3Client.DeleteObjectAsync(request).ConfigureAwait(false);
                }

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in DeleteObjectAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteObjectAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/buckets/{name}/objects/download - Download an object.
        /// Query param: key (required).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DownloadObjectAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                string key = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("key") ?? "");
                if (String.IsNullOrEmpty(bucketName) || String.IsNullOrEmpty(key))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Bucket name and key are required."))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                GetObjectResponse response = await _S3Client.GetObjectAsync(request).ConfigureAwait(false);

                string filename = key.Contains("/") ? key.Substring(key.LastIndexOf('/') + 1) : key;
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = response.Headers?.ContentType ?? "application/octet-stream";
                ctx.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + filename + "\"");

                using (MemoryStream ms = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(ms).ConfigureAwait(false);
                    byte[] data = ms.ToArray();
                    ctx.Response.ContentLength = data.Length;
                    await ctx.Response.Send(data).ConfigureAwait(false);
                }
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in DownloadObjectAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DownloadObjectAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/buckets/{name}/objects - Create an empty object (directory marker).
        /// Query param: key (required).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutObjectAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                string key = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("key") ?? "");
                if (String.IsNullOrEmpty(bucketName) || String.IsNullOrEmpty(key))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Bucket name and key are required."))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = new MemoryStream(Array.Empty<byte>())
                };

                await _S3Client.PutObjectAsync(request).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Key = key })).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Bucket not found."))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in PutObjectAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutObjectAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/buckets/{name}/objects/upload - Upload a file.
        /// Query param: key (required).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task UploadObjectAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string bucketName = ctx.Request.Url.Parameters["name"];
                string key = Uri.UnescapeDataString(ctx.Request.Query.Elements.Get("key") ?? "");
                if (String.IsNullOrEmpty(bucketName) || String.IsNullOrEmpty(key))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Bucket name and key are required."))).ConfigureAwait(false);
                    return;
                }

                // Non-global-admin users can only access their tenant's buckets
                if (!auth.IsGlobalAdmin && !bucketName.StartsWith(auth.TenantId + "_"))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                byte[] data = ctx.Request.DataAsBytes ?? Array.Empty<byte>();

                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = new MemoryStream(data),
                    ContentType = ctx.Request.ContentType ?? "application/octet-stream"
                };

                await _S3Client.PutObjectAsync(request).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { Key = key, Size = data.Length })).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e) when (s3e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Bucket not found."))).ConfigureAwait(false);
            }
            catch (AmazonS3Exception s3e)
            {
                Logging.Warn(_Header + "S3 exception in UploadObjectAsync: " + s3e.Message);
                ctx.Response.StatusCode = (int)s3e.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, s3e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in UploadObjectAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
