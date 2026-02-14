import React, { useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import CollectionFormModal from '../components/modals/CollectionFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function CollectionsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editCollection, setEditCollection] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'GUID', label: 'ID', filterable: true, render: (row) => <CopyableId id={row.GUID} /> },
    { key: 'Name', label: 'Name', filterable: true },
    { key: 'Description', label: 'Description', filterable: true, render: (row) => row.Description ? (row.Description.length > 50 ? row.Description.substring(0, 50) + '...' : row.Description) : '' },
    { key: 'Dimensionality', label: 'Dimensions' },
    { key: 'Active', label: 'Status', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
    { key: 'CreatedUtc', label: 'Created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getCollections(params);
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditCollection(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      if (editCollection) {
        await api.updateCollection(editCollection.GUID, data);
      } else {
        await api.createCollection(data);
      }
      setShowForm(false);
      setEditCollection(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save collection' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteCollection(deleteTarget.GUID);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete collection' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Collections</h1>
          <p className="content-subtitle">Manage vector collections for document storage and retrieval.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditCollection(null); setShowForm(true); }}>Create Collection</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {showForm && <CollectionFormModal collection={editCollection} onSave={handleSave} onClose={() => { setShowForm(false); setEditCollection(null); }} />}
      {showJson && <JsonViewModal title="Collection JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Collection" message={`Are you sure you want to delete collection "${deleteTarget.Name}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default CollectionsView;
