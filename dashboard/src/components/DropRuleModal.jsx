import React, { useState } from 'react';
import Modal from './Modal';

function DropRuleModal({ fileCount, ingestionRules, onConfirm, onClose }) {
  const rules = ingestionRules || [];
  const [ruleId, setRuleId] = useState(rules.length === 1 ? rules[0].Id : '');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [labels, setLabels] = useState([]);
  const [labelInput, setLabelInput] = useState('');
  const [tags, setTags] = useState({});
  const [tagKey, setTagKey] = useState('');
  const [tagValue, setTagValue] = useState('');

  const addLabel = () => {
    const trimmed = labelInput.trim();
    if (trimmed && !labels.includes(trimmed)) {
      setLabels([...labels, trimmed]);
      setLabelInput('');
    }
  };

  const removeLabel = (label) => setLabels(labels.filter(l => l !== label));

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

  const handleConfirm = () => {
    if (!ruleId) return;
    onConfirm(ruleId, labels, tags);
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
    <Modal title={`Upload ${fileCount} file(s)`} extraWide onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleConfirm} disabled={!ruleId}>Begin Upload</button>
      </>
    }>
      <div className="form-group">
        <label>Ingestion Rule</label>
        <select value={ruleId} onChange={(e) => setRuleId(e.target.value)} required>
          <option value="">Select an ingestion rule...</option>
          {rules.map(r => (
            <option key={r.Id} value={r.Id}>{r.Name} ({r.Id.substring(0, 12)}...)</option>
          ))}
        </select>
      </div>

      <button
        type="button"
        className="btn btn-ghost btn-sm"
        onClick={() => setShowAdvanced(!showAdvanced)}
        style={{ marginBottom: '0.75rem' }}
      >
        {showAdvanced ? '\u25BC' : '\u25B6'} Advanced
      </button>

      {showAdvanced && (
        <>
          <div className="form-group">
            <label>Labels</label>
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
            <label>Tags</label>
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
        </>
      )}
    </Modal>
  );
}

export default DropRuleModal;
