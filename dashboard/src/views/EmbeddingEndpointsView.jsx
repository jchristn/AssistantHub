import React, { useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import EmbeddingEndpointFormModal from '../components/modals/EmbeddingEndpointFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function EmbeddingEndpointsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editEndpoint, setEditEndpoint] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this embedding endpoint', render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Model', label: 'Model', tooltip: 'Model used for embedding generation', filterable: true },
    { key: 'Endpoint', label: 'Endpoint', tooltip: 'URL of the embedding endpoint', filterable: true },
    { key: 'ApiFormat', label: 'Format', tooltip: 'API format used by this endpoint', filterable: true },
    { key: 'Active', label: 'Active', tooltip: 'Whether this endpoint is currently active', render: (row) => row.Active ? 'Yes' : 'No' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the endpoint was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.enumerateEmbeddingEndpoints({
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
        await api.updateEmbeddingEndpoint(editEndpoint.Id, data);
      } else {
        await api.createEmbeddingEndpoint(data);
      }
      setShowForm(false);
      setEditEndpoint(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save embedding endpoint' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteEmbeddingEndpoint(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete embedding endpoint' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteEmbeddingEndpoint(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some embedding endpoints' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Embedding Endpoints</h1>
          <p className="content-subtitle">Manage Partio embedding endpoints for vector generation.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditEndpoint(null); setShowForm(true); }}>Create Embedding Endpoint</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <EmbeddingEndpointFormModal endpoint={editEndpoint} onSave={handleSave} onClose={() => { setShowForm(false); setEditEndpoint(null); }} />}
      {showJson && <JsonViewModal title="Embedding Endpoint JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Embedding Endpoint" message={`Are you sure you want to delete embedding endpoint "${deleteTarget.Model}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default EmbeddingEndpointsView;
