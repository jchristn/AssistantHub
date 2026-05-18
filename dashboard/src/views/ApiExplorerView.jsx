import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyButton from '../components/CopyButton';
import AlertModal from '../components/AlertModal';

const RECENT_REQUESTS_KEY = 'ah_api_explorer_recent';

const SPECIAL_OPERATION_OVERRIDES = {
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
  'POST:/v1.0/endpoints/embedding/{endpointId}/test': {
    summary: 'Test embedding endpoint',
    description: 'Run a smoke test through AssistantHub against a Partio embedding endpoint.',
    bodyTemplate: {
      Input: 'AssistantHub embedding smoke test input',
      L2Normalization: false,
    },
  },
};

const ASSISTANT_TEMPLATES = [
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
    key: 'assistant-compact',
    method: 'POST',
    path: '/v1.0/assistants/{assistantId}/compact',
    tags: ['Assistant Public APIs'],
    summary: 'Force conversation compaction',
    description: 'Compact an assistant conversation to reduce token pressure.',
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

function flattenOpenApiOperations(spec) {
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

function extractPathParamNames(path) {
  return Array.from(path.matchAll(/\{([^}]+)\}/g)).map((match) => match[1]);
}

function resolvePathTemplate(pathTemplate, pathParams) {
  let resolved = pathTemplate || '/';
  Object.entries(pathParams || {}).forEach(([key, value]) => {
    resolved = resolved.replace(`{${key}}`, encodeURIComponent(value || `{${key}}`));
  });
  return resolved;
}

function parseKeyValueText(text, delimiter = '=') {
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

function stringifyKeyValueObject(value, delimiter = '=') {
  if (!value || typeof value !== 'object') return '';
  return Object.entries(value)
    .filter(([key, entry]) => key && entry != null && entry !== '')
    .map(([key, entry]) => `${key}${delimiter} ${entry}`)
    .join('\n');
}

function normalizeBodyText(bodyText, headers) {
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

function upsertHeaderText(headersText, key, value) {
  const parsed = parseKeyValueText(headersText, ':');
  if (value) parsed[key] = value;
  else delete parsed[key];
  return stringifyKeyValueObject(parsed, ':');
}

function formatJson(value) {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  return JSON.stringify(value, null, 2);
}

function buildCurlSnippet(serverUrl, resolvedPath, queryObject, method, headers, includeAuth, bodyText) {
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

function buildFetchSnippet(serverUrl, resolvedPath, queryObject, method, headers, includeAuth, bodyText) {
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

function buildOperationOptionLabel(operation) {
  const canonicalName = normalizeOperationSummary(operation?.summary, operation?.method || 'GET', operation?.path || '/');
  return `${canonicalName} - ${operation?.method || 'GET'} ${operation?.path || '/'}`;
}

function createPendingResponseState(requestState) {
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

function ApiExplorerView() {
  const { serverUrl, credential } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const api = new ApiClient(serverUrl, credential?.BearerToken);

  const [mode, setMode] = useState('system');
  const [searchText, setSearchText] = useState('');
  const [operations, setOperations] = useState([]);
  const [assistants, setAssistants] = useState([]);
  const [selectedAssistantId, setSelectedAssistantId] = useState('');
  const [threadId, setThreadId] = useState('');
  const [selectedOperationKey, setSelectedOperationKey] = useState(null);
  const [requestState, setRequestState] = useState({
    method: 'GET',
    pathTemplate: '/',
    pathParams: {},
    headersText: '',
    queryText: '',
    bodyText: '',
    includeAuth: true,
    expectStream: false,
    operationName: '',
    description: '',
  });
  const [responseState, setResponseState] = useState(null);
  const [recentRequests, setRecentRequests] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(RECENT_REQUESTS_KEY) || '[]');
    } catch {
      return [];
    }
  });
  const [loadingSpec, setLoadingSpec] = useState(true);
  const [running, setRunning] = useState(false);
  const [alert, setAlert] = useState(null);
  const handledPresetRef = useRef(null);
  const responseCardRef = useRef(null);

  useEffect(() => {
    (async () => {
      try {
        const [spec, assistantResult] = await Promise.all([
          api.getOpenApiSpec(),
          api.getAssistants({ maxResults: 1000 }),
        ]);

        setOperations(flattenOpenApiOperations(spec));

        const items = assistantResult?.Objects || (Array.isArray(assistantResult) ? assistantResult : []);
        setAssistants(items);
        if (items.length === 1) setSelectedAssistantId(items[0].Id);
      } catch (err) {
        setAlert({ title: 'Error', message: err.message || 'Failed to load API explorer metadata' });
      } finally {
        setLoadingSpec(false);
      }
    })();
  }, [serverUrl, credential]);

  const systemOperations = useMemo(() => operations.filter((operation) => operation.tags?.[0] !== 'Assistant Public APIs'), [operations]);
  const assistantOperations = useMemo(() => ASSISTANT_TEMPLATES, []);

  const visibleOperations = useMemo(() => {
    const source = mode === 'assistant' ? assistantOperations : systemOperations;
    const lowered = searchText.trim().toLowerCase();
    if (!lowered) return source;

    return source.filter((operation) => {
      const haystack = [
        operation.summary,
        operation.description,
        operation.path,
        operation.method,
        ...(operation.tags || []),
      ].join(' ').toLowerCase();
      return haystack.includes(lowered);
    });
  }, [assistantOperations, mode, searchText, systemOperations]);

  const groupedOperations = useMemo(() => {
    return visibleOperations.reduce((groups, operation) => {
      const tag = operation.tags?.[0] || 'Misc';
      if (!groups[tag]) groups[tag] = [];
      groups[tag].push(operation);
      return groups;
    }, {});
  }, [visibleOperations]);

  const selectableOperations = useMemo(
    () => (mode === 'assistant' ? assistantOperations : systemOperations),
    [assistantOperations, mode, systemOperations]
  );

  const loadOperation = useCallback((operation, overrides = {}) => {
    const pathParamNames = extractPathParamNames(operation.path);
    const nextPathParams = {};

    pathParamNames.forEach((name) => {
      if (overrides.pathParams?.[name] != null) {
        nextPathParams[name] = overrides.pathParams[name];
      } else if (name === 'assistantId') {
        nextPathParams[name] = selectedAssistantId || '';
      } else if (name === 'threadId') {
        nextPathParams[name] = threadId || '';
      } else {
        nextPathParams[name] = requestState.pathParams?.[name] || '';
      }
    });

    const nextHeadersText = overrides.headersText ?? operation.headersText ?? '';
    const nextBodyText = overrides.bodyText ?? (operation.bodyTemplate ? JSON.stringify(operation.bodyTemplate, null, 2) : '');
    const nextQueryText = overrides.queryText ?? '';

    setSelectedOperationKey(operation.key);
    setRequestState({
      method: overrides.method || operation.method,
      pathTemplate: overrides.pathTemplate || operation.path,
      pathParams: nextPathParams,
      headersText: nextHeadersText,
      queryText: nextQueryText,
      bodyText: nextBodyText,
      includeAuth: overrides.includeAuth ?? operation.includeAuth ?? true,
      expectStream: overrides.expectStream ?? operation.expectStream ?? false,
      operationName: overrides.operationName || operation.summary || `${operation.method} ${operation.path}`,
      description: overrides.description || operation.description || '',
    });
    setResponseState(null);
  }, [requestState.pathParams, selectedAssistantId, threadId]);

  useEffect(() => {
    if (mode === 'assistant') {
      const defaultOperation = assistantOperations[0];
      if (!selectedOperationKey && defaultOperation) loadOperation(defaultOperation);
      return;
    }

    if (!selectedOperationKey && systemOperations.length > 0) {
      loadOperation(systemOperations[0]);
    }
  }, [assistantOperations, loadOperation, mode, selectedOperationKey, systemOperations]);

  useEffect(() => {
    if (mode !== 'assistant') return;

    setRequestState((current) => {
      const nextPathParams = { ...current.pathParams };
      if (Object.prototype.hasOwnProperty.call(nextPathParams, 'assistantId')) nextPathParams.assistantId = selectedAssistantId || '';
      if (Object.prototype.hasOwnProperty.call(nextPathParams, 'threadId')) nextPathParams.threadId = threadId || '';

      let headersText = current.headersText;
      if (current.pathTemplate.includes('/chat') || current.pathTemplate.includes('/compact')) {
        headersText = upsertHeaderText(headersText, 'X-Thread-ID', threadId || '');
      }

      return {
        ...current,
        pathParams: nextPathParams,
        headersText,
      };
    });
  }, [mode, selectedAssistantId, threadId]);

  useEffect(() => {
    const preset = location.state?.preset;
    if (!preset) return;

    const presetKey = JSON.stringify(preset);
    if (handledPresetRef.current === presetKey) return;
    handledPresetRef.current = presetKey;

    if (preset.type === 'requestHistoryReplay' && preset.entry) {
      const entry = preset.entry;
      const includeAuth = !['public', 'public-assistant'].includes(entry.SourceType);
      const requestHeaders = { ...(entry.RequestHeaders || {}) };
      delete requestHeaders.Authorization;
      delete requestHeaders.authorization;

      setMode(entry.RequestType === 'AssistantApi' ? 'assistant' : 'system');
      if (entry.AssistantId) setSelectedAssistantId(entry.AssistantId);
      if (entry.ThreadId) setThreadId(entry.ThreadId);
      setSelectedOperationKey(null);
      setRequestState({
        method: entry.HttpMethod || 'GET',
        pathTemplate: entry.RequestPath || '/',
        pathParams: {},
        headersText: stringifyKeyValueObject(requestHeaders, ':'),
        queryText: stringifyKeyValueObject(entry.QueryParameters || {}, '='),
        bodyText: entry.RequestBody || '',
        includeAuth,
        expectStream: false,
        operationName: `Replay ${entry.HttpMethod || 'GET'} ${entry.RequestPath || '/'}`,
        description: 'Loaded from request history.',
      });
      setResponseState(null);
      navigate(location.pathname, { replace: true, state: {} });
      return;
    }

    if (preset.type === 'completionTest') {
      setMode('system');
      setSelectedOperationKey('POST:/v1.0/endpoints/completion/{endpointId}/test');
      setRequestState({
        method: 'POST',
        pathTemplate: '/v1.0/endpoints/completion/{endpointId}/test',
        pathParams: { endpointId: preset.endpointId || '' },
        headersText: 'Content-Type: application/json',
        queryText: '',
        bodyText: JSON.stringify(SPECIAL_OPERATION_OVERRIDES['POST:/v1.0/endpoints/completion/{endpointId}/test'].bodyTemplate, null, 2),
        includeAuth: true,
        expectStream: false,
        operationName: preset.endpointName ? `Test completion endpoint: ${preset.endpointName}` : 'Test completion endpoint',
        description: 'Preset generated from the completion endpoints view.',
      });
      setResponseState(null);
      navigate(location.pathname, { replace: true, state: {} });
      return;
    }

    if (preset.type === 'embeddingTest') {
      setMode('system');
      setSelectedOperationKey('POST:/v1.0/endpoints/embedding/{endpointId}/test');
      setRequestState({
        method: 'POST',
        pathTemplate: '/v1.0/endpoints/embedding/{endpointId}/test',
        pathParams: { endpointId: preset.endpointId || '' },
        headersText: 'Content-Type: application/json',
        queryText: '',
        bodyText: JSON.stringify(SPECIAL_OPERATION_OVERRIDES['POST:/v1.0/endpoints/embedding/{endpointId}/test'].bodyTemplate, null, 2),
        includeAuth: true,
        expectStream: false,
        operationName: preset.endpointName ? `Test embedding endpoint: ${preset.endpointName}` : 'Test embedding endpoint',
        description: 'Preset generated from the embedding endpoints view.',
      });
      setResponseState(null);
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.pathname, location.state, navigate]);

  useEffect(() => {
    localStorage.setItem(RECENT_REQUESTS_KEY, JSON.stringify(recentRequests.slice(0, 12)));
  }, [recentRequests]);

  const resolvedPath = useMemo(() => resolvePathTemplate(requestState.pathTemplate, requestState.pathParams), [requestState.pathParams, requestState.pathTemplate]);
  const queryObject = useMemo(() => parseKeyValueText(requestState.queryText, '='), [requestState.queryText]);
  const headerObject = useMemo(() => parseKeyValueText(requestState.headersText, ':'), [requestState.headersText]);
  const responseHeadersText = useMemo(() => stringifyKeyValueObject(responseState?.headers || {}, ':'), [responseState]);
  const curlSnippet = useMemo(() => buildCurlSnippet(serverUrl, resolvedPath, queryObject, requestState.method, headerObject, requestState.includeAuth, requestState.bodyText), [headerObject, queryObject, requestState.bodyText, requestState.includeAuth, requestState.method, resolvedPath, serverUrl]);
  const fetchSnippet = useMemo(() => buildFetchSnippet(serverUrl, resolvedPath, queryObject, requestState.method, headerObject, requestState.includeAuth, requestState.bodyText), [headerObject, queryObject, requestState.bodyText, requestState.includeAuth, requestState.method, resolvedPath, serverUrl]);

  const runRequest = async () => {
    if (!serverUrl) {
      setAlert({ title: 'Server Required', message: 'No AssistantHub server URL is configured for the explorer.' });
      return;
    }

    const missingPathParams = extractPathParamNames(requestState.pathTemplate).filter((name) => !(requestState.pathParams?.[name] || '').trim());
    if (missingPathParams.length > 0) {
      setAlert({
        title: 'Missing Path Parameters',
        message: `Provide values for: ${missingPathParams.join(', ')}`,
      });
      return;
    }

    setRunning(true);
    setResponseState(createPendingResponseState(requestState));
    window.requestAnimationFrame(() => {
      responseCardRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    try {
      const body = normalizeBodyText(requestState.bodyText, headerObject);
      const requestDescriptor = {
        mode,
        selectedOperationKey,
        operationName: requestState.operationName,
        method: requestState.method,
        pathTemplate: requestState.pathTemplate,
        pathParams: requestState.pathParams,
        headersText: requestState.headersText,
        queryText: requestState.queryText,
        bodyText: requestState.bodyText,
        includeAuth: requestState.includeAuth,
        expectStream: requestState.expectStream,
      };

      const result = requestState.expectStream
        ? await api.requestStream({
            method: requestState.method,
            path: resolvedPath,
            query: queryObject,
            headers: headerObject,
            body,
            includeAuth: requestState.includeAuth,
            onEvent: (event) => {
              if (event.type === 'done') {
                setResponseState((current) => current ? {
                  ...current,
                  pending: true,
                  status: 'Finalizing stream...',
                } : current);
                return;
              }

              setResponseState((current) => {
                const previous = current || createPendingResponseState(requestState);
                const nextText = event.deltaContent ? `${previous.text || ''}${event.deltaContent}` : previous.text || '';
                const nextEvents = event.type === 'message'
                  ? [...(previous.events || []).slice(-199), event]
                  : previous.events || [];

                return {
                  ...previous,
                  streamed: true,
                  text: nextText,
                  events: nextEvents,
                  status: event.json?.status || previous.status || 'Streaming response...',
                  usage: event.json?.usage || previous.usage || null,
                  citations: event.json?.citations || previous.citations || null,
                };
              });
            },
          })
        : await api.requestRaw({
            method: requestState.method,
            path: resolvedPath,
            query: queryObject,
            headers: headerObject,
            body,
            includeAuth: requestState.includeAuth,
          });

      if (requestState.pathTemplate === '/v1.0/assistants/{assistantId}/threads' && result.ok && result.json?.ThreadId) {
        setThreadId(result.json.ThreadId);
      }

      setResponseState({
        ...result,
        pending: false,
        status: result.status || null,
      });
      setRecentRequests((current) => [requestDescriptor, ...current.filter((item) => JSON.stringify(item) !== JSON.stringify(requestDescriptor))].slice(0, 12));
    } catch (err) {
      setResponseState((current) => current ? {
        ...current,
        pending: false,
        errorMessage: err.message || 'Failed to execute request',
        status: 'Request failed before a response was received.',
      } : null);
      setAlert({ title: 'Error', message: err.message || 'Failed to execute request' });
    } finally {
      setRunning(false);
    }
  };

  const createThread = async () => {
    if (!selectedAssistantId) {
      setAlert({ title: 'Assistant Required', message: 'Select an assistant before creating a thread.' });
      return;
    }

    try {
      const result = await api.requestRaw({
        method: 'POST',
        path: `/v1.0/assistants/${encodeURIComponent(selectedAssistantId)}/threads`,
        includeAuth: false,
      });

      if (!result.ok) {
        throw new Error(result.errorMessage || 'Failed to create thread');
      }

      if (result.json?.ThreadId) {
        setThreadId(result.json.ThreadId);
      }
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to create thread' });
    }
  };

  const restoreRecentRequest = (recent) => {
    setMode(recent.mode || 'system');
    setSelectedOperationKey(recent.selectedOperationKey || null);
    setRequestState({
      method: recent.method || 'GET',
      pathTemplate: recent.pathTemplate || '/',
      pathParams: recent.pathParams || {},
      headersText: recent.headersText || '',
      queryText: recent.queryText || '',
      bodyText: recent.bodyText || '',
      includeAuth: recent.includeAuth ?? true,
      expectStream: recent.expectStream ?? false,
      operationName: recent.operationName || 'Restored Request',
      description: 'Restored from local recent-request history.',
    });
  };

  return (
    <div className="api-explorer-page">
      <div className="content-header">
        <div>
          <h1 className="content-title">API Explorer</h1>
          <p className="content-subtitle">Explore live AssistantHub routes, execute system APIs, and exercise assistants end-to-end.</p>
        </div>
        <div className="api-explorer-header-actions">
          <button
            type="button"
            className={`btn ${mode === 'system' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => {
              setMode('system');
              setSelectedOperationKey(null);
              setResponseState(null);
            }}
          >
            System APIs
          </button>
          <button
            type="button"
            className={`btn ${mode === 'assistant' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => {
              setMode('assistant');
              setSelectedOperationKey(null);
              setResponseState(null);
            }}
          >
            Assistant APIs
          </button>
        </div>
      </div>

      <div className="api-explorer-toolbar">
        <input
          className="request-history-filter-input api-explorer-search"
          type="text"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          placeholder="Search operations by method, path, summary, or tag"
        />
        <div className="api-explorer-toolbar-meta">
          <span>{loadingSpec ? 'Loading routes...' : `${operations.length} live operations loaded`}</span>
          <span>{recentRequests.length} recent request{recentRequests.length === 1 ? '' : 's'}</span>
        </div>
      </div>

      <div className="api-explorer-layout">
        <aside className="api-explorer-sidebar">
          {mode === 'assistant' && (
            <div className="api-explorer-card">
              <h3>Assistant Context</h3>
              <div className="form-group">
                <label>Assistant</label>
                <select value={selectedAssistantId} onChange={(e) => setSelectedAssistantId(e.target.value)}>
                  <option value="">Select assistant</option>
                  {assistants.map((assistant) => (
                    <option key={assistant.Id} value={assistant.Id}>
                      {assistant.Name} ({assistant.Id.slice(0, 8)}...)
                    </option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label>Thread ID</label>
                <input type="text" value={threadId} onChange={(e) => setThreadId(e.target.value)} placeholder="thread_..." />
              </div>
              <button type="button" className="btn btn-secondary btn-sm" onClick={createThread}>Create Thread</button>
            </div>
          )}

          <div className="api-explorer-card">
            <h3>Recent Requests</h3>
            {recentRequests.length < 1 ? (
              <p className="api-explorer-empty">Recent requests are stored locally after you execute them.</p>
            ) : recentRequests.map((recent, index) => (
              <button type="button" key={`${recent.method}-${recent.pathTemplate}-${index}`} className="api-explorer-recent" onClick={() => restoreRecentRequest(recent)}>
                <span className={`request-history-method method-${(recent.method || 'GET').toLowerCase()}`}>{recent.method}</span>
                <span>
                  <strong>{recent.operationName || recent.pathTemplate}</strong>
                  <small>{recent.pathTemplate}</small>
                </span>
              </button>
            ))}
          </div>
        </aside>

        <div className="api-explorer-main">
          <div className="api-explorer-card">
            <div className="form-group">
              <label>Operation</label>
              <select
                value={selectedOperationKey || ''}
                onChange={(e) => {
                  const operation = selectableOperations.find((item) => item.key === e.target.value);
                  if (operation) loadOperation(operation);
                }}
                disabled={visibleOperations.length < 1}
              >
                {visibleOperations.length < 1 ? (
                  <option value="">No operations match the current search</option>
                ) : (
                  Object.entries(groupedOperations).map(([tag, items]) => (
                    <optgroup key={tag} label={tag}>
                      {items.map((operation) => (
                        <option key={operation.key} value={operation.key}>
                          {buildOperationOptionLabel(operation)}
                        </option>
                      ))}
                    </optgroup>
                  ))
                )}
              </select>
            </div>
          </div>

          <div className="api-explorer-card">
            <div className="api-explorer-card-header">
              <div>
                <h3>{requestState.operationName || 'Request'}</h3>
                <p>{requestState.description || 'Edit and execute the request against the current AssistantHub server.'}</p>
              </div>
              <div className="api-explorer-card-actions">
                <label className="api-explorer-toggle">
                  <input type="checkbox" checked={requestState.includeAuth} onChange={(e) => setRequestState((current) => ({ ...current, includeAuth: e.target.checked }))} />
                  <span>Use dashboard auth</span>
                </label>
                <label className="api-explorer-toggle">
                  <input type="checkbox" checked={requestState.expectStream} onChange={(e) => setRequestState((current) => ({ ...current, expectStream: e.target.checked }))} />
                  <span>Expect stream</span>
                </label>
                <button type="button" className="btn btn-secondary" onClick={() => setResponseState(null)}>Clear Response</button>
                <button type="button" className="btn btn-primary" onClick={runRequest} disabled={running}>{running ? 'Running...' : 'Run Request'}</button>
              </div>
            </div>

            <div className="form-row api-explorer-method-row">
              <div className="form-group">
                <label>Method</label>
                <select value={requestState.method} onChange={(e) => setRequestState((current) => ({ ...current, method: e.target.value }))}>
                  <option value="GET">GET</option>
                  <option value="POST">POST</option>
                  <option value="PUT">PUT</option>
                  <option value="DELETE">DELETE</option>
                  <option value="HEAD">HEAD</option>
                  <option value="PATCH">PATCH</option>
                </select>
              </div>
              <div className="form-group api-explorer-path-group">
                <label>Path Template</label>
                <input type="text" value={requestState.pathTemplate} onChange={(e) => setRequestState((current) => ({ ...current, pathTemplate: e.target.value }))} />
              </div>
            </div>

            {extractPathParamNames(requestState.pathTemplate).length > 0 && (
              <div className="api-explorer-path-params">
                {extractPathParamNames(requestState.pathTemplate).map((name) => (
                  <div key={name} className="form-group">
                    <label>{name}</label>
                    <input
                      type="text"
                      value={requestState.pathParams?.[name] || ''}
                      onChange={(e) => setRequestState((current) => ({
                        ...current,
                        pathParams: { ...current.pathParams, [name]: e.target.value },
                      }))}
                    />
                  </div>
                ))}
              </div>
            )}

            <div className="api-explorer-resolved-path">
              <span>Resolved path</span>
              <code>{resolvedPath}</code>
            </div>

            <div className="api-explorer-editor-grid">
              <div className="form-group">
                <label>Query Parameters</label>
                <textarea
                  rows={8}
                  value={requestState.queryText}
                  onChange={(e) => setRequestState((current) => ({ ...current, queryText: e.target.value }))}
                  placeholder="maxResults=100&#10;assistantId=asst_123"
                />
              </div>
              <div className="form-group">
                <label>Headers</label>
                <textarea
                  rows={8}
                  value={requestState.headersText}
                  onChange={(e) => setRequestState((current) => ({ ...current, headersText: e.target.value }))}
                  placeholder="Content-Type: application/json&#10;X-Thread-ID: thread_123"
                />
              </div>
            </div>

            <div className="form-group">
              <label>Body</label>
              <textarea
                rows={14}
                value={requestState.bodyText}
                onChange={(e) => setRequestState((current) => ({ ...current, bodyText: e.target.value }))}
                placeholder='{"messages":[{"role":"user","content":"Hello"}]}'
              />
            </div>
          </div>

          <div className="api-explorer-card" ref={responseCardRef}>
            <div className="api-explorer-card-header">
              <div>
                <h3>Response</h3>
                <p>Inspect status, headers, body, and streaming output from the live request.</p>
              </div>
            </div>

            {!responseState ? (
              <p className="api-explorer-empty">Run a request to inspect the live response here.</p>
            ) : (
              <>
                <div className="api-explorer-response-stats">
                  <div className="stat-card">
                    <span className="stat-card-label">Status</span>
                    <span className="stat-card-value">{responseState.statusCode}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Elapsed</span>
                    <span className="stat-card-value">{responseState.elapsedMs} ms</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Body Type</span>
                    <span className="stat-card-value">{responseState.bodyType}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Content-Type</span>
                    <span className="stat-card-value api-explorer-content-type">{responseState.contentType || '-'}</span>
                  </div>
                </div>

                {responseState.pending && (
                  <div className="api-explorer-running-banner">
                    {responseState.status || 'Running request...'}
                  </div>
                )}

                {responseState.errorMessage && (
                  <div className="endpoint-test-error" style={{ marginBottom: '1rem' }}>
                    {responseState.errorMessage}
                  </div>
                )}

                {responseState.streamed && (
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Streaming Transcript</span>
                      <CopyButton text={responseState.text || ''} />
                    </div>
                    <pre className="json-view request-history-block-body">{responseState.text || '(no streamed content)'}</pre>
                  </div>
                )}

                <div className="api-explorer-response-grid">
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Headers</span>
                      <CopyButton text={responseHeadersText} />
                    </div>
                    <pre className="json-view request-history-block-body">{responseHeadersText || '{}'}</pre>
                  </div>
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Body</span>
                      <CopyButton text={responseState.json ? formatJson(responseState.json) : responseState.text || ''} />
                    </div>
                    <pre className="json-view request-history-block-body">{responseState.json ? formatJson(responseState.json) : responseState.text || '(empty)'}</pre>
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="api-explorer-card">
            <div className="api-explorer-card-header">
              <div>
                <h3>Code Snippets</h3>
                <p>Reuse the current request shape outside the dashboard.</p>
              </div>
            </div>

            <div className="api-explorer-response-grid">
              <div className="request-history-block">
                <div className="request-history-block-header">
                  <span>cURL</span>
                  <CopyButton text={curlSnippet} />
                </div>
                <pre className="json-view request-history-block-body">{curlSnippet}</pre>
              </div>
              <div className="request-history-block">
                <div className="request-history-block-header">
                  <span>JavaScript Fetch</span>
                  <CopyButton text={fetchSnippet} />
                </div>
                <pre className="json-view request-history-block-body">{fetchSnippet}</pre>
              </div>
            </div>
          </div>
        </div>
      </div>

      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default ApiExplorerView;
