import React, { useState, useEffect, useCallback } from 'react';
import { copyToClipboard } from '../../utils/clipboard';

function ProcessingLogModal({ api, documentId, onClose }) {
  const [log, setLog] = useState(undefined);
  const [error, setError] = useState(null);
  const [copied, setCopied] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const fetchLog = useCallback(async () => {
    try {
      setRefreshing(true);
      setError(null);
      const result = await api.getDocumentProcessingLog(documentId);
      setLog(result.Log || null);
    } catch (err) {
      setError(err.message || 'Failed to load processing log');
    } finally {
      setRefreshing(false);
    }
  }, [api, documentId]);

  useEffect(() => {
    fetchLog();
  }, [fetchLog]);

  const handleKeyDown = useCallback((e) => {
    if (e.key === 'Escape') onClose();
  }, [onClose]);

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown]);

  const handleOverlayClick = (e) => {
    if (e.target === e.currentTarget) onClose();
  };

  const handleCopy = async () => {
    if (!log) return;
    const success = await copyToClipboard(log);
    if (success) {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    }
  };

  const handleDownload = () => {
    if (!log) return;
    const blob = new Blob([log], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${documentId}.log`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="modal-overlay" onClick={handleOverlayClick}>
      <div className="modal-container" style={{ maxWidth: '95vw', width: '95vw' }}>
        <div className="modal-header">
          <h3 className="modal-title">Processing Log</h3>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </div>
        <div className="modal-body">
          {error && <p style={{ color: 'var(--danger-color, #e74c3c)' }}>{error}</p>}
          {log === undefined && !error && <p>Loading...</p>}
          {log === null && !error && <p>No processing logs available.</p>}
          {log && (
            <>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <button className="copy-btn" onClick={fetchLog} disabled={refreshing} title="Refresh log">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={refreshing ? { animation: 'spin 1s linear infinite' } : undefined}><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/></svg>
                </button>
                <button className={`copy-btn ${copied ? 'copied' : ''}`} onClick={handleCopy} title="Copy to clipboard">
                  {copied ? (
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="20 6 9 17 4 12"/></svg>
                  ) : (
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>
                  )}
                </button>
                <button className="copy-btn" onClick={handleDownload} title="Download log file">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                </button>
              </div>
              <pre style={{
                background: 'var(--bg-secondary, #1a1a2e)',
                padding: '1rem',
                borderRadius: '6px',
                overflow: 'auto',
                maxHeight: '70vh',
                fontSize: '0.85rem',
                lineHeight: '1.5',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}>{log}</pre>
            </>
          )}
        </div>
        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
}

export default ProcessingLogModal;
