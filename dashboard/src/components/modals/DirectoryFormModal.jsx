import React, { useState } from 'react';
import Modal from '../Modal';

function DirectoryFormModal({ onSave, onClose }) {
  const [name, setName] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const validate = (value) => {
    if (!value.trim()) return 'Directory name is required.';
    if (value.includes('/')) return 'Directory name cannot contain slashes.';
    return '';
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const err = validate(name);
    if (err) { setError(err); return; }
    setSaving(true);
    try {
      await onSave(name.trim());
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title="Create Directory" onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving || !name.trim()}>
          {saving ? 'Creating...' : 'Create'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Directory Name</label>
          <input
            type="text"
            value={name}
            onChange={(e) => { setName(e.target.value); setError(''); }}
            required
            autoFocus
          />
          {error && <small style={{ color: 'var(--danger-color)', marginTop: '4px', display: 'block' }}>{error}</small>}
          <small style={{ color: '#888', marginTop: '4px', display: 'block' }}>Creates an empty directory marker object. No slashes allowed.</small>
        </div>
      </form>
    </Modal>
  );
}

export default DirectoryFormModal;
