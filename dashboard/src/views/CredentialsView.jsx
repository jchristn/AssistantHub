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
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editCredential, setEditCredential] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const initialFilters = location.state?.initialFilters;

  const columns = [
    { key: 'Id', label: 'ID', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', filterable: true },
    { key: 'UserId', label: 'User ID', filterable: true, render: (row) => <CopyableId id={row.UserId} /> },
    { key: 'BearerToken', label: 'Bearer Token', render: (row) => row.BearerToken ? <CopyableId id={row.BearerToken} /> : <span className="status-badge info">Hidden</span> },
    { key: 'Active', label: 'Status', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getCredentials(params);
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditCredential(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      let result;
      if (editCredential) {
        result = await api.updateCredential(editCredential.Id, data);
      } else {
        result = await api.createCredential(data);
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
      await api.deleteCredential(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete credential' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Credentials</h1>
          <p className="content-subtitle">Manage API credentials and bearer tokens for authentication.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditCredential(null); setShowForm(true); }}>Create Credential</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} initialFilters={initialFilters} />
      {showForm && <CredentialFormModal credential={editCredential} onSave={handleSave} onClose={() => { setShowForm(false); setEditCredential(null); }} />}
      {showJson && <JsonViewModal title="Credential JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Credential" message={`Are you sure you want to delete credential "${deleteTarget.Name}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default CredentialsView;
