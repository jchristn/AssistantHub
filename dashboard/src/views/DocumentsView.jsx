import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import DocumentUploadModal from '../components/modals/DocumentUploadModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ProcessingLogModal from '../components/modals/ProcessingLogModal';
import IngestionPerformanceModal from '../components/modals/IngestionPerformanceModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import DropRuleModal from '../components/DropRuleModal';
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

function canReindexDocument(row) {
  const status = (row?.Status || '').toLowerCase();
  return status === 'completed' || status === 'indexed' || status === 'active';
}

function isDocumentProcessing(row) {
  const status = (row?.Status || '').toLowerCase();
  if (!status) return false;
  const finalStates = ['completed', 'indexed', 'active', 'failed', 'error', 'typedetectionfailed'];
  return !finalStates.includes(status);
}

function DocumentsView() {
  const { serverUrl, credential, isAdmin, isGlobalAdmin, isTenantAdmin } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const requestedDocumentId = searchParams.get('documentId') || '';
  const [showUpload, setShowUpload] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [showLogs, setShowLogs] = useState(null);
  const [showPerformance, setShowPerformance] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [cancelTarget, setCancelTarget] = useState(null);
  const [reprocessBlocked, setReprocessBlocked] = useState(null);
  const [cancelling, setCancelling] = useState(false);
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

  const { records, enqueueFiles } = useUploadQueue(api);
  const prevCompletedCount = useRef(0);
  const initialTableFilters = useMemo(() => (
    requestedDocumentId ? { Id: requestedDocumentId } : {}
  ), [requestedDocumentId]);

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

  const handleReindex = async (row) => {
    try {
      const result = await api.reindexDocument(row.Id);
      setRefresh(r => r + 1);
      setAlert({
        title: result.Success ? 'Reindex Complete' : 'Reindex Failed',
        message: result.Message || `Status: ${result.Status || 'Unknown'}`
      });
    } catch (err) {
      setAlert({ title: 'Reindex Failed', message: err.message || 'Failed to reindex document' });
    }
  };

  const handleReprocess = async (row) => {
    try {
      const result = await api.reprocessDocument(row.Id);
      if (result && result.SourceAvailable === false) {
        setReprocessBlocked(row);
        return;
      }
      setRefresh(r => r + 1);
      setAlert({
        title: 'Reprocessing Started',
        message: result.Message || 'Document ingestion has been restarted.'
      });
    } catch (err) {
      setAlert({ title: 'Reprocess Failed', message: err.message || 'Failed to reprocess document' });
    }
  };

  const getRowActions = (row) => {
    const actions = [
      { label: 'View JSON', onClick: () => setShowJson(row) },
      { label: 'View Processing Logs', onClick: () => setShowLogs(row) },
      { label: 'View Ingestion Performance', onClick: () => setShowPerformance(row) },
      { label: 'Reprocess', onClick: () => handleReprocess(row) },
    ];
    if ((isAdmin || isTenantAdmin) && canReindexDocument(row)) {
      actions.push({ label: 'Reindex into Verbex', onClick: () => handleReindex(row) });
    }
    if (row.CrawlOperationId) {
      actions.push({ label: 'View Crawl Operation', onClick: () => {
        navigate(`/crawlers?op=${row.CrawlOperationId}&plan=${row.CrawlPlanId}`);
      }});
    }
    if (isDocumentProcessing(row)) {
      actions.push({ label: 'Cancel Ingestion', danger: true, onClick: () => setCancelTarget(row) });
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

  const handleCancelIngestion = async () => {
    setCancelling(true);
    try {
      await api.deleteDocument(cancelTarget.Id);
      setCancelTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to cancel ingestion' });
    } finally {
      setCancelling(false);
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      await api.deleteDocuments(ids);
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
          <p className="content-subtitle">Upload and manage documents for knowledgebases. Ingested documents are stored in RecallDB collections and indexed into Verbex for text search.</p>
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
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} initialFilters={initialTableFilters} onBulkDelete={handleBulkDelete} />
      {showUpload && <DocumentUploadModal ingestionRules={ingestionRules} onUpload={handleUpload} onClose={() => setShowUpload(false)} />}
      {showJson && <JsonViewModal title="Document JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {showLogs && <ProcessingLogModal api={api} documentId={showLogs.Id} onClose={() => setShowLogs(null)} />}
      {showPerformance && <IngestionPerformanceModal api={api} doc={showPerformance} onClose={() => setShowPerformance(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Document" message={`Are you sure you want to delete document "${deleteTarget.Name || deleteTarget.OriginalFilename}"? This will delete the document from its bucket and remove all embeddings from its collection.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {cancelTarget && <ConfirmModal title="Cancel Ingestion" message="This document will be deleted. Are you sure you wish to cancel ingestion?" confirmLabel="Cancel Ingestion" loadingLabel="Cancelling..." isLoading={cancelling} danger onConfirm={handleCancelIngestion} onClose={() => setCancelTarget(null)} />}
      {reprocessBlocked && <ConfirmModal title="Source Object Not Available" message={`The source object for "${reprocessBlocked.Name || reprocessBlocked.OriginalFilename}" is not stored and must be uploaded again before it can be reprocessed.`} confirmLabel="Upload Document" onConfirm={() => { setReprocessBlocked(null); setShowUpload(true); }} onClose={() => setReprocessBlocked(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
      {pendingDropFiles && (
        <DropRuleModal
          fileCount={pendingDropFiles.length}
          ingestionRules={ingestionRules}
          onConfirm={handleDropConfirm}
          onClose={() => setPendingDropFiles(null)}
        />
      )}
    </div>
  );
}

export default DocumentsView;
