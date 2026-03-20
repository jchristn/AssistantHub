import React, { useMemo, useState } from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';
import './EndpointTestModal.css';

function EmbeddingEndpointTestModal({ api, endpoint, onClose }) {
  const [input, setInput] = useState('AssistantHub embedding smoke test input');
  const [l2Normalization, setL2Normalization] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const vectorPreview = useMemo(() => {
    const embedding = Array.isArray(result?.Embedding) ? result.Embedding : [];
    return embedding.slice(0, 16).map(value => Number(value).toFixed(6)).join(', ');
  }, [result]);

  const resultJson = result ? JSON.stringify(result, null, 2) : '';

  const runTest = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const response = await api.testEmbeddingEndpoint(endpoint.Id, {
        Input: input,
        L2Normalization: l2Normalization
      });
      setResult(response);
    } catch (err) {
      setError(err.message || 'Embedding test failed');
      setResult(null);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Modal
        title={`Test Embedding Endpoint${endpoint?.Name ? `: ${endpoint.Name}` : ''}`}
        onClose={onClose}
        extraWide
        footer={
          <>
            <button className="btn btn-secondary" onClick={onClose}>Close</button>
            <button className="btn btn-primary" onClick={runTest} disabled={submitting || !input.trim()}>
              {submitting ? 'Running...' : 'Run Test'}
            </button>
          </>
        }
      >
        <div className="endpoint-test-modal">
          <p className="endpoint-test-description">
            Run a smoke test through AssistantHub to verify this embedding endpoint works end-to-end via Partio.
          </p>

          <div className="endpoint-test-meta">
            <div><span>Name</span><strong>{endpoint?.Name || '(unnamed)'}</strong></div>
            <div><span>Model</span><strong>{endpoint?.Model || '-'}</strong></div>
            <div><span>Format</span><strong>{endpoint?.ApiFormat || '-'}</strong></div>
            <div><span>Endpoint</span><code>{endpoint?.Endpoint || '-'}</code></div>
          </div>

          <div className="form-group">
            <label>Input Text</label>
            <textarea
              className="endpoint-test-textarea"
              rows={7}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Enter the text to embed"
            />
          </div>

          <label className="endpoint-test-checkbox">
            <input
              type="checkbox"
              checked={l2Normalization}
              onChange={(e) => setL2Normalization(e.target.checked)}
            />
            <span>Apply L2 normalization</span>
          </label>

          {error && <div className="endpoint-test-error">{error}</div>}

          {result && (
            <div className="endpoint-test-result">
              <div className="endpoint-test-overview">
                <div className="endpoint-test-card">
                  <label>Result</label>
                  <span className={`status-badge ${result.Success ? 'active' : 'inactive'}`}>
                    {result.Success ? 'Success' : 'Failed'}
                  </span>
                </div>
                <div className="endpoint-test-card">
                  <label>Status Code</label>
                  <strong>{result.StatusCode ?? '-'}</strong>
                </div>
                <div className="endpoint-test-card">
                  <label>Response Time</label>
                  <strong>{result.ResponseTimeMs != null ? `${result.ResponseTimeMs} ms` : '-'}</strong>
                </div>
                <div className="endpoint-test-card">
                  <label>Dimensions</label>
                  <strong>{result.Dimensions ?? 0}</strong>
                </div>
              </div>

              {result.Error && <div className="endpoint-test-error">{result.Error}</div>}

              <div className="endpoint-test-grid">
                <div className="endpoint-test-card">
                  <label>Vector Preview</label>
                  <code>{vectorPreview ? `[${vectorPreview}${(result.Dimensions || 0) > 16 ? ', ...' : ''}]` : '(empty)'}</code>
                </div>
              </div>

              <div className="endpoint-test-actions">
                <span className="endpoint-test-actions-label">Response JSON</span>
                <CopyButton text={resultJson} />
              </div>

              {Array.isArray(result.EmbeddingCalls) && result.EmbeddingCalls.length > 0 && (
                <div>
                  <h4>Upstream Calls</h4>
                  <div className="endpoint-test-call-list">
                    {result.EmbeddingCalls.map((call, index) => (
                      <div key={`${call.TimestampUtc || 'call'}-${index}`} className="endpoint-test-call-card">
                        <div className="endpoint-test-call-header">
                          <strong>#{index + 1}</strong>
                          <span className={`status-badge ${call.Success ? 'active' : 'inactive'}`}>{call.Success ? 'Success' : 'Failed'}</span>
                          <code>{call.Method || 'POST'}</code>
                          <code>{call.Url || '-'}</code>
                        </div>
                        <div className="endpoint-test-call-grid">
                          <div className="endpoint-test-card">
                            <label>Status Code</label>
                            <strong>{call.StatusCode ?? '-'}</strong>
                          </div>
                          <div className="endpoint-test-card">
                            <label>Response Time</label>
                            <strong>{call.ResponseTimeMs != null ? `${call.ResponseTimeMs} ms` : '-'}</strong>
                          </div>
                        </div>
                        {call.Error && <div className="endpoint-test-error" style={{ marginTop: '0.75rem' }}>{call.Error}</div>}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </Modal>
    </>
  );
}

export default EmbeddingEndpointTestModal;
