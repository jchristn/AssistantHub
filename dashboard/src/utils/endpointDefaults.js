import { PROVIDERS, getProvider, getProviderDefaults, getHealthCheckDefaults, defaultProviderSettings } from './providers.js';
export const API_FORMAT_OPTIONS = PROVIDERS.map(provider => provider.value);
export const HEALTH_CHECK_METHOD_OPTIONS = ['GET', 'POST', 'HEAD'];

// Default per-endpoint request timeout (distinct from the health check timeout), in milliseconds.
export const DEFAULT_MAXIMUM_TIMEOUT_MS = 300000;

const FORMAT_DEFAULTS = {
  Ollama: {
    DefaultEmbeddingModel: 'nomic-embed-text',
    DefaultInferenceModel: 'gemma3:4b',
    Endpoint: 'http://localhost:11434',
    HealthCheckEnabled: true,
    HealthCheckMethod: 'GET',
    HealthCheckIntervalMs: 5000,
    HealthCheckTimeoutMs: 2000,
    HealthCheckExpectedStatusCode: 200,
    HealthyThreshold: 2,
    UnhealthyThreshold: 2,
    HealthCheckUseAuth: false
  },
  OpenAI: {
    DefaultEmbeddingModel: 'text-embedding-3-small',
    DefaultInferenceModel: 'gpt-4o-mini',
    Endpoint: 'https://api.openai.com/v1',
    HealthCheckEnabled: true,
    HealthCheckMethod: 'GET',
    HealthCheckIntervalMs: 30000,
    HealthCheckTimeoutMs: 10000,
    HealthCheckExpectedStatusCode: 200,
    HealthyThreshold: 2,
    UnhealthyThreshold: 2,
    HealthCheckUseAuth: true
  },
  Gemini: {
    DefaultEmbeddingModel: 'gemini-embedding-001',
    DefaultInferenceModel: 'gemini-2.5-flash',
    Endpoint: 'https://generativelanguage.googleapis.com',
    HealthCheckEnabled: true,
    HealthCheckMethod: 'GET',
    HealthCheckIntervalMs: 30000,
    HealthCheckTimeoutMs: 10000,
    HealthCheckExpectedStatusCode: 200,
    HealthyThreshold: 2,
    UnhealthyThreshold: 2,
    HealthCheckUseAuth: true
  }
};

FORMAT_DEFAULTS.OllamaCloud = {
  ...FORMAT_DEFAULTS.Ollama,
  Endpoint: 'https://ollama.com',
  DefaultEmbeddingModel: '',
  DefaultInferenceModel: '',
  HealthCheckUseAuth: true,
  HealthCheckIntervalMs: 30000,
  HealthCheckTimeoutMs: 10000
};
FORMAT_DEFAULTS.xAI = {
  ...FORMAT_DEFAULTS.OpenAI,
  Endpoint: 'https://api.x.ai',
  DefaultEmbeddingModel: '',
  DefaultInferenceModel: 'grok-4'
};

function trimTrailingSlash(value) {
  return (value || '').replace(/\/+$/, '');
}

function normalizeComparableUrl(value) {
  if (!value) return '';

  try {
    const url = new URL(value);
    url.pathname = trimTrailingSlash(url.pathname);
    return url.toString().replace(/\/$/, '');
  } catch {
    return trimTrailingSlash(value);
  }
}

function appendVersionedPath(endpoint, versionSegment, path) {
  const base = trimTrailingSlash(endpoint);
  if (!base) return '';
  return base.toLowerCase().endsWith(versionSegment.toLowerCase())
    ? `${base}${path}`
    : `${base}${versionSegment}${path}`;
}

export function getDefaultEndpoint(apiFormat = 'Ollama') {
  return FORMAT_DEFAULTS[apiFormat]?.Endpoint ?? getProviderDefaults(apiFormat, 'completion', defaultProviderSettings(apiFormat)).Endpoint;
}

export function getDefaultHealthCheckUrl(endpoint, apiFormat = 'Ollama') {
  if (!endpoint) return '';
  if (!FORMAT_DEFAULTS[apiFormat]) return getHealthCheckDefaults(apiFormat, endpoint, defaultProviderSettings(apiFormat)).HealthCheckUrl || '';

  switch (apiFormat) {
    case 'xAI':
    case 'OpenAI':
      return appendVersionedPath(endpoint, '/v1', '/models');
    case 'Gemini':
      return appendVersionedPath(endpoint, '/v1beta', '/models');
    case 'Ollama':
    default:
      return `${trimTrailingSlash(endpoint)}/api/tags`;
  }
}

// True when the health check URL is empty or still matches the default derived from the endpoint,
// i.e. the user has not manually customized it and it can safely auto-follow endpoint changes.
export function isDefaultHealthCheckUrl(healthCheckUrl, endpoint, apiFormat = 'Ollama') {
  if (!healthCheckUrl) return true;
  return normalizeComparableUrl(healthCheckUrl) === normalizeComparableUrl(getDefaultHealthCheckUrl(endpoint, apiFormat));
}

