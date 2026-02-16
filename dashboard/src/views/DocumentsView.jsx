import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import DocumentUploadModal from '../components/modals/DocumentUploadModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ProcessingLogModal from '../components/modals/ProcessingLogModal';
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
  if (s.includes('processing') || s.includes('detecting') || s.includes('chunking') || s.includes('storing') || s === 'indexing') return 'processing';
  if (s === 'pending' || s === 'queued' || s === 'uploading' || s === 'uploaded') return 'pending';
  return 'info';
}

function DocumentsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showUpload, setShowUpload] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [showLogs, setShowLogs] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [ingestionRules, setIngestionRules] = useState([]);
  const [buckets, setBuckets] = useState([]);
  const [collections, setCollections] = useState([]);
  const [bucketFilter, setBucketFilter] = useState('');
  const [collectionFilter, setCollectionFilter] = useState('');

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getIngestionRules({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setIngestionRules(items);
      } catch (err) {
        console.error('Failed to load ingestion rules', err);
      }
    })();
    (async () => {
      try {
        const result = await api.getBuckets();
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setBuckets(items);
      } catch (err) {
        console.error('Failed to load buckets', err);
      }
    })();
    (async () => {
      try {
        const result = await api.getCollections({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setCollections(items);
      } catch (err) {
        console.error('Failed to load collections', err);
      }
    })();
  }, [serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this document', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name assigned to this document', filterable: true },
    { key: 'OriginalFilename', label: 'Filename', tooltip: 'Original filename when the document was uploaded', filterable: true },
    { key: 'ContentType', label: 'Content Type', tooltip: 'MIME type of the document (e.g. application/pdf)', filterable: true },
    { key: 'SizeBytes', label: 'Size', tooltip: 'File size of the uploaded document', render: (row) => formatFileSize(row.SizeBytes) },
    { key: 'Status', label: 'Status', tooltip: 'Current processing state of the document', render: (row) => <span className={`status-badge ${getStatusBadgeClass(row.Status)}`}>{row.Status || 'Unknown'}</span> },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the document was uploaded', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    const filterParams = { ...params };
    if (bucketFilter) filterParams.bucketName = bucketFilter;
    if (collectionFilter) filterParams.collectionId = collectionFilter;
    return await api.getDocuments(filterParams);
  }, [serverUrl, credential, bucketFilter, collectionFilter]);

  const getRowActions = (row) => [
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'View Processing Logs', onClick: () => setShowLogs(row) },
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

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Documents</h1>
          <p className="content-subtitle">Upload and manage documents for knowledge bases.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowUpload(true)}>Upload Document</button>
      </div>
      <div className="filter-bar">
        <label className="filter-label">
          Bucket:
          <select value={bucketFilter} onChange={(e) => setBucketFilter(e.target.value)}>
            <option value="">All Buckets</option>
            {buckets.map((b) => (
              <option key={b.Name} value={b.Name}>{b.Name}</option>
            ))}
          </select>
        </label>
        <label className="filter-label">
          Collection:
          <select value={collectionFilter} onChange={(e) => setCollectionFilter(e.target.value)}>
            <option value="">All Collections</option>
            {collections.map((c) => (
              <option key={c.Id || c.GUID} value={c.Id || c.GUID}>{c.Name || c.Id || c.GUID}</option>
            ))}
          </select>
        </label>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {showUpload && <DocumentUploadModal ingestionRules={ingestionRules} onUpload={handleUpload} onClose={() => setShowUpload(false)} />}
      {showJson && <JsonViewModal title="Document JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {showLogs && <ProcessingLogModal api={api} documentId={showLogs.Id} onClose={() => setShowLogs(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Document" message={`Are you sure you want to delete document "${deleteTarget.Name || deleteTarget.OriginalFilename}"? This will delete the document from its bucket and remove all embeddings from its collection.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default DocumentsView;
