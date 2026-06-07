export const RECENT_REQUESTS_KEY = 'ah_api_explorer_recent';

export const SPECIAL_OPERATION_OVERRIDES = {
  'POST:/v1.0/endpoints/completion/{endpointId}/test': {
    summary: 'Test completion endpoint',
    description: 'Run a smoke test through AssistantHub against a Partio completion endpoint.',
    bodyTemplate: {
      SystemPrompt: 'You are a concise and accurate assistant.',
      Prompt: 'Respond with a one-sentence smoke test confirmation.',
      MaxTokens: 512,
      TimeoutMs: 60000,
    },
  },
  'POST:/v1.0/endpoints/completion/{endpointId}/load': {
    summary: 'Load completion endpoint model',
    description: 'Load or warm the configured model through AssistantHub and Partio.',
    bodyTemplate: {
      Strategy: 'Auto',
      TimeoutMs: 60000,
      KeepAlive: '30m',
      SampleInput: 'Partio model load probe',
      MaxTokens: 1,
      RecordRequestHistory: true,
      RequireNativeLoad: false,
    },
  },
  'POST:/v1.0/endpoints/embedding/{endpointId}/test': {
    summary: 'Test embedding endpoint',
    description: 'Run a smoke test through AssistantHub against a Partio embedding endpoint.',
    bodyTemplate: {
      Input: 'AssistantHub embedding smoke test input',
      L2Normalization: false,
    },
  },
  'POST:/v1.0/endpoints/embedding/{endpointId}/load': {
    summary: 'Load embedding endpoint model',
    description: 'Load or warm the configured embedding model through AssistantHub and Partio.',
    bodyTemplate: {
      Strategy: 'Auto',
      TimeoutMs: 60000,
      KeepAlive: '30m',
      SampleInput: 'Partio model load probe',
      MaxTokens: 1,
      RecordRequestHistory: true,
      RequireNativeLoad: false,
    },
  },
  'POST:/v1.0/crawlplans/connectivity': {
    summary: 'Test draft crawl-plan connectivity',
    description: 'Validate unsaved repository settings and credentials without creating or updating a crawl plan.',
    bodyTemplate: {
      Name: 'Connectivity Probe',
      RepositoryType: 'Web',
      RepositorySettings: {
        RepositoryType: 'Web',
        AuthenticationType: 'None',
        StartUrl: 'https://example.com',
        FollowLinks: false,
        FollowRedirects: true,
        ExtractSitemapLinks: false,
        RestrictToChildUrls: true,
        RestrictToSubdomain: true,
        RestrictToRootDomain: true,
        IgnoreRobotsTxt: false,
        UseHeadlessBrowser: false,
        MaxDepth: 1,
        MaxParallelTasks: 1,
        CrawlDelayMs: 100,
      },
    },
  },
};

