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

  return (
    <div className={`upload-progress-panel ${collapsed ? 'collapsed' : ''}`}>
      <div className="upload-progress-header" onClick={() => setCollapsed(!collapsed)}>
        <span className="upload-progress-title">Uploads ({records.length})</span>
        <button className="upload-progress-toggle" aria-label={collapsed ? 'Expand' : 'Collapse'}>
          {collapsed ? '\u25B2' : '\u25BC'}
        </button>
      </div>
      {!collapsed && (
        <div className="upload-progress-body">
          {records.map(r => (
            <div key={r.id} className={`upload-progress-item ${isError(r) ? 'error' : isComplete(r) ? 'complete' : ''}`}>
              <div className="upload-progress-item-main">
                <span className="upload-progress-filename" title={r.fileName}>{r.fileName}</span>
                <span className="upload-progress-percent">{r.percentage}%</span>
                <div className="upload-progress-bar-track">
                  <div
                    className={`upload-progress-bar-fill ${isError(r) ? 'error' : isComplete(r) ? 'complete' : ''}`}
                    style={{ width: `${r.percentage}%` }}
                  />
                </div>
                <button className="upload-progress-dismiss" onClick={() => onDismiss(r.id)} title="Dismiss">&times;</button>
              </div>
              {isError(r) && r.error && (
                <div className="upload-progress-error">{r.error}</div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default UploadProgressPanel;
