export class ApiClient {
  constructor(serverUrl, bearerToken) {
    this.serverUrl = serverUrl;
    this.bearerToken = bearerToken;
  }

  buildUrl(path, query = null) {
    const normalizedPath = path.startsWith('http://') || path.startsWith('https://')
      ? path
      : `${this.serverUrl}${path.startsWith('/') ? path : `/${path}`}`;

    const url = new URL(normalizedPath);
    if (query) {
      Object.entries(query).forEach(([key, value]) => {
        if (value == null || value === '') return;
        if (Array.isArray(value)) {
          value.forEach((item) => {
            if (item != null && item !== '') url.searchParams.append(key, item);
          });
        } else {
          url.searchParams.set(key, String(value));
        }
      });
    }

    return url.toString();
  }

  buildRequestHeaders(headers = {}, includeAuth = true) {
    const requestHeaders = {};

    if (includeAuth && this.bearerToken && !Object.keys(headers).some((key) => key.toLowerCase() === 'authorization')) {
      requestHeaders.Authorization = `Bearer ${this.bearerToken}`;
    }

    Object.entries(headers || {}).forEach(([key, value]) => {
      if (!key || value == null || value === '') return;
      requestHeaders[key] = value;
    });

    return requestHeaders;
  }

  buildDockerDirectOpenApiUrl() {
    try {
      const url = new URL(this.serverUrl);
      if (url.port !== '8801') return null;

      url.port = '8800';
      url.pathname = '/openapi.json';
      url.search = '';
      url.hash = '';
      return url.toString();
    } catch {
      return null;
    }
  }

  isTextLikeContentType(contentType = '') {
    const lowered = contentType.toLowerCase();
    if (!lowered) return true;
    if (lowered.startsWith('text/')) return true;
    if (lowered.includes('json')) return true;
    if (lowered.includes('xml')) return true;
    if (lowered.includes('javascript')) return true;
    if (lowered.includes('x-www-form-urlencoded')) return true;
    if (lowered.includes('svg')) return true;
    return false;
  }

  async parseResponsePayload(response, method = 'GET') {
    const contentType = response.headers.get('content-type') || '';

    if (method === 'HEAD' || response.status === 204 || response.headers.get('content-length') === '0') {
      return {
        bodyType: 'empty',
        text: '',
        json: null,
        byteLength: 0,
      };
    }

    if (!this.isTextLikeContentType(contentType)) {
      const bytes = await response.arrayBuffer();
      return {
        bodyType: 'binary',
        text: `[binary response omitted: ${bytes.byteLength} bytes${contentType ? `, ${contentType}` : ''}]`,
        json: null,
        byteLength: bytes.byteLength,
      };
    }

    const text = await response.text();
    if (contentType.includes('application/json') || contentType.includes('text/json')) {
      try {
        return {
          bodyType: 'json',
          text,
          json: text ? JSON.parse(text) : null,
          byteLength: new TextEncoder().encode(text).length,
        };
      } catch {
        return {
          bodyType: 'text',
          text,
          json: null,
          byteLength: new TextEncoder().encode(text).length,
        };
      }
    }

    try {
      return {
        bodyType: 'json',
        text,
        json: text ? JSON.parse(text) : null,
        byteLength: new TextEncoder().encode(text).length,
      };
    } catch {
      return {
        bodyType: 'text',
        text,
        json: null,
        byteLength: new TextEncoder().encode(text).length,
      };
    }
  }

  prepareRequestOptions(method, body = null, headers = {}, includeAuth = true) {
    const isFormData = typeof FormData !== 'undefined' && body instanceof FormData;
    const requestHeaders = this.buildRequestHeaders(headers, includeAuth);
    const options = { method, headers: requestHeaders };

    if (body != null && method !== 'GET' && method !== 'HEAD') {
      if (isFormData) {
        options.body = body;
      } else if (typeof body === 'string') {
        if (!Object.keys(requestHeaders).some((key) => key.toLowerCase() === 'content-type')) {
          requestHeaders['Content-Type'] = 'application/json';
        }
        options.body = body;
      } else {
        if (!Object.keys(requestHeaders).some((key) => key.toLowerCase() === 'content-type')) {
          requestHeaders['Content-Type'] = 'application/json';
        }
        options.body = JSON.stringify(body);
      }
    }

    return options;
  }

  async requestRaw({
    method = 'GET',
    path,
    query = null,
    headers = {},
    body = null,
    includeAuth = true,
    signal = null,
  }) {
    const started = performance.now();
    const url = this.buildUrl(path, query);
    const options = this.prepareRequestOptions(method, body, headers, includeAuth);
    if (signal) options.signal = signal;

    const response = await fetch(url, options);
    const payload = await this.parseResponsePayload(response, method);
    const responseHeaders = {};
    response.headers.forEach((value, key) => {
      responseHeaders[key] = value;
    });

    const result = {
      ok: response.ok,
      statusCode: response.status,
      elapsedMs: Math.round(performance.now() - started),
      url,
      method,
      headers: responseHeaders,
      contentType: response.headers.get('content-type') || '',
      bodyType: payload.bodyType,
      text: payload.text,
      json: payload.json,
      byteLength: payload.byteLength,
      errorMessage: null,
    };

    if (!response.ok) {
      result.errorMessage =
        payload.json?.Message ||
        payload.json?.message ||
        payload.json?.Detail ||
        payload.text ||
        `Request failed with status ${response.status}`;
    }

    return result;
  }

