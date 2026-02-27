import React, { useState, useEffect } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import ActionMenu from '../ActionMenu';
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

function OperationStatsModal({ operation, onClose }) {
  const op = operation;
  const runtimeMs = (op.StartUtc && op.FinishUtc)
    ? new Date(op.FinishUtc) - new Date(op.StartUtc)
    : null;

  const rows = [
    { label: 'Objects Enumerated', count: op.ObjectsEnumerated, bytes: op.BytesEnumerated },
    { label: 'Objects Added', count: op.ObjectsAdded, bytes: op.BytesAdded },
    { label: 'Objects Updated', count: op.ObjectsUpdated, bytes: op.BytesUpdated },
    { label: 'Objects Deleted', count: op.ObjectsDeleted, bytes: op.BytesDeleted },
    { label: 'Objects Succeeded', count: op.ObjectsSuccess, bytes: op.BytesSuccess },
    { label: 'Objects Failed', count: op.ObjectsFailed, bytes: op.BytesFailed },
  ];

  return (
    <Modal title="Operation Statistics" onClose={onClose} wide footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      <table className="data-table" style={{ fontSize: '0.8125rem' }}>
        <thead>
          <tr>
            <th>Metric</th>
            <th style={{ textAlign: 'right' }}>Count</th>
            <th style={{ textAlign: 'right' }}>Size</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i}>
              <td>{row.label}</td>
              <td style={{ textAlign: 'right' }}>{row.count ?? 0}</td>
              <td style={{ textAlign: 'right' }}>{formatFileSize(row.bytes ?? 0)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div style={{ marginTop: '1rem', fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
          <span>Start</span>
          <span>{op.StartUtc ? new Date(op.StartUtc).toLocaleString() : '-'}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
          <span>End</span>
          <span>{op.FinishUtc ? new Date(op.FinishUtc).toLocaleString() : '-'}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
          <span>Total Runtime</span>
          <span>{runtimeMs != null ? `${runtimeMs.toFixed(2)}ms` : '-'}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
          <span>State</span>
          <span>{op.State || '-'}</span>
        </div>
        {op.StatusMessage && (
          <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
            <span>Message</span>
            <span>{op.StatusMessage}</span>
          </div>
        )}
      </div>
    </Modal>
  );
}

function CrawlOperationsModal({ api, plan, onClose }) {
  const planId = plan.Id || plan.GUID;
  const [operations, setOperations] = useState([]);
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [showEnumeration, setShowEnumeration] = useState(null);
  const [showStats, setShowStats] = useState(null);

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
    { label: 'Total Operations', value: (statistics.SuccessfulRunCount ?? 0) + (statistics.FailedRunCount ?? 0) },
    { label: 'Successful', value: statistics.SuccessfulRunCount ?? '-' },
    { label: 'Failed', value: statistics.FailedRunCount ?? '-' },
    { label: 'Total Objects', value: statistics.ObjectCount ?? '-' },
    { label: 'Total Bytes', value: formatFileSize(statistics.BytesCrawled) },
    { label: 'Avg Duration', value: statistics.AvgRuntimeMs ? `${(statistics.AvgRuntimeMs / 1000).toFixed(1)}s` : '-' },
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
                    <td className="actions-cell">
                      <ActionMenu items={[
                        { label: 'Statistics', onClick: () => setShowStats(op) },
                        { label: 'Enumeration', onClick: () => setShowEnumeration({ planId, operationId: op.Id || op.GUID }) },
                        { label: 'Delete', danger: true, onClick: () => setDeleteTarget(op) },
                      ]} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {showStats && (
        <OperationStatsModal
          operation={showStats}
          onClose={() => setShowStats(null)}
        />
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
