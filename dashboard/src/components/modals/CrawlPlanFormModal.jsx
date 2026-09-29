import React, { useState, useEffect } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';
import PasswordInput from '../PasswordInput';

const REPOSITORY_TYPES = [
  { value: 'Web', label: 'Web' },
  { value: 'CIFS', label: 'CIFS File Server' },
  { value: 'NFS', label: 'NFS File Server' },
  { value: 'S3', label: 'Amazon S3 / S3-Compatible' },
  { value: 'AzureBlob', label: 'Azure Blob Storage' },
  { value: 'GoogleCloud', label: 'Google Cloud Storage' },
  { value: 'LocalDisk', label: 'Local Disk (Server Folder)' },
  { value: 'Git', label: 'Git Repository (GitHub)' },
];

// Help text, shown both as the label tooltip and as the input's hover title.
const HELP = {
  S3Endpoint: 'Leave empty for Amazon S3. For an S3-compatible store (MinIO, Less3, Ceph, Wasabi, Cloudflare R2) enter the service URL only, with scheme and port and without the bucket: http://minio.example.com:9000/ or https://s3.wasabisys.com/. http:// or https:// decides whether TLS is used. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host (host.docker.internal).',
  S3Region: 'AWS region of the bucket, for example us-east-1 or eu-west-2; required for Amazon S3. Most S3-compatible stores accept any value (us-east-1 is typical).',
  S3BucketName: 'Name of the bucket only, for example company-docs: not s3://company-docs, a URL, or a path. To crawl a folder in the bucket, enter the bucket here and the folder in Object Prefix under Filters (for example policies/).',
  S3AccessKey: 'Access key ID of an IAM user or role (or the store\'s equivalent) allowed to list and read the bucket: s3:ListBucket and s3:GetObject. Leave both keys empty only for a public bucket.',
  S3SecretKey: 'Secret access key paired with the access key ID. Stored with the crawl plan; treat crawl-plan JSON as sensitive.',
  AzureAccountName: 'Storage account name only: 3 to 24 lowercase letters and digits, for example contosodocs (from https://contosodocs.blob.core.windows.net). Not a URL or connection string. For the Azurite emulator use devstoreaccount1.',
  AzureAccessKey: 'Account access key (key1 or key2) from the storage account\'s Access keys page in the Azure portal. Stored with the crawl plan; treat crawl-plan JSON as sensitive. SAS tokens and connection strings are not accepted.',
  AzureContainer: 'Container name only: 3 to 63 lowercase letters, digits and hyphens, for example documents. To crawl a virtual folder inside it, enter the container here and the folder in Object Prefix under Filters (for example policies/).',
  AzureEndpoint: 'Leave empty for https://{account}.blob.core.windows.net/. Set it for Azurite (http://127.0.0.1:10000/devstoreaccount1/), sovereign clouds, or a custom domain. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host.',
  GcpProjectId: 'ID of the Google Cloud project that owns the bucket, for example contoso-prod-123456 (the project ID, not its display name or number).',
  GcpBucketName: 'Name of the bucket only, for example contoso-documents: not gs://contoso-documents or a path. To crawl a folder, enter the bucket here and the folder in Object Prefix under Filters (for example policies/).',
  GcpJsonCredentials: 'Paste the whole JSON key file of a service account that has the Storage Object Viewer role on the bucket (IAM & Admin > Service Accounts > Keys > Add key > JSON). Stored with the crawl plan; treat crawl-plan JSON as sensitive.',
  GcpEndpoint: 'Leave empty for Google Cloud Storage. Set it only for an emulator or private endpoint, for example http://localhost:4443/.',
  DiskPath: 'Absolute path of a folder on the AssistantHub server, for example /data/documents or D:\\Shared\\Documents. It must be one of the folders the operator allows in Crawl.AllowedLocalPaths (server settings), or inside one; otherwise the plan is rejected. In Docker the path is inside the container: the standard compose file mounts docker/assistanthub/crawl-sources read-only at /app/crawl-sources, so a folder handbook placed there is /app/crawl-sources/handbook. Files reached through symbolic links or junctions are skipped.',
  DiskSubdirectories: 'Crawl files in folders below the folder (and below Object Prefix, when set). When off, only files directly in the folder are crawled.',
  GitRepositoryUrl: 'URL of a GitHub repository: https://github.com/owner/repo, https://github.com/owner/repo.git or git@github.com:owner/repo.git. The default branch is crawled. Not a branch, folder or file URL (…/tree/main/docs): to crawl a folder, enter the repository URL and the folder in Object Prefix under Filters (for example docs/). Only github.com is supported.',
  GitAccessToken: 'GitHub personal access token. Required for private repositories: a fine-grained token with Contents: Read-only on the repository, or a classic token with the repo scope. Recommended for public repositories too: GitHub allows 60 API requests an hour without a token (one per folder) and 5,000 with one. Stored with the crawl plan; treat crawl-plan JSON as sensitive.',
};

const AUTH_TYPES = [
  { value: 'None', label: 'None' },
  { value: 'Basic', label: 'Basic' },
  { value: 'BearerToken', label: 'Bearer Token' },
  { value: 'ApiKey', label: 'API Key' },
];

const PREFIX_HELP = 'Only crawl objects whose key starts with this text. For every type except Web, the key is the path relative to the root of the share, export, bucket, container, folder or repository, with forward slashes and no leading slash: Policies/ crawls the Policies folder, Policies/2026/ a folder below it (in a Git repository, docs/ crawls the docs folder). Type folder names with their exact case: the prefix is case-sensitive on S3, Azure Blob, Google Cloud Storage, NFS and Linux folders (policies/ does not match Policies/), and case-insensitive on CIFS, Windows folders and Git. For web crawls the key is the page URL.';

// NFS crawling supports NFSv3 only (Blobject 6, OpenNFS).
const NFS_VERSIONS = ['V3'];
const INTERVAL_TYPES = ['Minutes', 'Hours', 'Days', 'Weeks'];

const defaultRepositories = {
  Web: {
    RepositoryType: 'Web',
    StartUrl: '',
    AuthType: 'None',
    Username: '',
    Password: '',
    BearerToken: '',
    ApiKeyHeader: '',
    ApiKey: '',
    UserAgent: '',
    FollowLinks: true,
    FollowRedirects: true,
    MaxRedirects: 10,
    CredentialOrigins: '',
    ExtractSitemapLinks: true,
    RestrictToChildUrls: true,
    RestrictToSubdomain: true,
    RestrictToRootDomain: true,
    IgnoreRobotsTxt: false,
    UseHeadlessBrowser: true,
    MaxDepth: 5,
    MaxParallelTasks: 4,
    CrawlDelayMs: 100,
  },
  CIFS: {
    RepositoryType: 'CIFS',
    CifsHostname: '',
    CifsUsername: '',
    CifsPassword: '',
    CifsShareName: '',
    CifsDomain: '',
    CifsPort: '',
    IncludeSubdirectories: true,
  },
  NFS: {
    RepositoryType: 'NFS',
    NfsHostname: '',
    NfsUserId: '',
    NfsGroupId: '',
    NfsShareName: '',
    NfsVersion: 'V3',
    NfsPort: '',
    NfsMountPort: '',
    NfsPortmapperPort: '',
    IncludeSubdirectories: true,
  },
  S3: {
    RepositoryType: 'S3',
    S3Endpoint: '',
    S3Region: 'us-east-1',
    S3BucketName: '',
    S3AccessKey: '',
    S3SecretKey: '',
  },
  AzureBlob: {
    RepositoryType: 'AzureBlob',
    AzureAccountName: '',
    AzureAccessKey: '',
    AzureContainer: '',
    AzureEndpoint: '',
  },
  GoogleCloud: {
    RepositoryType: 'GoogleCloud',
    GcpProjectId: '',
    GcpBucketName: '',
    GcpJsonCredentials: '',
    GcpEndpoint: '',
  },
  LocalDisk: {
    RepositoryType: 'LocalDisk',
    DiskPath: '',
    IncludeSubdirectories: true,
  },
  Git: {
    RepositoryType: 'Git',
    GitRepositoryUrl: '',
    GitAccessToken: '',
  },
};

