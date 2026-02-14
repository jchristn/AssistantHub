import React, { useState } from 'react';
import Modal from '../Modal';

function AssistantSettingsFormModal({ settings, onSave, onClose }) {
  const [form, setForm] = useState({
    Temperature: settings?.Temperature ?? 0.7,
    TopP: settings?.TopP ?? 1.0,
    SystemPrompt: settings?.SystemPrompt || 'You are a helpful assistant. Use the provided context to answer questions accurately.',
    MaxTokens: settings?.MaxTokens || 4096,
    ContextWindow: settings?.ContextWindow || 8192,
    Model: settings?.Model || 'gpt-4o',
    CollectionId: settings?.CollectionId || '',
    RetrievalTopK: settings?.RetrievalTopK || 5,
    RetrievalScoreThreshold: settings?.RetrievalScoreThreshold ?? 0.7,
    InferenceProvider: settings?.InferenceProvider || 'OpenAI',
    InferenceEndpoint: settings?.InferenceEndpoint || '',
    InferenceApiKey: settings?.InferenceApiKey || '',
    Streaming: settings?.Streaming ?? false
  });
  const [saving, setSaving] = useState(false);

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
        <label>Model</label>
        <input type="text" value={form.Model} onChange={(e) => handleChange('Model', e.target.value)} />
      </div>
      <div className="form-row">
        <div className="form-group">
          <label>Temperature <span className="range-value">{form.Temperature}</span></label>
          <input type="range" min="0" max="2" step="0.1" value={form.Temperature} onChange={(e) => handleChange('Temperature', parseFloat(e.target.value))} />
        </div>
        <div className="form-group">
          <label>Top P <span className="range-value">{form.TopP}</span></label>
          <input type="range" min="0" max="1" step="0.05" value={form.TopP} onChange={(e) => handleChange('TopP', parseFloat(e.target.value))} />
        </div>
      </div>
      <div className="form-group">
        <label>System Prompt</label>
        <textarea value={form.SystemPrompt} onChange={(e) => handleChange('SystemPrompt', e.target.value)} rows={4} />
      </div>
      <div className="form-row">
        <div className="form-group">
          <label>Max Tokens</label>
          <input type="number" value={form.MaxTokens} onChange={(e) => handleChange('MaxTokens', parseInt(e.target.value) || 0)} min="1" />
        </div>
        <div className="form-group">
          <label>Context Window</label>
          <input type="number" value={form.ContextWindow} onChange={(e) => handleChange('ContextWindow', parseInt(e.target.value) || 0)} min="1" />
        </div>
      </div>
      <div className="form-group">
        <label>Collection ID (RecallDB)</label>
        <input type="text" value={form.CollectionId} onChange={(e) => handleChange('CollectionId', e.target.value)} />
      </div>
      <div className="form-row">
        <div className="form-group">
          <label>Retrieval Top K</label>
          <input type="number" value={form.RetrievalTopK} onChange={(e) => handleChange('RetrievalTopK', parseInt(e.target.value) || 1)} min="1" />
        </div>
        <div className="form-group">
          <label>Score Threshold <span className="range-value">{form.RetrievalScoreThreshold}</span></label>
          <input type="range" min="0" max="1" step="0.05" value={form.RetrievalScoreThreshold} onChange={(e) => handleChange('RetrievalScoreThreshold', parseFloat(e.target.value))} />
        </div>
      </div>
      <div className="form-group">
        <label>Inference Provider</label>
        <select value={form.InferenceProvider} onChange={(e) => handleChange('InferenceProvider', e.target.value)}>
          <option value="OpenAI">OpenAI</option>
          <option value="Ollama">Ollama</option>
        </select>
      </div>
      <div className="form-group">
        <label>Inference Endpoint</label>
        <input type="text" value={form.InferenceEndpoint} onChange={(e) => handleChange('InferenceEndpoint', e.target.value)} placeholder="Leave blank to use server default" />
      </div>
      <div className="form-group">
        <label>Inference API Key</label>
        <input type="password" value={form.InferenceApiKey} onChange={(e) => handleChange('InferenceApiKey', e.target.value)} placeholder="Leave blank to use server default" />
      </div>
      <div className="form-group form-toggle">
        <label>
          <input type="checkbox" checked={form.Streaming} onChange={(e) => handleChange('Streaming', e.target.checked)} />
          Streaming
        </label>
      </div>
    </Modal>
  );
}

export default AssistantSettingsFormModal;
