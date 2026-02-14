import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Pagination from '../components/Pagination';
import ActionMenu from '../components/ActionMenu';
import CopyableId from '../components/CopyableId';
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

  const columns = [
    { key: 'GUID', label: 'ID', render: (row) => <CopyableId id={row.GUID} /> },
    { key: 'CreatedUtc', label: 'Created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Records</h1>
          <p className="content-subtitle">Browse records in vector collections.</p>
        </div>
      </div>

      <div style={{ marginBottom: '1rem' }}>
        <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)', marginRight: '0.5rem' }}>Collection:</label>
        <select
          value={selectedCollection}
          onChange={(e) => { setSelectedCollection(e.target.value); setCurrentPage(1); }}
          style={{ padding: '0.5rem 0.75rem', border: '1px solid var(--input-border)', borderRadius: 'var(--radius-sm)', background: 'var(--input-bg)', color: 'var(--text-primary)', fontSize: '0.875rem', minWidth: '300px' }}
        >
          <option value="">Select a collection...</option>
          {collections.map(c => <option key={c.GUID || c.Id} value={c.GUID || c.Id}>{c.Name || c.GUID || c.Id}</option>)}
        </select>
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
          ) : data.length === 0 ? (
            <div className="empty-state"><p>No records found in this collection.</p></div>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  {columns.map((col) => (
                    <th key={col.key}>{col.label}</th>
                  ))}
                  <th className="actions-cell"></th>
                </tr>
              </thead>
              <tbody>
                {data.map((row, idx) => (
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
