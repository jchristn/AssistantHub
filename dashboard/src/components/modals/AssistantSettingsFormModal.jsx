import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../../context/AuthContext';
import { ApiClient } from '../../utils/api';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function AssistantSettingsFormModal({ settings, onSave, onClose }) {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [collections, setCollections] = useState([]);
  const [inferenceEndpoints, setInferenceEndpoints] = useState([]);
  const [embeddingEndpoints, setEmbeddingEndpoints] = useState([]);
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
    SearchMode: settings?.SearchMode || 'Vector',
    TextWeight: settings?.TextWeight ?? 0.3,
    FullTextSearchType: settings?.FullTextSearchType || 'TsRank',
    FullTextLanguage: settings?.FullTextLanguage || 'english',
    FullTextNormalization: settings?.FullTextNormalization ?? 32,
    FullTextMinimumScore: settings?.FullTextMinimumScore ?? '',
    InferenceEndpointId: settings?.InferenceEndpointId || '',
    EmbeddingEndpointId: settings?.EmbeddingEndpointId || '',
    Streaming: settings?.Streaming ?? true,
    EnableCitations: settings?.EnableCitations ?? false,
    CitationLinkMode: settings?.CitationLinkMode || 'None'
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

  const loadEndpoints = useCallback(async () => {
    try {
      const [completionResult, embeddingResult] = await Promise.all([
        api.enumerateCompletionEndpoints({ maxResults: 1000 }),
        api.enumerateEmbeddingEndpoints({ maxResults: 1000 })
      ]);
      const completionItems = (completionResult && completionResult.Objects) ? completionResult.Objects : Array.isArray(completionResult) ? completionResult : [];
      const embeddingItems = (embeddingResult && embeddingResult.Objects) ? embeddingResult.Objects : Array.isArray(embeddingResult) ? embeddingResult : [];
      setInferenceEndpoints(completionItems);
      setEmbeddingEndpoints(embeddingItems);
    } catch (err) {
      console.error('Failed to load endpoints:', err);
    }
  }, [serverUrl, credential]);

  useEffect(() => { loadCollections(); loadEndpoints(); }, [loadCollections, loadEndpoints]);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async () => {
    setSaving(true);
    try {
      const payload = {
        ...form,
        TextWeight: parseFloat(form.TextWeight) || 0.3,
        FullTextNormalization: parseInt(form.FullTextNormalization) || 32,
        FullTextMinimumScore: form.FullTextMinimumScore === '' || form.FullTextMinimumScore === null
          ? null
          : parseFloat(form.FullTextMinimumScore)
      };
      await onSave(payload);
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
        <label><Tooltip text="How documents are retrieved: Vector (semantic similarity), FullText (keyword matching), or Hybrid (both combined)">Search Mode</Tooltip></label>
        <select value={form.SearchMode} onChange={(e) => handleChange('SearchMode', e.target.value)}>
          <option value="Vector">Vector</option>
          <option value="FullText">FullText</option>
          <option value="Hybrid">Hybrid</option>
        </select>
      </div>
      {form.SearchMode === 'Hybrid' && (
        <div className="form-group">
          <label><Tooltip text="Balance between vector and text scoring in hybrid mode. 0.0 = pure vector, 1.0 = pure text. Recommended: 0.3 for quality embeddings">Text Weight</Tooltip> <span className="range-value">{form.TextWeight}</span></label>
          <input type="range" min="0" max="1" step="0.05" value={form.TextWeight} onChange={(e) => handleChange('TextWeight', parseFloat(e.target.value))} />
        </div>
      )}
      {(form.SearchMode === 'FullText' || form.SearchMode === 'Hybrid') && (
        <>
          <div className="form-row">
            <div className="form-group">
              <label><Tooltip text="TsRank: standard term frequency scoring. TsRankCd: cover density, rewards terms appearing close together">Full-Text Ranking</Tooltip></label>
              <select value={form.FullTextSearchType} onChange={(e) => handleChange('FullTextSearchType', e.target.value)}>
                <option value="TsRank">TsRank</option>
                <option value="TsRankCd">TsRankCd</option>
              </select>
            </div>
            <div className="form-group">
              <label><Tooltip text="Text search language for stemming and stop words. Use 'simple' to disable stemming">Language</Tooltip></label>
              <select value={form.FullTextLanguage} onChange={(e) => handleChange('FullTextLanguage', e.target.value)}>
                <option value="english">english</option>
                <option value="simple">simple</option>
                <option value="spanish">spanish</option>
                <option value="french">french</option>
                <option value="german">german</option>
              </select>
            </div>
          </div>
          <div className="form-group">
            <label><Tooltip text="Documents with text relevance below this threshold are excluded. Leave empty for no threshold">Minimum Text Score</Tooltip></label>
            <input type="number" min="0" max="1" step="0.05" value={form.FullTextMinimumScore} onChange={(e) => handleChange('FullTextMinimumScore', e.target.value)} placeholder="Optional (0.0-1.0)" />
          </div>
        </>
      )}
      {form.SearchMode !== 'FullText' && (
      <div className="form-group">
        <label><Tooltip text="Managed embedding endpoint used for RAG retrieval queries. Leave blank to use the server default">Embedding Endpoint</Tooltip></label>
        <select value={form.EmbeddingEndpointId} onChange={(e) => handleChange('EmbeddingEndpointId', e.target.value)}>
          <option value="">-- Use server default --</option>
          {(embeddingEndpoints || []).map(ep => (
            <option key={ep.Id} value={ep.Id}>{ep.Model || ep.Id}</option>
          ))}
        </select>
      </div>
      )}
      <div className="form-group">
        <label><Tooltip text="Managed completion endpoint used for inference. Leave blank to use the server default">Inference Endpoint</Tooltip></label>
        <select value={form.InferenceEndpointId} onChange={(e) => handleChange('InferenceEndpointId', e.target.value)}>
          <option value="">-- Use server default --</option>
          {(inferenceEndpoints || []).map(ep => (
            <option key={ep.Id} value={ep.Id}>{ep.Name || ep.Model || ep.Id}</option>
          ))}
        </select>
      </div>
      <div className="form-group form-toggle">
        <label>
          <input type="checkbox" checked={form.Streaming} onChange={(e) => handleChange('Streaming', e.target.checked)} />
          <Tooltip text="Enable streaming responses for real-time token-by-token output">Streaming</Tooltip>
        </label>
      </div>
      <div className="form-group form-toggle">
        <label>
          <input type="checkbox" checked={form.EnableCitations} onChange={(e) => handleChange('EnableCitations', e.target.checked)} />
          <Tooltip text="Include citation metadata linking response claims to source documents">Include Citations</Tooltip>
        </label>
      </div>
      {form.EnableCitations && (
        <div className="form-group">
          <label><Tooltip text="Controls document download linking in citation cards. None: display-only. Authenticated: requires bearer token. Public: presigned S3 URL.">Citation Link Mode</Tooltip></label>
          <select value={form.CitationLinkMode} onChange={(e) => handleChange('CitationLinkMode', e.target.value)}>
            <option value="None">None (display only)</option>
            <option value="Authenticated">Authenticated (bearer token required)</option>
            <option value="Public">Public (presigned S3 URL)</option>
          </select>
        </div>
      )}
    </Modal>
  );
}

export default AssistantSettingsFormModal;
