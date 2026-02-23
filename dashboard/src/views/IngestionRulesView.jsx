import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import IngestionRuleFormModal from '../components/modals/IngestionRuleFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function IngestionRulesView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editRule, setEditRule] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [buckets, setBuckets] = useState([]);
  const [collections, setCollections] = useState([]);
  const [inferenceEndpoints, setInferenceEndpoints] = useState([]);
  const [embeddingEndpoints, setEmbeddingEndpoints] = useState([]);

  useEffect(() => {
    const loadLookups = async () => {
      try {
        const bucketsResult = await api.getBuckets({ maxResults: 1000 });
        const bucketItems = (bucketsResult && bucketsResult.Objects) ? bucketsResult.Objects : Array.isArray(bucketsResult) ? bucketsResult : [];
        setBuckets(bucketItems);

        const collectionsResult = await api.getCollections({ maxResults: 1000 });
        const collectionItems = (collectionsResult && collectionsResult.Objects) ? collectionsResult.Objects : Array.isArray(collectionsResult) ? collectionsResult : [];
        setCollections(collectionItems);

        const inferenceResult = await api.enumerateCompletionEndpoints({ maxResults: 1000 });
        const inferenceItems = (inferenceResult && inferenceResult.Objects) ? inferenceResult.Objects : Array.isArray(inferenceResult) ? inferenceResult : [];
        setInferenceEndpoints(inferenceItems);

        const embeddingResult = await api.enumerateEmbeddingEndpoints({ maxResults: 1000 });
        const embeddingItems = (embeddingResult && embeddingResult.Objects) ? embeddingResult.Objects : Array.isArray(embeddingResult) ? embeddingResult : [];
        setEmbeddingEndpoints(embeddingItems);
      } catch (err) {
        setAlert({ title: 'Error', message: err.message || 'Failed to load buckets or collections' });
      }
    };
    loadLookups();
  }, [serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this ingestion rule', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name for this ingestion rule', filterable: true },
    { key: 'Bucket', label: 'Bucket', tooltip: 'Source storage bucket that this rule monitors', filterable: true },
    { key: 'CollectionName', label: 'Collection', tooltip: 'Target vector collection for processed documents', filterable: true },
    { key: 'Summarization', label: 'Summarization', tooltip: 'Whether summarization is configured for this rule', render: (row) => row.Summarization ? 'Enabled' : 'Disabled' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the rule was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getIngestionRules(params);
  }, [serverUrl, credential]);

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditRule(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      if (editRule) {
        await api.updateIngestionRule(editRule.Id, data);
      } else {
        await api.createIngestionRule(data);
      }
      setShowForm(false);
      setEditRule(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save ingestion rule' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteIngestionRule(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete ingestion rule' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteIngestionRule(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some ingestion rules' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Ingestion Rules</h1>
          <p className="content-subtitle">Manage ingestion rules that define how documents are processed, chunked, and embedded.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditRule(null); setShowForm(true); }}>Create Ingestion Rule</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <IngestionRuleFormModal rule={editRule} buckets={buckets} collections={collections} inferenceEndpoints={inferenceEndpoints} embeddingEndpoints={embeddingEndpoints} onSave={handleSave} onClose={() => { setShowForm(false); setEditRule(null); }} />}
      {showJson && <JsonViewModal title="Ingestion Rule JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Ingestion Rule" message={`Are you sure you want to delete ingestion rule "${deleteTarget.Name}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default IngestionRulesView;
