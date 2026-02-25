import React, { useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import Modal from '../components/Modal';
import Tooltip from '../components/Tooltip';

function TenantFormModal({ tenant, onSave, onClose }) {
  const isEdit = !!tenant;
  const [form, setForm] = useState({
    Name: tenant?.Name || '',
    Active: tenant?.Active !== undefined ? tenant.Active : true,
    IsProtected: tenant?.IsProtected !== undefined ? tenant.IsProtected : false,
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
      if (isEdit) data.Id = tenant.Id;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? 'Edit Tenant' : 'Create Tenant'} onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label><Tooltip text="Display name for this tenant organization">Name</Tooltip></label>
          <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} required />
        </div>
        {isEdit && (
          <>
            <div className="form-group">
              <div className="form-toggle">
                <label className="toggle-switch">
                  <input type="checkbox" checked={form.Active} onChange={(e) => handleChange('Active', e.target.checked)} />
                  <span className="toggle-slider"></span>
                </label>
                <span><Tooltip text="Whether this tenant is currently active">Active</Tooltip></span>
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
      {!isEdit && (
        <div className="form-help" style={{ marginTop: '12px', padding: '8px 12px', background: 'var(--bg-secondary)', borderRadius: '6px', fontSize: '13px', color: 'var(--text-secondary)' }}>
          Creating a tenant will automatically provision a default admin user, credential, ingestion rule, and RecallDB collection.
        </div>
      )}
    </Modal>
  );
}

function ProvisioningResultModal({ result, onClose }) {
  return (
    <Modal title="Tenant Provisioned" onClose={onClose} footer={
      <button className="btn btn-primary" onClick={onClose}>Close</button>
    }>
      <div style={{ fontFamily: 'monospace', fontSize: '13px', lineHeight: '1.8' }}>
        <div><strong>Tenant ID:</strong> {result.Tenant?.Id}</div>
        <div><strong>Tenant Name:</strong> {result.Tenant?.Name}</div>
        <hr style={{ margin: '8px 0', border: 'none', borderTop: '1px solid var(--border-color)' }} />
        <div><strong>Admin User ID:</strong> {result.Provisioning?.User?.Id || result.Provisioning?.AdminUserId}</div>
        <div><strong>Admin Email:</strong> {result.Provisioning?.User?.Email || result.Provisioning?.AdminEmail}</div>
        <div><strong>Admin Password:</strong> {result.Provisioning?.AdminPassword}</div>
        <hr style={{ margin: '8px 0', border: 'none', borderTop: '1px solid var(--border-color)' }} />
        <div><strong>Credential ID:</strong> {result.Provisioning?.Credential?.Id}</div>
        <div><strong>Bearer Token:</strong> {result.Provisioning?.Credential?.BearerToken || result.Provisioning?.BearerToken}</div>
      </div>
      <div className="form-help" style={{ marginTop: '12px', padding: '8px 12px', background: 'var(--bg-warning, #fff3cd)', borderRadius: '6px', fontSize: '13px' }}>
        Save these credentials now. The password and token cannot be retrieved later.
      </div>
    </Modal>
  );
}

function TenantsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editTenant, setEditTenant] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [provisioningResult, setProvisioningResult] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique tenant identifier', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Tenant display name', filterable: true },
    { key: 'Active', label: 'Status', tooltip: 'Whether this tenant is currently active', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
    { key: 'IsProtected', label: 'Protected', tooltip: 'Protected records cannot be deleted', render: (row) => row.IsProtected ? <span className="status-badge active">Yes</span> : <span className="status-badge inactive">No</span> },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'When this tenant was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleDateString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getTenants(params);
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditTenant(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    ...(!row.IsProtected ? [{ label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) }] : []),
  ];

  const handleSave = async (data) => {
    try {
      if (editTenant) {
        await api.updateTenant(editTenant.Id, data);
        setShowForm(false);
        setEditTenant(null);
        setRefresh(r => r + 1);
      } else {
        const result = await api.createTenant(data);
        setShowForm(false);
        setEditTenant(null);
        setProvisioningResult(result);
        setRefresh(r => r + 1);
      }
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save tenant' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteTenant(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete tenant' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteTenant(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some tenants' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Tenants</h1>
          <p className="content-subtitle">Manage tenant organizations. Creating a tenant auto-provisions default resources.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditTenant(null); setShowForm(true); }}>Create Tenant</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <TenantFormModal tenant={editTenant} onSave={handleSave} onClose={() => { setShowForm(false); setEditTenant(null); }} />}
      {provisioningResult && <ProvisioningResultModal result={provisioningResult} onClose={() => setProvisioningResult(null)} />}
      {showJson && <JsonViewModal title="Tenant JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Tenant" message={`Are you sure you want to delete tenant "${deleteTarget.Name}"? This will permanently delete all users, credentials, assistants, documents, and other resources belonging to this tenant.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default TenantsView;
