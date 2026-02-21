import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

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

const DEFAULT_SUMMARIZATION_PROMPT =
  'Summarize the following content in at most {tokens} tokens.\n\n' +
  'Content:\n{content}\n\n' +
  'Context:\n{context}';

const defaultSummarization = {
  CompletionEndpointId: '',
  Order: 'BottomUp',
  SummarizationPrompt: DEFAULT_SUMMARIZATION_PROMPT,
  MaxSummaryTokens: 1024,
  MinCellLength: 128,
  MaxParallelTasks: 4,
  MaxRetriesPerSummary: 3,
  MaxRetries: 9,
  TimeoutMs: 30000
};

const SUMMARIZATION_ORDER_OPTIONS = ['BottomUp', 'TopDown'];

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
  EmbeddingEndpointId: '',
  L2Normalization: false
};

function IngestionRuleFormModal({ rule, buckets, collections, inferenceEndpoints, embeddingEndpoints, onSave, onClose }) {
  const isEdit = !!rule;

  const [form, setForm] = useState({
    Name: rule?.Name || '',
    Description: rule?.Description || '',
    Bucket: rule?.Bucket || '',
    CollectionId: rule?.CollectionId || '',
    CollectionName: rule?.CollectionName || '',
    Labels: rule?.Labels ? [...rule.Labels] : [],
    Tags: rule?.Tags ? { ...rule.Tags } : {},
    Summarization: rule?.Summarization ? { ...defaultSummarization, ...rule.Summarization } : { ...defaultSummarization },
    Chunking: rule?.Chunking ? { ...defaultChunking, ...rule.Chunking } : { ...defaultChunking },
    Embedding: rule?.Embedding ? { ...defaultEmbedding, ...rule.Embedding } : { ...defaultEmbedding }
  });

  const [saving, setSaving] = useState(false);
  const [summarizationEnabled, setSummarizationEnabled] = useState(!!rule?.Summarization);
  const [labelInput, setLabelInput] = useState('');
  const [tagKeyInput, setTagKeyInput] = useState('');
  const [tagValueInput, setTagValueInput] = useState('');
  const [summarizationOpen, setSummarizationOpen] = useState(false);
  const [chunkingOpen, setChunkingOpen] = useState(false);
  const [embeddingOpen, setEmbeddingOpen] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSummarizationChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Summarization: { ...prev.Summarization, [field]: value }
    }));
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
        Summarization: summarizationEnabled ? {
          CompletionEndpointId: form.Summarization.CompletionEndpointId || undefined,
          Order: form.Summarization.Order || 'BottomUp',
          SummarizationPrompt: form.Summarization.SummarizationPrompt || undefined,
          MaxSummaryTokens: parseInt(form.Summarization.MaxSummaryTokens) || 1024,
          MinCellLength: parseInt(form.Summarization.MinCellLength) || 128,
          MaxParallelTasks: parseInt(form.Summarization.MaxParallelTasks) || 4,
          MaxRetriesPerSummary: parseInt(form.Summarization.MaxRetriesPerSummary) || 3,
          MaxRetries: parseInt(form.Summarization.MaxRetries) || 9,
          TimeoutMs: parseInt(form.Summarization.TimeoutMs) || 30000
        } : null,
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
          EmbeddingEndpointId: form.Embedding.EmbeddingEndpointId || undefined,
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
          <label><Tooltip text="Display name for this ingestion rule">Name</Tooltip></label>
          <input
            type="text"
            value={form.Name}
            onChange={(e) => handleChange('Name', e.target.value)}
            required
          />
        </div>

        {/* Description */}
        <div className="form-group">
          <label><Tooltip text="Optional description of what this ingestion rule does">Description</Tooltip></label>
          <textarea
            value={form.Description}
            onChange={(e) => handleChange('Description', e.target.value)}
            rows={3}
          />
        </div>

        {/* Bucket */}
        <div className="form-group">
          <label><Tooltip text="Source S3 storage bucket that this rule monitors for new documents">Bucket</Tooltip></label>
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
          <label><Tooltip text="Target vector collection where processed document chunks and embeddings are stored">Collection</Tooltip></label>
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
          <label><Tooltip text="Labels applied to all documents ingested by this rule, useful for filtering and organization">Labels</Tooltip></label>
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
          <label><Tooltip text="Key-value metadata tags applied to all documents ingested by this rule">Tags</Tooltip></label>
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

        {/* Summarization (collapsible) */}
        <div className="form-group">
          <button
            type="button"
            style={collapsibleButtonStyle}
            onClick={() => setSummarizationOpen(prev => !prev)}
          >
            {summarizationOpen ? '\u25BE' : '\u25B8'} Summarization
          </button>
          {summarizationOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <label style={{ margin: 0 }}><Tooltip text="Enable or disable summarization for documents processed by this rule">Enabled</Tooltip></label>
                  <label className="toggle-switch" style={{ margin: 0 }}>
                    <input
                      type="checkbox"
                      checked={summarizationEnabled}
                      onChange={(e) => setSummarizationEnabled(e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>
              {summarizationEnabled && (
              <>
              <div className="form-group">
                <label><Tooltip text="Inference endpoint used to generate summaries via completion API">Inference Endpoint</Tooltip></label>
                <select
                  value={form.Summarization.CompletionEndpointId}
                  onChange={(e) => handleSummarizationChange('CompletionEndpointId', e.target.value)}
                >
                  <option value="">-- Select Inference Endpoint --</option>
                  {(inferenceEndpoints || []).map(ep => (
                    <option key={ep.Id} value={ep.Id}>{ep.Name || ep.Model || ep.Id}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label><Tooltip text="Summarization strategy: BottomUp processes from leaf nodes to root (child summaries inform parent), TopDown processes from root to leaves (parent context informs children)">Strategy</Tooltip></label>
                <select
                  value={form.Summarization.Order}
                  onChange={(e) => handleSummarizationChange('Order', e.target.value)}
                >
                  {SUMMARIZATION_ORDER_OPTIONS.map(opt => (
                    <option key={opt} value={opt}>{opt}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label><Tooltip text="Prompt template sent to the inference endpoint. Supports placeholders: {tokens} (max summary token count), {content} (the text to summarize), {context} (surrounding context from parent or child cells)">Summarization Prompt</Tooltip></label>
                <textarea
                  value={form.Summarization.SummarizationPrompt}
                  onChange={(e) => handleSummarizationChange('SummarizationPrompt', e.target.value)}
                  rows={4}
                  placeholder="Custom prompt template for summarization"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Maximum number of tokens the model should produce for each summary (minimum 128)">Max Summary Tokens</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.MaxSummaryTokens}
                  onChange={(e) => handleSummarizationChange('MaxSummaryTokens', e.target.value)}
                  min="128"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Minimum character length a cell must have before summarization is attempted. Cells shorter than this are skipped (minimum 0)">Min Cell Length</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.MinCellLength}
                  onChange={(e) => handleSummarizationChange('MinCellLength', e.target.value)}
                  min="0"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Maximum number of summarization requests that can run concurrently (minimum 1)">Max Parallel Tasks</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.MaxParallelTasks}
                  onChange={(e) => handleSummarizationChange('MaxParallelTasks', e.target.value)}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Maximum number of retry attempts for a single cell's summarization request before giving up on that cell">Max Retries Per Summary</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.MaxRetriesPerSummary}
                  onChange={(e) => handleSummarizationChange('MaxRetriesPerSummary', e.target.value)}
                  min="0"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Global failure counter upper limit across all cells. When this many total failures are reached, the entire summarization job is aborted (circuit breaker)">Max Retries</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.MaxRetries}
                  onChange={(e) => handleSummarizationChange('MaxRetries', e.target.value)}
                  min="0"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Timeout in milliseconds for each individual summarization request to the inference endpoint (minimum 100)">Timeout (ms)</Tooltip></label>
                <input
                  type="number"
                  value={form.Summarization.TimeoutMs}
                  onChange={(e) => handleSummarizationChange('TimeoutMs', e.target.value)}
                  min="100"
                  step="1000"
                />
              </div>
              </>
              )}
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
                <label><Tooltip text="Algorithm used to split document content into chunks for embedding">Strategy</Tooltip></label>
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
                <label><Tooltip text="Number of tokens per chunk when using the FixedTokenCount strategy">Fixed Token Count</Tooltip></label>
                <input
                  type="number"
                  value={form.Chunking.FixedTokenCount}
                  onChange={(e) => handleChunkingChange('FixedTokenCount', e.target.value)}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Number of tokens to overlap between consecutive chunks for context continuity">Overlap Count</Tooltip></label>
                <input
                  type="number"
                  value={form.Chunking.OverlapCount}
                  onChange={(e) => handleChunkingChange('OverlapCount', e.target.value)}
                  min="0"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Percentage of chunk size to overlap between consecutive chunks (0.0 to 1.0). Alternative to Overlap Count">Overlap Percentage</Tooltip></label>
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
                <label><Tooltip text="Strategy for determining overlap boundaries between chunks">Overlap Strategy</Tooltip></label>
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
                <label><Tooltip text="Number of rows to group together when using RowGroupWithHeaders chunking strategy">Row Group Size</Tooltip></label>
                <input
                  type="number"
                  value={form.Chunking.RowGroupSize}
                  onChange={(e) => handleChunkingChange('RowGroupSize', e.target.value)}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Optional text prepended to each chunk to provide additional context">Context Prefix</Tooltip></label>
                <input
                  type="text"
                  value={form.Chunking.ContextPrefix}
                  onChange={(e) => handleChunkingChange('ContextPrefix', e.target.value)}
                  placeholder="Optional"
                />
              </div>

              <div className="form-group">
                <label><Tooltip text="Regular expression pattern used to split content when using the RegexBased chunking strategy">Regex Pattern</Tooltip></label>
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
                <label><Tooltip text="Embedding endpoint used to generate vector embeddings for document chunks">Embedding Endpoint</Tooltip></label>
                <select
                  value={form.Embedding.EmbeddingEndpointId}
                  onChange={(e) => handleEmbeddingChange('EmbeddingEndpointId', e.target.value)}
                >
                  <option value="">-- Select Embedding Endpoint --</option>
                  {(embeddingEndpoints || []).map(ep => (
                    <option key={ep.Id} value={ep.Id}>{ep.Model || ep.Id}</option>
                  ))}
                </select>
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
                  <span><Tooltip text="Apply L2 normalization to embedding vectors, normalizing them to unit length for cosine similarity comparisons">L2 Normalization</Tooltip></span>
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