export const ASSISTANT_TEMPLATES = [
  {
    key: 'assistant-public',
    method: 'GET',
    path: '/v1.0/assistants/{assistantId}/public',
    tags: ['Assistant Public APIs'],
    summary: 'Get public assistant metadata',
    description: 'Load the public branding and descriptive metadata for an assistant.',
    includeAuth: false,
  },
  {
    key: 'assistant-thread-create',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/threads',
    tags: ['Assistant Public APIs'],
    summary: 'Create a thread',
    description: 'Create a new public thread identifier for assistant chat history.',
    includeAuth: false,
  },
  {
    key: 'assistant-thread-history',
    method: 'GET',
    path: '/v1.0/assistants/{assistantId}/threads/{threadId}/history',
    tags: ['Assistant Public APIs'],
    summary: 'Get thread history',
    description: 'Retrieve public chat history for a specific assistant thread.',
    includeAuth: false,
  },
  {
    key: 'assistant-chat',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/chat',
    tags: ['Assistant Public APIs'],
    summary: 'Chat with assistant',
    description: 'Execute assistant chat and inspect streaming or non-streaming responses.',
    includeAuth: false,
    expectStream: true,
    headersText: 'X-Thread-ID: ',
    bodyTemplate: {
      messages: [
        { role: 'user', content: 'Hello. Please give me a short response.' },
      ],
    },
  },
  {
    key: 'assistant-chat-open',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/chat/open',
    tags: ['Assistant Public APIs'],
    summary: 'Open assistant chat',
    description: 'Load or warm the configured assistant endpoint models before a chat session.',
    includeAuth: false,
  },
  {
    key: 'assistant-compact',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/compact',
    tags: ['Assistant Public APIs'],
    summary: 'Force conversation compaction',
    description: 'Compact an assistant conversation to reduce token pressure and return a summary system message for the next turn.',
    includeAuth: false,
    headersText: 'X-Thread-ID: ',
    bodyTemplate: {
      messages: [
        { role: 'user', content: 'Summarize our conversation so far.' },
      ],
    },
  },
  {
    key: 'assistant-generate',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/generate',
    tags: ['Assistant Public APIs'],
    summary: 'Generate without RAG',
    description: 'Run inference directly against the assistant without retrieval or history persistence.',
    includeAuth: false,
    bodyTemplate: {
      messages: [
        { role: 'user', content: 'Give me a one-line summary of AssistantHub.' },
      ],
    },
  },
  {
    key: 'assistant-feedback',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/feedback',
    tags: ['Assistant Public APIs'],
    summary: 'Submit public feedback',
    description: 'Submit end-user feedback for an assistant response.',
    includeAuth: false,
    bodyTemplate: {
      UserMessage: 'What is AssistantHub?',
      AssistantResponse: 'AssistantHub is an assistant management and observability platform.',
      Rating: 'ThumbsUp',
      FeedbackText: '',
      MessageHistory: [],
    },
  },
  {
    key: 'assistant-document-download',
    method: 'GET',
    path: '/v1.0/assistants/{assistantId}/documents/{documentId}/download',
    tags: ['Assistant Public APIs'],
    summary: 'Download public assistant document',
    description: 'Download a document exposed through the public assistant document route.',
    includeAuth: false,
  },
  {
    key: 'assistant-labels',
    method: 'GET',
    path: '/v1.0/assistants/{assistantId}/labels/distinct',
    tags: ['Assistant Public APIs'],
    summary: 'Get distinct labels',
    description: 'Enumerate distinct labels available to the assistant collection.',
    includeAuth: false,
  },
  {
    key: 'assistant-tags',
    method: 'GET',
    path: '/v1.0/assistants/{assistantId}/tags/distinct',
    tags: ['Assistant Public APIs'],
    summary: 'Get distinct tags',
    description: 'Enumerate distinct tag keys available to the assistant collection.',
    includeAuth: false,
  },
];

const COMPOUND_RESOURCE_NAMES = {
  'assistants': { collection: 'assistants', single: 'assistant' },
  'assistants/settings': { collection: 'assistant settings', single: 'assistant settings' },
  'assistants/settings/slack': { collection: 'assistant Slack settings', single: 'assistant Slack settings' },
  'assistants/threads': { collection: 'threads', single: 'thread' },
  'buckets': { collection: 'buckets', single: 'bucket' },
  'buckets/objects': { collection: 'bucket objects', single: 'bucket object' },
  'collections': { collection: 'collections', single: 'collection' },
  'collections/records': { collection: 'collection records', single: 'collection record' },
  'credentials': { collection: 'credentials', single: 'credential' },
  'crawlplans': { collection: 'crawl plans', single: 'crawl plan' },
  'crawlplans/operations': { collection: 'crawl operations', single: 'crawl operation' },
  'documents': { collection: 'documents', single: 'document' },
  'endpoints/completion': { collection: 'completion endpoints', single: 'completion endpoint' },
  'endpoints/embedding': { collection: 'embedding endpoints', single: 'embedding endpoint' },
  'eval/facts': { collection: 'evaluation facts', single: 'evaluation fact' },
  'eval/results': { collection: 'evaluation results', single: 'evaluation result' },
  'eval/runs': { collection: 'evaluation runs', single: 'evaluation run' },
  'feedback': { collection: 'feedback entries', single: 'feedback entry' },
  'history': { collection: 'chat history', single: 'chat history entry' },
  'ingestion-rules': { collection: 'ingestion rules', single: 'ingestion rule' },
  'models': { collection: 'models', single: 'model' },
  'requesthistory': { collection: 'request history', single: 'request history entry' },
  'threads': { collection: 'threads', single: 'thread' },
  'users': { collection: 'users', single: 'user' },
};

const ACTION_SUFFIXES = new Set([
  'chat',
  'compact',
  'connectivity',
  'detail',
  'distinct',
  'download',
  'enumerate',
  'feedback',
  'generate',
  'health',
  'history',
  'metadata',
  'processing-log',
  'public',
  'start',
  'status',
  'stop',
  'stream',
  'summary',
  'test',
  'upload',
  'verify',
]);

