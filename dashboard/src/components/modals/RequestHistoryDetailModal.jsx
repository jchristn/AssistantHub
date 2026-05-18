import React from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';
import CopyableId from '../CopyableId';

function formatJsonBlock(value) {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  return JSON.stringify(value, null, 2);
}

function RequestHistoryDetailModal({ entry, onClose, onReplay }) {
  if (!entry) return null;

  const requestHeaders = formatJsonBlock(entry.RequestHeaders);
  const responseHeaders = formatJsonBlock(entry.ResponseHeaders);
  const routeParameters = formatJsonBlock(entry.RouteParameters);
  const queryParameters = formatJsonBlock(entry.QueryParameters);
  const requestBody = entry.RequestBody || '';
  const responseBody = entry.ResponseBody || '';

  const footer = (
    <>
      {onReplay && (
        <button className="btn btn-primary" onClick={() => onReplay(entry)}>
          Replay In Explorer
        </button>
      )}
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    </>
  );

  return (
    <Modal title={`Request History: ${entry.Id}`} onClose={onClose} extraWide footer={footer}>
      <div className="request-history-detail">
        <div className="request-history-detail-hero">
          <div className="request-history-detail-status">
            <span className={`request-history-method method-${(entry.HttpMethod || 'GET').toLowerCase()}`}>
              {entry.HttpMethod || 'GET'}
            </span>
            <span className={`status-badge ${entry.Success ? 'active' : 'failed'}`}>
              HTTP {entry.StatusCode}
            </span>
            <span className="request-history-detail-pill">{entry.RequestType || 'SystemApi'}</span>
            <span className="request-history-detail-pill">{entry.SourceType || 'api'}</span>
          </div>
          <div className="request-history-detail-path">{entry.RequestUrl || entry.RequestPath}</div>
          <div className="request-history-detail-stats">
            <div className="stat-card">
              <span className="stat-card-label">Duration</span>
              <span className="stat-card-value">{Math.round(entry.DurationMs || 0)} ms</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Request Size</span>
              <span className="stat-card-value">{Number(entry.RequestSizeBytes || 0).toLocaleString()} B</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Response Size</span>
              <span className="stat-card-value">{Number(entry.ResponseSizeBytes || 0).toLocaleString()} B</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Created</span>
              <span className="stat-card-value request-history-timestamp">{entry.CreatedUtc ? new Date(entry.CreatedUtc).toLocaleString() : '-'}</span>
            </div>
          </div>
        </div>

        <div className="request-history-detail-grid">
          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Identifiers</div>
            <div className="request-history-detail-meta">
              <div className="request-history-detail-meta-row">
                <span>ID</span>
                <CopyableId id={entry.Id} />
              </div>
              <div className="request-history-detail-meta-row">
                <span>Tenant</span>
                {entry.TenantId ? <CopyableId id={entry.TenantId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>User</span>
                {entry.UserId ? <CopyableId id={entry.UserId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Credential</span>
                {entry.CredentialId ? <CopyableId id={entry.CredentialId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Assistant</span>
                {entry.AssistantId ? <CopyableId id={entry.AssistantId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Thread</span>
                {entry.ThreadId ? <CopyableId id={entry.ThreadId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Principal</span>
                <strong>{entry.PrincipalName || '-'}</strong>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Source IP</span>
                <strong>{entry.SourceIp || '-'}</strong>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Route Template</span>
                <code>{entry.RouteTemplate || '-'}</code>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Request Content-Type</span>
                <code>{entry.RequestContentType || '-'}</code>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Response Content-Type</span>
                <code>{entry.ResponseContentType || '-'}</code>
              </div>
            </div>
          </section>

          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Request Parameters</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Route Parameters</span>
                <CopyButton text={routeParameters} />
              </div>
              <pre className="json-view request-history-block-body">{routeParameters || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Query Parameters</span>
                <CopyButton text={queryParameters} />
              </div>
              <pre className="json-view request-history-block-body">{queryParameters || '{}'}</pre>
            </div>
          </section>
        </div>

        <div className="request-history-panels">
          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Request</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Headers</span>
                <CopyButton text={requestHeaders} />
              </div>
              <pre className="json-view request-history-block-body">{requestHeaders || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Body</span>
                <div className="request-history-block-flags">
                  {entry.RequestBodyTruncated && <span className="request-history-detail-pill warning">Truncated</span>}
                  {entry.RequestBodyIsBinary && <span className="request-history-detail-pill">Binary</span>}
                  <CopyButton text={requestBody} />
                </div>
              </div>
              <pre className="json-view request-history-block-body">{requestBody || '(empty)'}</pre>
            </div>
          </section>

          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Response</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Headers</span>
                <CopyButton text={responseHeaders} />
              </div>
              <pre className="json-view request-history-block-body">{responseHeaders || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Body</span>
                <div className="request-history-block-flags">
                  {entry.ResponseBodyTruncated && <span className="request-history-detail-pill warning">Truncated</span>}
                  {entry.ResponseBodyIsBinary && <span className="request-history-detail-pill">Binary</span>}
                  <CopyButton text={responseBody} />
                </div>
              </div>
              <pre className="json-view request-history-block-body">{responseBody || '(empty)'}</pre>
            </div>
          </section>
        </div>
      </div>
    </Modal>
  );
}

export default RequestHistoryDetailModal;
