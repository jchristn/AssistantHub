import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyButton from '../components/CopyButton';
import AlertModal from '../components/AlertModal';
import {
  ASSISTANT_TEMPLATES,
  RECENT_REQUESTS_KEY,
  SPECIAL_OPERATION_OVERRIDES,
  buildCurlSnippet,
  buildFetchSnippet,
  buildOperationOptionLabel,
  createPendingResponseState,
  extractPathParamNames,
  flattenOpenApiOperations,
  formatJson,
  normalizeBodyText,
  parseKeyValueText,
  resolvePathTemplate,
  stringifyKeyValueObject,
  upsertHeaderText,
} from './apiExplorerUtils';

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
  const assistantOperations = useMemo(() => {
    const publicOperations = operations.filter((operation) => operation.tags?.[0] === 'Assistant Public APIs');
    if (publicOperations.length < 1) return ASSISTANT_TEMPLATES;

    const templatesByRoute = new Map(ASSISTANT_TEMPLATES.map((template) => [`${template.method}:${template.path}`, template]));
    const mergedRoutes = new Set();
    const merged = publicOperations.map((operation) => {
      const routeKey = `${operation.method}:${operation.path}`;
      const template = templatesByRoute.get(routeKey) || {};
      mergedRoutes.add(routeKey);
      return {
        ...operation,
        ...template,
        key: operation.key,
        method: operation.method,
        path: operation.path,
        tags: operation.tags,
      };
    });

    ASSISTANT_TEMPLATES.forEach((template) => {
      const routeKey = `${template.method}:${template.path}`;
      if (!mergedRoutes.has(routeKey)) merged.push(template);
    });

    return merged;
  }, [operations]);

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