function isPathParameter(segment) {
  return segment.startsWith('{') && segment.endsWith('}');
}

function stripVersionPrefix(path) {
  const stripped = (path || '/').replace(/^\/v\d+\.\d+/, '');
  return stripped || '/';
}

function humanizeSegment(segment) {
  return segment
    .replace(/[-_]/g, ' ')
    .replace(/\bapi\b/gi, 'API')
    .replace(/\bid\b/gi, 'ID')
    .trim();
}

function singularizePhrase(phrase) {
  const specialCases = {
    assistants: 'assistant',
    buckets: 'bucket',
    collections: 'collection',
    credentials: 'credential',
    documents: 'document',
    endpoints: 'endpoint',
    entries: 'entry',
    facts: 'fact',
    models: 'model',
    objects: 'object',
    operations: 'operation',
    plans: 'plan',
    records: 'record',
    results: 'result',
    rules: 'rule',
    runs: 'run',
    settings: 'settings',
    threads: 'thread',
    users: 'user',
  };

  const words = phrase.split(' ');
  const lastWord = words[words.length - 1];
  const normalized = lastWord.toLowerCase();

  let singular = specialCases[normalized];
  if (!singular) {
    if (normalized.endsWith('ies')) singular = `${lastWord.slice(0, -3)}y`;
    else if (normalized.endsWith('ses')) singular = lastWord.slice(0, -2);
    else if (normalized.endsWith('s') && !normalized.endsWith('ss')) singular = lastWord.slice(0, -1);
    else singular = lastWord;
  }

  words[words.length - 1] = singular;
  return words.join(' ');
}

function getResourcePhrase(staticSegments, useSingle) {
  for (let index = 0; index < staticSegments.length; index += 1) {
    const key = staticSegments.slice(index).join('/');
    const mapped = COMPOUND_RESOURCE_NAMES[key];
    if (mapped) return useSingle ? mapped.single : mapped.collection;
  }

  const lastSegment = staticSegments[staticSegments.length - 1] || 'resource';
  const phrase = humanizeSegment(lastSegment);
  return useSingle ? singularizePhrase(phrase) : phrase;
}