export function getHealthCheckUrlForEndpointChange(currentHealthCheckUrl, previousEndpoint, nextEndpoint, apiFormat = 'Ollama') {
  const nextDefault = getDefaultHealthCheckUrl(nextEndpoint, apiFormat);
  if (!currentHealthCheckUrl) return nextDefault;

  const previousDefault = getDefaultHealthCheckUrl(previousEndpoint, apiFormat);
  if (normalizeComparableUrl(currentHealthCheckUrl) === normalizeComparableUrl(previousDefault)) {
    return nextDefault;
  }

  try {
    const healthUrl = new URL(currentHealthCheckUrl);
    const previousUrl = new URL(previousEndpoint);
    const nextUrl = new URL(nextEndpoint);
    const previousBasePath = trimTrailingSlash(previousUrl.pathname);
    const healthPath = healthUrl.pathname || '/';
    const isSameBase =
      healthUrl.origin === previousUrl.origin
      && (!previousBasePath || healthPath === previousBasePath || healthPath.startsWith(`${previousBasePath}/`));

    if (!isSameBase) return currentHealthCheckUrl;

    const suffixPath = previousBasePath && healthPath.startsWith(previousBasePath)
      ? healthPath.slice(previousBasePath.length)
      : healthPath;
    const nextBasePath = trimTrailingSlash(nextUrl.pathname);
    nextUrl.pathname = `${nextBasePath}${suffixPath.startsWith('/') ? suffixPath : `/${suffixPath}`}`;
    nextUrl.search = healthUrl.search;
    nextUrl.hash = healthUrl.hash;
    return nextUrl.toString().replace(/\/$/, '');
  } catch {
    return currentHealthCheckUrl;
  }
}

export function getApiFormatDefaults(apiFormat = 'Ollama', endpoint = null) {
  const defaults = FORMAT_DEFAULTS[apiFormat] || { ...getProviderDefaults(apiFormat, 'completion', defaultProviderSettings(apiFormat)), ...getHealthCheckDefaults(apiFormat, endpoint, defaultProviderSettings(apiFormat)) };
  const resolvedEndpoint = endpoint || defaults.Endpoint;

  return {
    ...defaults,
    MaximumTimeoutMs: DEFAULT_MAXIMUM_TIMEOUT_MS,
    Endpoint: resolvedEndpoint,
    HealthCheckUrl: getDefaultHealthCheckUrl(resolvedEndpoint, apiFormat)
  };
}

export function getDefaultModel(apiFormat = 'Ollama', endpointType = 'inference') {
  if (!FORMAT_DEFAULTS[apiFormat]) return getProvider(apiFormat).model.defaults[endpointType === 'embedding' ? 'embedding' : 'completion'] || '';
  const defaults = FORMAT_DEFAULTS[apiFormat];
  return endpointType === 'embedding'
    ? defaults.DefaultEmbeddingModel
    : defaults.DefaultInferenceModel;
}

export function getDefaultToolCallingApiFormat(apiFormat) {
  return ['Ollama', 'OllamaCloud'].includes(apiFormat) ? 'OllamaChat' : ['OpenAI', 'xAI', 'vLLM'].includes(apiFormat) ? 'OpenAIChatCompletions' : 'Native';
}

const PROVIDER_FIELDS = {
  Ollama: {
    credentialLabel: 'Bearer Token', credentialRequired: false,
    credentialHelp: 'Optional bearer token for an authenticated Ollama server.',
    endpointHelp: 'Base URL of your Ollama server.',
    inferenceModelPlaceholder: 'e.g. gemma3:4b', embeddingModelPlaceholder: 'e.g. nomic-embed-text'
  },
  OllamaCloud: {
    credentialLabel: 'Bearer Token', credentialRequired: true, fixedEndpoint: true,
    credentialHelp: 'Ollama Cloud bearer token from ollama.com/settings/keys. Sent in the Authorization header.',
    endpointHelp: 'Ollama Cloud always uses https://ollama.com.',
    inferenceModelPlaceholder: 'Model available in your Ollama Cloud account', embeddingModelPlaceholder: 'Embedding model available in your Ollama Cloud account'
  },
  OpenAI: {
    credentialLabel: 'API Key', credentialRequired: false,
    credentialHelp: 'OpenAI API key, sent as a bearer token. Optional for compatible servers that do not require authentication.',
    endpointHelp: 'OpenAI or compatible API base URL; a trailing /v1 is accepted.',
    inferenceModelPlaceholder: 'e.g. gpt-4o-mini', embeddingModelPlaceholder: 'e.g. text-embedding-3-small'
  },
  xAI: {
    credentialLabel: 'Bearer Token', credentialRequired: true,
    credentialHelp: 'xAI API token from your xAI account. Sent in the Authorization header.',
    endpointHelp: 'xAI API base URL; defaults to https://api.x.ai. A trailing /v1 is accepted.',
    inferenceModelPlaceholder: 'Grok model available in your xAI account', embeddingModelPlaceholder: 'Embedding model available in your xAI account'
  },
  Gemini: {
    credentialLabel: 'API Key', credentialRequired: true,
    credentialHelp: 'Google Gemini API key. Sent in the x-goog-api-key header.',
    endpointHelp: 'Google Gemini API base URL.',
    inferenceModelPlaceholder: 'e.g. gemini-2.5-flash', embeddingModelPlaceholder: 'e.g. gemini-embedding-001'
  }
};

export function getEndpointProviderFields(apiFormat = 'Ollama') {
  return PROVIDER_FIELDS[apiFormat] || PROVIDER_FIELDS.Ollama;
}

export function resolveProviderEndpoint(apiFormat, endpoint) {
  return getEndpointProviderFields(apiFormat).fixedEndpoint
    ? getDefaultEndpoint(apiFormat)
    : (endpoint || getDefaultEndpoint(apiFormat));
}
