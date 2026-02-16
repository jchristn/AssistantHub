import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import Tooltip from '../components/Tooltip';
import CopyableId from '../components/CopyableId';
import HistoryViewModal from '../components/modals/HistoryViewModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function HistoryView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [viewHistory, setViewHistory] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [assistantFilter, setAssistantFilter] = useState('');
  const [threadFilter, setThreadFilter] = useState('');
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
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this history entry', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'ThreadId', label: 'Thread ID', tooltip: 'Conversation thread identifier', render: (row) => <CopyableId id={row.ThreadId} /> },
    { key: 'AssistantId', label: 'Assistant ID', tooltip: 'The assistant for this conversation', render: (row) => <CopyableId id={row.AssistantId} /> },
    { key: 'UserMessage', label: 'User Message', tooltip: 'The message the user sent', filterable: true, render: (row) => row.UserMessage ? (row.UserMessage.length > 80 ? row.UserMessage.substring(0, 80) + '...' : row.UserMessage) : '' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the entry was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    const queryParams = { ...params };
    if (assistantFilter) queryParams.assistantId = assistantFilter;
    if (threadFilter) queryParams.threadId = threadFilter;
    return await api.getHistoryList(queryParams);
  }, [serverUrl, credential, assistantFilter, threadFilter]);

  const getRowActions = (row) => [
    { label: 'View', onClick: () => setViewHistory(row) },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleDelete = async () => {
    try {
      await api.deleteHistory(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete history entry' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteHistory(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some history entries' });
    }
  };

  const handleAssistantFilterChange = (e) => {
    setAssistantFilter(e.target.value);
    setRefresh(r => r + 1);
  };

  const handleThreadFilterChange = (e) => {
    setThreadFilter(e.target.value);
    setRefresh(r => r + 1);
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">History</h1>
          <p className="content-subtitle">View chat conversation history with timing metrics.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)' }}><Tooltip text="Filter history by a specific assistant">Assistant:</Tooltip></label>
          <select
            value={assistantFilter}
            onChange={handleAssistantFilterChange}
            style={{
              padding: '0.5rem 0.75rem',
              border: '1px solid var(--input-border)',
              borderRadius: 'var(--radius-sm)',
              background: 'var(--input-bg)',
              color: 'var(--text-primary)',
              fontSize: '0.875rem',
              minWidth: '220px',
            }}
          >
            <option value="">All Assistants</option>
            {assistants.map(a => (
              <option key={a.Id} value={a.Id}>
                {a.Name} ({a.Id.substring(0, 8)}...)
              </option>
            ))}
          </select>
          <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)' }}><Tooltip text="Filter by thread ID">Thread:</Tooltip></label>
          <input
            type="text"
            placeholder="Thread ID..."
            value={threadFilter}
            onChange={handleThreadFilterChange}
            style={{
              padding: '0.5rem 0.75rem',
              border: '1px solid var(--input-border)',
              borderRadius: 'var(--radius-sm)',
              background: 'var(--input-bg)',
              color: 'var(--text-primary)',
              fontSize: '0.875rem',
              minWidth: '200px',
            }}
          />
        </div>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {viewHistory && <HistoryViewModal history={viewHistory} onClose={() => setViewHistory(null)} />}
      {showJson && <JsonViewModal title="History JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete History" message="Are you sure you want to delete this history entry? This action cannot be undone." confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default HistoryView;
