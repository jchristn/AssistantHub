import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function AssistantFormModal({ assistant, onSave, onClose }) {
  const isEdit = !!assistant;
  const [form, setForm] = useState({
    Name: assistant?.Name || '',
    Description: assistant?.Description || '',
    Active: assistant?.Active !== undefined ? assistant.Active : true
  });
  const [saving, setSaving] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const data = { ...form };
      if (isEdit) data.Id = assistant.Id;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? 'Edit Assistant' : 'Create Assistant'} onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="Display name for the assistant shown to users">Name</Tooltip></label>
          <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} required />
        </div>
        <div className="form-group">
          <label><Tooltip text="Optional description of the assistant's purpose and capabilities">Description</Tooltip></label>
          <textarea value={form.Description} onChange={(e) => handleChange('Description', e.target.value)} rows={3} />
        </div>
        {isEdit && (
          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input type="checkbox" checked={form.Active} onChange={(e) => handleChange('Active', e.target.checked)} />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text="Whether this assistant is active and available for use">Active</Tooltip></span>
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
}

export default AssistantFormModal;
