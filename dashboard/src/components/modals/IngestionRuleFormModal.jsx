import React, { useState } from 'react';
import Modal from '../Modal';

const STRATEGY_OPTIONS = [
  'FixedTokenCount',
  'SentenceBased',
  'ParagraphBased',
  'RegexBased',
  'WholeList',
  'ListEntry',
  'Row',
  'RowWithHeaders',
  'RowGroupWithHeaders',
  'KeyValuePairs',
  'WholeTable'
];

const OVERLAP_STRATEGY_OPTIONS = [
  '',
  'SlidingWindow',
  'SentenceBoundaryAware',
  'SemanticBoundaryAware'
];

const defaultChunking = {
  Strategy: 'FixedTokenCount',
  FixedTokenCount: 256,
  OverlapCount: 0,
  OverlapPercentage: '',
  OverlapStrategy: '',
  RowGroupSize: 5,
  ContextPrefix: '',
  RegexPattern: ''
};

const defaultEmbedding = {
  Model: '',
  L2Normalization: false
};

function IngestionRuleFormModal({ rule, buckets, collections, onSave, onClose }) {
  const isEdit = !!rule;

  const [form, setForm] = useState({
    Name: rule?.Name || '',
    Description: rule?.Description || '',
    Bucket: rule?.Bucket || '',
    CollectionId: rule?.CollectionId || '',
    CollectionName: rule?.CollectionName || '',
    Labels: rule?.Labels ? [...rule.Labels] : [],
    Tags: rule?.Tags ? { ...rule.Tags } : {},
    Chunking: rule?.Chunking ? { ...defaultChunking, ...rule.Chunking } : { ...defaultChunking },
    Embedding: rule?.Embedding ? { ...defaultEmbedding, ...rule.Embedding } : { ...defaultEmbedding }
  });

  const [saving, setSaving] = useState(false);
  const [labelInput, setLabelInput] = useState('');
  const [tagKeyInput, setTagKeyInput] = useState('');
  const [tagValueInput, setTagValueInput] = useState('');
  const [chunkingOpen, setChunkingOpen] = useState(false);
  const [embeddingOpen, setEmbeddingOpen] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleChunkingChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Chunking: { ...prev.Chunking, [field]: value }
    }));
  };

  const handleEmbeddingChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Embedding: { ...prev.Embedding, [field]: value }
    }));
  };

  const addLabel = () => {
    const trimmed = labelInput.trim();
    if (!trimmed) return;
    setForm(prev => ({
      ...prev,
      Labels: [...prev.Labels, trimmed]
    }));
    setLabelInput('');
  };

  const removeLabel = (index) => {
    setForm(prev => ({
      ...prev,
      Labels: prev.Labels.filter((_, i) => i !== index)
    }));
  };

  const addTag = () => {
    const key = tagKeyInput.trim();
    const value = tagValueInput.trim();
    if (!key) return;
    setForm(prev => ({
      ...prev,
      Tags: { ...prev.Tags, [key]: value }
    }));
    setTagKeyInput('');
    setTagValueInput('');
  };

  const removeTag = (key) => {
    setForm(prev => {
      const updated = { ...prev.Tags };
      delete updated[key];
      return { ...prev, Tags: updated };
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const data = {
        Name: form.Name,
        Description: form.Description,
        Bucket: form.Bucket,
        CollectionId: form.CollectionId,
        CollectionName: form.CollectionName,
        Labels: form.Labels,
        Tags: form.Tags,
        Chunking: {
          Strategy: form.Chunking.Strategy,
          FixedTokenCount: parseInt(form.Chunking.FixedTokenCount) || 256,
          OverlapCount: parseInt(form.Chunking.OverlapCount) || 0,
          OverlapPercentage: form.Chunking.OverlapPercentage !== '' ? parseFloat(form.Chunking.OverlapPercentage) : undefined,
          OverlapStrategy: form.Chunking.OverlapStrategy || undefined,
          RowGroupSize: parseInt(form.Chunking.RowGroupSize) || 5,
          ContextPrefix: form.Chunking.ContextPrefix || undefined,
          RegexPattern: form.Chunking.RegexPattern || undefined
        },
        Embedding: {
          Model: form.Embedding.Model || undefined,
          L2Normalization: form.Embedding.L2Normalization
        }
      };
      if (isEdit && rule.GUID) data.GUID = rule.GUID;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  const chipStyle = {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.25rem',
    padding: '0.25rem 0.5rem',
    background: 'var(--bg-tertiary)',
    borderRadius: 'var(--radius-sm)',
    fontSize: '0.8rem'
  };

  const collapsibleButtonStyle = {
    background: 'none',
    border: 'none',
    padding: 0,
    cursor: 'pointer',
    fontSize: '0.95rem',
    fontWeight: 600,
    color: 'var(--text-primary)'
  };

  return (
    <Modal
      title={isEdit ? 'Edit Ingestion Rule' : 'Create Ingestion Rule'}
      onClose={onClose}
      wide
      footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button
            className="btn btn-primary"
            onClick={handleSubmit}
            disabled={saving || !form.Name.trim() || !form.Bucket || !form.CollectionId || !form.CollectionName}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </>
      }
    >
      <form onSubmit={handleSubmit}>
        {/* Name */}
        <div className="form-group">
          <label>Name</label>
          <input
            type="text"
            value={form.Name}
            onChange={(e) => handleChange('Name', e.target.value)}
            required
          />
        </div>

        {/* Description */}
        <div className="form-group">
          <label>Description</label>
          <textarea
            value={form.Description}
            onChange={(e) => handleChange('Description', e.target.value)}
            rows={3}
          />
        </div>

        {/* Bucket */}
        <div className="form-group">
          <label>Bucket</label>
          <select
            value={form.Bucket}
            onChange={(e) => handleChange('Bucket', e.target.value)}
            required
          >
            <option value="">-- Select Bucket --</option>
            {buckets.map(b => (
              <option key={b.Name} value={b.Name}>{b.Name}</option>
            ))}
          </select>
        </div>

        {/* Collection */}
        <div className="form-group">
          <label>Collection</label>
          <select
            value={form.CollectionId}
            onChange={(e) => {
              const id = e.target.value;
              const col = collections.find(c => (c.GUID || c.Id) === id);
              setForm(prev => ({
                ...prev,
                CollectionId: id,
                CollectionName: col ? (col.Name || '') : ''
              }));
            }}
            required
          >
            <option value="">-- Select Collection --</option>
            {collections.map(c => (
              <option key={c.GUID || c.Id} value={c.GUID || c.Id}>{c.Name || c.GUID || c.Id}</option>
            ))}
          </select>
        </div>

        {/* Labels */}
        <div className="form-group">
          <label>Labels</label>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <input
              type="text"
              value={labelInput}
              onChange={(e) => setLabelInput(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addLabel(); } }}
              placeholder="Add a label"
            />
            <button type="button" className="btn btn-secondary" onClick={addLabel}>Add</button>
          </div>
          {form.Labels.length > 0 && (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.5rem' }}>
              {form.Labels.map((label, i) => (
                <span key={i} style={chipStyle}>
                  {label}
                  <button
                    type="button"
                    onClick={() => removeLabel(i)}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, lineHeight: 1, fontSize: '0.9rem', color: 'inherit' }}
                  >
                    &times;
                  </button>
                </span>
              ))}
            </div>
          )}
        </div>

        {/* Tags */}
        <div className="form-group">
          <label>Tags</label>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <input
              type="text"
              value={tagKeyInput}
              onChange={(e) => setTagKeyInput(e.target.value)}
              placeholder="Key"
            />
            <input
              type="text"
              value={tagValueInput}
              onChange={(e) => setTagValueInput(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTag(); } }}
              placeholder="Value"
            />
            <button type="button" className="btn btn-secondary" onClick={addTag}>Add</button>
          </div>
          {Object.keys(form.Tags).length > 0 && (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.5rem' }}>
              {Object.entries(form.Tags).map(([key, value]) => (
                <span key={key} style={chipStyle}>
                  {key}: {value}
                  <button
                    type="button"
                    onClick={() => removeTag(key)}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, lineHeight: 1, fontSize: '0.9rem', color: 'inherit' }}
                  >
                    &times;
                  </button>
                </span>
              ))}
            </div>
          )}
        </div>

        {/* Chunking (collapsible) */}
        <div className="form-group">
          <button
            type="button"
            style={collapsibleButtonStyle}
            onClick={() => setChunkingOpen(prev => !prev)}
          >
            {chunkingOpen ? '\u25BE' : '\u25B8'} Chunking
          </button>
          {chunkingOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label>Strategy</label>
                <select
                  value={form.Chunking.Strategy}
                  onChange={(e) => handleChunkingChange('Strategy', e.target.value)}
                >
                  {STRATEGY_OPTIONS.map(opt => (
                    <option key={opt} value={opt}>{opt}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Fixed Token Count</label>
                <input
                  type="number"
                  value={form.Chunking.FixedTokenCount}
                  onChange={(e) => handleChunkingChange('FixedTokenCount', e.target.value)}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label>Overlap Count</label>
                <input
                  type="number"
                  value={form.Chunking.OverlapCount}
                  onChange={(e) => handleChunkingChange('OverlapCount', e.target.value)}
                  min="0"
                />
              </div>

              <div className="form-group">
                <label>Overlap Percentage</label>
                <input
                  type="number"
                  value={form.Chunking.OverlapPercentage}
                  onChange={(e) => handleChunkingChange('OverlapPercentage', e.target.value)}
                  min="0"
                  max="1"
                  step="0.01"
                  placeholder="0.0 - 1.0"
                />
              </div>

              <div className="form-group">
                <label>Overlap Strategy</label>
                <select
                  value={form.Chunking.OverlapStrategy}
                  onChange={(e) => handleChunkingChange('OverlapStrategy', e.target.value)}
                >
                  {OVERLAP_STRATEGY_OPTIONS.map(opt => (
                    <option key={opt} value={opt}>{opt || '(none)'}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Row Group Size</label>
                <input
                  type="number"
                  value={form.Chunking.RowGroupSize}
                  onChange={(e) => handleChunkingChange('RowGroupSize', e.target.value)}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label>Context Prefix</label>
                <input
                  type="text"
                  value={form.Chunking.ContextPrefix}
                  onChange={(e) => handleChunkingChange('ContextPrefix', e.target.value)}
                  placeholder="Optional"
                />
              </div>

              <div className="form-group">
                <label>Regex Pattern</label>
                <input
                  type="text"
                  value={form.Chunking.RegexPattern}
                  onChange={(e) => handleChunkingChange('RegexPattern', e.target.value)}
                  placeholder="Optional"
                />
              </div>
            </div>
          )}
        </div>

        {/* Embedding (collapsible) */}
        <div className="form-group">
          <button
            type="button"
            style={collapsibleButtonStyle}
            onClick={() => setEmbeddingOpen(prev => !prev)}
          >
            {embeddingOpen ? '\u25BE' : '\u25B8'} Embedding
          </button>
          {embeddingOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label>Model</label>
                <input
                  type="text"
                  value={form.Embedding.Model}
                  onChange={(e) => handleEmbeddingChange('Model', e.target.value)}
                  placeholder="Optional"
                />
              </div>

              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={form.Embedding.L2Normalization}
                      onChange={(e) => handleEmbeddingChange('L2Normalization', e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                  <span>L2 Normalization</span>
                </div>
              </div>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
}

export default IngestionRuleFormModal;
