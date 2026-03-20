export const API_FORMAT_OPTIONS = ['Ollama', 'OpenAI', 'Gemini'];
export const HEALTH_CHECK_METHOD_OPTIONS = ['GET', 'POST', 'HEAD'];

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

function trimTrailingSlash(value) {
  return (value || '').replace(/\/+$/, '');
}

function appendVersionedPath(endpoint, versionSegment, path) {
  const base = trimTrailingSlash(endpoint);
  if (!base) return '';
  return base.toLowerCase().endsWith(versionSegment.toLowerCase())
    ? `${base}${path}`
    : `${base}${versionSegment}${path}`;
}

export function getDefaultEndpoint(apiFormat = 'Ollama') {
  return FORMAT_DEFAULTS[apiFormat]?.Endpoint || FORMAT_DEFAULTS.Ollama.Endpoint;
}

export function getDefaultHealthCheckUrl(endpoint, apiFormat = 'Ollama') {
  if (!endpoint) return '';

  switch (apiFormat) {
    case 'OpenAI':
      return appendVersionedPath(endpoint, '/v1', '/models');
    case 'Gemini':
      return appendVersionedPath(endpoint, '/v1beta', '/models');
    case 'Ollama':
    default:
      return `${trimTrailingSlash(endpoint)}/api/tags`;
  }
}

export function getApiFormatDefaults(apiFormat = 'Ollama', endpoint = null) {
  const defaults = FORMAT_DEFAULTS[apiFormat] || FORMAT_DEFAULTS.Ollama;
  const resolvedEndpoint = endpoint || defaults.Endpoint;

  return {
    ...defaults,
    Endpoint: resolvedEndpoint,
    HealthCheckUrl: getDefaultHealthCheckUrl(resolvedEndpoint, apiFormat)
  };
}

export function getDefaultModel(apiFormat = 'Ollama', endpointType = 'inference') {
  const defaults = FORMAT_DEFAULTS[apiFormat] || FORMAT_DEFAULTS.Ollama;
  return endpointType === 'embedding'
    ? defaults.DefaultEmbeddingModel
    : defaults.DefaultInferenceModel;
}