  async requestStream({
    method = 'POST',
    path,
    query = null,
    headers = {},
    body = null,
    includeAuth = true,
    signal = null,
    onEvent = null,
  }) {
    const started = performance.now();
    const url = this.buildUrl(path, query);
    const options = this.prepareRequestOptions(method, body, headers, includeAuth);
    if (signal) options.signal = signal;

    const response = await fetch(url, options);
    const contentType = response.headers.get('content-type') || '';

    if (!contentType.includes('text/event-stream')) {
      const payload = await this.parseResponsePayload(response, method);
      const responseHeaders = {};
      response.headers.forEach((value, key) => {
        responseHeaders[key] = value;
      });

      return {
        ok: response.ok,
        statusCode: response.status,
        elapsedMs: Math.round(performance.now() - started),
        url,
        method,
        streamed: false,
        headers: responseHeaders,
        contentType,
        bodyType: payload.bodyType,
        text: payload.text,
        json: payload.json,
        byteLength: payload.byteLength,
        errorMessage: response.ok
          ? null
          : payload.json?.Message || payload.json?.message || payload.json?.Detail || payload.text || `Request failed with status ${response.status}`,
      };
    }

    const responseHeaders = {};
    response.headers.forEach((value, key) => {
      responseHeaders[key] = value;
    });

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let text = '';
    let usage = null;
    let citations = null;
    let status = null;
    const events = [];

    const flushEvent = (rawEvent) => {
      if (!rawEvent) return;

      const normalized = rawEvent.replace(/\r\n/g, '\n');
      const lines = normalized.split('\n');
      const dataLines = [];
      let eventType = 'message';

      for (const line of lines) {
        if (line.startsWith('event:')) eventType = line.substring(6).trim() || 'message';
        if (line.startsWith('data:')) dataLines.push(line.substring(5).trimStart());
      }

      if (dataLines.length < 1) return;

      const data = dataLines.join('\n');
      if (data === '[DONE]') {
        if (onEvent) onEvent({ type: 'done' });
        return;
      }

      let parsed = null;
      try {
        parsed = JSON.parse(data);
      } catch {
        parsed = null;
      }

      if (parsed?.usage) usage = parsed.usage;
      if (parsed?.citations) citations = parsed.citations;
      if (parsed?.status) status = parsed.status;

      const deltaContent = parsed?.choices?.[0]?.delta?.content ?? parsed?.choices?.[0]?.message?.content ?? '';
      if (deltaContent) text += deltaContent;

      const effectiveType = parsed?.event_type || parsed?.type || eventType;
      const event = {
        type: effectiveType,
        data,
        json: parsed,
        deltaContent,
      };

      if (events.length < 200) events.push(event);
      if (onEvent) onEvent(event);
    };

    if (reader) {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        let separatorIndex = buffer.indexOf('\n\n');
        while (separatorIndex >= 0) {
          const rawEvent = buffer.substring(0, separatorIndex);
          buffer = buffer.substring(separatorIndex + 2);
          flushEvent(rawEvent);
          separatorIndex = buffer.indexOf('\n\n');
        }
      }
    }

    buffer += decoder.decode();
    if (buffer.trim()) flushEvent(buffer.trim());

