import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function BucketFormModal({ onSave, onClose }) {
  const [form, setForm] = useState({ Name: '' });
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSave({ Name: form.Name });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title="Create Bucket" onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving || !form.Name.trim()}>
          {saving ? 'Creating...' : 'Create'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="Unique name for the S3 storage bucket. Lowercase letters, numbers, and hyphens only">Name</Tooltip></label>
          <input type="text" value={form.Name} onChange={(e) => setForm({ Name: e.target.value })} required />
          <small style={{ color: '#888', marginTop: '4px', display: 'block' }}>Lowercase letters, numbers, and hyphens only. No spaces.</small>
        </div>
      </form>
    </Modal>
  );
}

export default BucketFormModal;
