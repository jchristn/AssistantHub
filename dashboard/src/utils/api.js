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
      return { success: true, statusCode: response.status };
    }

    if (method === 'HEAD') {
      return { success: response.ok, statusCode: response.status };
    }

    const data = await response.json();
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

  // Documents
  uploadDocument(doc) { return this.request('PUT', '/v1.0/documents', doc); }
  getDocuments(params) { return this.request('GET', '/v1.0/documents' + this.buildQuery(params)); }
  getDocument(id) { return this.request('GET', `/v1.0/documents/${id}`); }
  deleteDocument(id) { return this.request('DELETE', `/v1.0/documents/${id}`); }

  // Feedback
  getFeedbackList(params) { return this.request('GET', '/v1.0/feedback' + this.buildQuery(params)); }
  getFeedback(id) { return this.request('GET', `/v1.0/feedback/${id}`); }
  deleteFeedback(id) { return this.request('DELETE', `/v1.0/feedback/${id}`); }

  // Models
  getModels() { return this.request('GET', '/v1.0/models'); }
  async pullModel(name) {
    const headers = { 'Content-Type': 'application/json' };
    if (this.bearerToken) headers['Authorization'] = `Bearer ${this.bearerToken}`;
    const response = await fetch(`${this.serverUrl}/v1.0/models/pull`, {
      method: 'POST', headers, body: JSON.stringify({ Name: name })
    });
    return { statusCode: response.status, ok: response.ok };
  }
  getPullStatus() { return this.request('GET', '/v1.0/models/pull/status'); }
  deleteModel(name) { return this.request('DELETE', `/v1.0/models/${encodeURIComponent(name)}`); }

  // Configuration
  getConfiguration() { return this.request('GET', '/v1.0/configuration'); }
  updateConfiguration(settings) { return this.request('PUT', '/v1.0/configuration', settings); }

  // Chat (unauthenticated) - handles both JSON and SSE streaming responses
  static async chat(serverUrl, assistantId, messages, onDelta) {
    const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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

    // Return in the same shape as a non-streaming response
    return {
      choices: [{
        index: 0,
        message: { role: 'assistant', content: fullContent },
        finish_reason: 'stop'
      }]
    };
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
    return parts.length > 0 ? '?' + parts.join('&') : '';
  }
}
