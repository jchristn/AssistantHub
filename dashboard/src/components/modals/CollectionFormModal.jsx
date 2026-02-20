import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function CollectionFormModal({ collection, onSave, onClose }) {
  const isEdit = !!collection;
  const [form, setForm] = useState({
    Name: collection?.Name || '',
    Description: collection?.Description || '',
    Dimensionality: collection?.Dimensionality || 384,
    Active: collection?.Active !== undefined ? collection.Active : true
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
      if (isEdit) data.GUID = collection.GUID;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? 'Edit Collection' : 'Create Collection'} onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving || !form.Name.trim()}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="Display name for the vector collection">Name</Tooltip></label>
          <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} required />
        </div>
        <div className="form-group">
          <label><Tooltip text="Optional description of the collection's contents or purpose">Description</Tooltip></label>
          <input type="text" value={form.Description} onChange={(e) => handleChange('Description', e.target.value)} />
        </div>
        <div className="form-group">
          <label><Tooltip text="Number of dimensions for embedding vectors. Must match the output size of your embedding model. Cannot be changed after creation">Dimensionality</Tooltip></label>
          <input type="number" value={form.Dimensionality} onChange={(e) => handleChange('Dimensionality', parseInt(e.target.value) || 1)} min="1" disabled={isEdit} />
        </div>
        {isEdit && (
          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input type="checkbox" checked={form.Active} onChange={(e) => handleChange('Active', e.target.checked)} />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text="Whether this collection is active and available for queries">Active</Tooltip></span>
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
}

export default CollectionFormModal;
