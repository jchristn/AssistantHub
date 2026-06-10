import React, { useEffect, useMemo, useState } from 'react';
import Modal from '../Modal';
import { ApiClient } from '../../utils/api';

function formatBytes(bytes) {
  const value = Number(bytes || 0);
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(value) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleDateString();
}

function displayName(doc) {
  return doc?.Name || doc?.OriginalFilename || doc?.Id || 'Untitled document';
}

function DocumentAttachmentModal({ serverUrl, assistantId, selectedDocuments, maxCount = 10, onApply, onClose }) {
  const [query, setQuery] = useState('');
  const [documents, setDocuments] = useState([]);
  const [continuationToken, setContinuationToken] = useState(null);
  const [nextToken, setNextToken] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [draft, setDraft] = useState(() => selectedDocuments || []);

  const selectedIds = useMemo(() => new Set(draft.map(doc => doc.Id)), [draft]);

  const loadDocuments = async (token = null, append = false) => {
    if (!assistantId) return;
    setLoading(true);
    setError(null);
    try {
      const result = await ApiClient.getAssistantDocuments(serverUrl, assistantId, {
        maxResults: 100,
        continuationToken: token,
        query: query.trim() || null
      });
      const items = result?.Objects || [];
      setDocuments(prev => append ? [...prev, ...items] : items);
      setNextToken(result?.ContinuationToken || null);
      setContinuationToken(token);
    } catch (err) {
      setError(err.message || 'Failed to load documents');
      if (!append) setDocuments([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => loadDocuments(null, false), 250);
    return () => clearTimeout(timeout);
  }, [assistantId, serverUrl, query]);

  const toggleDocument = (doc) => {
    if (!doc?.Id) return;
    setDraft(current => {
      if (current.some(item => item.Id === doc.Id)) {
        return current.filter(item => item.Id !== doc.Id);
      }
      if (current.length >= maxCount) return current;
      return [...current, doc];
    });
  };

  const selectedCount = draft.length;

  return (
    <Modal title="Attach Documents" onClose={onClose} wide footer={
      <div className="document-attachment-footer">
        <button className="btn btn-secondary" type="button" onClick={() => setDraft([])} disabled={selectedCount === 0}>Clear</button>
        <div className="document-attachment-footer-actions">
          <span className="document-attachment-count">{selectedCount} / {maxCount} selected</span>
          <button className="btn btn-secondary" type="button" onClick={onClose}>Cancel</button>
          <button className="btn btn-primary" type="button" onClick={() => onApply(draft)}>Apply</button>
        </div>
      </div>
    }>
      <div className="document-attachment-modal">
        <input
          className="form-input"
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search documents"
          autoFocus
        />
        {error && <div className="document-attachment-error">{error}</div>}
        {!error && documents.length === 0 && !loading && (
          <div className="document-attachment-empty">No completed documents found.</div>
        )}
        <div className="document-attachment-list">
          {documents.map(doc => {
            const checked = selectedIds.has(doc.Id);
            const disabled = !checked && selectedCount >= maxCount;
            const created = formatDate(doc.CreatedUtc);
            const updated = formatDate(doc.LastUpdateUtc);
            const meta = [
              doc.ContentType,
              formatBytes(doc.SizeBytes),
              created ? `Created ${created}` : null,
              updated ? `Updated ${updated}` : null,
              doc.SourceUrl
            ].filter(Boolean);
            return (
              <label key={doc.Id} className={`document-attachment-row${disabled ? ' disabled' : ''}`}>
                <input
                  type="checkbox"
                  checked={checked}
                  disabled={disabled}
                  onChange={() => toggleDocument(doc)}
                />
                <span className="document-attachment-main">
                  <span className="document-attachment-name" title={displayName(doc)}>{displayName(doc)}</span>
                  <span className="document-attachment-meta">
                    {meta.map((value) => (
                      <span key={value}>{value}</span>
                    ))}
                  </span>
                </span>
              </label>
            );
          })}
        </div>
        {loading && <div className="document-attachment-loading">Loading documents...</div>}
        {nextToken && (
          <button
            className="btn btn-secondary document-attachment-more"
            type="button"
            onClick={() => loadDocuments(nextToken, true)}
            disabled={loading || continuationToken === nextToken}
          >
            Load More
          </button>
        )}
      </div>
    </Modal>
  );
}

export default DocumentAttachmentModal;
