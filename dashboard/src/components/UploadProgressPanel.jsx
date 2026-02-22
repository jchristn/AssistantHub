import React, { useState } from 'react';

function UploadProgressPanel({ records, onDismiss }) {
  const [collapsed, setCollapsed] = useState(false);

  if (!records || records.length === 0) return null;

  const isError = (r) => {
    if (!r.status) return false;
    const s = r.status.toLowerCase();
    return s === 'failed' || s === 'error';
  };

  const isComplete = (r) => {
    if (!r.status) return false;
    const s = r.status.toLowerCase();
    return s === 'completed' || s === 'indexed' || s === 'active';
  };

  const activeCount = records.filter(r => !isComplete(r) && !isError(r)).length;
  const completedCount = records.filter(r => isComplete(r)).length;
  const failedCount = records.filter(r => isError(r)).length;

  let summary = `${records.length} file${records.length !== 1 ? 's' : ''}`;
  const parts = [];
  if (activeCount > 0) parts.push(`${activeCount} active`);
  if (completedCount > 0) parts.push(`${completedCount} done`);
  if (failedCount > 0) parts.push(`${failedCount} failed`);
  if (parts.length > 0) summary += ` (${parts.join(', ')})`;

  return (
    <div className={`upload-progress-panel ${collapsed ? 'collapsed' : ''}`}>
      <div className="upload-progress-header" onClick={() => setCollapsed(!collapsed)}>
        <span className="upload-progress-title">Ingestion Progress — {summary}</span>
        <button className="upload-progress-toggle" aria-label={collapsed ? 'Expand' : 'Collapse'}>
          {collapsed ? '\u25B2' : '\u25BC'}
        </button>
      </div>
      {!collapsed && (
        <div className="upload-progress-body">
          <div className="upload-progress-table-header">
            <span className="upload-progress-col-name">File</span>
            <span className="upload-progress-col-step">Step</span>
            <span className="upload-progress-col-progress">Progress</span>
            <span className="upload-progress-col-action"></span>
          </div>
          <div className="upload-progress-list">
            {records.map(r => (
              <div key={r.id} className={`upload-progress-item ${isError(r) ? 'error' : isComplete(r) ? 'complete' : ''}`}>
                <span className="upload-progress-col-name upload-progress-filename" title={r.fileName}>{r.fileName}</span>
                <span className={`upload-progress-col-step upload-progress-step ${isError(r) ? 'step-error' : isComplete(r) ? 'step-complete' : 'step-active'}`}>
                  {r.stepLabel || r.status || ''}
                </span>
                <span className="upload-progress-col-progress">
                  <div className="upload-progress-bar-track">
                    <div
                      className={`upload-progress-bar-fill ${isError(r) ? 'error' : isComplete(r) ? 'complete' : ''}`}
                      style={{ width: `${r.percentage}%` }}
                    />
                  </div>
                  <span className="upload-progress-percent">{r.percentage}%</span>
                </span>
                <span className="upload-progress-col-action">
                  <button className="upload-progress-dismiss" onClick={() => onDismiss(r.id)} title="Dismiss">&times;</button>
                </span>
                {isError(r) && r.error && (
                  <div className="upload-progress-error">{r.error}</div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default UploadProgressPanel;
