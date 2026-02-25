import React, { useState, useCallback } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import CredentialFormModal from '../components/modals/CredentialFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function CredentialsView() {
  const location = useLocation();
  const { serverUrl, credential, tenantId, isGlobalAdmin } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editCredential, setEditCredential] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const initialFilters = location.state?.initialFilters;

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this credential', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    ...(isGlobalAdmin ? [{ key: 'TenantId', label: 'Tenant', tooltip: 'Tenant this credential belongs to', filterable: true, render: (row) => <CopyableId id={row.TenantId} /> }] : []),
    { key: 'Name', label: 'Name', tooltip: 'Display name for this credential', filterable: true },
    { key: 'UserId', label: 'User ID', tooltip: 'The user account this credential belongs to', filterable: true, render: (row) => <CopyableId id={row.UserId} /> },
    { key: 'BearerToken', label: 'Bearer Token', tooltip: 'Authentication token used for API access', render: (row) => row.BearerToken ? <CopyableId id={row.BearerToken} /> : <span className="status-badge info">Hidden</span> },
    { key: 'Active', label: 'Status', tooltip: 'Whether this credential is currently active', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
    { key: 'IsProtected', label: 'Protected', tooltip: 'Protected records cannot be deleted', render: (row) => row.IsProtected ? <span className="status-badge active">Yes</span> : <span className="status-badge inactive">No</span> },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getCredentials(tenantId, params);
  }, [serverUrl, credential, tenantId]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditCredential(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    ...(!row.IsProtected ? [{ label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) }] : []),
  ];

  const handleSave = async (data) => {
    try {
      let result;
      const tid = data.TenantId || tenantId;
      if (editCredential) {
        result = await api.updateCredential(tid, editCredential.Id, data);
      } else {
        result = await api.createCredential(data, tid);
      }
      setShowForm(false);
      setEditCredential(null);
      setRefresh(r => r + 1);
      return result;
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save credential' });
    }
  };

  const handleDelete = async () => {
    try {
      const tid = deleteTarget.TenantId || tenantId;
      await api.deleteCredential(tid, deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete credential' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteCredential(tenantId, id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some credentials' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Credentials</h1>
          <p className="content-subtitle">Manage bearer tokens used for API authentication.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditCredential(null); setShowForm(true); }}>Create Credential</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} initialFilters={initialFilters} onBulkDelete={handleBulkDelete} />
      {showForm && <CredentialFormModal credential={editCredential} onSave={handleSave} onClose={() => { setShowForm(false); setEditCredential(null); }} />}
      {showJson && <JsonViewModal title="Credential JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Credential" message={`Are you sure you want to delete credential "${deleteTarget.Name}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default CredentialsView;