function generateOperationSummary(method, path) {
  const normalizedPath = stripVersionPrefix(path);
  const segments = normalizedPath.split('/').filter(Boolean);
  const staticSegments = segments.filter((segment) => !isPathParameter(segment));
  const lastStatic = staticSegments[staticSegments.length - 1] || '';
  const subjectSegments = ACTION_SUFFIXES.has(lastStatic) ? staticSegments.slice(0, -1) : staticSegments;
  const hasTrailingParameter = isPathParameter(segments[segments.length - 1] || '');
  const collectionSubject = getResourcePhrase(subjectSegments, false);
  const singleSubject = getResourcePhrase(subjectSegments, true);

  if (normalizedPath === '/authenticate') return 'Authenticate';
  if (normalizedPath === '/configuration') return method === 'GET' ? 'Retrieve configuration' : 'Update configuration';
  if (normalizedPath === '/models/pull') return 'Pull model';
  if (normalizedPath === '/models/pull/status') return 'Retrieve model pull status';
  if (normalizedPath === '/requesthistory/summary') return 'Retrieve request history summary';
  if (normalizedPath === '/requesthistory/{requestId}/detail') return 'Retrieve request history detail';
  if (normalizedPath === '/requesthistory/bulk') return 'Delete filtered request history';
  if (normalizedPath === '/assistants/{assistantId}/public') return 'Retrieve public assistant metadata';
  if (normalizedPath === '/assistants/{assistantId}/threads') return method === 'POST' ? 'Create thread' : 'Retrieve threads';
  if (normalizedPath === '/assistants/{assistantId}/threads/{threadId}/history') return 'Retrieve thread history';
  if (normalizedPath === '/assistants/{assistantId}/chat') return 'Chat with assistant';
  if (normalizedPath === '/assistants/{assistantId}/compact') return 'Compact conversation';
  if (normalizedPath === '/assistants/{assistantId}/generate') return 'Generate assistant response';
  if (normalizedPath === '/assistants/{assistantId}/feedback') return 'Submit assistant feedback';
  if (normalizedPath === '/buckets/{name}/objects/metadata') return 'Retrieve bucket object metadata';
  if (normalizedPath === '/buckets/{name}/objects/download') return 'Download bucket object';
  if (normalizedPath === '/buckets/{name}/objects/upload') return 'Upload bucket object';
  if (normalizedPath === '/eval/runs/{runId}/results') return 'Retrieve evaluation run results';
  if (normalizedPath === '/eval/judge-prompt/default') return 'Retrieve default evaluation judge prompt';
  if (normalizedPath.endsWith('/labels/distinct')) return 'Retrieve distinct labels';
  if (normalizedPath.endsWith('/tags/distinct')) return 'Retrieve distinct tags';
  if (normalizedPath.endsWith('/processing-log')) return 'Retrieve document processing log';
  if (normalizedPath.endsWith('/summary')) return `Retrieve ${singleSubject} summary`;
  if (normalizedPath.endsWith('/detail')) return `Retrieve ${singleSubject} detail`;
  if (normalizedPath.endsWith('/metadata')) return `Retrieve ${singleSubject} metadata`;
  if (normalizedPath.endsWith('/download')) return `Download ${singleSubject}`;
  if (normalizedPath.endsWith('/upload')) return `Upload ${singleSubject}`;
  if (normalizedPath.endsWith('/health')) return `Retrieve ${singleSubject} health`;
  if (normalizedPath.endsWith('/test')) return `Test ${singleSubject}`;
  if (normalizedPath.endsWith('/verify')) return `Verify ${singleSubject}`;
  if (normalizedPath.endsWith('/enumerate')) return `Enumerate ${collectionSubject}`;
  if (normalizedPath.endsWith('/stream')) return `Stream ${singleSubject}`;

  switch (method) {
    case 'GET':
      return hasTrailingParameter ? `Retrieve ${singleSubject}` : `Retrieve all ${collectionSubject}`;
    case 'PUT':
      return hasTrailingParameter ? `Update ${singleSubject}` : `Create ${singleSubject}`;
    case 'POST':
      return `Create ${singleSubject}`;
    case 'DELETE':
      return `Delete ${singleSubject}`;
    case 'HEAD':
      return `Check ${singleSubject} existence`;
    case 'PATCH':
      return `Patch ${singleSubject}`;
    default:
      return `${method} ${path}`;
  }
}

function normalizeOperationSummary(summary, method, path) {
  const trimmed = (summary || '').trim();
  if (!trimmed) return generateOperationSummary(method, path);
  if (trimmed === `${method} ${path}`) return generateOperationSummary(method, path);
  if (/^(GET|POST|PUT|DELETE|HEAD|PATCH)\s+\/v\d+\.\d+\/.+/i.test(trimmed)) return generateOperationSummary(method, path);
  return trimmed;
}

export function flattenOpenApiOperations(spec) {
  const operations = [];
  const paths = spec?.paths || {};

  Object.entries(paths).forEach(([path, pathItem]) => {
    Object.entries(pathItem || {}).forEach(([method, operation]) => {
      const upperMethod = method.toUpperCase();
      if (!['GET', 'POST', 'PUT', 'DELETE', 'HEAD', 'PATCH'].includes(upperMethod)) return;

      const key = `${upperMethod}:${path}`;
      const override = SPECIAL_OPERATION_OVERRIDES[key] || {};
      const parameters = [...(pathItem.parameters || []), ...(operation?.parameters || [])];

      operations.push({
        key,
        method: upperMethod,
        path,
        summary: override.summary || normalizeOperationSummary(operation?.summary, upperMethod, path),
        description: override.description || operation?.description || '',
        tags: operation?.tags || ['Misc'],
        parameters,
        includeAuth: Array.isArray(operation?.security) ? operation.security.length > 0 : false,
        expectStream: path.endsWith('/stream'),
        headersText: override.headersText || '',
        bodyTemplate: override.bodyTemplate,
      });
    });
  });

  return operations.sort((a, b) => {
    const aTag = a.tags?.[0] || 'Misc';
    const bTag = b.tags?.[0] || 'Misc';
    return aTag.localeCompare(bTag) || a.path.localeCompare(b.path) || a.method.localeCompare(b.method);
  });
}

export function extractPathParamNames(path) {
  return Array.from(path.matchAll(/\{([^}]+)\}/g)).map((match) => match[1]);
}

