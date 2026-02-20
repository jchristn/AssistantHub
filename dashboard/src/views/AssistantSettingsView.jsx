import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Tooltip from '../components/Tooltip';
import AlertModal from '../components/AlertModal';

function AssistantSettingsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [assistants, setAssistants] = useState([]);
  const [collections, setCollections] = useState([]);
  const [inferenceEndpoints, setInferenceEndpoints] = useState([]);
  const [embeddingEndpoints, setEmbeddingEndpoints] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [settings, setSettings] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [alert, setAlert] = useState(null);
  const [dirty, setDirty] = useState(false);

  const loadCollections = useCallback(async () => {
    try {
      const result = await api.getCollections({ maxResults: 1000 });
      const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
      setCollections(items);
    } catch (err) {
      console.error('Failed to load collections:', err);
    }
  }, [serverUrl, credential]);

  const loadAssistants = useCallback(async () => {
    try {
      const result = await api.getAssistants({ maxResults: 1000 });
      const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
      setAssistants(items);
      if (items.length === 1) {
        setSelectedId(items[0].Id);
        loadSettings(items[0].Id);
      }
    } catch (err) {
      console.error('Failed to load assistants:', err);
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

  useEffect(() => { loadAssistants(); loadCollections(); loadEndpoints(); }, [loadAssistants, loadCollections, loadEndpoints]);

  const loadSettings = useCallback(async (id) => {
    if (!id) { setSettings(null); return; }
    setLoading(true);
    try {
      const result = await api.getAssistantSettings(id);
      setSettings({
        Temperature: result?.Temperature ?? 0.7,
        TopP: result?.TopP ?? 1.0,
        SystemPrompt: result?.SystemPrompt || 'You are a helpful assistant. Use the provided context to answer questions accurately.',
        MaxTokens: result?.MaxTokens || 4096,
        ContextWindow: result?.ContextWindow || 8192,
        Model: result?.Model || 'gemma3:4b',
        EnableRag: result?.EnableRag ?? false,
        CollectionId: result?.CollectionId || '',
        RetrievalTopK: result?.RetrievalTopK || 5,
        RetrievalScoreThreshold: result?.RetrievalScoreThreshold ?? 0.7,
        InferenceEndpointId: result?.InferenceEndpointId || '',
        EmbeddingEndpointId: result?.EmbeddingEndpointId || '',
        Title: result?.Title || '',
        LogoUrl: result?.LogoUrl || '',
        FaviconUrl: result?.FaviconUrl || '',
        Streaming: result?.Streaming ?? true,
      });
      setDirty(false);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load settings' });
      setSettings(null);
    } finally {
      setLoading(false);
    }
  }, [serverUrl, credential]);

  const handleSelectAssistant = (e) => {
    const id = e.target.value;
    setSelectedId(id);
    loadSettings(id);
  };

  const handleChange = (field, value) => {
    setSettings(prev => ({ ...prev, [field]: value }));
    setDirty(true);
  };

  const handleSave = async () => {
    if (!selectedId || !settings) return;
    setSaving(true);
    try {
      await api.updateAssistantSettings(selectedId, settings);
      setDirty(false);
      setAlert({ title: 'Success', message: 'Settings saved successfully.' });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save settings' });
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    loadSettings(selectedId);
  };

  const selectedAssistant = assistants.find(a => a.Id === selectedId);

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Assistant Settings</h1>
          <p className="content-subtitle">Configure model, retrieval, and inference settings for each assistant.</p>
        </div>
      </div>
      <div className="settings-view">
        <div className="form-group">
          <label className="form-label"><Tooltip text="Choose which assistant's settings to configure">Select Assistant</Tooltip></label>
          <select
            className="form-input"
            value={selectedId}
            onChange={handleSelectAssistant}
          >
            <option value="">-- Select an assistant --</option>
            {assistants.map(a => (
              <option key={a.Id} value={a.Id}>{a.Name} ({a.Id.substring(0, 8)}...)</option>
            ))}
          </select>
        </div>

        {loading && (
          <div className="loading"><div className="spinner" /></div>
        )}

        {settings && !loading && (
          <div className="settings-form">
            {selectedAssistant && (
              <div className="settings-assistant-info">
                Editing settings for <strong>{selectedAssistant.Name}</strong>
              </div>
            )}
            <div className="settings-section">
              <h3 className="settings-section-title">Appearance</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Heading displayed at the top of the chat window">Title</Tooltip></label>
                <input className="form-input" type="text" value={settings.Title} onChange={(e) => handleChange('Title', e.target.value)} placeholder="Heading shown on the chat window" />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="URL of the image shown in the chat header (max 192x192)">Logo URL</Tooltip></label>
                  <input className="form-input" type="text" value={settings.LogoUrl} onChange={(e) => handleChange('LogoUrl', e.target.value)} placeholder="Image URL for chat logo (max 192x192)" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="URL of the icon shown in the browser tab">Favicon URL</Tooltip></label>
                  <input className="form-input" type="text" value={settings.FaviconUrl} onChange={(e) => handleChange('FaviconUrl', e.target.value)} placeholder="Image URL for browser tab favicon" />
                </div>
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Model Configuration</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Model name to use for generating responses">Model</Tooltip></label>
                <input className="form-input" type="text" value={settings.Model} onChange={(e) => handleChange('Model', e.target.value)} />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Controls randomness: lower values are more focused, higher values are more creative (0-2)">Temperature</Tooltip> <span className="range-value">{settings.Temperature}</span></label>
                  <input type="range" min="0" max="2" step="0.1" value={settings.Temperature} onChange={(e) => handleChange('Temperature', parseFloat(e.target.value))} />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Nucleus sampling: limits token selection to a cumulative probability threshold (0-1)">Top P</Tooltip> <span className="range-value">{settings.TopP}</span></label>
                  <input type="range" min="0" max="1" step="0.05" value={settings.TopP} onChange={(e) => handleChange('TopP', parseFloat(e.target.value))} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Maximum number of tokens the model can generate per response">Max Tokens</Tooltip></label>
                  <input className="form-input" type="number" value={settings.MaxTokens} onChange={(e) => handleChange('MaxTokens', parseInt(e.target.value) || 0)} min="1" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Maximum number of tokens available for input context and conversation history">Context Window</Tooltip></label>
                  <input className="form-input" type="number" value={settings.ContextWindow} onChange={(e) => handleChange('ContextWindow', parseInt(e.target.value) || 0)} min="1" />
                </div>
              </div>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.Streaming} onChange={(e) => handleChange('Streaming', e.target.checked)} />
                  <Tooltip text="Enable real-time token-by-token response streaming">Streaming</Tooltip>
                </label>
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">System Prompt</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Instructions that define the assistant's behavior and personality">System Prompt</Tooltip></label>
                <textarea className="form-input" value={settings.SystemPrompt} onChange={(e) => handleChange('SystemPrompt', e.target.value)} rows={6} />
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Retrieval (RAG)</h3>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.EnableRag} onChange={(e) => handleChange('EnableRag', e.target.checked)} />
                  <Tooltip text="Enable Retrieval-Augmented Generation to use documents as context">Enable RAG</Tooltip>
                </label>
              </div>
              {settings.EnableRag && (
                <>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Vector collection to search for relevant document chunks">Collection ID</Tooltip></label>
                    <select className="form-input" value={settings.CollectionId} onChange={(e) => handleChange('CollectionId', e.target.value)}>
                      <option value="">-- Select a collection --</option>
                      {collections.map(c => (
                        <option key={c.Id} value={c.Id}>{c.Name || c.Id}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Number of most relevant document chunks to retrieve per query">Retrieval Top K</Tooltip></label>
                      <input className="form-input" type="number" value={settings.RetrievalTopK} onChange={(e) => handleChange('RetrievalTopK', parseInt(e.target.value) || 1)} min="1" />
                    </div>
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Minimum similarity score for retrieved chunks to be included (0-1)">Score Threshold</Tooltip> <span className="range-value">{settings.RetrievalScoreThreshold}</span></label>
                      <input type="range" min="0" max="1" step="0.05" value={settings.RetrievalScoreThreshold} onChange={(e) => handleChange('RetrievalScoreThreshold', parseFloat(e.target.value))} />
                    </div>
                  </div>
                </>
              )}
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Endpoints</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Managed completion endpoint used for inference. Leave blank to use the server default">Inference Endpoint</Tooltip></label>
                <select className="form-input" value={settings.InferenceEndpointId} onChange={(e) => handleChange('InferenceEndpointId', e.target.value)}>
                  <option value="">-- Use server default --</option>
                  {(inferenceEndpoints || []).map(ep => (
                    <option key={ep.Id} value={ep.Id}>{ep.Name || ep.Model || ep.Id}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Managed embedding endpoint used for RAG retrieval queries. Leave blank to use the server default">Embedding Endpoint</Tooltip></label>
                <select className="form-input" value={settings.EmbeddingEndpointId} onChange={(e) => handleChange('EmbeddingEndpointId', e.target.value)}>
                  <option value="">-- Use server default --</option>
                  {(embeddingEndpoints || []).map(ep => (
                    <option key={ep.Id} value={ep.Id}>{ep.Model || ep.Id}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="settings-actions">
              <button className="btn btn-secondary" onClick={handleReset} disabled={!dirty || saving}>Reset</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={!dirty || saving}>
                {saving ? 'Saving...' : 'Save Settings'}
              </button>
            </div>
          </div>
        )}

        {!settings && !loading && selectedId && (
          <div className="empty-state"><p>No settings found for this assistant.</p></div>
        )}
      </div>
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default AssistantSettingsView;
