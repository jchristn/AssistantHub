import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import Tooltip from '../components/Tooltip';
import CopyableId from '../components/CopyableId';
import FeedbackViewModal from '../components/modals/FeedbackViewModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function FeedbackView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [viewFeedback, setViewFeedback] = useState(null);
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
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this feedback entry', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'AssistantId', label: 'Assistant ID', tooltip: 'The assistant that received this feedback', render: (row) => <CopyableId id={row.AssistantId} /> },
    { key: 'Rating', label: 'Rating', tooltip: "User's thumbs up or thumbs down rating", render: (row) => row.Rating === 'ThumbsUp' ? '\uD83D\uDC4D' : '\uD83D\uDC4E' },
    { key: 'UserMessage', label: 'User Message', tooltip: 'The message the user sent before giving feedback', filterable: true, render: (row) => row.UserMessage ? (row.UserMessage.length > 80 ? row.UserMessage.substring(0, 80) + '...' : row.UserMessage) : '' },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the feedback was submitted', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchData = useCallback(async (params) => {
    const queryParams = { ...params };
    if (assistantFilter) {
      queryParams.assistantId = assistantFilter;
    }
    return await api.getFeedbackList(queryParams);
  }, [serverUrl, credential, assistantFilter]);

  const getRowActions = (row) => [
    { label: 'View', onClick: () => setViewFeedback(row) },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleDelete = async () => {
    try {
      await api.deleteFeedback(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete feedback' });
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
          <h1 className="content-title">Feedback</h1>
          <p className="content-subtitle">Review user feedback and ratings for assistant conversations.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)' }}><Tooltip text="Filter feedback by a specific assistant">Assistant:</Tooltip></label>
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
        </div>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {viewFeedback && <FeedbackViewModal feedback={viewFeedback} onClose={() => setViewFeedback(null)} />}
      {showJson && <JsonViewModal title="Feedback JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Feedback" message="Are you sure you want to delete this feedback entry? This action cannot be undone." confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default FeedbackView;
