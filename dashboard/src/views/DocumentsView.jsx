import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import DocumentUploadModal from '../components/modals/DocumentUploadModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ProcessingLogModal from '../components/modals/ProcessingLogModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import DropRuleModal from '../components/DropRuleModal';
import UploadProgressPanel from '../components/UploadProgressPanel';
import { useUploadQueue } from '../hooks/useUploadQueue';
import { extractFilesFromDrop } from '../utils/fileDropUtils';
import Tooltip from '../components/Tooltip';

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
  const { serverUrl, credential, isGlobalAdmin } = useAuth();
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
  const [crawlerFilter, setCrawlerFilter] = useState('');
  const [crawlPlans, setCrawlPlans] = useState([]);

  const [isDragOver, setIsDragOver] = useState(false);
  const [pendingDropFiles, setPendingDropFiles] = useState(null);
  const dragCounter = useRef(0);

  const { records, enqueueFiles, dismissRecord } = useUploadQueue(api);
  const prevCompletedCount = useRef(0);

  // Auto-refresh table when uploads complete
  useEffect(() => {
    const completedCount = records.filter(r => {
      const s = (r.status || '').toLowerCase();
      return s === 'completed' || s === 'indexed' || s === 'active';
    }).length;
    if (completedCount > prevCompletedCount.current) {
      setRefresh(r => r + 1);
    }
    prevCompletedCount.current = completedCount;
  }, [records]);

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
    (async () => {
      try {
        const result = await api.getCrawlPlans({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setCrawlPlans(items);
      } catch (err) {
        console.error('Failed to load crawl plans', err);
      }
    })();
  }, [serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this document', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    ...(isGlobalAdmin ? [{ key: 'TenantId', label: 'Tenant', tooltip: 'Owning tenant ID', filterable: true, render: (row) => <CopyableId id={row.TenantId} /> }] : []),
    { key: 'OriginalFilename', label: 'Filename', tooltip: 'Original filename when the document was uploaded', filterable: true, render: (row) => {
      const name = row.OriginalFilename || '';
      return name.length > 40 ? <span title={name}>{name.slice(0, 37)}...</span> : name;
    }},
    { key: 'ContentType', label: 'Content Type', tooltip: 'MIME type of the document (e.g. application/pdf)', filterable: true },
    { key: 'SizeBytes', label: 'Size', tooltip: 'File size of the uploaded document', render: (row) => formatFileSize(row.SizeBytes) },
    { key: 'Status', label: 'Status', tooltip: 'Current processing state of the document', render: (row) => (
      <>
        <span className={`status-badge ${getStatusBadgeClass(row.Status)}`}>{row.Status || 'Unknown'}</span>
        {row.CrawlPlanId && <span className="status-badge badge-crawled" style={{ marginLeft: '0.25rem' }}>Crawled</span>}
      </>
    )},
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the document was uploaded', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    const filterParams = { ...params };
    if (bucketFilter) filterParams.bucketName = bucketFilter;
    if (collectionFilter) filterParams.collectionId = collectionFilter;
    if (crawlerFilter) filterParams.crawlPlanId = crawlerFilter;
    return await api.getDocuments(filterParams);
  }, [serverUrl, credential, bucketFilter, collectionFilter, crawlerFilter]);

  const getRowActions = (row) => {
    const actions = [
      { label: 'View JSON', onClick: () => setShowJson(row) },
      { label: 'View Processing Logs', onClick: () => setShowLogs(row) },
    ];
    if (row.CrawlOperationId) {
      actions.push({ label: 'View Crawl Operation', onClick: () => {
        window.open(`#/crawlers?op=${row.CrawlOperationId}&plan=${row.CrawlPlanId}`, '_self');
      }});
    }
    actions.push({ label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) });
    return actions;
  };

  const handleUpload = (docData) => {
    const { file, IngestionRuleId, Labels, Tags } = docData;
    enqueueFiles([file], IngestionRuleId, Labels || [], Tags || {});
    setShowUpload(false);
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

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteDocument(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some documents' });
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDragEnter = (e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounter.current++;
    if (dragCounter.current === 1) {
      setIsDragOver(true);
    }
  };

  const handleDragLeave = (e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounter.current--;
    if (dragCounter.current === 0) {
      setIsDragOver(false);
    }
  };

  const handleDrop = async (e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounter.current = 0;
    setIsDragOver(false);
    const files = await extractFilesFromDrop(e.dataTransfer);
    if (files.length > 0) {
      setPendingDropFiles(files);
    }
  };

  const handleDropConfirm = (ruleId, labels, tags) => {
    if (pendingDropFiles) {
      enqueueFiles(pendingDropFiles, ruleId, labels, tags);
    }
    setPendingDropFiles(null);
  };

  return (
    <div
      className={`documents-view ${isDragOver ? 'drag-over' : ''}`}
      onDragOver={handleDragOver}
      onDragEnter={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <div className="content-header">
        <div>
          <h1 className="content-title">Documents</h1>
          <p className="content-subtitle">Upload and manage documents for knowledgebases.  Ingested documents are stored within RecallDB collections and optionally stored in S3 buckets.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowUpload(true)}>Upload Document</button>
      </div>
      <div className="filter-bar">
        <label className="filter-label">
          <Tooltip text="Filter documents by storage bucket">Bucket:</Tooltip>
          <select value={bucketFilter} onChange={(e) => setBucketFilter(e.target.value)}>
            <option value="">All Buckets</option>
            {buckets.map((b) => (
              <option key={b.Name} value={b.Name}>{b.Name}</option>
            ))}
          </select>
        </label>
        <label className="filter-label">
          <Tooltip text="Filter documents by vector collection">Collection:</Tooltip>
          <select value={collectionFilter} onChange={(e) => setCollectionFilter(e.target.value)}>
            <option value="">All Collections</option>
            {collections.map((c) => (
              <option key={c.Id || c.GUID} value={c.Id || c.GUID}>{c.Name || c.Id || c.GUID}</option>
            ))}
          </select>
        </label>
        <label className="filter-label">
          <Tooltip text="Filter documents by crawl plan">Crawler:</Tooltip>
          <select value={crawlerFilter} onChange={(e) => setCrawlerFilter(e.target.value)}>
            <option value="">All Sources</option>
            {crawlPlans.map((cp) => (
              <option key={cp.Id || cp.GUID} value={cp.Id || cp.GUID}>{cp.Name || cp.Id || cp.GUID}</option>
            ))}
          </select>
        </label>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showUpload && <DocumentUploadModal ingestionRules={ingestionRules} onUpload={handleUpload} onClose={() => setShowUpload(false)} />}
      {showJson && <JsonViewModal title="Document JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {showLogs && <ProcessingLogModal api={api} documentId={showLogs.Id} onClose={() => setShowLogs(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Document" message={`Are you sure you want to delete document "${deleteTarget.Name || deleteTarget.OriginalFilename}"? This will delete the document from its bucket and remove all embeddings from its collection.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
      {pendingDropFiles && (
        <DropRuleModal
          fileCount={pendingDropFiles.length}
          ingestionRules={ingestionRules}
          onConfirm={handleDropConfirm}
          onClose={() => setPendingDropFiles(null)}
        />
      )}
      <UploadProgressPanel records={records} onDismiss={dismissRecord} />
    </div>
  );
}

export default DocumentsView;
