import React, { useMemo } from 'react';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';

function parseArray(value) {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  if (typeof value !== 'string') return [];

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function pick(doc, ...keys) {
  for (const key of keys) {
    const value = doc?.[key];
    if (value !== undefined && value !== null && value !== '') return value;
  }
  return null;
}

function formatBytes(value) {
  const bytes = Number(value || 0);
  if (!isFinite(bytes) || bytes <= 0) return 'Size unknown';
  if (bytes < 1024) return `${bytes.toLocaleString()} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
}

function formatDate(value) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleString();
}

function normalizeDocuments(history) {
  const ids = parseArray(history?.AttachedDocumentIdsJson);
  const docs = parseArray(history?.AttachedDocumentsJson);
  const byId = new Map();

  docs.forEach((doc) => {
    const id = pick(doc, 'Id', 'id', 'DocumentId', 'document_id');
    if (!id) return;
    byId.set(id, { ...doc, Id: id });
  });

  ids.forEach((id) => {
    if (!id || byId.has(id)) return;
    byId.set(id, { Id: id });
  });

  return [...byId.values()];
}

function AssistantAttachedDocumentsSection({ history }) {
  const documents = useMemo(() => normalizeDocuments(history), [history]);

  if (!documents.length) return null;

  return (
    <section className="history-section attached-documents-section">
      <div className="history-section-header">
        <Tooltip text="Documents selected on this assistant turn and persisted with chat history.">
          Attached Documents
        </Tooltip>
        <Tooltip className="history-section-badge" text="Number of selected documents persisted for this chat turn.">
          {documents.length}
        </Tooltip>
      </div>

      <div className="attached-documents-list">
        {documents.map((doc) => {
          const id = pick(doc, 'Id', 'id', 'DocumentId', 'document_id');
          const title = pick(doc, 'Name', 'name', 'OriginalFilename', 'original_filename') || id || 'Document';
          const originalFilename = pick(doc, 'OriginalFilename', 'original_filename');
          const contentType = pick(doc, 'ContentType', 'content_type');
          const sizeBytes = pick(doc, 'SizeBytes', 'size_bytes');
          const sourceUrl = pick(doc, 'SourceUrl', 'source_url');
          const createdUtc = formatDate(pick(doc, 'CreatedUtc', 'created_utc'));
          const updatedUtc = formatDate(pick(doc, 'LastUpdateUtc', 'last_update_utc'));

          return (
            <article key={id || title} className="attached-document-card">
              <div className="attached-document-card-main">
                <Tooltip as="div" className="attached-document-title" text={`Attached document: ${title}.`}>
                  {title}
                </Tooltip>
                {originalFilename && originalFilename !== title && (
                  <Tooltip as="div" className="attached-document-subtitle" text="Original uploaded filename.">
                    {originalFilename}
                  </Tooltip>
                )}
                <div className="attached-document-meta">
                  {contentType && <span className="request-history-detail-pill">{contentType}</span>}
                  <span className="request-history-detail-pill">{formatBytes(sizeBytes)}</span>
                  {createdUtc && <span className="request-history-detail-pill">Created {createdUtc}</span>}
                  {updatedUtc && <span className="request-history-detail-pill">Updated {updatedUtc}</span>}
                </div>
                {sourceUrl && (
                  <a className="attached-document-source" href={sourceUrl} target="_blank" rel="noreferrer">
                    {sourceUrl}
                  </a>
                )}
              </div>
              {id && (
                <Tooltip className="attached-document-id" text={`Assistant document identifier: ${id}.`}>
                  <CopyableId id={id} />
                </Tooltip>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}

export default AssistantAttachedDocumentsSection;