export function resolvePathTemplate(pathTemplate, pathParams) {
  let resolved = pathTemplate || '/';
  Object.entries(pathParams || {}).forEach(([key, value]) => {
    resolved = resolved.replace(`{${key}}`, encodeURIComponent(value || `{${key}}`));
  });
  return resolved;
}

export function parseKeyValueText(text, delimiter = '=') {
  const result = {};
  if (!text) return result;

  text.split('\n').forEach((rawLine) => {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) return;

    const index = line.indexOf(delimiter);
    if (index < 0) {
      result[line] = '';
      return;
    }

    const key = line.substring(0, index).trim();
    const value = line.substring(index + 1).trim();
    if (!key) return;
    result[key] = value;
  });

  return result;
}

export function stringifyKeyValueObject(value, delimiter = '=') {
  if (!value || typeof value !== 'object') return '';
  return Object.entries(value)
    .filter(([key, entry]) => key && entry != null && entry !== '')
    .map(([key, entry]) => `${key}${delimiter} ${entry}`)
    .join('\n');
}

export function normalizeBodyText(bodyText, headers) {
  const trimmed = (bodyText || '').trim();
  if (!trimmed) return null;

  const contentTypeHeader = Object.entries(headers || {}).find(([key]) => key.toLowerCase() === 'content-type');
  const contentType = contentTypeHeader?.[1]?.toLowerCase() || '';

  if (!contentType || contentType.includes('json')) {
    try {
      return JSON.parse(trimmed);
    } catch {
      return trimmed;
    }
  }

  return trimmed;
}

export function upsertHeaderText(headersText, key, value) {
  const parsed = parseKeyValueText(headersText, ':');
  if (value) parsed[key] = value;
  else delete parsed[key];
  return stringifyKeyValueObject(parsed, ':');
}

export function formatJson(value) {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  return JSON.stringify(value, null, 2);
}

export function buildCurlSnippet(serverUrl, resolvedPath, queryObject, method, headers, includeAuth, bodyText) {
  const url = new URL(`${serverUrl}${resolvedPath}`);
  Object.entries(queryObject || {}).forEach(([key, value]) => {
    if (value == null || value === '') return;
    url.searchParams.set(key, value);
  });

  const lines = [`curl -X ${method} "${url.toString()}"`];

  Object.entries(headers || {}).forEach(([key, value]) => {
    if (!key || value == null || value === '') return;
    lines.push(`  -H "${key}: ${value}"`);
  });

  if (includeAuth) {
    lines.push('  -H "Authorization: Bearer <token>"');
  }

  if (bodyText && method !== 'GET' && method !== 'HEAD') {
    lines.push(`  --data '${bodyText.replace(/'/g, "'\\''")}'`);
  }

  return lines.join(' \\\n');
}

export function buildFetchSnippet(serverUrl, resolvedPath, queryObject, method, headers, includeAuth, bodyText) {
  const url = new URL(`${serverUrl}${resolvedPath}`);
  Object.entries(queryObject || {}).forEach(([key, value]) => {
    if (value == null || value === '') return;
    url.searchParams.set(key, value);
  });

  const snippetHeaders = { ...headers };
  if (includeAuth) snippetHeaders.Authorization = 'Bearer <token>';

  const options = {
    method,
    headers: snippetHeaders,
  };

  if (bodyText && method !== 'GET' && method !== 'HEAD') {
    options.body = bodyText;
  }

  return [
    `const response = await fetch("${url.toString()}", ${JSON.stringify(options, null, 2)});`,
    'const text = await response.text();',
    'console.log(response.status, text);',
  ].join('\n');
}

export function buildOperationOptionLabel(operation) {
  const canonicalName = normalizeOperationSummary(operation?.summary, operation?.method || 'GET', operation?.path || '/');
  return `${canonicalName} - ${operation?.method || 'GET'} ${operation?.path || '/'}`;
}

export function createPendingResponseState(requestState) {
  return {
    ok: false,
    pending: true,
    statusCode: '...',
    elapsedMs: 0,
    url: '',
    method: requestState.method,
    headers: {},
    contentType: requestState.expectStream ? 'text/event-stream' : '',
    bodyType: requestState.expectStream ? 'sse' : 'pending',
    text: '',
    json: null,
    byteLength: 0,
    errorMessage: null,
    streamed: requestState.expectStream,
    events: [],
    usage: null,
    citations: null,
    status: requestState.expectStream ? 'Waiting for stream...' : 'Running request...',
  };
}
