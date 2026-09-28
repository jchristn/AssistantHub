import React, { useState } from 'react';
import Modal from '../Modal';

function parseIds(value) {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function DocumentSupersedesModal({ doc, onSave, onClose }) {
  const [text, setText] = useState(parseIds(doc?.Supersedes).join('\n'));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const handleSave = async () => {
    const ids = text.split(/[\s,]+/).map(s => s.trim()).filter(Boolean);
    setSaving(true);
    setError(null);
    try {
      await onSave(ids);
    } catch (err) {
      setError(err.message || 'Failed to save');
      setSaving(false);
    }
  };

  return (
    <Modal title="Set Superseded Documents" onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : 'Save'}</button>
      </>
    }>
      <p className="settings-help">
        List the documents that <strong>{doc?.OriginalFilename || doc?.Name || doc?.Id}</strong> replaces, one ID per line
        (for example the previous version of a policy). Assistants then treat their content as outdated according to
        their Superseded Documents setting. Leave empty to clear the links.
      </p>
      <textarea className="form-input" rows={6} value={text} onChange={(e) => setText(e.target.value)} placeholder="adoc_..." />
      {error && <div className="tool-policy-warning danger" style={{ marginTop: '0.75rem' }}>{error}</div>}
    </Modal>
  );
}

export default DocumentSupersedesModal;
