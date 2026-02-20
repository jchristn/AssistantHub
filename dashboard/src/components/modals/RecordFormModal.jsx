import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

const CONTENT_TYPES = ['Text', 'Code', 'List', 'Table', 'Binary', 'Image', 'Hyperlink', 'Meta'];

function RecordFormModal({ onSave, onClose }) {
  const [form, setForm] = useState({
    Content: '',
    ContentType: 'Text',
    DocumentId: '',
    Position: 0,
    Labels: [],
    Tags: [{ key: '', value: '' }],
    Embeddings: '',
  });
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);

  const update = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
    setErrors(prev => ({ ...prev, [field]: '' }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const newErrors = {};
    if (!form.Content.trim()) newErrors.Content = 'Content is required.';

    let embeddings = null;
    if (form.Embeddings.trim()) {
      try {
        embeddings = form.Embeddings.split(',').map(v => {
          const f = parseFloat(v.trim());
          if (isNaN(f)) throw new Error();
          return f;
        });
      } catch {
        newErrors.Embeddings = 'Embeddings must be comma-separated numbers.';
      }
    }

    if (Object.keys(newErrors).length > 0) { setErrors(newErrors); return; }

    const labels = form.Labels.filter(l => l.trim());
    const tags = {};
    form.Tags.forEach(t => { if (t.key.trim()) tags[t.key.trim()] = t.value; });

    const record = {
      Content: form.Content,
      ContentType: form.ContentType,
      Embeddings: embeddings,
      DocumentId: form.DocumentId || null,
      Position: form.Position || 0,
      Labels: labels.length > 0 ? labels : null,
      Tags: Object.keys(tags).length > 0 ? tags : null,
    };

    setSaving(true);
    try {
      await onSave(record);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title="Create Record" onClose={onClose} wide footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving || !form.Content.trim()}>
          {saving ? 'Creating...' : 'Create'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="The text content of this record that will be stored and made searchable">Content</Tooltip> <span style={{ color: 'var(--danger-color)' }}>*</span></label>
          <textarea
            value={form.Content}
            onChange={(e) => update('Content', e.target.value)}
            rows={4}
            required
            autoFocus
            style={{ fontFamily: 'inherit' }}
          />
          {errors.Content && <small style={{ color: 'var(--danger-color)', marginTop: '4px', display: 'block' }}>{errors.Content}</small>}
        </div>
        <div className="form-group">
          <label><Tooltip text="Type of content stored in this record (Text, Code, List, Table, etc.)">Content Type</Tooltip></label>
          <select value={form.ContentType} onChange={(e) => update('ContentType', e.target.value)}>
            {CONTENT_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
        <div className="form-group">
          <label><Tooltip text="Optional identifier to group related records under a single document">Document ID</Tooltip> <span style={{ color: '#888', fontWeight: 400 }}>(optional)</span></label>
          <input
            type="text"
            value={form.DocumentId}
            onChange={(e) => update('DocumentId', e.target.value)}
            placeholder="Optional — groups related chunks"
          />
        </div>
        <div className="form-group">
          <label><Tooltip text="Numeric position of this record within its document, used for ordering">Position</Tooltip></label>
          <input
            type="number"
            value={form.Position}
            onChange={(e) => update('Position', parseInt(e.target.value) || 0)}
          />
        </div>
        <div className="form-group">
          <label><Tooltip text="Optional labels for categorizing and filtering this record">Labels</Tooltip> <span style={{ color: '#888', fontWeight: 400 }}>(optional)</span></label>
          {form.Labels.map((label, i) => (
            <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 4, alignItems: 'center' }}>
              <input
                type="text"
                value={label}
                onChange={(e) => {
                  const labels = [...form.Labels];
                  labels[i] = e.target.value;
                  update('Labels', labels);
                }}
                placeholder="Label"
                style={{ flex: 1 }}
              />
              <button type="button" className="btn btn-secondary" style={{ padding: '4px 8px' }} onClick={() => {
                const labels = form.Labels.filter((_, j) => j !== i);
                update('Labels', labels);
              }}>×</button>
            </div>
          ))}
          <button type="button" className="btn btn-secondary" style={{ marginTop: 4, fontSize: 12 }} onClick={() => update('Labels', [...form.Labels, ''])}>+ Add Label</button>
        </div>
        <div className="form-group">
          <label><Tooltip text="Optional key-value metadata tags for this record">Tags</Tooltip> <span style={{ color: '#888', fontWeight: 400 }}>(optional)</span></label>
          {form.Tags.map((tag, i) => (
            <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 4 }}>
              <input
                type="text"
                placeholder="Key"
                value={tag.key}
                onChange={(e) => {
                  const tags = [...form.Tags];
                  tags[i] = { ...tags[i], key: e.target.value };
                  update('Tags', tags);
                }}
                style={{ flex: 1 }}
              />
              <input
                type="text"
                placeholder="Value"
                value={tag.value}
                onChange={(e) => {
                  const tags = [...form.Tags];
                  tags[i] = { ...tags[i], value: e.target.value };
                  update('Tags', tags);
                }}
                style={{ flex: 1 }}
              />
              <button type="button" className="btn btn-secondary" style={{ padding: '4px 8px' }} onClick={() => {
                const tags = form.Tags.filter((_, j) => j !== i);
                update('Tags', tags.length ? tags : [{ key: '', value: '' }]);
              }}>×</button>
            </div>
          ))}
          <button type="button" className="btn btn-secondary" style={{ marginTop: 4, fontSize: 12 }} onClick={() => update('Tags', [...form.Tags, { key: '', value: '' }])}>+ Add Tag</button>
        </div>
        <div className="form-group">
          <label><Tooltip text="Pre-computed embedding vector for this record. If not provided, embeddings will be generated automatically">Embeddings</Tooltip> <span style={{ color: '#888', fontWeight: 400 }}>(optional, comma-separated floats)</span></label>
          <input
            type="text"
            value={form.Embeddings}
            onChange={(e) => update('Embeddings', e.target.value)}
            placeholder="0.1, 0.2, 0.3"
            style={{ fontFamily: 'monospace', fontSize: '0.8125rem' }}
          />
          {errors.Embeddings && <small style={{ color: 'var(--danger-color)', marginTop: '4px', display: 'block' }}>{errors.Embeddings}</small>}
        </div>
      </form>
    </Modal>
  );
}

export default RecordFormModal;