const defaultSchedule = {
  IntervalType: 'Hours',
  IntervalValue: 24,
};

const defaultFilter = {
  ObjectPrefix: '',
  ObjectSuffix: '',
  AllowedContentTypes: '',
  MinimumSize: '',
  MaximumSize: '',
};

const defaultProcessing = {
  ProcessAdditions: true,
  ProcessUpdates: true,
  ProcessDeletions: true,
  MaxDrainTasks: 4,
};

const getRepositoryDefaults = (repositoryType) => ({
  ...(defaultRepositories[repositoryType] || defaultRepositories.Web),
});

// Location previews and common-mistake warnings for file-share crawl plans.
const trimSlashes = (value) => String(value || '').trim().replace(/^[\\/]+|[\\/]+$/g, '');

const describeCifsLocation = (repo) => {
  const host = String(repo?.CifsHostname || '').trim();
  const share = trimSlashes(repo?.CifsShareName);
  const warnings = [];
  if (/[\\/]/.test(host) || /^[a-z]+:/i.test(host)) warnings.push('Hostname must be only the server name or IPv4 address, without slashes or smb:// (for example fileserver.example.com).');
  if (/[\\/]/.test(share)) warnings.push('Share Name must be the share only. Put the folder part (' + share.split(/[\\/]/).slice(1).join('/') + '/) in Object Prefix under Filters.');
  const port = repo?.CifsPort ? ' (port ' + repo.CifsPort + ')' : '';
  const user = String(repo?.CifsUsername || '').trim();
  const domain = String(repo?.CifsDomain || '').trim();
  const account = user ? (domain && !user.includes('\\') ? domain + '\\' + user : user) : '';
  // A UNC path has no port, so the port is shown after it; nothing is shown while the hostname is invalid.
  const hostValid = host && !/[\\/]/.test(host) && !/^[a-z]+:/i.test(host);
  const location = hostValid && share ? '\\\\' + host + '\\' + share.split(/[\\/]/)[0] + port : null;
  return { location, detail: account ? 'as ' + account : null, warnings };
};

const describeNfsLocation = (repo) => {
  const host = String(repo?.NfsHostname || '').trim();
  const exportPath = String(repo?.NfsShareName || '').trim().replace(/\\/g, '/');
  const warnings = [];
  if (/[\\/]/.test(host) || host.includes(':') || /^[a-z]+:/i.test(host)) warnings.push('Hostname must be only the server name or IPv4 address, without the export path or nfs:// (for example nfs.example.com).');
  if (exportPath && !exportPath.startsWith('/')) warnings.push('Export Path must be the absolute path the server exports, starting with / (for example /exports/content).');
  const port = repo?.NfsPort ? ' (port ' + repo.NfsPort + ')' : '';
  const location = host && exportPath && !/[\\/:]/.test(host) ? host + ':' + exportPath + port : null;
  const uid = repo?.NfsUserId === '' || repo?.NfsUserId === null || repo?.NfsUserId === undefined ? null : repo.NfsUserId;
  const gid = repo?.NfsGroupId === '' || repo?.NfsGroupId === null || repo?.NfsGroupId === undefined ? null : repo.NfsGroupId;
  if (String(uid) === '0') warnings.push('UID 0 (root) is mapped to nobody on most servers (root_squash); use an account that can read the files unless the export allows no_root_squash.');
  return { location, detail: uid !== null && gid !== null ? 'as UID ' + uid + ', GID ' + gid + ', NFSv3' : null, warnings };
};

const isHttpUrl = (value) => /^https?:\/\/[^\s/]+/i.test(String(value || '').trim());

const describeS3Location = (repo) => {
  const bucket = String(repo?.S3BucketName || '').trim();
  const endpoint = String(repo?.S3Endpoint || '').trim();
  const region = String(repo?.S3Region || '').trim();
  const warnings = [];
  if (/^(s3|https?):/i.test(bucket)) warnings.push('Bucket Name must be the bucket only (for example company-docs), without s3:// or a URL.');
  else if (/[\\/]/.test(bucket)) warnings.push('Bucket Name must be the bucket only. Put the folder part (' + trimSlashes(bucket).split(/[\\/]/).slice(1).join('/') + '/) in Object Prefix under Filters.');
  if (endpoint && !isHttpUrl(endpoint)) warnings.push('Endpoint must start with http:// or https://, for example http://minio.example.com:9000/.');
  if (endpoint && isHttpUrl(endpoint) && bucket && new RegExp('/' + bucket.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '/?$', 'i').test(endpoint)) warnings.push('Endpoint should be the service URL without the bucket; the bucket goes in Bucket Name.');
  if (/amazonaws\.com/i.test(endpoint)) warnings.push('For Amazon S3, leave Endpoint empty and set Region instead.');
  if (!endpoint && !region) warnings.push('Region is required for Amazon S3 (for example us-east-1).');
  const hasAccess = !!String(repo?.S3AccessKey || '').trim();
  const hasSecret = !!String(repo?.S3SecretKey || '').trim();
  if (hasAccess !== hasSecret) warnings.push('Set both Access Key and Secret Key, or leave both empty for a public bucket.');
  const bucketValid = bucket && !/[\\/:]/.test(bucket);
  const location = bucketValid ? 's3://' + bucket : null;
  const where = endpoint ? 'at ' + endpoint : (region ? 'in ' + region : null);
  return { location, detail: where ? where + (hasAccess ? '' : ', anonymously') : null, warnings };
};

const describeAzureLocation = (repo) => {
  const account = String(repo?.AzureAccountName || '').trim();
  const container = String(repo?.AzureContainer || '').trim();
  const endpoint = String(repo?.AzureEndpoint || '').trim();
  const warnings = [];
  if (/[:/.]/.test(account) || /AccountName=/i.test(account)) warnings.push('Account Name must be the storage account name only (for example contosodocs), not a URL or connection string.');
  else if (account && !/^[a-z0-9]{3,24}$/.test(account)) warnings.push('Account Name must be 3 to 24 lowercase letters and digits.');
  if (/[\\/]/.test(trimSlashes(container))) warnings.push('Container must be the container only. Put the folder part (' + trimSlashes(container).split(/[\\/]/).slice(1).join('/') + '/) in Object Prefix under Filters.');
  else if (container && !/^(\$root|[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])?)$/.test(trimSlashes(container))) warnings.push('Container names are 3 to 63 lowercase letters, digits and hyphens.');
  if (endpoint && !isHttpUrl(endpoint)) warnings.push('Endpoint must start with http:// or https://.');
  const base = endpoint ? endpoint.replace(/\/+$/, '') : (account && /^[a-z0-9]{3,24}$/.test(account) ? 'https://' + account + '.blob.core.windows.net' : null);
  const location = base && container && !/[\\/]/.test(trimSlashes(container)) ? base + '/' + trimSlashes(container) : null;
  return { location, detail: null, warnings };
};

const describeGcpLocation = (repo) => {
  const bucket = String(repo?.GcpBucketName || '').trim();
  const project = String(repo?.GcpProjectId || '').trim();
  const json = String(repo?.GcpJsonCredentials || '').trim();
  const warnings = [];
  if (/^(gs|https?):/i.test(bucket)) warnings.push('Bucket Name must be the bucket only (for example contoso-documents), without gs:// or a URL.');
  else if (/[\\/]/.test(bucket)) warnings.push('Bucket Name must be the bucket only. Put the folder part (' + trimSlashes(bucket).split(/[\\/]/).slice(1).join('/') + '/) in Object Prefix under Filters.');
  let detail = null;
  if (json) {
    try {
      const key = JSON.parse(json);
      if (!key || !key.private_key || !key.client_email) warnings.push('The credentials JSON is not a service account key: it must contain private_key and client_email.');
      else detail = 'as ' + key.client_email;
      if (key?.project_id && project && key.project_id !== project) warnings.push('The key belongs to project ' + key.project_id + ', not ' + project + '. That works only if the service account was granted access to this bucket.');
    } catch {
      warnings.push('The credentials are not valid JSON; paste the whole service account key file.');
    }
  }
  const location = bucket && !/[\\/:]/.test(bucket) ? 'gs://' + bucket : null;
  return { location, detail, warnings };
};

