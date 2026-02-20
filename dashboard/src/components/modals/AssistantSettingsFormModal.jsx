import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../../context/AuthContext';
import { ApiClient } from '../../utils/api';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function AssistantSettingsFormModal({ settings, onSave, onClose }) {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [collections, setCollections] = useState([]);
  const [form, setForm] = useState({
    Temperature: settings?.Temperature ?? 0.7,
    TopP: settings?.TopP ?? 1.0,
    SystemPrompt: settings?.SystemPrompt || 'You are a helpful assistant. Use the provided context to answer questions accurately.',
    MaxTokens: settings?.MaxTokens || 4096,
    ContextWindow: settings?.ContextWindow || 8192,
    Model: settings?.Model || 'gemma3:4b',
    CollectionId: settings?.CollectionId || '',
    RetrievalTopK: settings?.RetrievalTopK || 5,
    RetrievalScoreThreshold: settings?.RetrievalScoreThreshold ?? 0.7,
    InferenceProvider: settings?.InferenceProvider || 'Ollama',
    InferenceEndpoint: settings?.InferenceEndpoint || 'http://ollama:11434',
    InferenceApiKey: settings?.InferenceApiKey || '',
    Streaming: settings?.Streaming ?? true
  });
  const [saving, setSaving] = useState(false);

  const loadCollections = useCallback(async () => {
    try {
      const result = await api.getCollections({ maxResults: 1000 });
      const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
      setCollections(items);
    } catch (err) {
      console.error('Failed to load collections:', err);
    }
  }, [serverUrl, credential]);

  useEffect(() => { loadCollections(); }, [loadCollections]);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async () => {
    setSaving(true);
    try {
      await onSave(form);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title="Assistant Settings" onClose={onClose} wide footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <div className="form-group">
        <label><Tooltip text="Name of the language model to use for this assistant (e.g. gemma3:4b, gpt-4)">Model</Tooltip></label>
        <input type="text" value={form.Model} onChange={(e) => handleChange('Model', e.target.value)} />
      </div>
      <div className="form-row">
        <div className="form-group">
          <label><Tooltip text="Controls randomness in responses. Lower values (0.0) produce more deterministic output, higher values (2.0) increase creativity">Temperature</Tooltip> <span className="range-value">{form.Temperature}</span></label>
          <input type="range" min="0" max="2" step="0.1" value={form.Temperature} onChange={(e) => handleChange('Temperature', parseFloat(e.target.value))} />
        </div>
        <div className="form-group">
          <label><Tooltip text="Nucleus sampling threshold. Limits token selection to the smallest set whose cumulative probability exceeds this value">Top P</Tooltip> <span className="range-value">{form.TopP}</span></label>
          <input type="range" min="0" max="1" step="0.05" value={form.TopP} onChange={(e) => handleChange('TopP', parseFloat(e.target.value))} />
        </div>
      </div>
      <div className="form-group">
        <label><Tooltip text="Instructions given to the model that define its behavior, personality, and constraints">System Prompt</Tooltip></label>
        <textarea value={form.SystemPrompt} onChange={(e) => handleChange('SystemPrompt', e.target.value)} rows={4} />
      </div>
      <div className="form-row">
        <div className="form-group">
          <label><Tooltip text="Maximum number of tokens the model can generate in a single response">Max Tokens</Tooltip></label>
          <input type="number" value={form.MaxTokens} onChange={(e) => handleChange('MaxTokens', parseInt(e.target.value) || 0)} min="1" />
        </div>
        <div className="form-group">
          <label><Tooltip text="Total token capacity for the model's input context, including system prompt, conversation history, and retrieved documents">Context Window</Tooltip></label>
          <input type="number" value={form.ContextWindow} onChange={(e) => handleChange('ContextWindow', parseInt(e.target.value) || 0)} min="1" />
        </div>
      </div>
      <div className="form-group">
        <label><Tooltip text="Vector collection used for retrieving relevant documents during RAG (Retrieval-Augmented Generation)">Collection ID (RecallDB)</Tooltip></label>
        <select value={form.CollectionId} onChange={(e) => handleChange('CollectionId', e.target.value)}>
          <option value="">-- Select a collection --</option>
          {collections.map(c => (
            <option key={c.Id} value={c.Id}>{c.Name || c.Id}</option>
          ))}
        </select>
      </div>
      <div className="form-row">
        <div className="form-group">
          <label><Tooltip text="Maximum number of document chunks retrieved from the vector collection per query">Retrieval Top K</Tooltip></label>
          <input type="number" value={form.RetrievalTopK} onChange={(e) => handleChange('RetrievalTopK', parseInt(e.target.value) || 1)} min="1" />
        </div>
        <div className="form-group">
          <label><Tooltip text="Minimum similarity score (0.0-1.0) a document chunk must have to be included in the retrieval results">Score Threshold</Tooltip> <span className="range-value">{form.RetrievalScoreThreshold}</span></label>
          <input type="range" min="0" max="1" step="0.05" value={form.RetrievalScoreThreshold} onChange={(e) => handleChange('RetrievalScoreThreshold', parseFloat(e.target.value))} />
        </div>
      </div>
      <div className="form-group">
        <label><Tooltip text="API provider type for the inference backend">Inference Provider</Tooltip></label>
        <select value={form.InferenceProvider} onChange={(e) => handleChange('InferenceProvider', e.target.value)}>
          <option value="OpenAI">OpenAI</option>
          <option value="Ollama">Ollama</option>
        </select>
      </div>
      <div className="form-group">
        <label><Tooltip text="Base URL of the inference API server. Leave blank to use the server default">Inference Endpoint</Tooltip></label>
        <input type="text" value={form.InferenceEndpoint} onChange={(e) => handleChange('InferenceEndpoint', e.target.value)} placeholder="Leave blank to use server default" />
      </div>
      <div className="form-group">
        <label><Tooltip text="API key for authenticating with the inference endpoint. Leave blank to use the server default">Inference API Key</Tooltip></label>
        <input type="password" value={form.InferenceApiKey} onChange={(e) => handleChange('InferenceApiKey', e.target.value)} placeholder="Leave blank to use server default" />
      </div>
      <div className="form-group form-toggle">
        <label>
          <input type="checkbox" checked={form.Streaming} onChange={(e) => handleChange('Streaming', e.target.checked)} />
          <Tooltip text="Enable streaming responses for real-time token-by-token output">Streaming</Tooltip>
        </label>
      </div>
    </Modal>
  );
}

export default AssistantSettingsFormModal;
