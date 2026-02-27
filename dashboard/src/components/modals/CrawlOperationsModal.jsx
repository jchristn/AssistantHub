import React, { useState, useEffect } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import CrawlEnumerationModal from './CrawlEnumerationModal';
import ConfirmModal from '../ConfirmModal';
import AlertModal from '../AlertModal';

function formatFileSize(bytes) {
  if (bytes == null) return '';
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
}

function CrawlOperationsModal({ api, plan, onClose }) {
  const planId = plan.Id || plan.GUID;
  const [operations, setOperations] = useState([]);
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [showEnumeration, setShowEnumeration] = useState(null);

  const loadData = async () => {
    setLoading(true);
    try {
      const [opsResult, statsResult] = await Promise.all([
        api.getCrawlOperations(planId, { maxResults: 1000 }),
        api.getCrawlOperationStatistics(planId).catch(() => null),
      ]);
      const ops = (opsResult && opsResult.Objects) ? opsResult.Objects : Array.isArray(opsResult) ? opsResult : [];
      setOperations(ops);
      setStatistics(statsResult);
    } catch (err) {
      console.error('Failed to load operations', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadData(); }, [planId]);

  const handleDelete = async () => {
    try {
      await api.deleteCrawlOperation(planId, deleteTarget.Id || deleteTarget.GUID);
      setDeleteTarget(null);
      loadData();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete operation' });
    }
  };

  const statCards = statistics ? [
    { label: 'Total Operations', value: statistics.TotalOperations ?? statistics.Total ?? '-' },
    { label: 'Successful', value: statistics.Successful ?? statistics.SuccessCount ?? '-' },
    { label: 'Failed', value: statistics.Failed ?? statistics.FailedCount ?? '-' },
    { label: 'Total Objects', value: statistics.TotalObjects ?? statistics.ObjectCount ?? '-' },
    { label: 'Total Bytes', value: formatFileSize(statistics.TotalBytes ?? statistics.ByteCount) },
    { label: 'Avg Duration', value: statistics.AverageDurationMs ? `${(statistics.AverageDurationMs / 1000).toFixed(1)}s` : '-' },
  ] : [];

  return (
    <Modal title={`Operations - ${plan.Name || planId}`} onClose={onClose} extraWide footer={
      <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end', width: '100%' }}>
        <button className="btn btn-secondary" onClick={loadData} disabled={loading}>Refresh</button>
        <button className="btn btn-secondary" onClick={onClose}>Close</button>
      </div>
    }>
      {/* Statistics */}
      {statistics && (
        <div className="stats-grid" style={{ marginBottom: '1.5rem' }}>
          {statCards.map((card, i) => (
            <div key={i} className="stat-card">
              <div className="stat-card-label">{card.label}</div>
              <div className="stat-card-value">{card.value}</div>
            </div>
          ))}
        </div>
      )}

      {/* Operations Table */}
      {loading ? (
        <div className="loading"><div className="spinner" /></div>
      ) : operations.length === 0 ? (
        <div className="empty-state"><p>No operations found.</p></div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Start</th>
                <th>End</th>
                <th>Status</th>
                <th>Objects</th>
                <th>Bytes</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {operations.map((op, idx) => {
                const status = op.Status || op.State || 'Unknown';
                const statusCls = status.toLowerCase() === 'success' || status.toLowerCase() === 'completed' ? 'active'
                  : status.toLowerCase() === 'failed' || status.toLowerCase() === 'error' ? 'failed'
                  : status.toLowerCase() === 'running' || status.toLowerCase() === 'processing' ? 'processing'
                  : 'info';
                return (
                  <tr key={op.Id || op.GUID || idx}>
                    <td><CopyableId id={op.Id || op.GUID} /></td>
                    <td>{op.StartUtc ? new Date(op.StartUtc).toLocaleString() : ''}</td>
                    <td>{op.FinishUtc ? new Date(op.FinishUtc).toLocaleString() : ''}</td>
                    <td><span className={`status-badge ${statusCls}`}>{status}</span></td>
                    <td>{op.ObjectsEnumerated ?? ''}</td>
                    <td>{formatFileSize(op.BytesEnumerated)}</td>
                    <td>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button className="btn btn-secondary" style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem' }}
                          onClick={() => setShowEnumeration({ planId, operationId: op.Id || op.GUID })}>
                          Enumeration
                        </button>
                        <button className="btn btn-danger" style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem' }}
                          onClick={() => setDeleteTarget(op)}>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {showEnumeration && (
        <CrawlEnumerationModal
          api={api}
          planId={showEnumeration.planId}
          operationId={showEnumeration.operationId}
          onClose={() => setShowEnumeration(null)}
        />
      )}
      {deleteTarget && (
        <ConfirmModal
          title="Delete Operation"
          message={`Are you sure you want to delete operation "${deleteTarget.Id || deleteTarget.GUID}"?`}
          confirmLabel="Delete"
          danger
          onConfirm={handleDelete}
          onClose={() => setDeleteTarget(null)}
        />
      )}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </Modal>
  );
}

export default CrawlOperationsModal;
