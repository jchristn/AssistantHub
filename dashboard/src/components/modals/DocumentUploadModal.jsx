import React, { useState, useRef } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function DocumentUploadModal({ ingestionRules, onUpload, onClose }) {
  const rules = ingestionRules || [];
  const [file, setFile] = useState(null);
  const [name, setName] = useState('');
  const [ruleId, setRuleId] = useState(rules.length === 1 ? rules[0].Id : '');
  const [uploading, setUploading] = useState(false);
  const [labels, setLabels] = useState([]);
  const [labelInput, setLabelInput] = useState('');
  const [tags, setTags] = useState({});
  const [tagKey, setTagKey] = useState('');
  const [tagValue, setTagValue] = useState('');
  const fileRef = useRef(null);

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) {
      setFile(selectedFile);
      if (!name) setName(selectedFile.name);
    }
  };

  const addLabel = () => {
    const trimmed = labelInput.trim();
    if (trimmed && !labels.includes(trimmed)) {
      setLabels([...labels, trimmed]);
      setLabelInput('');
    }
  };

  const removeLabel = (label) => {
    setLabels(labels.filter(l => l !== label));
  };

  const addTag = () => {
    const k = tagKey.trim();
    const v = tagValue.trim();
    if (k) {
      setTags({ ...tags, [k]: v });
      setTagKey('');
      setTagValue('');
    }
  };

  const removeTag = (key) => {
    const next = { ...tags };
    delete next[key];
    setTags(next);
  };

  const handleSubmit = async () => {
    if (!file || !ruleId) return;
    setUploading(true);
    try {
      await onUpload({
        IngestionRuleId: ruleId,
        Name: name || file.name,
        OriginalFilename: file.name,
        ContentType: file.type || 'application/octet-stream',
        Labels: labels.length > 0 ? labels : undefined,
        Tags: Object.keys(tags).length > 0 ? tags : undefined,
        file: file
      });
    } finally {
      setUploading(false);
    }
  };

  const chipStyle = {
    display: 'inline-flex', alignItems: 'center', gap: '0.25rem',
    padding: '0.25rem 0.5rem', background: 'var(--bg-tertiary)',
    borderRadius: 'var(--radius-sm)', fontSize: '0.8rem'
  };

  const chipRemoveStyle = {
    background: 'none', border: 'none', cursor: 'pointer',
    padding: 0, fontSize: '0.9rem', lineHeight: 1, color: 'var(--text-secondary)'
  };

  return (
    <Modal title="Upload Document" onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={!file || !ruleId || uploading}>
          {uploading ? 'Uploading...' : 'Upload'}
        </button>
      </>
    }>
      <div className="form-group">
        <label><Tooltip text="Ingestion rule that defines how this document will be processed, chunked, and embedded">Ingestion Rule</Tooltip></label>
        <select value={ruleId} onChange={(e) => setRuleId(e.target.value)} required>
          <option value="">Select an ingestion rule...</option>
          {rules.map(r => (
            <option key={r.Id} value={r.Id}>{r.Name} ({r.Id.substring(0, 12)}...)</option>
          ))}
        </select>
      </div>
      <div className="form-group">
        <label><Tooltip text="Display name for the uploaded document. Defaults to the filename if not specified">Document Name</Tooltip></label>
        <input type="text" value={name} onChange={(e) => setName(e.target.value)} placeholder="Enter document name" />
      </div>
      <div className="form-group">
        <label><Tooltip text="Document file to upload for ingestion">File</Tooltip></label>
        <input ref={fileRef} type="file" onChange={handleFileChange} />
      </div>
      {file && (
        <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.75rem' }}>
          <p>File: {file.name}</p>
          <p>Size: {(file.size / 1024).toFixed(1)} KB</p>
          <p>Type: {file.type || 'unknown'}</p>
        </div>
      )}
      <div className="form-group">
        <label><Tooltip text="Optional labels for categorizing and filtering this document">Labels</Tooltip></label>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <input type="text" value={labelInput} onChange={(e) => setLabelInput(e.target.value)}
            placeholder="Add a label" onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addLabel(); } }} style={{ flex: 1 }} />
          <button type="button" className="btn btn-secondary" onClick={addLabel}>Add</button>
        </div>
        {labels.length > 0 && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.25rem', marginTop: '0.5rem' }}>
            {labels.map(label => (
              <span key={label} style={chipStyle}>
                {label}
                <button type="button" style={chipRemoveStyle} onClick={() => removeLabel(label)}>&times;</button>
              </span>
            ))}
          </div>
        )}
      </div>
      <div className="form-group">
        <label><Tooltip text="Optional key-value metadata tags for this document">Tags</Tooltip></label>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <input type="text" value={tagKey} onChange={(e) => setTagKey(e.target.value)} placeholder="Key" style={{ flex: 1 }}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTag(); } }} />
          <input type="text" value={tagValue} onChange={(e) => setTagValue(e.target.value)} placeholder="Value" style={{ flex: 1 }}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTag(); } }} />
          <button type="button" className="btn btn-secondary" onClick={addTag}>Add</button>
        </div>
        {Object.keys(tags).length > 0 && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.25rem', marginTop: '0.5rem' }}>
            {Object.entries(tags).map(([k, v]) => (
              <span key={k} style={chipStyle}>
                {k}: {v}
                <button type="button" style={chipRemoveStyle} onClick={() => removeTag(k)}>&times;</button>
              </span>
            ))}
          </div>
        )}
      </div>
    </Modal>
  );
}

export default DocumentUploadModal;
