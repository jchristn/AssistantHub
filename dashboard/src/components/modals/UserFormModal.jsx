import React, { useState } from 'react';
import Modal from '../Modal';

function UserFormModal({ user, onSave, onClose }) {
  const isEdit = !!user;
  const [form, setForm] = useState({
    Email: user?.Email || '',
    Password: '',
    FirstName: user?.FirstName || '',
    LastName: user?.LastName || '',
    IsAdmin: user?.IsAdmin || false,
    Active: user?.Active !== undefined ? user.Active : true
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
      if (isEdit && !data.Password) delete data.Password;
      if (isEdit) data.Id = user.Id;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? 'Edit User' : 'Create User'} onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Email</label>
          <input type="email" value={form.Email} onChange={(e) => handleChange('Email', e.target.value)} required />
        </div>
        <div className="form-group">
          <label>Password {isEdit && '(leave blank to keep current)'}</label>
          <input type="password" value={form.Password} onChange={(e) => handleChange('Password', e.target.value)} {...(!isEdit ? { required: true } : {})} />
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>First Name</label>
            <input type="text" value={form.FirstName} onChange={(e) => handleChange('FirstName', e.target.value)} />
          </div>
          <div className="form-group">
            <label>Last Name</label>
            <input type="text" value={form.LastName} onChange={(e) => handleChange('LastName', e.target.value)} />
          </div>
        </div>
        <div className="form-group">
          <div className="form-toggle">
            <label className="toggle-switch">
              <input type="checkbox" checked={form.IsAdmin} onChange={(e) => handleChange('IsAdmin', e.target.checked)} />
              <span className="toggle-slider"></span>
            </label>
            <span>Administrator</span>
          </div>
        </div>
        {isEdit && (
          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input type="checkbox" checked={form.Active} onChange={(e) => handleChange('Active', e.target.checked)} />
                <span className="toggle-slider"></span>
              </label>
              <span>Active</span>
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
}

export default UserFormModal;
