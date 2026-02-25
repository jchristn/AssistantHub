import React, { useState, useCallback, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import EmbeddingEndpointFormModal from '../components/modals/EmbeddingEndpointFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import { HealthHistogram, HealthDetailModal } from '../components/HealthHistogram';

function EmbeddingEndpointsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editEndpoint, setEditEndpoint] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [healthData, setHealthData] = useState({});
  const [healthDetailModal, setHealthDetailModal] = useState({ isOpen: false, data: null });

  const loadHealth = useCallback(async () => {
    try {
      const result = await api.getAllEmbeddingEndpointHealth();
      const map = {};
      if (Array.isArray(result)) {
        for (const h of result) {
          map[h.EndpointId] = h;
        }
      }
      setHealthData(map);
    } catch (err) { console.error('Failed to load health data:', err); }
  }, [serverUrl, credential]);

  useEffect(() => { loadHealth(); }, [loadHealth]);

  useEffect(() => {
    const interval = setInterval(loadHealth, 15000);
    return () => clearInterval(interval);
  }, [loadHealth]);

  const openHealthDetail = async (endpointId) => {
    try {
      const result = await api.getEmbeddingEndpointHealth(endpointId);
      setHealthDetailModal({ isOpen: true, data: result });
    } catch (err) {
      setAlert({ title: 'Error', message: 'Health data not available: ' + err.message });
    }
  };

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this embedding endpoint', render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name for this embedding endpoint', filterable: true },
    { key: 'Model', label: 'Model', tooltip: 'Model used for embedding generation', filterable: true },
    { key: 'Endpoint', label: 'Endpoint', tooltip: 'URL of the embedding endpoint', filterable: true },
    { key: 'ApiFormat', label: 'Format', tooltip: 'API format used by this endpoint', filterable: true },
    { key: 'Active', label: 'Active', tooltip: 'Whether this endpoint is currently active', render: (row) => row.Active ? 'Yes' : 'No' },
    {
      key: 'Health',
      label: 'Health',
      tooltip: 'Live health check status and recent history (click for details)',
      render: (row) => {
        if (!row.HealthCheckEnabled) return <span style={{ color: 'var(--text-secondary)' }}>N/A</span>;
        const h = healthData[row.Id];
        if (!h) return <span style={{ color: 'var(--text-secondary)' }}>Pending</span>;
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }} onClick={() => openHealthDetail(row.Id)}>
            <span className={`status-badge ${h.IsHealthy ? 'active' : 'inactive'}`}>
              {h.IsHealthy ? 'Healthy' : 'Unhealthy'}
            </span>
            <HealthHistogram history={h.History || []} width={80} height={18} />
          </div>
        );
      }
    },
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
    ...(row.HealthCheckEnabled ? [{ label: 'Health Detail', onClick: () => openHealthDetail(row.Id) }] : []),
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
      loadHealth();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save embedding endpoint' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteEmbeddingEndpoint(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
      loadHealth();
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
      loadHealth();
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
      {deleteTarget && <ConfirmModal title="Delete Embedding Endpoint" message={`Are you sure you want to delete embedding endpoint "${deleteTarget.Name || deleteTarget.Model}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
      <HealthDetailModal
        isOpen={healthDetailModal.isOpen}
        onClose={() => setHealthDetailModal({ isOpen: false, data: null })}
        healthData={healthDetailModal.data}
      />
    </div>
  );
}

export default EmbeddingEndpointsView;