    return {
      ok: response.ok,
      statusCode: response.status,
      elapsedMs: Math.round(performance.now() - started),
      url,
      method,
      streamed: true,
      headers: responseHeaders,
      contentType,
      bodyType: 'sse',
      text,
      json: null,
      events,
      usage,
      citations,
      status,
      errorMessage: response.ok ? null : `Request failed with status ${response.status}`,
    };
  }

  async request(method, path, body = null, isFormData = false) {
    const headers = {};
    if (this.bearerToken) {
      headers['Authorization'] = `Bearer ${this.bearerToken}`;
    }
    if (body && !isFormData) {
      headers['Content-Type'] = 'application/json';
    }

    const options = { method, headers };
    if (body) {
      options.body = isFormData ? body : JSON.stringify(body);
    }

    const response = await fetch(`${this.serverUrl}${path}`, options);

    if (response.status === 204 || response.headers.get('content-length') === '0') {
      if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
      return { success: true, statusCode: response.status };
    }

    if (method === 'HEAD') {
      return { success: response.ok, statusCode: response.status };
    }

    const contentType = response.headers.get('content-type') || '';
    if (!contentType.includes('application/json') && !contentType.includes('text/json')) {
      const text = await response.text();
      if (!response.ok) {
        if (response.status === 413) throw new Error('Request payload too large. Try a smaller file.');
        throw new Error(`Server returned ${response.status}: non-JSON response`);
      }
      // Try parsing anyway in case content-type header is missing
      try {
        const parsed = JSON.parse(text);
        if (Array.isArray(parsed)) return parsed;
        return { ...parsed, statusCode: response.status };
      } catch {
        throw new Error(`Server returned unexpected non-JSON response (${response.status})`);
      }
    }

    const data = await response.json();

    if (!response.ok) {
      const primary = data?.Message || data?.message;
      const detail = data?.Description || data?.description || data?.Detail || data?.detail;
      const message = detail && (!primary || primary === 'The requested resource was not found.')
        ? detail
        : primary || detail || `Request failed with status ${response.status}`;
      throw new Error(message);
    }

    if (Array.isArray(data)) return data;
    return { ...data, statusCode: response.status };
  }

  // Tenants
  createTenant(tenant) { return this.request('PUT', '/v1.0/tenants', tenant); }
  getTenants(params) { return this.request('GET', '/v1.0/tenants' + this.buildQuery(params)); }
  getTenant(id) { return this.request('GET', `/v1.0/tenants/${id}`); }
  updateTenant(id, tenant) { return this.request('PUT', `/v1.0/tenants/${id}`, tenant); }
  deleteTenant(id) { return this.request('DELETE', `/v1.0/tenants/${id}`); }

  // WhoAmI
  whoami() { return this.request('GET', '/v1.0/whoami'); }

  // Users (tenant-scoped)
  createUser(user, tenantId) { return this.request('PUT', `/v1.0/tenants/${tenantId}/users`, user); }
  getUsers(tenantId, params) { return this.request('GET', `/v1.0/tenants/${tenantId}/users` + this.buildQuery(params)); }
  getUser(tenantId, id) { return this.request('GET', `/v1.0/tenants/${tenantId}/users/${id}`); }
  updateUser(tenantId, id, user) { return this.request('PUT', `/v1.0/tenants/${tenantId}/users/${id}`, user); }
  deleteUser(tenantId, id) { return this.request('DELETE', `/v1.0/tenants/${tenantId}/users/${id}`); }
  headUser(tenantId, id) { return this.request('HEAD', `/v1.0/tenants/${tenantId}/users/${id}`); }

  // Credentials (tenant-scoped)
  createCredential(cred, tenantId) { return this.request('PUT', `/v1.0/tenants/${tenantId}/credentials`, cred); }
  getCredentials(tenantId, params) { return this.request('GET', `/v1.0/tenants/${tenantId}/credentials` + this.buildQuery(params)); }
  getCredential(tenantId, id) { return this.request('GET', `/v1.0/tenants/${tenantId}/credentials/${id}`); }
  updateCredential(tenantId, id, cred) { return this.request('PUT', `/v1.0/tenants/${tenantId}/credentials/${id}`, cred); }
  deleteCredential(tenantId, id) { return this.request('DELETE', `/v1.0/tenants/${tenantId}/credentials/${id}`); }

  // Buckets
  createBucket(bucket) { return this.request('PUT', '/v1.0/buckets', bucket); }
  getBuckets(params) { return this.request('GET', '/v1.0/buckets' + this.buildQuery(params)); }
  getBucket(name) { return this.request('GET', `/v1.0/buckets/${name}`); }
  deleteBucket(name) { return this.request('DELETE', `/v1.0/buckets/${name}`); }

  // Bucket Objects
  createDirectory(bucketName, key) {
    return this.request('PUT', `/v1.0/buckets/${bucketName}/objects?key=${encodeURIComponent(key)}`);
  }
  getObjects(bucketName, prefix = '', delimiter = '/') {
    const params = new URLSearchParams();
    if (prefix) params.set('prefix', prefix);
    if (delimiter) params.set('delimiter', delimiter);
    return this.request('GET', `/v1.0/buckets/${bucketName}/objects?${params.toString()}`);
  }
  getObjectMetadata(bucketName, key) {
    return this.request('GET', `/v1.0/buckets/${bucketName}/objects/metadata?key=${encodeURIComponent(key)}`);
  }
  deleteObject(bucketName, key) {
    return this.request('DELETE', `/v1.0/buckets/${bucketName}/objects?key=${encodeURIComponent(key)}`);
  }
  getObjectDownloadUrl(bucketName, key) {
    return `${this.serverUrl}/v1.0/buckets/${bucketName}/objects/download?key=${encodeURIComponent(key)}&token=${encodeURIComponent(this.bearerToken)}`;
  }
  uploadObject(bucketName, key, file) {
    const headers = {};
    if (this.bearerToken) headers['Authorization'] = `Bearer ${this.bearerToken}`;
    headers['Content-Type'] = file.type || 'application/octet-stream';
    return fetch(`${this.serverUrl}/v1.0/buckets/${bucketName}/objects/upload?key=${encodeURIComponent(key)}`, {
      method: 'POST', headers, body: file
    }).then(r => r.json());
  }

  // Collections
  createCollection(collection) { return this.request('PUT', '/v1.0/collections', collection); }
  getCollections(params) { return this.request('GET', '/v1.0/collections' + this.buildQuery(params)); }
  getCollection(id) { return this.request('GET', `/v1.0/collections/${id}`); }
  updateCollection(id, collection) { return this.request('PUT', `/v1.0/collections/${id}`, collection); }
  deleteCollection(id) { return this.request('DELETE', `/v1.0/collections/${id}`); }
  searchCollection(collectionId, request) { return this.request('POST', `/v1.0/collections/${collectionId}/search`, request); }

  // Collection Records
  createRecord(collectionId, record) { return this.request('PUT', `/v1.0/collections/${collectionId}/records`, record); }
  getRecords(collectionId, params) { return this.request('GET', `/v1.0/collections/${collectionId}/records` + this.buildQuery(params)); }
  getRecord(collectionId, recordId) { return this.request('GET', `/v1.0/collections/${collectionId}/records/${recordId}`); }
  deleteRecord(collectionId, recordId) { return this.request('DELETE', `/v1.0/collections/${collectionId}/records/${recordId}`); }

  // Indices
  createIndex(index) { return this.request('PUT', '/v1.0/indices', index); }
  getIndices(params) { return this.request('GET', '/v1.0/indices' + this.buildQuery(params)); }
  getIndex(id) { return this.request('GET', `/v1.0/indices/${id}`); }
  updateIndex(id, index) { return this.request('PUT', `/v1.0/indices/${id}`, index); }
  deleteIndex(id) { return this.request('DELETE', `/v1.0/indices/${id}`); }
  searchIndex(id, request) { return this.request('POST', `/v1.0/indices/${id}/search`, request); }
  getIndexTopTerms(id, params) { return this.request('GET', `/v1.0/indices/${id}/terms/top` + this.buildQuery(params)); }
  updateIndexLabels(id, labels) { return this.request('PUT', `/v1.0/indices/${id}/labels`, labels); }
  updateIndexTags(id, tags) { return this.request('PUT', `/v1.0/indices/${id}/tags`, tags); }
  updateIndexCustomMetadata(id, metadata) { return this.request('PUT', `/v1.0/indices/${id}/custom-metadata`, metadata); }
  rebuildIndexCache(id, request = {}) { return this.request('POST', `/v1.0/indices/${id}/cache/rebuild`, request); }

  // Index Records
  createIndexRecord(indexId, record) { return this.request('PUT', `/v1.0/indices/${indexId}/records`, record); }
  createIndexRecordsBatch(indexId, request) { return this.request('POST', `/v1.0/indices/${indexId}/records/batch`, request); }
  getIndexRecords(indexId, params) { return this.request('GET', `/v1.0/indices/${indexId}/records` + this.buildQuery(params)); }
  getIndexRecord(indexId, recordId) { return this.request('GET', `/v1.0/indices/${indexId}/records/${recordId}`); }
  deleteIndexRecord(indexId, recordId) { return this.request('DELETE', `/v1.0/indices/${indexId}/records/${recordId}`); }
  deleteIndexRecords(indexId, ids) { return this.request('DELETE', `/v1.0/indices/${indexId}/records` + this.buildQuery({ ids: Array.isArray(ids) ? ids.join(',') : ids })); }
  indexRecordsExist(indexId, request) { return this.request('POST', `/v1.0/indices/${indexId}/records/exists`, request); }
  updateIndexRecordLabels(indexId, recordId, labels) { return this.request('PUT', `/v1.0/indices/${indexId}/records/${recordId}/labels`, labels); }
  updateIndexRecordTags(indexId, recordId, tags) { return this.request('PUT', `/v1.0/indices/${indexId}/records/${recordId}/tags`, tags); }
  updateIndexRecordCustomMetadata(indexId, recordId, metadata) { return this.request('PUT', `/v1.0/indices/${indexId}/records/${recordId}/custom-metadata`, metadata); }

  // Assistants
  createAssistant(asst) { return this.request('PUT', '/v1.0/assistants', asst); }
  getAssistants(params) { return this.request('GET', '/v1.0/assistants' + this.buildQuery(params)); }
  getAssistant(id) { return this.request('GET', `/v1.0/assistants/${id}`); }
  updateAssistant(id, asst) { return this.request('PUT', `/v1.0/assistants/${id}`, asst); }
  deleteAssistant(id) { return this.request('DELETE', `/v1.0/assistants/${id}`); }
  getAssistantPublic(serverUrl, id) { return fetch(`${serverUrl}/v1.0/assistants/${id}/public`).then(r => r.json()); }

  // Assistant Settings
  getAssistantSettings(assistantId) { return this.request('GET', `/v1.0/assistants/${assistantId}/settings`); }
  getAssistantTools(assistantId) { return this.request('GET', `/v1.0/assistants/${assistantId}/tools`); }
  validateAssistantToolPolicy(assistantId, request) { return this.request('POST', `/v1.0/assistants/${assistantId}/settings/tools/validate`, request); }
  testAssistantToolPolicy(assistantId, request) { return this.request('POST', `/v1.0/assistants/${assistantId}/settings/tools/test`, request); }
  updateAssistantSettings(assistantId, settings) { return this.request('PUT', `/v1.0/assistants/${assistantId}/settings`, settings); }
  verifyAssistantSlackSettings(assistantId, settings) { return this.request('POST', `/v1.0/assistants/${assistantId}/settings/slack/verify`, settings); }

  // Assistant Analytics
  getAssistantAnalyticsOverview(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/overview` + this.buildQuery(params)); }
  getAssistantAnalyticsTimeSeries(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/timeseries` + this.buildQuery(params)); }
  getAssistantAnalyticsStages(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/stages` + this.buildQuery(params)); }
  getAssistantAnalyticsEndpoints(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/endpoints` + this.buildQuery(params)); }
  getAssistantAnalyticsSlowest(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/slowest` + this.buildQuery(params)); }
  getAssistantAnalyticsFeedback(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/analytics/feedback` + this.buildQuery(params)); }

  // Assistant Tool Calls
  getAssistantToolCalls(assistantId, params) { return this.request('GET', `/v1.0/assistants/${assistantId}/tool-calls` + this.buildQuery(params)); }
  getAssistantToolCall(assistantId, toolCallRecordId) { return this.request('GET', `/v1.0/assistants/${assistantId}/tool-calls/${toolCallRecordId}`); }
  deleteAssistantToolCalls(assistantId, params) { return this.request('DELETE', `/v1.0/assistants/${assistantId}/tool-calls` + this.buildQuery(params)); }
  deleteAssistantToolCall(assistantId, toolCallRecordId) { return this.request('DELETE', `/v1.0/assistants/${assistantId}/tool-calls/${toolCallRecordId}`); }

  // Embedding Endpoints
  createEmbeddingEndpoint(endpoint) { return this.request('PUT', '/v1.0/endpoints/embedding', endpoint); }
  enumerateEmbeddingEndpoints(params) { return this.request('POST', '/v1.0/endpoints/embedding/enumerate', params || {}); }
  getEmbeddingEndpoint(id) { return this.request('GET', `/v1.0/endpoints/embedding/${id}`); }
  updateEmbeddingEndpoint(id, endpoint) { return this.request('PUT', `/v1.0/endpoints/embedding/${id}`, endpoint); }
  deleteEmbeddingEndpoint(id) { return this.request('DELETE', `/v1.0/endpoints/embedding/${id}`); }

  // Embedding Endpoint Health
  getAllEmbeddingEndpointHealth() { return this.request('GET', '/v1.0/endpoints/embedding/health'); }
  getEmbeddingEndpointHealth(id) { return this.request('GET', `/v1.0/endpoints/embedding/${id}/health`); }
  testEmbeddingEndpoint(id, body) { return this.request('POST', `/v1.0/endpoints/embedding/${id}/test`, body || {}); }
  async loadEmbeddingEndpointModel(id, body) {
    const response = await this.requestRaw({
      method: 'POST',
      path: `/v1.0/endpoints/embedding/${id}/load`,
      body: body || {}
    });
    return {
      ...(response.json || {}),
      HttpStatusCode: response.statusCode,
      ResponseHeaders: response.headers,
      RawResponseText: response.text,
      RequestElapsedMs: response.elapsedMs,
      Success: response.json?.Success ?? response.ok,
      StatusCode: response.json?.StatusCode ?? response.statusCode,
      ErrorMessage: response.errorMessage
    };
  }

  // Completion Endpoints
  createCompletionEndpoint(endpoint) { return this.request('PUT', '/v1.0/endpoints/completion', endpoint); }
  enumerateCompletionEndpoints(params) { return this.request('POST', '/v1.0/endpoints/completion/enumerate', params || {}); }
  getCompletionEndpoint(id) { return this.request('GET', `/v1.0/endpoints/completion/${id}`); }
  updateCompletionEndpoint(id, endpoint) { return this.request('PUT', `/v1.0/endpoints/completion/${id}`, endpoint); }
  deleteCompletionEndpoint(id) { return this.request('DELETE', `/v1.0/endpoints/completion/${id}`); }

  // Completion Endpoint Health
  getAllCompletionEndpointHealth() { return this.request('GET', '/v1.0/endpoints/completion/health'); }
  getCompletionEndpointHealth(id) { return this.request('GET', `/v1.0/endpoints/completion/${id}/health`); }
  testCompletionEndpoint(id, body) { return this.request('POST', `/v1.0/endpoints/completion/${id}/test`, body || {}); }
  async loadCompletionEndpointModel(id, body) {
    const response = await this.requestRaw({
      method: 'POST',
      path: `/v1.0/endpoints/completion/${id}/load`,
      body: body || {}
    });
    return {
      ...(response.json || {}),
      HttpStatusCode: response.statusCode,
      ResponseHeaders: response.headers,
      RawResponseText: response.text,
      RequestElapsedMs: response.elapsedMs,
      Success: response.json?.Success ?? response.ok,
      StatusCode: response.json?.StatusCode ?? response.statusCode,
      ErrorMessage: response.errorMessage
    };
  }

  // Ingestion Rules
  createIngestionRule(rule) { return this.request('PUT', '/v1.0/ingestion-rules', rule); }
  getIngestionRules(params) { return this.request('GET', '/v1.0/ingestion-rules' + this.buildQuery(params)); }
  getIngestionRule(id) { return this.request('GET', `/v1.0/ingestion-rules/${id}`); }
  updateIngestionRule(id, rule) { return this.request('PUT', `/v1.0/ingestion-rules/${id}`, rule); }
  deleteIngestionRule(id) { return this.request('DELETE', `/v1.0/ingestion-rules/${id}`); }

  // Documents
  uploadDocument(doc) { return this.request('PUT', '/v1.0/documents', doc); }
  getDocuments(params) { return this.request('GET', '/v1.0/documents' + this.buildQuery(params)); }
  getDocument(id) { return this.request('GET', `/v1.0/documents/${id}`); }
  getDocumentProcessingLog(id) { return this.request('GET', `/v1.0/documents/${id}/processing-log`); }
  reindexDocument(id) { return this.request('POST', `/v1.0/documents/${id}/reindex`, {}); }
  deleteDocument(id) { return this.request('DELETE', `/v1.0/documents/${id}`); }
  deleteDocuments(ids) { return this.request('POST', '/v1.0/documents/delete', { DocumentIds: ids }); }

  // Feedback
  getFeedbackList(params) { return this.request('GET', '/v1.0/feedback' + this.buildQuery(params)); }
  getFeedback(id) { return this.request('GET', `/v1.0/feedback/${id}`); }
  deleteFeedback(id) { return this.request('DELETE', `/v1.0/feedback/${id}`); }

  // History
  getHistoryList(params) { return this.request('GET', '/v1.0/history' + this.buildQuery(params)); }
  getHistory(id) { return this.request('GET', `/v1.0/history/${id}`); }
  deleteHistory(id) { return this.request('DELETE', `/v1.0/history/${id}`); }
  getThreads(params) { return this.request('GET', '/v1.0/threads' + this.buildQuery(params)); }

  // Request History
  getRequestHistory(params) { return this.request('GET', '/v1.0/requesthistory' + this.buildQuery(params)); }
  getRequestHistorySummary(params) { return this.request('GET', '/v1.0/requesthistory/summary' + this.buildQuery(params)); }
  getRequestHistoryEntry(id) { return this.request('GET', `/v1.0/requesthistory/${id}`); }
  getRequestHistoryEntryDetail(id) { return this.request('GET', `/v1.0/requesthistory/${id}/detail`); }
  deleteRequestHistoryEntry(id) { return this.request('DELETE', `/v1.0/requesthistory/${id}`); }
  deleteRequestHistoryBulk(params) { return this.request('DELETE', '/v1.0/requesthistory/bulk' + this.buildQuery(params)); }

  // OpenAPI
  async getOpenApiSpec() {
    const directDockerOpenApiUrl = this.buildDockerDirectOpenApiUrl();
    const candidates = [
      { path: '/v1.0/openapi.json', includeAuth: true, label: '/v1.0/openapi.json' },
      { path: '/openapi.json', includeAuth: false, label: '/openapi.json' },
      ...(directDockerOpenApiUrl ? [{ path: directDockerOpenApiUrl, includeAuth: false, label: directDockerOpenApiUrl }] : []),
    ];
    const failures = [];

    for (const candidate of candidates) {
      const response = await this.requestRaw({
        method: 'GET',
        path: candidate.path,
        includeAuth: candidate.includeAuth,
      });

      if (!response.ok) {
        failures.push(`${candidate.label}: ${response.errorMessage || `HTTP ${response.statusCode}`}`);
        continue;
      }

      if (response.json && typeof response.json === 'object') {
        return response.json;
      }

      const trimmed = (response.text || '').trim();
      if (trimmed.startsWith('<!DOCTYPE') || trimmed.startsWith('<html')) {
        failures.push(`${candidate.label}: returned HTML instead of JSON`);
        continue;
      }

      failures.push(`${candidate.label}: returned an unexpected non-JSON response`);
    }

    throw new Error(`Failed to load OpenAPI spec. Tried ${failures.join('; ')}`);
  }

  // Models
  getModels(assistantId) {
    const query = assistantId ? `?assistantId=${encodeURIComponent(assistantId)}` : '';
    return this.request('GET', '/v1.0/models' + query);
  }
  async pullModel(name, assistantId) {
    const headers = { 'Content-Type': 'application/json' };
    if (this.bearerToken) headers['Authorization'] = `Bearer ${this.bearerToken}`;
    const query = assistantId ? `?assistantId=${encodeURIComponent(assistantId)}` : '';
    const response = await fetch(`${this.serverUrl}/v1.0/models/pull${query}`, {
      method: 'POST', headers, body: JSON.stringify({ Name: name })
    });
    return { statusCode: response.status, ok: response.ok };
  }
  getPullStatus() { return this.request('GET', '/v1.0/models/pull/status'); }
  deleteModel(name, assistantId) {
    const query = assistantId ? `?assistantId=${encodeURIComponent(assistantId)}` : '';
    return this.request('DELETE', `/v1.0/models/${encodeURIComponent(name)}${query}`);
  }

  // Configuration
  getConfiguration() { return this.request('GET', '/v1.0/configuration'); }
  getExternalSearchStatus() { return this.request('GET', '/v1.0/configuration/external-search/status'); }
  updateConfiguration(settings) { return this.request('PUT', '/v1.0/configuration', settings); }

  // Thread creation (unauthenticated)
  static async createThread(serverUrl, assistantId) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/threads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    return response.json();
  }

  // Chat open hook (unauthenticated)
  static async openChat(serverUrl, assistantId) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/chat/open`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    if (!response.ok) throw new Error(`Chat open failed with status ${response.status}`);
    return response.json();
  }

  // Thread history (unauthenticated)
  static async getThreadHistory(serverUrl, assistantId, threadId) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/threads/${threadId}/history`);
    if (!response.ok) return [];
    return response.json();
  }

  // Public assistant document selection list (unauthenticated)
  static async getAssistantDocuments(serverUrl, assistantId, options = {}) {
    const params = new URLSearchParams();
    if (options.maxResults) params.set('maxResults', String(options.maxResults));
      if (options.continuationToken) params.set('continuationToken', options.continuationToken);
      if (options.ordering) params.set('ordering', options.ordering);
      if (options.query) params.set('query', options.query);
      if (options.contentType) params.set('contentType', options.contentType);
      const query = params.toString();
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/documents${query ? '?' + query : ''}`);
    if (!response.ok) {
      let message = `Document listing failed with status ${response.status}`;
      try {
        const body = await response.json();
        message = body.Description || body.Message || message;
      } catch {}
      throw new Error(message);
    }
    return response.json();
  }

  // Chat (unauthenticated) - handles both JSON and SSE streaming responses
  static async chat(serverUrl, assistantId, messages, onDelta, threadId, signal, metadataFilter = null, attachedDocumentIds = null) {
    const headers = { 'Content-Type': 'application/json' };
    if (threadId) headers['X-Thread-ID'] = threadId;

    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/chat`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        messages,
        ...(metadataFilter ? { metadata_filter: metadataFilter } : {}),
        ...(attachedDocumentIds && attachedDocumentIds.length > 0 ? { attached_document_ids: attachedDocumentIds } : {})
      }),
      signal
    });

    const contentType = response.headers.get('content-type') || '';

    // Non-streaming: return parsed JSON as before
    if (!contentType.includes('text/event-stream')) {
      return response.json();
    }

    // Streaming: read SSE events and accumulate content
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let fullContent = '';
    let buffer = '';
    let status = null;
    let usage = null;
    let citations = null;
    let sawToolEvent = false;
    const toolEvents = [];

    const formatToolStatus = (eventType, payload, events) => {
      const name = payload?.display_label || payload?.tool_name || 'tool';
      if (eventType.endsWith('tool_iteration.started')) {
        return payload?.summary || 'Checking whether tools are needed.';
      }
      if (eventType.endsWith('.started')) {
        const count = (events || []).filter(evt =>
          (evt.event_type || '').endsWith('.started') &&
          ((evt.display_label || evt.tool_name || 'tool') === name)
        ).length;
        if (count > 1) {
          const noun = name.toLowerCase().includes('search') ? 'searches' : 'calls';
          return `Running tool: ${name} (${count} ${noun})`;
        }
        return `Running tool: ${name}`;
      }
      if (eventType.endsWith('.completed')) return `${name} completed`;
      if (eventType.endsWith('.failed')) return 'One tool failed; trying another source';
      if (eventType.endsWith('.denied')) return `${name} denied`;
      if (eventType.endsWith('.heartbeat')) return `Running tool: ${name} still running`;
      return payload?.summary || `Running tool: ${name}`;
    };

    const flushEvent = (rawEvent) => {
      if (!rawEvent) return;

      const normalized = rawEvent.replace(/\r\n/g, '\n');
      const lines = normalized.split('\n');
      let eventType = 'message';
      const dataLines = [];

      for (const line of lines) {
        if (line.startsWith('event:')) eventType = line.substring(6).trim() || 'message';
        if (line.startsWith('data:')) dataLines.push(line.substring(5).trimStart());
      }

      if (dataLines.length < 1) return;
      const data = dataLines.join('\n');
      if (data === '[DONE]') return;

      let chunk = null;
      try {
        chunk = JSON.parse(data);
      } catch (e) {
        return;
      }

      const effectiveEventType = chunk.event_type || chunk.type || eventType;
      if (typeof effectiveEventType === 'string' && effectiveEventType.startsWith('assistant.tool_')) {
        const toolEvent = { ...chunk, event_type: effectiveEventType };
        sawToolEvent = true;
        toolEvents.push(toolEvent);
        status = formatToolStatus(effectiveEventType, toolEvent, toolEvents);
        if (onDelta) onDelta({ toolEvent, status });
        return;
      }

      // Capture usage data when present
      if (chunk.usage) {
        usage = chunk.usage;
      }

      // Capture citations from finish chunk
      if (chunk.citations) {
        citations = chunk.citations;
      }

      // Surface status messages (e.g. "Compacting the conversation...")
      if (chunk.status) {
        status = chunk.status;
        if (onDelta) onDelta({ status: chunk.status });
      }

      const delta = chunk.choices?.[0]?.delta;
      if (delta?.content) {
        fullContent += delta.content;
        if (onDelta) onDelta({ content: delta.content });
      }
    };

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n');
        let separatorIndex = buffer.indexOf('\n\n');
        while (separatorIndex >= 0) {
          const rawEvent = buffer.substring(0, separatorIndex);
          buffer = buffer.substring(separatorIndex + 2);
          flushEvent(rawEvent);
          separatorIndex = buffer.indexOf('\n\n');
        }
      }
    } catch (err) {
      if (sawToolEvent && err?.name !== 'AbortError') {
        const interruptedEvent = {
          event_type: 'assistant.tool_call.interrupted',
          status_code: 'tool_stream_interrupted',
          display_label: 'Tool status',
          summary: 'Tool progress stream interrupted.'
        };
        toolEvents.push(interruptedEvent);
        status = 'Tool status interrupted';
        if (onDelta) onDelta({ toolEvent: interruptedEvent, status });
        err.toolStreamInterrupted = true;
      }
      throw err;
    }

    // Process any remaining data left in the buffer after stream ends
    if (buffer.trim()) {
      flushEvent(buffer.trim());
    }

    // Return in the same shape as a non-streaming response
    return {
      choices: [{
        index: 0,
        message: { role: 'assistant', content: fullContent },
        finish_reason: 'stop'
      }],
      usage,
      citations,
      status,
      tool_events: toolEvents
    };
  }

  // Generate (unauthenticated) - lightweight inference-only, no RAG/compaction/history
  static async generate(serverUrl, assistantId, messages) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ messages })
    });
    return response.json();
  }

  // Compact (unauthenticated) - force conversation compaction
  static async compact(serverUrl, assistantId, messages, threadId) {
    const headers = { 'Content-Type': 'application/json' };
    if (threadId) headers['X-Thread-ID'] = threadId;

    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/compact`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ messages })
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      throw new Error(err.Message || err.message || `Compact failed with status ${response.status}`);
    }

    return response.json();
  }

  // Distinct labels/tags (unauthenticated)
  static async getDistinctLabels(serverUrl, assistantId) {
    try {
      const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/labels/distinct`);
      if (!response.ok) return [];
      return response.json();
    } catch { return []; }
  }

  static async getDistinctTags(serverUrl, assistantId) {
    try {
      const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/tags/distinct`);
      if (!response.ok) return [];
      return response.json();
    } catch { return []; }
  }

  // Feedback (unauthenticated)
  static submitFeedback(serverUrl, feedback) {
    return fetch(`${serverUrl}/v1.0/assistants/${feedback.AssistantId}/feedback`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(feedback)
    }).then(r => r.json());
  }

  // Evaluation
  createEvalFact(fact) { return this.request('PUT', '/v1.0/eval/facts', fact); }
  getEvalFacts(params) { return this.request('GET', '/v1.0/eval/facts' + this.buildQuery(params)); }
  getEvalFact(id) { return this.request('GET', `/v1.0/eval/facts/${id}`); }
  updateEvalFact(id, fact) { return this.request('PUT', `/v1.0/eval/facts/${id}`, fact); }
  deleteEvalFact(id) { return this.request('DELETE', `/v1.0/eval/facts/${id}`); }
  startEvalRun(body) { return this.request('POST', '/v1.0/eval/runs', body); }
  getEvalRuns(params) { return this.request('GET', '/v1.0/eval/runs' + this.buildQuery(params)); }
  getEvalRun(id) { return this.request('GET', `/v1.0/eval/runs/${id}`); }
  deleteEvalRun(id) { return this.request('DELETE', `/v1.0/eval/runs/${id}`); }
  getEvalRunResults(runId) { return this.request('GET', `/v1.0/eval/runs/${runId}/results`); }
  getEvalResult(id) { return this.request('GET', `/v1.0/eval/results/${id}`); }
  getDefaultJudgePrompt() { return this.request('GET', '/v1.0/eval/judge-prompt/default'); }
  getEvalRunStreamUrl(runId) {
    return `${this.serverUrl}/v1.0/eval/runs/${runId}/stream`;
  }

  // Crawl Plans
  getCrawlPlans(params) { return this.request('GET', '/v1.0/crawlplans' + this.buildQuery(params)); }
  getCrawlPlan(id) { return this.request('GET', `/v1.0/crawlplans/${id}`); }
  createCrawlPlan(plan) { return this.request('PUT', '/v1.0/crawlplans', plan); }
  updateCrawlPlan(id, plan) { return this.request('PUT', `/v1.0/crawlplans/${id}`, plan); }
  deleteCrawlPlan(id) { return this.request('DELETE', `/v1.0/crawlplans/${id}`); }
  startCrawl(id) { return this.request('POST', `/v1.0/crawlplans/${id}/start`); }
  stopCrawl(id) { return this.request('POST', `/v1.0/crawlplans/${id}/stop`); }
  testCrawlConnectivity(id) { return this.request('POST', `/v1.0/crawlplans/${id}/connectivity`); }
  testDraftCrawlConnectivity(plan) { return this.request('POST', '/v1.0/crawlplans/connectivity', plan); }
  enumerateCrawlContents(id, params) { return this.request('GET', `/v1.0/crawlplans/${id}/enumerate` + this.buildQuery(params)); }

  // Crawl Operations
  getCrawlOperations(planId, params) { return this.request('GET', `/v1.0/crawlplans/${planId}/operations` + this.buildQuery(params)); }
  getCrawlOperationStatistics(planId) { return this.request('GET', `/v1.0/crawlplans/${planId}/operations/statistics`); }
  getCrawlOperation(planId, id) { return this.request('GET', `/v1.0/crawlplans/${planId}/operations/${id}`); }
  getCrawlOperationStats(planId, id) { return this.request('GET', `/v1.0/crawlplans/${planId}/operations/${id}/statistics`); }
  deleteCrawlOperation(planId, id) { return this.request('DELETE', `/v1.0/crawlplans/${planId}/operations/${id}`); }
  getCrawlOperationEnumeration(planId, id) { return this.request('GET', `/v1.0/crawlplans/${planId}/operations/${id}/enumeration`); }

  buildQuery(params) {
    if (!params) return '';

    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value == null || value === '') return;

      if (Array.isArray(value)) {
        value.forEach((item) => {
          if (item != null && item !== '') search.append(key, item);
        });
        return;
      }

      search.set(key, typeof value === 'boolean' ? (value ? 'true' : 'false') : String(value));
    });

    const query = search.toString();
    return query ? `?${query}` : '';
  }
}
