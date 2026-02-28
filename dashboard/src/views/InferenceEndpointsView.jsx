import React, { useState, useCallback, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import InferenceEndpointFormModal from '../components/modals/InferenceEndpointFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import { HealthHistogram, HealthDetailModal } from '../components/HealthHistogram';

function InferenceEndpointsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editEndpoint, setEditEndpoint] = useState(null);
  const [initialFormData, setInitialFormData] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [healthData, setHealthData] = useState({});
  const [healthDetailModal, setHealthDetailModal] = useState({ isOpen: false, data: null });

  const loadHealth = useCallback(async () => {
    try {
      const result = await api.getAllCompletionEndpointHealth();
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
      const result = await api.getCompletionEndpointHealth(endpointId);
      setHealthDetailModal({ isOpen: true, data: result });
    } catch (err) {
      setAlert({ title: 'Error', message: 'Health data not available: ' + err.message });
    }
  };

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this inference endpoint', render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name for this inference endpoint', filterable: true },
    { key: 'Model', label: 'Model', tooltip: 'Model used for completion and inference', filterable: true },
    { key: 'Endpoint', label: 'Endpoint', tooltip: 'URL of the inference endpoint', filterable: true },
    { key: 'ApiFormat', label: 'Format', tooltip: 'API format used by this endpoint' },
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
    return await api.enumerateCompletionEndpoints({
      MaxResults: params.maxResults,
      ContinuationToken: params.continuationToken,
      Ordering: params.ordering
    });
  }, [serverUrl, credential]);

  const handleDuplicate = (row) => {
    const { Id, GUID, CreatedUtc, ...rest } = row;
    setEditEndpoint(null);
    setInitialFormData({ ...rest, Name: (rest.Name ? rest.Name + ' (Copy)' : '') });
    setShowForm(true);
  };

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditEndpoint(row); setInitialFormData(null); setShowForm(true); } },
    { label: 'Duplicate', onClick: () => handleDuplicate(row) },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    ...(row.HealthCheckEnabled ? [{ label: 'Health Detail', onClick: () => openHealthDetail(row.Id) }] : []),
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
      setInitialFormData(null);
      setRefresh(r => r + 1);
      loadHealth();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save inference endpoint' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteCompletionEndpoint(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
      loadHealth();
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
      loadHealth();
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
        <button className="btn btn-primary" onClick={() => { setEditEndpoint(null); setInitialFormData(null); setShowForm(true); }}>Create Inference Endpoint</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <InferenceEndpointFormModal endpoint={editEndpoint} initialData={initialFormData} onSave={handleSave} onClose={() => { setShowForm(false); setEditEndpoint(null); setInitialFormData(null); }} />}
      {showJson && <JsonViewModal title="Inference Endpoint JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Inference Endpoint" message={`Are you sure you want to delete inference endpoint "${deleteTarget.Name || deleteTarget.Model}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
      <HealthDetailModal
        isOpen={healthDetailModal.isOpen}
        onClose={() => setHealthDetailModal({ isOpen: false, data: null })}
        healthData={healthDetailModal.data}
      />
    </div>
  );
}

export default InferenceEndpointsView;
