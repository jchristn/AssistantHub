import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import DocumentUploadModal from '../components/modals/DocumentUploadModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function formatFileSize(bytes) {
  if (bytes == null) return '';
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

function getStatusBadgeClass(status) {
  if (!status) return 'info';
  const s = status.toLowerCase();
  if (s === 'completed' || s === 'indexed' || s === 'active') return 'active';
  if (s === 'failed' || s === 'error') return 'failed';
  if (s === 'processing' || s === 'indexing') return 'processing';
  if (s === 'pending' || s === 'queued') return 'pending';
  return 'info';
}

function DocumentsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showUpload, setShowUpload] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [assistantFilter, setAssistantFilter] = useState('');
  const [assistants, setAssistants] = useState([]);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getAssistants({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setAssistants(items);
        if (items.length === 1) {
          setAssistantFilter(items[0].Id);
        }
      } catch (err) {
        console.error('Failed to load assistants', err);
      }
    })();
  }, [serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', filterable: true },
    { key: 'AssistantId', label: 'Assistant ID', render: (row) => <CopyableId id={row.AssistantId} /> },
    { key: 'OriginalFilename', label: 'Filename', filterable: true },
    { key: 'ContentType', label: 'Content Type', filterable: true },
    { key: 'SizeBytes', label: 'Size', render: (row) => formatFileSize(row.SizeBytes) },
    { key: 'Status', label: 'Status', render: (row) => <span className={`status-badge ${getStatusBadgeClass(row.Status)}`}>{row.Status || 'Unknown'}</span> },
    { key: 'CreatedUtc', label: 'Created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    const queryParams = { ...params };
    if (assistantFilter) {
      queryParams.assistantId = assistantFilter;
    }
    return await api.getDocuments(queryParams);
  }, [serverUrl, credential, assistantFilter]);

  const getRowActions = (row) => [
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleUpload = async (docData) => {
    try {
      const { file, ...metadata } = docData;

      const reader = new FileReader();
      const base64Content = await new Promise((resolve, reject) => {
        reader.onload = () => {
          const base64 = reader.result.split(',')[1];
          resolve(base64);
        };
        reader.onerror = reject;
        reader.readAsDataURL(file);
      });

      const uploadPayload = {
        ...metadata,
        Base64Content: base64Content,
      };

      const result = await api.uploadDocument(uploadPayload);
      if (result && result.statusCode && result.statusCode >= 400) {
        throw new Error(result.ErrorMessage || 'Upload failed');
      }
      setShowUpload(false);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Upload Error', message: err.message || 'Failed to upload document' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteDocument(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete document' });
    }
  };

  const handleFilterChange = (e) => {
    setAssistantFilter(e.target.value);
    setRefresh(r => r + 1);
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Documents</h1>
          <p className="content-subtitle">Upload and manage documents for assistant knowledge bases.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <select
            value={assistantFilter}
            onChange={handleFilterChange}
            style={{
              padding: '0.5rem 0.75rem',
              border: '1px solid var(--input-border)',
              borderRadius: 'var(--radius-sm)',
              background: 'var(--input-bg)',
              color: 'var(--text-primary)',
              fontSize: '0.875rem',
              minWidth: '280px',
            }}
          >
            <option value="">All Assistants</option>
            {assistants.map(a => (
              <option key={a.Id} value={a.Id}>
                {a.Name} ({a.Id.substring(0, 8)}...)
              </option>
            ))}
          </select>
          <button className="btn btn-primary" onClick={() => setShowUpload(true)}>Upload Document</button>
        </div>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {showUpload && <DocumentUploadModal assistantId={assistantFilter} onUpload={handleUpload} onClose={() => setShowUpload(false)} />}
      {showJson && <JsonViewModal title="Document JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Document" message={`Are you sure you want to delete document "${deleteTarget.Name || deleteTarget.OriginalFilename}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default DocumentsView;