const describeDiskLocation = (repo) => {
  const path = String(repo?.DiskPath || '').trim();
  const warnings = [];
  const absolute = path.startsWith('/') || /^[a-z]:[\\/]/i.test(path) || path.startsWith('\\\\');
  if (path && !absolute) warnings.push('Folder Path must be an absolute path on the AssistantHub server, for example /app/crawl-sources/handbook or D:\\Shared\\Documents.');
  if (/^\\\\/.test(path)) warnings.push('For a network share, use a CIFS crawl plan instead of a UNC path.');
  if (/^[a-z]:[\\/]/i.test(path)) warnings.push('A Windows drive path only works when AssistantHub runs directly on Windows, not in Docker.');
  return { location: path && absolute ? path : null, detail: 'on the AssistantHub server; it must be inside Crawl.AllowedLocalPaths', warnings };
};

const GITHUB_URL = /^(?:https?:\/\/github\.com\/|git@github\.com:)([A-Za-z0-9-]+)\/([A-Za-z0-9._-]+?)(?:\.git)?\/?$/i;

const describeGitLocation = (repo) => {
  const url = String(repo?.GitRepositoryUrl || '').trim();
  const warnings = [];
  let location = null;
  const match = url.match(GITHUB_URL);
  if (match) {
    location = 'github.com/' + match[1] + '/' + match[2];
  } else if (url) {
    const deep = url.match(/^https?:\/\/github\.com\/([^/]+)\/([^/]+)\/(?:tree|blob)\/[^/]+\/?(.*)$/i);
    if (deep) {
      warnings.push('Enter the repository URL (https://github.com/' + deep[1] + '/' + deep[2].replace(/\.git$/i, '') + ') and ' + (deep[3] ? 'put ' + deep[3].replace(/\/?$/, '/') + ' in Object Prefix under Filters' : 'the default branch is crawled') + '. Branch, folder and file URLs are not accepted.');
    } else if (/^https?:\/\//i.test(url) || url.startsWith('git@')) {
      warnings.push('Only github.com repositories are supported, as https://github.com/owner/repo.');
    } else {
      warnings.push('Enter the full repository URL, for example https://github.com/owner/repo.');
    }
  }
  const detail = String(repo?.GitAccessToken || '').trim()
    ? '(default branch) with the access token'
    : '(default branch) without a token: public repositories only, 60 GitHub API requests an hour';
  return { location, detail: location ? detail : null, warnings };
};

const renderSharePreview = ({ location, detail, warnings }) => (
  <>
    {location && (
      <p className="settings-help" title="The location the crawler connects to, built from the fields above.">
        Crawls <code>{location}</code>{detail ? ' ' + detail : ''}. Use Object Prefix under Filters to limit the crawl to a folder.
      </p>
    )}
    {warnings.map((warning) => (
      <div key={warning} className="tool-policy-warning">{warning}</div>
    ))}
  </>
);

const normalizeRepositorySettings = (repoSettings, repositoryType) => {
  const selectedType = repoSettings?.RepositoryType || repositoryType || 'Web';
  const defaults = getRepositoryDefaults(selectedType);
  const normalized = {
    ...defaults,
    ...(repoSettings || {}),
  };

  if (selectedType === 'Web') {
    normalized.AuthType = repoSettings?.AuthenticationType || repoSettings?.AuthType || defaults.AuthType;
    normalized.ApiKey = repoSettings?.ApiKeyValue || repoSettings?.ApiKey || defaults.ApiKey;
    normalized.ApiKeyHeader = repoSettings?.ApiKeyHeader || defaults.ApiKeyHeader;
    normalized.CredentialOrigins = Array.isArray(repoSettings?.CredentialOrigins)
      ? repoSettings.CredentialOrigins.join('\n')
      : (repoSettings?.CredentialOrigins || defaults.CredentialOrigins);
  }

  if (selectedType === 'NFS') {
    normalized.NfsUserId = repoSettings?.NfsUserId ?? defaults.NfsUserId;
    normalized.NfsGroupId = repoSettings?.NfsGroupId ?? defaults.NfsGroupId;
  }

  return normalized;
};

const parseNumberOrDefault = (value, fallback) => {
  const parsed = parseInt(value, 10);
  return Number.isNaN(parsed) ? fallback : parsed;
};

const parseOptionalNumber = (value) => {
  if (value === '' || value === null || value === undefined) return undefined;
  const parsed = parseInt(value, 10);
  return Number.isNaN(parsed) ? undefined : parsed;
};

const isNonNegativeInteger = (value) => {
  if (value === '' || value === null || value === undefined) return false;
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 0;
};

