import React, { useState, useEffect } from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';

function formatFileSize(bytes) {
  if (bytes == null) return '';
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
}

function EnumerationSection({ title, files, totalBytes }) {
  const [expanded, setExpanded] = useState(false);
  const items = files || [];
  const count = items.length;

  return (
    <div className="collapsible-section">
      <button
        type="button"
        className="collapsible-section-header"
        onClick={() => setExpanded(prev => !prev)}
      >
        <span className="collapsible-section-arrow">{expanded ? '\u25BE' : '\u25B8'}</span>
        <span>{title}</span>
        <span className="collapsible-section-meta">
          {count} file{count !== 1 ? 's' : ''}
          {totalBytes != null ? ` (${formatFileSize(totalBytes)})` : ''}
        </span>
      </button>
      {expanded && items.length > 0 && (
        <div className="collapsible-section-body">
          <table className="data-table" style={{ fontSize: '0.8125rem' }}>
            <thead>
              <tr>
                <th>Key / URL</th>
                <th>Content Type</th>
                <th>Size</th>
                <th>Last Modified</th>
              </tr>
            </thead>
            <tbody>
              {items.map((file, i) => (
                <tr key={i}>
                  <td style={{ maxWidth: '400px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    <span title={file.Key || file.Url || file.ObjectKey || ''}>{file.Key || file.Url || file.ObjectKey || ''}</span>
                  </td>
                  <td>{file.ContentType || ''}</td>
                  <td>{formatFileSize(file.ContentLength)}</td>
                  <td>{file.LastModifiedUtc ? new Date(file.LastModifiedUtc).toLocaleString() : ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {expanded && items.length === 0 && (
        <div className="collapsible-section-body" style={{ color: 'var(--text-secondary)', padding: '0.75rem 1rem', fontSize: '0.85rem' }}>
          No files in this category.
        </div>
      )}
    </div>
  );
}

function CrawlEnumerationModal({ api, planId, operationId, onClose }) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    (async () => {
      setLoading(true);
      try {
        let result;
        if (operationId) {
          result = await api.getCrawlOperationEnumeration(planId, operationId);
        } else {
          result = await api.enumerateCrawlContents(planId);
        }
        setData(result);
      } catch (err) {
        setError(err.message || 'Failed to load enumeration');
      } finally {
        setLoading(false);
      }
    })();
  }, [planId, operationId]);

  const stats = data?.Statistics;
  const sections = data ? [
    { title: 'All Files', files: data.AllFiles || [], totalBytes: stats?.TotalBytes },
    { title: 'New Files', files: data.Added || [], totalBytes: stats?.AddedBytes },
    { title: 'Changed Files', files: data.Changed || [], totalBytes: stats?.ChangedBytes },
    { title: 'Deleted Files', files: data.Deleted || [], totalBytes: stats?.DeletedBytes },
    { title: 'Successfully Crawled', files: data.Success || [], totalBytes: stats?.SuccessBytes },
    { title: 'Failed', files: data.Failed || [], totalBytes: stats?.FailedBytes },
  ] : [];

  return (
    <Modal title={<span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem' }}>Crawl Enumeration{data && <CopyButton text={JSON.stringify(data, null, 2)} />}</span>} onClose={onClose} extraWide footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      {loading ? (
        <div className="loading"><div className="spinner" /></div>
      ) : error ? (
        <div className="empty-state"><p>{error}</p></div>
      ) : !data ? (
        <div className="empty-state"><p>No enumeration data available.</p></div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {sections.map((section, i) => (
            <EnumerationSection
              key={i}
              title={section.title}
              files={section.files}
              totalBytes={section.totalBytes}
            />
          ))}
        </div>
      )}
    </Modal>
  );
}

export default CrawlEnumerationModal;
