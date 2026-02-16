import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Pagination from '../components/Pagination';
import ActionMenu from '../components/ActionMenu';
import CopyableId from '../components/CopyableId';
import Tooltip from '../components/Tooltip';
import JsonViewModal from '../components/modals/JsonViewModal';
import RecordFormModal from '../components/modals/RecordFormModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function RecordsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);

  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState('');
  const [data, setData] = useState([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [loading, setLoading] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refreshState, setRefreshState] = useState('idle');
  const [showCreateRecord, setShowCreateRecord] = useState(false);
  const [documentKeyFilter, setDocumentKeyFilter] = useState('');
  const refreshTimerRef = useRef(null);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getCollections({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setCollections(items);
        if (items.length === 1) {
          const id = items[0].GUID || items[0].Id;
          if (id) setSelectedCollection(id);
        }
      } catch (err) { console.error('Failed to load collections', err); }
    })();
  }, [serverUrl, credential]);

  useEffect(() => {
    return () => { if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current); };
  }, []);

  const loadData = useCallback(async () => {
    if (!selectedCollection) { setData([]); setTotalRecords(0); return; }
    setLoading(true);
    try {
      const offset = (currentPage - 1) * pageSize;
      const result = await api.getRecords(selectedCollection, { maxResults: pageSize, continuationToken: offset > 0 ? String(offset) : null });
      if (result && result.Objects) {
        setData(result.Objects);
        setTotalRecords(result.TotalRecords || 0);
      } else if (result && Array.isArray(result)) {
        setData(result);
        setTotalRecords(result.length);
      }
    } catch (err) {
      console.error('Failed to load records', err);
    } finally {
      setLoading(false);
    }
  }, [selectedCollection, currentPage, pageSize, serverUrl, credential]);

  useEffect(() => { loadData(); }, [loadData]);

  const handleRefresh = async () => {
    if (refreshState === 'spinning') return;
    setRefreshState('spinning');
    await loadData();
    setRefreshState('done');
    refreshTimerRef.current = setTimeout(() => setRefreshState('idle'), 1500);
  };

  const handleViewJson = async (row) => {
    try {
      const full = await api.getRecord(selectedCollection, row.GUID);
      setShowJson(full);
    } catch {
      setShowJson(row);
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteRecord(selectedCollection, deleteTarget.GUID);
      setDeleteTarget(null);
      loadData();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete record' });
    }
  };

  const filteredData = documentKeyFilter
    ? data.filter(row => row.DocumentKey && row.DocumentKey === documentKeyFilter)
    : data;

  const documentKeys = [...new Set(data.map(row => row.DocumentKey).filter(Boolean))].sort();

  const columns = [
    { key: 'GUID', label: 'ID', tooltip: 'Unique identifier for this vector record', render: (row) => <CopyableId id={row.GUID} /> },
    { key: 'DocumentKey', label: 'Document Key', tooltip: 'Key linking this record to its source document' },
    { key: 'ContentLength', label: 'Content Length', tooltip: 'Length of the text content in characters' },
    { key: 'Position', label: 'Position', tooltip: 'Position or chunk index within the source document' },
    { key: 'ContentType', label: 'Content Type', tooltip: 'Type of content (e.g. Text, Code, Table)' },
    { key: 'Embeddings', label: 'Embeddings', tooltip: 'Dimensionality of the embedding vector', render: (row) => Array.isArray(row.Embeddings) ? row.Embeddings.length : '' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the record was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Records</h1>
          <p className="content-subtitle">Browse records in vector collections.</p>
        </div>
      </div>

      <div className="filter-bar">
        <label className="filter-label">
          <Tooltip text="Vector collection to browse records from">Collection:</Tooltip>
          <select
            value={selectedCollection}
            onChange={(e) => { setSelectedCollection(e.target.value); setDocumentKeyFilter(''); setCurrentPage(1); }}
          >
            <option value="">Select a collection...</option>
            {collections.map(c => <option key={c.GUID || c.Id} value={c.GUID || c.Id}>{c.Name || c.GUID || c.Id}</option>)}
          </select>
        </label>
        {selectedCollection && (
          <label className="filter-label">
            <Tooltip text="Filter records by document key">Document:</Tooltip>
            <select
              value={documentKeyFilter}
              onChange={(e) => { setDocumentKeyFilter(e.target.value); setCurrentPage(1); }}
            >
              <option value="">All Documents</option>
              {documentKeys.map(key => <option key={key} value={key}>{key}</option>)}
            </select>
          </label>
        )}
      </div>

      {selectedCollection && (
        <div className="data-table-container">
          <div className="data-table-toolbar">
            <div className="data-table-toolbar-left" />
            <div className="data-table-toolbar-right">
              <button className="btn btn-primary btn-sm" onClick={() => setShowCreateRecord(true)}>Create Record</button>
            </div>
          </div>
          <Pagination
            totalRecords={totalRecords}
            maxResults={pageSize}
            currentPage={currentPage}
            onPageChange={(page) => setCurrentPage(page)}
            onPageSizeChange={(size) => { setPageSize(size); setCurrentPage(1); }}
            onRefresh={handleRefresh}
            refreshState={refreshState}
          />
          {loading ? (
            <div className="loading"><div className="spinner" /></div>
          ) : filteredData.length === 0 ? (
            <div className="empty-state"><p>{documentKeyFilter ? 'No records match the selected document filter.' : 'No records found in this collection.'}</p></div>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  {columns.map((col) => (
                    <th key={col.key}>{col.tooltip ? <Tooltip text={col.tooltip}>{col.label}</Tooltip> : col.label}</th>
                  ))}
                  <th className="actions-cell"></th>
                </tr>
              </thead>
              <tbody>
                {filteredData.map((row, idx) => (
                  <tr key={row.GUID || idx}>
                    {columns.map((col) => (
                      <td key={col.key}>{col.render ? col.render(row) : row[col.key]}</td>
                    ))}
                    <td className="actions-cell">
                      <ActionMenu items={[
                        { label: 'View JSON', onClick: () => handleViewJson(row) },
                        { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
                      ]} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {showCreateRecord && <RecordFormModal onSave={async (data) => { await api.createRecord(selectedCollection, data); setShowCreateRecord(false); loadData(); }} onClose={() => setShowCreateRecord(false)} />}
      {showJson && <JsonViewModal title="Record JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Record" message={`Are you sure you want to delete record "${deleteTarget.GUID}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default RecordsView;
