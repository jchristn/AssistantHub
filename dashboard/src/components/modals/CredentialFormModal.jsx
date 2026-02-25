import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';
import CopyableId from '../CopyableId';

function CredentialFormModal({ credential, onSave, onClose }) {
  const isEdit = !!credential;
  const [form, setForm] = useState({
    Name: credential?.Name || 'Default credential',
    UserId: credential?.UserId || '',
    Active: credential?.Active !== undefined ? credential.Active : true,
    IsProtected: credential?.IsProtected !== undefined ? credential.IsProtected : false
  });
  const [saving, setSaving] = useState(false);
  const [createdToken, setCreatedToken] = useState(null);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const data = { ...form };
      if (isEdit) data.Id = credential.Id;
      const result = await onSave(data);
      if (result && result.BearerToken && !isEdit) {
        setCreatedToken(result.BearerToken);
      }
    } finally {
      setSaving(false);
    }
  };

  if (createdToken) {
    return (
      <Modal title="Credential Created" onClose={onClose} footer={
        <button className="btn btn-primary" onClick={onClose}>Done</button>
      }>
        <p style={{ marginBottom: '1rem' }}>Save this bearer token - it won't be shown again:</p>
        <CopyableId id={createdToken} />
      </Modal>
    );
  }

  return (
    <Modal title={isEdit ? 'Edit Credential' : 'Create Credential'} onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="Descriptive name for this API credential">Name</Tooltip></label>
          <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} />
        </div>
        <div className="form-group">
          <label><Tooltip text="User account this credential is associated with">User ID</Tooltip></label>
          <input type="text" value={form.UserId} onChange={(e) => handleChange('UserId', e.target.value)} required disabled={isEdit} />
        </div>
        {isEdit && (
          <>
            <div className="form-group">
              <div className="form-toggle">
                <label className="toggle-switch">
                  <input type="checkbox" checked={form.Active} onChange={(e) => handleChange('Active', e.target.checked)} />
                  <span className="toggle-slider"></span>
                </label>
                <span><Tooltip text="Whether this credential can be used for API authentication">Active</Tooltip></span>
              </div>
            </div>
            <div className="form-group">
              <div className="form-toggle">
                <label className="toggle-switch">
                  <input type="checkbox" checked={form.IsProtected} onChange={(e) => handleChange('IsProtected', e.target.checked)} />
                  <span className="toggle-slider"></span>
                </label>
                <span><Tooltip text="Protected records cannot be deleted">Protected</Tooltip></span>
              </div>
            </div>
          </>
        )}
      </form>
    </Modal>
  );
}

export default CredentialFormModal;
