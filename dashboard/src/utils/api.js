export class ApiClient {
  constructor(serverUrl, bearerToken) {
    this.serverUrl = serverUrl;
    this.bearerToken = bearerToken;
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
      const message = data?.Message || data?.message || data?.Detail || `Request failed with status ${response.status}`;
      throw new Error(message);
    }

    if (Array.isArray(data)) return data;
    return { ...data, statusCode: response.status };
  }

  // Users
  createUser(user) { return this.request('PUT', '/v1.0/users', user); }
  getUsers(params) { return this.request('GET', '/v1.0/users' + this.buildQuery(params)); }
  getUser(id) { return this.request('GET', `/v1.0/users/${id}`); }
  updateUser(id, user) { return this.request('PUT', `/v1.0/users/${id}`, user); }
  deleteUser(id) { return this.request('DELETE', `/v1.0/users/${id}`); }
  headUser(id) { return this.request('HEAD', `/v1.0/users/${id}`); }

  // Credentials
  createCredential(cred) { return this.request('PUT', '/v1.0/credentials', cred); }
  getCredentials(params) { return this.request('GET', '/v1.0/credentials' + this.buildQuery(params)); }
  getCredential(id) { return this.request('GET', `/v1.0/credentials/${id}`); }
  updateCredential(id, cred) { return this.request('PUT', `/v1.0/credentials/${id}`, cred); }
  deleteCredential(id) { return this.request('DELETE', `/v1.0/credentials/${id}`); }

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

  // Collection Records
  createRecord(collectionId, record) { return this.request('PUT', `/v1.0/collections/${collectionId}/records`, record); }
  getRecords(collectionId, params) { return this.request('GET', `/v1.0/collections/${collectionId}/records` + this.buildQuery(params)); }
  getRecord(collectionId, recordId) { return this.request('GET', `/v1.0/collections/${collectionId}/records/${recordId}`); }
  deleteRecord(collectionId, recordId) { return this.request('DELETE', `/v1.0/collections/${collectionId}/records/${recordId}`); }

  // Assistants
  createAssistant(asst) { return this.request('PUT', '/v1.0/assistants', asst); }
  getAssistants(params) { return this.request('GET', '/v1.0/assistants' + this.buildQuery(params)); }
  getAssistant(id) { return this.request('GET', `/v1.0/assistants/${id}`); }
  updateAssistant(id, asst) { return this.request('PUT', `/v1.0/assistants/${id}`, asst); }
  deleteAssistant(id) { return this.request('DELETE', `/v1.0/assistants/${id}`); }
  getAssistantPublic(serverUrl, id) { return fetch(`${serverUrl}/v1.0/assistants/${id}/public`).then(r => r.json()); }

  // Assistant Settings
  getAssistantSettings(assistantId) { return this.request('GET', `/v1.0/assistants/${assistantId}/settings`); }
  updateAssistantSettings(assistantId, settings) { return this.request('PUT', `/v1.0/assistants/${assistantId}/settings`, settings); }

  // Embedding Endpoints
  createEmbeddingEndpoint(endpoint) { return this.request('PUT', '/v1.0/endpoints/embedding', endpoint); }
  enumerateEmbeddingEndpoints(params) { return this.request('POST', '/v1.0/endpoints/embedding/enumerate', params || {}); }
  getEmbeddingEndpoint(id) { return this.request('GET', `/v1.0/endpoints/embedding/${id}`); }
  updateEmbeddingEndpoint(id, endpoint) { return this.request('PUT', `/v1.0/endpoints/embedding/${id}`, endpoint); }
  deleteEmbeddingEndpoint(id) { return this.request('DELETE', `/v1.0/endpoints/embedding/${id}`); }

  // Completion Endpoints
  createCompletionEndpoint(endpoint) { return this.request('PUT', '/v1.0/endpoints/completion', endpoint); }
  enumerateCompletionEndpoints(params) { return this.request('POST', '/v1.0/endpoints/completion/enumerate', params || {}); }
  getCompletionEndpoint(id) { return this.request('GET', `/v1.0/endpoints/completion/${id}`); }
  updateCompletionEndpoint(id, endpoint) { return this.request('PUT', `/v1.0/endpoints/completion/${id}`, endpoint); }
  deleteCompletionEndpoint(id) { return this.request('DELETE', `/v1.0/endpoints/completion/${id}`); }

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
  deleteDocument(id) { return this.request('DELETE', `/v1.0/documents/${id}`); }

  // Feedback
  getFeedbackList(params) { return this.request('GET', '/v1.0/feedback' + this.buildQuery(params)); }
  getFeedback(id) { return this.request('GET', `/v1.0/feedback/${id}`); }
  deleteFeedback(id) { return this.request('DELETE', `/v1.0/feedback/${id}`); }

  // History
  getHistoryList(params) { return this.request('GET', '/v1.0/history' + this.buildQuery(params)); }
  getHistory(id) { return this.request('GET', `/v1.0/history/${id}`); }
  deleteHistory(id) { return this.request('DELETE', `/v1.0/history/${id}`); }
  getThreads(params) { return this.request('GET', '/v1.0/threads' + this.buildQuery(params)); }

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
  updateConfiguration(settings) { return this.request('PUT', '/v1.0/configuration', settings); }

  // Thread creation (unauthenticated)
  static async createThread(serverUrl, assistantId) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/threads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    return response.json();
  }

  // Thread history (unauthenticated)
  static async getThreadHistory(serverUrl, assistantId, threadId) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/threads/${threadId}/history`);
    if (!response.ok) return [];
    return response.json();
  }

  // Chat (unauthenticated) - handles both JSON and SSE streaming responses
  static async chat(serverUrl, assistantId, messages, onDelta, threadId) {
    const headers = { 'Content-Type': 'application/json' };
    if (threadId) headers['X-Thread-ID'] = threadId;

    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/chat`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ messages })
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

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop(); // keep incomplete line in buffer

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue;
        const data = line.substring(6);
        if (data === '[DONE]') continue;

        try {
          const chunk = JSON.parse(data);

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
        } catch (e) {
          // skip unparseable lines
        }
      }
    }

    // Process any remaining data left in the buffer after stream ends
    if (buffer.trim()) {
      const remaining = buffer.trim();
      if (remaining.startsWith('data: ') && remaining.substring(6) !== '[DONE]') {
        try {
          const chunk = JSON.parse(remaining.substring(6));
          if (chunk.usage) usage = chunk.usage;
          if (chunk.citations) citations = chunk.citations;
        } catch (e) {
          // skip unparseable remainder
        }
      }
    }

    // Return in the same shape as a non-streaming response
    return {
      choices: [{
        index: 0,
        message: { role: 'assistant', content: fullContent },
        finish_reason: 'stop'
      }],
      usage,
      citations
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

  // Feedback (unauthenticated)
  static submitFeedback(serverUrl, feedback) {
    return fetch(`${serverUrl}/v1.0/assistants/${feedback.AssistantId}/feedback`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(feedback)
    }).then(r => r.json());
  }

  buildQuery(params) {
    if (!params) return '';
    const parts = [];
    if (params.maxResults) parts.push(`maxResults=${params.maxResults}`);
    if (params.continuationToken) parts.push(`continuationToken=${params.continuationToken}`);
    if (params.ordering) parts.push(`ordering=${params.ordering}`);
    if (params.assistantId) parts.push(`assistantId=${params.assistantId}`);
    if (params.bucketName) parts.push(`bucketName=${encodeURIComponent(params.bucketName)}`);
    if (params.collectionId) parts.push(`collectionId=${encodeURIComponent(params.collectionId)}`);
    if (params.threadId) parts.push(`threadId=${encodeURIComponent(params.threadId)}`);
    return parts.length > 0 ? '?' + parts.join('&') : '';
  }
}
