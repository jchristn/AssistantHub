import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import BucketFormModal from '../components/modals/BucketFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function BucketsView() {
  const navigate = useNavigate();
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'Name', label: 'Name', tooltip: 'Name of the S3-compatible storage bucket', filterable: true },
    { key: 'CreationDate', label: 'Created', tooltip: 'Date and time the bucket was created', render: (row) => row.CreationDate ? new Date(row.CreationDate).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getBuckets(params);
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'View Objects', onClick: () => navigate('/objects', { state: { bucket: row.Name } }) },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      await api.createBucket(data);
      setShowForm(false);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to create bucket' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteBucket(deleteTarget.Name);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete bucket' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Buckets</h1>
          <p className="content-subtitle">Manage S3-compatible storage buckets.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>Create Bucket</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {showForm && <BucketFormModal onSave={handleSave} onClose={() => setShowForm(false)} />}
      {showJson && <JsonViewModal title="Bucket JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Bucket" message={`Are you sure you want to delete bucket "${deleteTarget.Name}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default BucketsView;