function CrawlPlanFormModal({ plan, initialData, ingestionRules, buckets, onSave, onTestConnectivity, onClose }) {
  const isEdit = !!plan;
  const source = plan || initialData;

  const repoSettings = source?.RepositorySettings;
  const ingestionSettings = source?.IngestionSettings;
  const filterSettings = source?.Filter;
  const initialRepositoryType = source?.RepositoryType || repoSettings?.RepositoryType || 'Web';

  const [form, setForm] = useState({
    Name: source?.Name || '',
    RepositoryType: initialRepositoryType,
    IngestionRuleId: ingestionSettings?.IngestionRuleId || '',
    StoreInS3: ingestionSettings?.StoreInS3 ?? false,
    S3BucketName: ingestionSettings?.S3BucketName || '',
    Repository: normalizeRepositorySettings(repoSettings, initialRepositoryType),
    Schedule: source?.Schedule ? { ...defaultSchedule, ...source.Schedule } : { ...defaultSchedule },
    Filter: filterSettings ? {
      ObjectPrefix: filterSettings.ObjectPrefix || '',
      ObjectSuffix: filterSettings.ObjectSuffix || '',
      AllowedContentTypes: (filterSettings.AllowedContentTypes || []).join(', '),
      MinimumSize: filterSettings.MinimumSize ?? '',
      MaximumSize: filterSettings.MaximumSize ?? '',
    } : { ...defaultFilter },
    Processing: {
      ProcessAdditions: source?.ProcessAdditions ?? defaultProcessing.ProcessAdditions,
      ProcessUpdates: source?.ProcessUpdates ?? defaultProcessing.ProcessUpdates,
      ProcessDeletions: source?.ProcessDeletions ?? defaultProcessing.ProcessDeletions,
      MaxDrainTasks: source?.MaxDrainTasks ?? defaultProcessing.MaxDrainTasks,
    },
    RetentionDays: source?.RetentionDays ?? 7,
  });

  // Auto-select S3 bucket if there is exactly one
  useEffect(() => {
    if (!form.S3BucketName && buckets && buckets.length === 1) {
      const name = buckets[0].Name || buckets[0].name || buckets[0];
      handleChange('S3BucketName', name);
    }
  }, [buckets]);

  const [saving, setSaving] = useState(false);
  const [testingConnectivity, setTestingConnectivity] = useState(false);
  const [connectivityResult, setConnectivityResult] = useState(null);
  const [generalOpen, setGeneralOpen] = useState(true);
  const [ingestionOpen, setIngestionOpen] = useState(false);
  const [repoOpen, setRepoOpen] = useState(false);
  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [filterOpen, setFilterOpen] = useState(false);
  const [processingOpen, setProcessingOpen] = useState(false);
  const [retentionOpen, setRetentionOpen] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleRepoChange = (field, value) => {
    setConnectivityResult(null);
    setForm(prev => ({
      ...prev,
      Repository: { ...prev.Repository, [field]: value }
    }));
  };

  const handleRepositoryTypeChange = (repositoryType) => {
    setConnectivityResult(null);
    setForm(prev => ({
      ...prev,
      RepositoryType: repositoryType,
      Repository: getRepositoryDefaults(repositoryType),
    }));
  };

  const handleScheduleChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Schedule: { ...prev.Schedule, [field]: value }
    }));
  };

  const handleFilterChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Filter: { ...prev.Filter, [field]: value }
    }));
  };

  const handleProcessingChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Processing: { ...prev.Processing, [field]: value }
    }));
  };

  const buildRepositorySettings = () => {
    if (form.RepositoryType === 'CIFS') {
      return {
        RepositoryType: 'CIFS',
        CifsHostname: form.Repository.CifsHostname,
        CifsUsername: form.Repository.CifsUsername,
        CifsPassword: form.Repository.CifsPassword,
        CifsShareName: form.Repository.CifsShareName,
        CifsDomain: form.Repository.CifsDomain?.trim() || null,
        CifsPort: parseOptionalNumber(form.Repository.CifsPort) ?? null,
        IncludeSubdirectories: form.Repository.IncludeSubdirectories,
      };
    }

    if (form.RepositoryType === 'NFS') {
      return {
        RepositoryType: 'NFS',
        NfsHostname: form.Repository.NfsHostname,
        NfsUserId: parseOptionalNumber(form.Repository.NfsUserId),
        NfsGroupId: parseOptionalNumber(form.Repository.NfsGroupId),
        NfsShareName: form.Repository.NfsShareName,
        NfsVersion: form.Repository.NfsVersion || 'V3',
        NfsPort: parseOptionalNumber(form.Repository.NfsPort) ?? null,
        NfsMountPort: parseOptionalNumber(form.Repository.NfsMountPort) ?? null,
        NfsPortmapperPort: parseOptionalNumber(form.Repository.NfsPortmapperPort) ?? null,
        IncludeSubdirectories: form.Repository.IncludeSubdirectories,
      };
    }

    const optional = (value) => String(value || '').trim() || null;

    if (form.RepositoryType === 'S3') {
      return {
        RepositoryType: 'S3',
        S3Endpoint: optional(form.Repository.S3Endpoint),
        S3Region: optional(form.Repository.S3Region),
        S3BucketName: String(form.Repository.S3BucketName || '').trim(),
        S3AccessKey: optional(form.Repository.S3AccessKey),
        S3SecretKey: optional(form.Repository.S3SecretKey),
      };
    }

    if (form.RepositoryType === 'AzureBlob') {
      return {
        RepositoryType: 'AzureBlob',
        AzureAccountName: String(form.Repository.AzureAccountName || '').trim(),
        AzureAccessKey: String(form.Repository.AzureAccessKey || '').trim(),
        AzureContainer: String(form.Repository.AzureContainer || '').trim(),
        AzureEndpoint: optional(form.Repository.AzureEndpoint),
      };
    }

    if (form.RepositoryType === 'GoogleCloud') {
      return {
        RepositoryType: 'GoogleCloud',
        GcpProjectId: String(form.Repository.GcpProjectId || '').trim(),
        GcpBucketName: String(form.Repository.GcpBucketName || '').trim(),
        GcpJsonCredentials: form.Repository.GcpJsonCredentials,
        GcpEndpoint: optional(form.Repository.GcpEndpoint),
      };
    }

    if (form.RepositoryType === 'LocalDisk') {
      return {
        RepositoryType: 'LocalDisk',
        DiskPath: String(form.Repository.DiskPath || '').trim(),
        IncludeSubdirectories: form.Repository.IncludeSubdirectories,
      };
    }

    if (form.RepositoryType === 'Git') {
      return {
        RepositoryType: 'Git',
        GitRepositoryUrl: String(form.Repository.GitRepositoryUrl || '').trim(),
        GitAccessToken: optional(form.Repository.GitAccessToken),
      };
    }

    return {
      RepositoryType: 'Web',
      StartUrl: form.Repository.StartUrl,
      AuthenticationType: form.Repository.AuthType,
      ...(form.Repository.AuthType === 'Basic' ? { Username: form.Repository.Username, Password: form.Repository.Password } : {}),
      ...(form.Repository.AuthType === 'BearerToken' ? { BearerToken: form.Repository.BearerToken } : {}),
      ...(form.Repository.AuthType === 'ApiKey' ? { ApiKeyHeader: form.Repository.ApiKeyHeader, ApiKeyValue: form.Repository.ApiKey } : {}),
      UserAgent: form.Repository.UserAgent || undefined,
      FollowLinks: form.Repository.FollowLinks,
      FollowRedirects: form.Repository.FollowRedirects,
      MaxRedirects: Math.min(50, Math.max(1, parseNumberOrDefault(form.Repository.MaxRedirects, 10))),
      CredentialOrigins: String(form.Repository.CredentialOrigins || '').split(/[\s,]+/).map(s => s.trim()).filter(Boolean),
      ExtractSitemapLinks: form.Repository.ExtractSitemapLinks,
      RestrictToChildUrls: form.Repository.RestrictToChildUrls,
      RestrictToSubdomain: form.Repository.RestrictToSubdomain,
      RestrictToRootDomain: form.Repository.RestrictToRootDomain,
      IgnoreRobotsTxt: form.Repository.IgnoreRobotsTxt,
      UseHeadlessBrowser: form.Repository.UseHeadlessBrowser,
      MaxDepth: parseNumberOrDefault(form.Repository.MaxDepth, 5),
      MaxParallelTasks: parseNumberOrDefault(form.Repository.MaxParallelTasks, 8),
      CrawlDelayMs: parseNumberOrDefault(form.Repository.CrawlDelayMs, 100),
    };
  };

  const isRepositoryValid = () => {
    if (form.RepositoryType === 'CIFS') {
      return !!(
        form.Repository.CifsHostname?.trim()
        && form.Repository.CifsUsername?.trim()
        && form.Repository.CifsPassword?.trim()
        && form.Repository.CifsShareName?.trim()
      );
    }

    if (form.RepositoryType === 'NFS') {
      return !!(
        form.Repository.NfsHostname?.trim()
        && isNonNegativeInteger(form.Repository.NfsUserId)
        && isNonNegativeInteger(form.Repository.NfsGroupId)
        && form.Repository.NfsShareName?.trim()
      );
    }

    if (form.RepositoryType === 'S3') {
      const r = form.Repository;
      return !!(
        r.S3BucketName?.trim()
        && (r.S3Endpoint?.trim() || r.S3Region?.trim())
        && !!r.S3AccessKey?.trim() === !!r.S3SecretKey?.trim()
      );
    }

    if (form.RepositoryType === 'AzureBlob') {
      return !!(form.Repository.AzureAccountName?.trim() && form.Repository.AzureAccessKey?.trim() && form.Repository.AzureContainer?.trim());
    }

    if (form.RepositoryType === 'GoogleCloud') {
      return !!(form.Repository.GcpProjectId?.trim() && form.Repository.GcpBucketName?.trim() && form.Repository.GcpJsonCredentials?.trim());
    }

    if (form.RepositoryType === 'LocalDisk') {
      return !!form.Repository.DiskPath?.trim();
    }

    if (form.RepositoryType === 'Git') {
      return GITHUB_URL.test(String(form.Repository.GitRepositoryUrl || '').trim());
    }

    return !!form.Repository.StartUrl?.trim();
  };

  const buildCrawlPlanPayload = () => {
    const data = {
      Name: form.Name,
      RepositoryType: form.RepositoryType,
      IngestionSettings: {
        IngestionRuleId: form.IngestionRuleId || undefined,
        StoreInS3: form.StoreInS3,
        S3BucketName: form.StoreInS3 ? form.S3BucketName : undefined,
      },
      RepositorySettings: buildRepositorySettings(),
      Schedule: {
        IntervalType: form.Schedule.IntervalType,
        IntervalValue: parseNumberOrDefault(form.Schedule.IntervalValue, 24),
      },
      Filter: {
        ObjectPrefix: form.Filter.ObjectPrefix || undefined,
        ObjectSuffix: form.Filter.ObjectSuffix || undefined,
        AllowedContentTypes: form.Filter.AllowedContentTypes
          ? form.Filter.AllowedContentTypes.split(',').map(s => s.trim()).filter(Boolean)
          : undefined,
        MinimumSize: parseOptionalNumber(form.Filter.MinimumSize),
        MaximumSize: parseOptionalNumber(form.Filter.MaximumSize),
      },
      ProcessAdditions: form.Processing.ProcessAdditions,
      ProcessUpdates: form.Processing.ProcessUpdates,
      ProcessDeletions: form.Processing.ProcessDeletions,
      MaxDrainTasks: parseNumberOrDefault(form.Processing.MaxDrainTasks, 8),
      RetentionDays: form.RetentionDays !== '' ? parseNumberOrDefault(form.RetentionDays, 7) : 7,
    };

    if (isEdit && (plan.Id || plan.GUID)) {
      data.Id = plan.Id || plan.GUID;
      data.GUID = plan.GUID || plan.Id;
    }

    return data;
  };

  const handleTestConnectivity = async () => {
    if (!onTestConnectivity || testingConnectivity || !isRepositoryValid()) return;

    setTestingConnectivity(true);
    setConnectivityResult({ success: null, message: 'Testing repository connectivity...' });

    try {
      const result = await onTestConnectivity(buildCrawlPlanPayload());
      const success = result?.Success ?? result?.success ?? false;
      setConnectivityResult({
        success,
        message: result?.Message || result?.message || (success ? 'Repository connectivity verified.' : 'Repository is not reachable with the supplied settings.'),
      });
    } catch (err) {
      setConnectivityResult({
        success: false,
        message: err.message || 'Connectivity test failed.',
      });
    } finally {
      setTestingConnectivity(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSave(buildCrawlPlanPayload());
    } finally {
      setSaving(false);
    }
  };

  const collapsibleButtonStyle = {
    background: 'none',
    border: 'none',
    padding: 0,
    cursor: 'pointer',
    fontSize: '0.95rem',
    fontWeight: 600,
    color: 'var(--text-primary)',
  };

  return (
    <Modal
      title={isEdit ? 'Edit Crawl Plan' : 'Create Crawl Plan'}
      onClose={onClose}
      wide
      footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button
            className="btn btn-primary"
            onClick={handleSubmit}
            disabled={saving || !form.Name.trim() || !isRepositoryValid()}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </>
      }
    >
      <form onSubmit={handleSubmit}>
        {/* General (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setGeneralOpen(prev => !prev)}>
            {generalOpen ? '\u25BE' : '\u25B8'} General
          </button>
          {generalOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-row">
                <div className="form-group">
                  <label><Tooltip text="Display name for this crawl plan">Name</Tooltip></label>
                  <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} required />
                </div>
                <div className="form-group">
                  <label><Tooltip text="Repository type to crawl">Repository Type</Tooltip></label>
                  <select value={form.RepositoryType} onChange={(e) => handleRepositoryTypeChange(e.target.value)}>
                    {REPOSITORY_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                  </select>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Ingestion (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setIngestionOpen(prev => !prev)}>
            {ingestionOpen ? '\u25BE' : '\u25B8'} Ingestion
          </button>
          {ingestionOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Ingestion rule used to process crawled documents">Ingestion Rule</Tooltip></label>
                <select value={form.IngestionRuleId} onChange={(e) => handleChange('IngestionRuleId', e.target.value)}>
                  <option value="">-- Select Ingestion Rule --</option>
                  {(ingestionRules || []).map(r => (
                    <option key={r.GUID || r.Id} value={r.GUID || r.Id}>{r.Name || r.GUID || r.Id}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.StoreInS3} onChange={(e) => handleChange('StoreInS3', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Store crawled content in an S3 bucket">Store in S3</Tooltip></span>
                </div>
              </div>
              {form.StoreInS3 && (
                <div className="form-group">
                  <label><Tooltip text="S3 bucket to store crawled content">S3 Bucket Name</Tooltip></label>
                  <select value={form.S3BucketName} onChange={(e) => handleChange('S3BucketName', e.target.value)}>
                    <option value="">-- Select Bucket --</option>
                    {(buckets || []).map(b => {
                      const name = b.Name || b.name || b;
                      return <option key={name} value={name}>{name}</option>;
                    })}
                  </select>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Repository Settings (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setRepoOpen(prev => !prev)}>
            {repoOpen ? '\u25BE' : '\u25B8'} Repository Settings
          </button>
          {repoOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              {form.RepositoryType === 'Web' && (
                <>
              <div className="form-group">
                <label><Tooltip text="Starting URL for the web crawl">Start URL</Tooltip></label>
                <input type="text" value={form.Repository.StartUrl} onChange={(e) => handleRepoChange('StartUrl', e.target.value)} placeholder="https://example.com" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Authentication type for accessing the repository">Auth Type</Tooltip></label>
                <select value={form.Repository.AuthType} onChange={(e) => handleRepoChange('AuthType', e.target.value)}>
                  {AUTH_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              {form.Repository.AuthType !== 'None' && (
                <div className="form-group">
                  <label><Tooltip text="Credentials are sent only to the start URL's site. List other sites (one per line, for example https://docs.example.com) that should also receive them.">Credential Origins</Tooltip></label>
                  <textarea className="form-input" rows={2} title="Other sites that receive the crawl credentials, one per line." value={form.Repository.CredentialOrigins} onChange={(e) => handleRepoChange('CredentialOrigins', e.target.value)} placeholder="https://docs.example.com" />
                </div>
              )}
              {form.Repository.AuthType === 'Basic' && (
                <div className="form-row">
                  <div className="form-group">
                    <label><Tooltip text="Username for basic authentication">Username</Tooltip></label>
                    <input type="text" value={form.Repository.Username} onChange={(e) => handleRepoChange('Username', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label><Tooltip text="Password for basic authentication">Password</Tooltip></label>
                    <PasswordInput value={form.Repository.Password} onChange={(e) => handleRepoChange('Password', e.target.value)} />
                  </div>
                </div>
              )}
              {form.Repository.AuthType === 'BearerToken' && (
                <div className="form-group">
                  <label><Tooltip text="Bearer token for authentication">Bearer Token</Tooltip></label>
                  <PasswordInput value={form.Repository.BearerToken} onChange={(e) => handleRepoChange('BearerToken', e.target.value)} />
                </div>
              )}
              {form.Repository.AuthType === 'ApiKey' && (
                <div className="form-row">
                  <div className="form-group">
                    <label><Tooltip text="Header name used to send the API key">API Key Header</Tooltip></label>
                    <input type="text" value={form.Repository.ApiKeyHeader} onChange={(e) => handleRepoChange('ApiKeyHeader', e.target.value)} placeholder="x-api-key" />
                  </div>
                  <div className="form-group">
                    <label><Tooltip text="API key for authentication">API Key</Tooltip></label>
                    <PasswordInput value={form.Repository.ApiKey} onChange={(e) => handleRepoChange('ApiKey', e.target.value)} />
                  </div>
                </div>
              )}
              <div className="form-group">
                <label><Tooltip text="User agent string sent with HTTP requests. Default: assistanthub-crawler">User Agent</Tooltip></label>
                <input type="text" value={form.Repository.UserAgent} onChange={(e) => handleRepoChange('UserAgent', e.target.value)} placeholder="assistanthub-crawler" />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.FollowLinks} onChange={(e) => handleRepoChange('FollowLinks', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Follow hyperlinks discovered on crawled pages">Follow Links</Tooltip></span>
                  </div>
                </div>
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.FollowRedirects} onChange={(e) => handleRepoChange('FollowRedirects', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Follow HTTP redirects (301, 302, etc.), up to Max Redirects hops. A redirect loop ends the chain and the page is skipped. When off, redirecting pages are skipped.">Follow Redirects</Tooltip></span>
                  </div>
                </div>
                {form.Repository.FollowRedirects && (
                  <div className="form-group">
                    <label><Tooltip text="Most redirects followed for one page (1-50, default 10). A longer chain is skipped.">Max Redirects</Tooltip></label>
                    <input type="number" title="Most redirects followed for one page (1-50)." value={form.Repository.MaxRedirects} onChange={(e) => handleRepoChange('MaxRedirects', e.target.value)} min="1" max="50" />
                  </div>
                )}
              </div>
              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.ExtractSitemapLinks} onChange={(e) => handleRepoChange('ExtractSitemapLinks', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Extract and follow URLs found in the root domain sitemap.xml (e.g. example.com/sitemap.xml, not example.com/path/sitemap.xml)">Extract Sitemap Links</Tooltip></span>
                  </div>
                </div>
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.RestrictToChildUrls} onChange={(e) => handleRepoChange('RestrictToChildUrls', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Only crawl URLs that are children of the start URL path">Restrict to Child URLs</Tooltip></span>
                  </div>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.RestrictToSubdomain} onChange={(e) => handleRepoChange('RestrictToSubdomain', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Only crawl URLs within the same subdomain as the start URL">Restrict to Subdomain</Tooltip></span>
                  </div>
                </div>
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.RestrictToRootDomain} onChange={(e) => handleRepoChange('RestrictToRootDomain', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Only crawl URLs within the same root domain">Restrict to Root Domain</Tooltip></span>
                  </div>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.IgnoreRobotsTxt} onChange={(e) => handleRepoChange('IgnoreRobotsTxt', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Ignore robots.txt rules when crawling">Ignore robots.txt</Tooltip></span>
                  </div>
                </div>
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Repository.UseHeadlessBrowser} onChange={(e) => handleRepoChange('UseHeadlessBrowser', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Use a headless browser to render JavaScript-heavy pages">Use Headless Browser</Tooltip></span>
                  </div>
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label><Tooltip text="Maximum depth to follow links from the start URL. Range: 1-100, default: 5">Max Depth</Tooltip></label>
                  <input type="number" value={form.Repository.MaxDepth} onChange={(e) => handleRepoChange('MaxDepth', e.target.value)} min="1" max="100" />
                </div>
                <div className="form-group">
                  <label><Tooltip text="Maximum number of pages to crawl concurrently. Range: 1-64, default: 8">Max Parallel Tasks</Tooltip></label>
                  <input type="number" value={form.Repository.MaxParallelTasks} onChange={(e) => handleRepoChange('MaxParallelTasks', e.target.value)} min="1" max="64" />
                </div>
              </div>
              <div className="form-group">
                <label><Tooltip text="Delay in milliseconds between consecutive requests to the same host. Range: 0-60000, default: 100">Crawl Delay (ms)</Tooltip></label>
                <input type="number" value={form.Repository.CrawlDelayMs} onChange={(e) => handleRepoChange('CrawlDelayMs', e.target.value)} min="0" max="60000" step="100" />
              </div>
                </>
              )}
              {form.RepositoryType === 'CIFS' && (
                <>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="DNS name or IPv4 address of the SMB/CIFS file server, without slashes or a protocol: fileserver.example.com or 10.0.0.12, not \\fileserver\share or smb://fileserver. Put the share in Share Name and a subfolder in Object Prefix. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host (host.docker.internal).">Hostname</Tooltip></label>
                      <input type="text" title="DNS name or IPv4 address of the SMB/CIFS file server, without slashes or a protocol: fileserver.example.com or 10.0.0.12, not \\fileserver\share or smb://fileserver. Put the share in Share Name and a subfolder in Object Prefix. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host (host.docker.internal)." value={form.Repository.CifsHostname} onChange={(e) => handleRepoChange('CifsHostname', e.target.value)} placeholder="fileserver.example.com" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="Name of the share only, as it appears after the server in a UNC path: for \\fileserver\Documents enter Documents. Leading and trailing slashes are ignored, but a subfolder cannot be part of the share name: to crawl \\fileserver\Documents\Policies, enter Documents here and Policies/ in Object Prefix under Filters. Administrative shares such as C$ work when the account may use them.">Share Name</Tooltip></label>
                      <input type="text" title="Name of the share only, as it appears after the server in a UNC path: for \\fileserver\Documents enter Documents. Leading and trailing slashes are ignored, but a subfolder cannot be part of the share name: to crawl \\fileserver\Documents\Policies, enter Documents here and Policies/ in Object Prefix under Filters. Administrative shares such as C$ work when the account may use them." value={form.Repository.CifsShareName} onChange={(e) => handleRepoChange('CifsShareName', e.target.value)} placeholder="Documents" />
                    </div>
                  </div>
                  {renderSharePreview(describeCifsLocation(form.Repository))}
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="Active Directory domain (for example CORP) or workgroup (usually WORKGROUP) of the account. Leave empty for a local server account, or when Username already includes DOMAIN\user.">Domain</Tooltip></label>
                      <input type="text" title="Active Directory domain (for example CORP) or workgroup (usually WORKGROUP) of the account. Leave empty for a local server account, or when Username already includes DOMAIN\user." value={form.Repository.CifsDomain ?? ''} onChange={(e) => handleRepoChange('CifsDomain', e.target.value)} placeholder="Optional" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="TCP port of the SMB server. Leave empty for the standard port 445; set it only for servers on a non-standard port.">Port</Tooltip></label>
                      <input type="number" title="TCP port of the SMB server. Leave empty for the standard port 445; set it only for servers on a non-standard port." value={form.Repository.CifsPort ?? ''} onChange={(e) => handleRepoChange('CifsPort', e.target.value)} min="1" max="65535" placeholder="445" />
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="Account used to sign in to the share. Enter the user name, or DOMAIN\user (for example CORP\svc-crawler) for a domain account; alternatively put the domain in Domain. user@domain.com also works where the server accepts it. The account needs read access to the share and every folder to crawl.">Username</Tooltip></label>
                      <input type="text" title="Account used to sign in to the share. Enter the user name, or DOMAIN\user (for example CORP\svc-crawler) for a domain account; alternatively put the domain in Domain. user@domain.com also works where the server accepts it. The account needs read access to the share and every folder to crawl." value={form.Repository.CifsUsername} onChange={(e) => handleRepoChange('CifsUsername', e.target.value)} placeholder="svc-crawler or CORP\svc-crawler" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="Password of the account. Stored with the crawl plan; treat crawl-plan JSON as sensitive.">Password</Tooltip></label>
                      <PasswordInput title="Password of the account. Stored with the crawl plan; treat crawl-plan JSON as sensitive." value={form.Repository.CifsPassword} onChange={(e) => handleRepoChange('CifsPassword', e.target.value)} />
                    </div>
                  </div>
                  <div className="form-group">
                    <div className="form-toggle">
                      <label className="toggle-switch">
                        <input type="checkbox" checked={form.Repository.IncludeSubdirectories} onChange={(e) => handleRepoChange('IncludeSubdirectories', e.target.checked)} />
                        <span className="toggle-slider"></span>
                      </label>
                      <span><Tooltip text="Crawl files in folders below the share root (and below Object Prefix, when set). When off, only files directly in the share root (or the prefix folder) are crawled.">Include Subdirectories</Tooltip></span>
                    </div>
                  </div>
                </>
              )}
              {form.RepositoryType === 'NFS' && (
                <>
                  <div className="form-group">
                    <label><Tooltip text="DNS name or IPv4 address of the NFS server, without the export path: nfs.example.com or 10.0.0.20, not nfs.example.com:/exports/content. Put the export in Export Path. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host (host.docker.internal).">Hostname</Tooltip></label>
                    <input type="text" title="DNS name or IPv4 address of the NFS server, without the export path: nfs.example.com or 10.0.0.20, not nfs.example.com:/exports/content. Put the export in Export Path. When AssistantHub runs in Docker, localhost and 127.0.0.1 are sent to the Docker host (host.docker.internal)." value={form.Repository.NfsHostname} onChange={(e) => handleRepoChange('NfsHostname', e.target.value)} placeholder="nfs.example.com" />
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="Numeric Unix user ID sent with every request (AUTH_SYS), for example 1000. Files are read with this identity's permissions, so it needs read access to the files and execute access to the folders. Many servers map 0 (root) to nobody (root_squash); use an ordinary UID unless the export allows no_root_squash.">User ID</Tooltip></label>
                      <input type="number" title="Numeric Unix user ID sent with every request (AUTH_SYS), for example 1000. Files are read with this identity's permissions, so it needs read access to the files and execute access to the folders. Many servers map 0 (root) to nobody (root_squash); use an ordinary UID unless the export allows no_root_squash." placeholder="1000" value={form.Repository.NfsUserId} onChange={(e) => handleRepoChange('NfsUserId', e.target.value)} min="0" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="Numeric Unix group ID sent with every request, for example 1000. Group read permissions on the files apply to this group.">Group ID</Tooltip></label>
                      <input type="number" title="Numeric Unix group ID sent with every request, for example 1000. Group read permissions on the files apply to this group." placeholder="1000" value={form.Repository.NfsGroupId} onChange={(e) => handleRepoChange('NfsGroupId', e.target.value)} min="0" />
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="Absolute export path exactly as the server exports it (see /etc/exports or showmount -e server), starting with /: for nfs.example.com:/exports/content enter /exports/content. On NFSv4 pseudo-root servers such as nfs-ganesha, use the export's pseudo path. To crawl only a subfolder, enter the export here and the subfolder in Object Prefix under Filters.">Export Path</Tooltip></label>
                      <input type="text" title="Absolute export path exactly as the server exports it (see /etc/exports or showmount -e server), starting with /: for nfs.example.com:/exports/content enter /exports/content. On NFSv4 pseudo-root servers such as nfs-ganesha, use the export's pseudo path. To crawl only a subfolder, enter the export here and the subfolder in Object Prefix under Filters." value={form.Repository.NfsShareName} onChange={(e) => handleRepoChange('NfsShareName', e.target.value)} placeholder="/exports/content" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="NFS protocol version. Only NFSv3 is supported; the server must allow NFSv3 over TCP.">NFS Version</Tooltip></label>
                      <select title="NFS protocol version. Only NFSv3 is supported; the server must allow NFSv3 over TCP." value={form.Repository.NfsVersion} onChange={(e) => handleRepoChange('NfsVersion', e.target.value)}>
                        {NFS_VERSIONS.map(version => <option key={version} value={version}>{version}</option>)}
                      </select>
                    </div>
                  </div>
                  {renderSharePreview(describeNfsLocation(form.Repository))}
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text="TCP port of the NFS service. Leave empty for the standard port 2049.">NFS Port</Tooltip></label>
                      <input type="number" title="TCP port of the NFS service. Leave empty for the standard port 2049." value={form.Repository.NfsPort ?? ''} onChange={(e) => handleRepoChange('NfsPort', e.target.value)} min="1" max="65535" placeholder="2049" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="TCP port of the MOUNT service. Leave empty to discover it through the portmapper (rpcbind); set it when the portmapper is blocked or the server runs MOUNT on a fixed port, for example 20048.">Mount Port</Tooltip></label>
                      <input type="number" title="TCP port of the MOUNT service. Leave empty to discover it through the portmapper (rpcbind); set it when the portmapper is blocked or the server runs MOUNT on a fixed port, for example 20048." value={form.Repository.NfsMountPort ?? ''} onChange={(e) => handleRepoChange('NfsMountPort', e.target.value)} min="0" max="65535" placeholder="Discover" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text="TCP port of the portmapper (rpcbind), used to discover the MOUNT port. Leave empty for the standard port 111.">Portmapper Port</Tooltip></label>
                      <input type="number" title="TCP port of the portmapper (rpcbind), used to discover the MOUNT port. Leave empty for the standard port 111." value={form.Repository.NfsPortmapperPort ?? ''} onChange={(e) => handleRepoChange('NfsPortmapperPort', e.target.value)} min="1" max="65535" placeholder="111" />
                    </div>
                  </div>
                  <div className="form-group">
                    <div className="form-toggle">
                      <label className="toggle-switch">
                        <input type="checkbox" checked={form.Repository.IncludeSubdirectories} onChange={(e) => handleRepoChange('IncludeSubdirectories', e.target.checked)} />
                        <span className="toggle-slider"></span>
                      </label>
                      <span><Tooltip text="Crawl files in folders below the export root (and below Object Prefix, when set). When off, only files directly in the export root (or the prefix folder) are crawled.">Include Subdirectories</Tooltip></span>
                    </div>
                  </div>
                </>
              )}
              {form.RepositoryType === 'S3' && (
                <>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text={HELP.S3BucketName}>Bucket Name</Tooltip></label>
                      <input type="text" title={HELP.S3BucketName} value={form.Repository.S3BucketName} onChange={(e) => handleRepoChange('S3BucketName', e.target.value)} placeholder="company-docs" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text={HELP.S3Region}>Region</Tooltip></label>
                      <input type="text" title={HELP.S3Region} value={form.Repository.S3Region ?? ''} onChange={(e) => handleRepoChange('S3Region', e.target.value)} placeholder="us-east-1" />
                    </div>
                  </div>
                  <div className="form-group">
                    <label><Tooltip text={HELP.S3Endpoint}>Endpoint</Tooltip></label>
                    <input type="text" title={HELP.S3Endpoint} value={form.Repository.S3Endpoint ?? ''} onChange={(e) => handleRepoChange('S3Endpoint', e.target.value)} placeholder="Empty for Amazon S3, or http://minio.example.com:9000/" />
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text={HELP.S3AccessKey}>Access Key ID</Tooltip></label>
                      <input type="text" title={HELP.S3AccessKey} value={form.Repository.S3AccessKey ?? ''} onChange={(e) => handleRepoChange('S3AccessKey', e.target.value)} placeholder="Empty for a public bucket" autoComplete="off" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text={HELP.S3SecretKey}>Secret Access Key</Tooltip></label>
                      <PasswordInput title={HELP.S3SecretKey} value={form.Repository.S3SecretKey ?? ''} onChange={(e) => handleRepoChange('S3SecretKey', e.target.value)} />
                    </div>
                  </div>
                  {renderSharePreview(describeS3Location(form.Repository))}
                </>
              )}
              {form.RepositoryType === 'AzureBlob' && (
                <>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text={HELP.AzureAccountName}>Account Name</Tooltip></label>
                      <input type="text" title={HELP.AzureAccountName} value={form.Repository.AzureAccountName} onChange={(e) => handleRepoChange('AzureAccountName', e.target.value)} placeholder="contosodocs" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text={HELP.AzureContainer}>Container</Tooltip></label>
                      <input type="text" title={HELP.AzureContainer} value={form.Repository.AzureContainer} onChange={(e) => handleRepoChange('AzureContainer', e.target.value)} placeholder="documents" />
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text={HELP.AzureAccessKey}>Access Key</Tooltip></label>
                      <PasswordInput title={HELP.AzureAccessKey} value={form.Repository.AzureAccessKey} onChange={(e) => handleRepoChange('AzureAccessKey', e.target.value)} />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text={HELP.AzureEndpoint}>Endpoint</Tooltip></label>
                      <input type="text" title={HELP.AzureEndpoint} value={form.Repository.AzureEndpoint ?? ''} onChange={(e) => handleRepoChange('AzureEndpoint', e.target.value)} placeholder="Optional" />
                    </div>
                  </div>
                  {renderSharePreview(describeAzureLocation(form.Repository))}
                </>
              )}
              {form.RepositoryType === 'GoogleCloud' && (
                <>
                  <div className="form-row">
                    <div className="form-group">
                      <label><Tooltip text={HELP.GcpProjectId}>Project ID</Tooltip></label>
                      <input type="text" title={HELP.GcpProjectId} value={form.Repository.GcpProjectId} onChange={(e) => handleRepoChange('GcpProjectId', e.target.value)} placeholder="contoso-prod-123456" />
                    </div>
                    <div className="form-group">
                      <label><Tooltip text={HELP.GcpBucketName}>Bucket Name</Tooltip></label>
                      <input type="text" title={HELP.GcpBucketName} value={form.Repository.GcpBucketName} onChange={(e) => handleRepoChange('GcpBucketName', e.target.value)} placeholder="contoso-documents" />
                    </div>
                  </div>
                  <div className="form-group">
                    <label><Tooltip text={HELP.GcpJsonCredentials}>Service Account Key (JSON)</Tooltip></label>
                    <textarea title={HELP.GcpJsonCredentials} value={form.Repository.GcpJsonCredentials} onChange={(e) => handleRepoChange('GcpJsonCredentials', e.target.value)} rows={5} spellCheck={false} autoComplete="off" placeholder={'{ "type": "service_account", "project_id": "...", "private_key": "...", "client_email": "..." }'} style={{ fontFamily: 'monospace', fontSize: '0.8rem' }} />
                  </div>
                  <div className="form-group">
                    <label><Tooltip text={HELP.GcpEndpoint}>Endpoint</Tooltip></label>
                    <input type="text" title={HELP.GcpEndpoint} value={form.Repository.GcpEndpoint ?? ''} onChange={(e) => handleRepoChange('GcpEndpoint', e.target.value)} placeholder="Optional" />
                  </div>
                  {renderSharePreview(describeGcpLocation(form.Repository))}
                </>
              )}
              {form.RepositoryType === 'LocalDisk' && (
                <>
                  <div className="form-group">
                    <label><Tooltip text={HELP.DiskPath}>Folder Path</Tooltip></label>
                    <input type="text" title={HELP.DiskPath} value={form.Repository.DiskPath} onChange={(e) => handleRepoChange('DiskPath', e.target.value)} placeholder="/app/crawl-sources/handbook" />
                  </div>
                  {renderSharePreview(describeDiskLocation(form.Repository))}
                  <div className="form-group">
                    <div className="form-toggle">
                      <label className="toggle-switch">
                        <input type="checkbox" checked={form.Repository.IncludeSubdirectories} onChange={(e) => handleRepoChange('IncludeSubdirectories', e.target.checked)} />
                        <span className="toggle-slider"></span>
                      </label>
                      <span><Tooltip text={HELP.DiskSubdirectories}>Include Subdirectories</Tooltip></span>
                    </div>
                  </div>
                </>
              )}
              {form.RepositoryType === 'Git' && (
                <>
                  <div className="form-group">
                    <label><Tooltip text={HELP.GitRepositoryUrl}>Repository URL</Tooltip></label>
                    <input type="text" title={HELP.GitRepositoryUrl} value={form.Repository.GitRepositoryUrl} onChange={(e) => handleRepoChange('GitRepositoryUrl', e.target.value)} placeholder="https://github.com/owner/repo" />
                  </div>
                  <div className="form-group">
                    <label><Tooltip text={HELP.GitAccessToken}>Access Token</Tooltip></label>
                    <PasswordInput title={HELP.GitAccessToken} value={form.Repository.GitAccessToken ?? ''} onChange={(e) => handleRepoChange('GitAccessToken', e.target.value)} placeholder="Optional for public repositories" />
                  </div>
                  {renderSharePreview(describeGitLocation(form.Repository))}
                </>
              )}
              <div className="form-group">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={handleTestConnectivity}
                  disabled={!onTestConnectivity || testingConnectivity || !isRepositoryValid()}
                >
                  {testingConnectivity ? 'Testing...' : 'Test Connectivity'}
                </button>
                {connectivityResult && (
                  <div
                    style={{
                      marginTop: '0.5rem',
                      fontSize: '0.875rem',
                      color: connectivityResult.success === true
                        ? 'var(--success-color, #15803d)'
                        : connectivityResult.success === null
                          ? 'var(--text-secondary)'
                          : 'var(--danger-color, #b91c1c)',
                    }}
                  >
                    {connectivityResult.message}
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Schedule (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setScheduleOpen(prev => !prev)}>
            {scheduleOpen ? '\u25BE' : '\u25B8'} Schedule
          </button>
          {scheduleOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-row">
                <div className="form-group">
                  <label><Tooltip text="Unit of time for the crawl interval. Default: Hours">Interval Type</Tooltip></label>
                  <select value={form.Schedule.IntervalType} onChange={(e) => handleScheduleChange('IntervalType', e.target.value)}>
                    {INTERVAL_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label><Tooltip text="Number of interval units between crawls. Range: 1-10080, default: 24">Interval Value</Tooltip></label>
                  <input type="number" value={form.Schedule.IntervalValue} onChange={(e) => handleScheduleChange('IntervalValue', e.target.value)} min="1" max="10080" />
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Filter (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setFilterOpen(prev => !prev)}>
            {filterOpen ? '\u25BE' : '\u25B8'} Filter
          </button>
          {filterOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-row">
                <div className="form-group">
                  <label><Tooltip text={PREFIX_HELP}>Object Prefix</Tooltip></label>
                  <input type="text" title={PREFIX_HELP} value={form.Filter.ObjectPrefix} onChange={(e) => handleFilterChange('ObjectPrefix', e.target.value)} placeholder={form.RepositoryType === 'Web' ? 'Optional' : 'Optional, e.g. Policies/'} />
                  {form.RepositoryType !== 'Web' && /^[\\/]/.test(form.Filter.ObjectPrefix || '') && (
                    <div className="tool-policy-warning">Remove the leading slash: keys are relative to the root of the share, export, bucket, container, folder or repository, for example Policies/.</div>
                  )}
                </div>
                <div className="form-group">
                  <label><Tooltip text="Only crawl objects whose key ends with this text, for example .pdf or .docx. Matching is case-insensitive.">Object Suffix</Tooltip></label>
                  <input type="text" title="Only crawl objects whose key ends with this text, for example .pdf or .docx. Matching is case-insensitive." value={form.Filter.ObjectSuffix} onChange={(e) => handleFilterChange('ObjectSuffix', e.target.value)} placeholder="Optional" />
                </div>
              </div>
              <div className="form-group">
                <label><Tooltip text="Comma-separated list of allowed MIME types (e.g. text/html, application/pdf)">Allowed Content Types</Tooltip></label>
                <input type="text" value={form.Filter.AllowedContentTypes} onChange={(e) => handleFilterChange('AllowedContentTypes', e.target.value)} placeholder="e.g. text/html, application/pdf" />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label><Tooltip text="Minimum file size in bytes to process. Default: 0 (no minimum)">Minimum Size (bytes)</Tooltip></label>
                  <input type="number" value={form.Filter.MinimumSize} onChange={(e) => handleFilterChange('MinimumSize', e.target.value)} min="0" placeholder="0" />
                </div>
                <div className="form-group">
                  <label><Tooltip text="Maximum file size in bytes to process. Leave empty for no limit">Maximum Size (bytes)</Tooltip></label>
                  <input type="number" value={form.Filter.MaximumSize} onChange={(e) => handleFilterChange('MaximumSize', e.target.value)} min="0" placeholder="No limit" />
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Processing (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setProcessingOpen(prev => !prev)}>
            {processingOpen ? '\u25BE' : '\u25B8'} Processing
          </button>
          {processingOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Processing.ProcessAdditions} onChange={(e) => handleProcessingChange('ProcessAdditions', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Process new files discovered during crawl">Process Additions</Tooltip></span>
                  </div>
                </div>
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input type="checkbox" checked={form.Processing.ProcessUpdates} onChange={(e) => handleProcessingChange('ProcessUpdates', e.target.checked)} />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Process files that have been modified since the last crawl">Process Updates</Tooltip></span>
                  </div>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Processing.ProcessDeletions} onChange={(e) => handleProcessingChange('ProcessDeletions', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Remove documents that no longer exist at the source">Process Deletions</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <label><Tooltip text="Maximum number of concurrent document processing tasks. Range: 1-64, default: 8">Max Concurrent Tasks</Tooltip></label>
                <input type="number" value={form.Processing.MaxDrainTasks} onChange={(e) => handleProcessingChange('MaxDrainTasks', e.target.value)} min="1" max="64" />
              </div>
            </div>
          )}
        </div>

        {/* Retention (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setRetentionOpen(prev => !prev)}>
            {retentionOpen ? '\u25BE' : '\u25B8'} Retention
          </button>
          {retentionOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Number of days to retain crawl operation history. Range: 0-14, default: 7. Set to 0 to delete operations immediately after completion.">Retention Days</Tooltip></label>
                <input type="number" value={form.RetentionDays} onChange={(e) => handleChange('RetentionDays', e.target.value)} min="0" max="14" />
              </div>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
}

export default CrawlPlanFormModal;
