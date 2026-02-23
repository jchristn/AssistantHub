import React, { useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import InferenceEndpointFormModal from '../components/modals/InferenceEndpointFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function InferenceEndpointsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editEndpoint, setEditEndpoint] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this inference endpoint', render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name for this inference endpoint', filterable: true },
    { key: 'Model', label: 'Model', tooltip: 'Model used for completion and inference', filterable: true },
    { key: 'Endpoint', label: 'Endpoint', tooltip: 'URL of the inference endpoint', filterable: true },
    { key: 'ApiFormat', label: 'Format', tooltip: 'API format used by this endpoint' },
    { key: 'Active', label: 'Active', tooltip: 'Whether this endpoint is currently active', render: (row) => row.Active ? 'Yes' : 'No' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the endpoint was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.enumerateCompletionEndpoints({
      MaxResults: params.maxResults,
      ContinuationToken: params.continuationToken,
      Ordering: params.ordering
    });
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditEndpoint(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      if (editEndpoint) {
        await api.updateCompletionEndpoint(editEndpoint.Id, data);
      } else {
        await api.createCompletionEndpoint(data);
      }
      setShowForm(false);
      setEditEndpoint(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save inference endpoint' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteCompletionEndpoint(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete inference endpoint' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteCompletionEndpoint(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some inference endpoints' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Inference Endpoints</h1>
          <p className="content-subtitle">Manage Partio completion endpoints for summarization and inference.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditEndpoint(null); setShowForm(true); }}>Create Inference Endpoint</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <InferenceEndpointFormModal endpoint={editEndpoint} onSave={handleSave} onClose={() => { setShowForm(false); setEditEndpoint(null); }} />}
      {showJson && <JsonViewModal title="Inference Endpoint JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Inference Endpoint" message={`Are you sure you want to delete inference endpoint "${deleteTarget.Name || deleteTarget.Model}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default InferenceEndpointsView;
