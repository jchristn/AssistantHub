import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Modal from '../components/Modal';
import AlertModal from '../components/AlertModal';
import ConfirmModal from '../components/ConfirmModal';

function formatBytes(bytes) {
  if (bytes === 0) return '—';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return (bytes / Math.pow(1024, i)).toFixed(1) + ' ' + units[i];
}

function formatDate(dateStr) {
  if (!dateStr) return '—';
  return new Date(dateStr).toLocaleString();
}

function ModelsView() {
  const { serverUrl, credential, isAdmin } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [models, setModels] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showPull, setShowPull] = useState(false);
  const [pullName, setPullName] = useState('');
  const [pulling, setPulling] = useState(false);
  const [pullProgress, setPullProgress] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refreshState, setRefreshState] = useState('idle');
  const refreshTimerRef = useRef(null);

  const loadModels = useCallback(async () => {
    setLoading(true);
    try {
      const result = await api.getModels();
      if (Array.isArray(result)) {
        setModels(result);
      } else {
        setModels([]);
      }
    } catch (err) {
      console.error('Failed to load models:', err);
    } finally {
      setLoading(false);
    }
  }, [serverUrl, credential]);

  useEffect(() => { loadModels(); }, [loadModels]);

  useEffect(() => {
    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    };
  }, []);

  const handleRefresh = async () => {
    if (refreshState === 'spinning') return;
    setRefreshState('spinning');
    await loadModels();
    setRefreshState('done');
    refreshTimerRef.current = setTimeout(() => setRefreshState('idle'), 1500);
  };

  const pollIntervalRef = useRef(null);

  const stopPolling = () => {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
      pollIntervalRef.current = null;
    }
  };

  const startPolling = () => {
    stopPolling();
    pollIntervalRef.current = setInterval(async () => {
      try {
        const status = await api.getPullStatus();
        if (status.statusCode === 404) {
          stopPolling();
          setPulling(false);
          setPullProgress(null);
          setShowPull(false);
          setPullName('');
          loadModels();
          return;
        }
        setPullProgress(status);
        if (status.HasError) {
          stopPolling();
          setPulling(false);
          setPullProgress(null);
          setShowPull(false);
          setPullName('');
          setAlert({ title: 'Pull Failed', message: status.ErrorMessage || 'Failed to pull model.' });
        } else if (status.IsComplete) {
          stopPolling();
          setPulling(false);
          setPullProgress(null);
          setShowPull(false);
          setPullName('');
          loadModels();
        }
      } catch {
        // ignore transient poll errors
      }
    }, 1000);
  };

  useEffect(() => {
    return () => stopPolling();
  }, []);

  const handlePull = async () => {
    if (!pullName.trim()) return;
    setPulling(true);
    setPullProgress({ Status: 'starting', PercentComplete: 0, TotalBytes: 0, CompletedBytes: 0 });

    try {
      const result = await api.pullModel(pullName.trim());
      if (!result.ok) {
        setPulling(false);
        setPullProgress(null);
        setAlert({ title: 'Pull Failed', message: `Server returned status ${result.statusCode}` });
        return;
      }
      startPolling();
    } catch (err) {
      setPulling(false);
      setPullProgress(null);
      setAlert({ title: 'Error', message: err.message || 'Failed to pull model.' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteModel(deleteTarget.Name);
      setDeleteTarget(null);
      loadModels();
    } catch (err) {
      setDeleteTarget(null);
      setAlert({ title: 'Delete Failed', message: err.message || 'Failed to delete model.' });
    }
  };

  const pullSupported = models.length > 0 ? models[0].PullSupported : true;

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Models</h1>
          <p className="content-subtitle">View and manage available inference models.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <button
            className={`refresh-btn ${refreshState}`}
            onClick={handleRefresh}
            disabled={refreshState === 'spinning'}
            title="Refresh"
          >
            {refreshState === 'done' ? (
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="3,8 7,12 13,4" />
              </svg>
            ) : (
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M14 8A6 6 0 1 1 10 2.5" />
                <polyline points="14,2 14,6 10,6" />
              </svg>
            )}
          </button>
          {isAdmin && pullSupported && (
            <button className="btn btn-primary" onClick={() => setShowPull(true)}>Pull Model</button>
          )}
        </div>
      </div>
      <div className="data-table-container">
        {loading ? (
          <div className="loading"><div className="spinner" /></div>
        ) : models.length === 0 ? (
          <div className="empty-state"><p>No models found.</p></div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Size</th>
                <th>Modified</th>
                <th>Owned By</th>
                {isAdmin && pullSupported && <th>Actions</th>}
              </tr>
            </thead>
            <tbody>
              {models.map((model, idx) => (
                <tr key={model.Name || idx}>
                  <td>{model.Name}</td>
                  <td>{formatBytes(model.SizeBytes)}</td>
                  <td>{formatDate(model.ModifiedUtc)}</td>
                  <td>{model.OwnedBy || '—'}</td>
                  {isAdmin && pullSupported && (
                    <td>
                      <button className="btn btn-danger btn-sm" onClick={() => setDeleteTarget(model)}>Delete</button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      {showPull && (
        <Modal title="Pull Model" onClose={() => { stopPolling(); setPulling(false); setPullProgress(null); setShowPull(false); setPullName(''); }} footer={
          pulling ? (
            <button className="btn btn-secondary" onClick={() => { stopPolling(); setPulling(false); setPullProgress(null); setShowPull(false); setPullName(''); }}>Close</button>
          ) : (
            <>
              <button className="btn btn-secondary" onClick={() => { setShowPull(false); setPullName(''); }}>Cancel</button>
              <button className="btn btn-primary" onClick={handlePull} disabled={!pullName.trim()}>
                Pull
              </button>
            </>
          )
        }>
          {pulling && pullProgress ? (
            <div>
              <p style={{ marginBottom: '0.25rem', fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Progress</p>
              <p style={{ marginBottom: '0.5rem', fontFamily: 'monospace', fontSize: '0.875rem' }}>{pullProgress.Status || 'Starting...'}</p>
              {pullProgress.TotalBytes > 0 && (
                <p style={{ marginBottom: '0.5rem', fontFamily: 'monospace', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                  {formatBytes(pullProgress.CompletedBytes)} / {formatBytes(pullProgress.TotalBytes)}
                </p>
              )}
              <div style={{ width: '100%', height: '8px', backgroundColor: 'var(--border-color, #e0e0e0)', borderRadius: '4px', overflow: 'hidden' }}>
                <div style={{
                  width: `${pullProgress.PercentComplete || 0}%`,
                  height: '100%',
                  backgroundColor: 'var(--primary-color, #4f46e5)',
                  borderRadius: '4px',
                  transition: 'width 0.3s ease'
                }} />
              </div>
              <p style={{ marginTop: '0.5rem', fontFamily: 'monospace', fontSize: '0.875rem', color: 'var(--text-secondary)', textAlign: 'right' }}>
                {pullProgress.PercentComplete || 0}%
              </p>
              <p style={{ marginTop: '0.75rem', fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
                This pull operation will continue in the background if you close this window.
              </p>
            </div>
          ) : (
            <div className="form-group">
              <label className="form-label">Model Name</label>
              <input
                type="text"
                className="form-input"
                placeholder="e.g. gemma3:4b"
                value={pullName}
                onChange={(e) => setPullName(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter' && pullName.trim()) handlePull(); }}
              />
            </div>
          )}
        </Modal>
      )}
      {deleteTarget && (
        <ConfirmModal
          title="Delete Model"
          message={`Are you sure you want to delete model "${deleteTarget.Name}"? This cannot be undone.`}
          confirmLabel="Delete"
          danger
          onConfirm={handleDelete}
          onClose={() => setDeleteTarget(null)}
        />
      )}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default ModelsView;
