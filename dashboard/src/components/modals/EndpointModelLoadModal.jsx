import React, { useMemo, useState } from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';
import './EndpointTestModal.css';

function EndpointModelLoadModal({ api, endpoint, endpointType, onClose }) {
  const isEmbedding = endpointType === 'embedding';
  const [strategy, setStrategy] = useState('Auto');
  const [timeoutMs, setTimeoutMs] = useState(60000);
  const [keepAlive, setKeepAlive] = useState('30m');
  const [sampleInput, setSampleInput] = useState('Partio model load probe');
  const [maxTokens, setMaxTokens] = useState(1);
  const [recordRequestHistory, setRecordRequestHistory] = useState(true);
  const [requireNativeLoad, setRequireNativeLoad] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const titlePrefix = isEmbedding ? 'Embedding Endpoint' : 'Inference Endpoint';
  const resultJson = result ? JSON.stringify(result, null, 2) : '';
  const upstreamCalls = useMemo(() => {
    if (Array.isArray(result?.EmbeddingCalls)) return result.EmbeddingCalls;
    if (Array.isArray(result?.CompletionCalls)) return result.CompletionCalls;
    return [];
  }, [result]);

  const loadModel = async () => {
    setSubmitting(true);
    setError(null);

    const request = {
      Strategy: strategy,
      TimeoutMs: parseInt(timeoutMs, 10) || 0,
      KeepAlive: keepAlive.trim() || null,
      SampleInput: sampleInput,
      MaxTokens: parseInt(maxTokens, 10) || 1,
      RecordRequestHistory: recordRequestHistory,
      RequireNativeLoad: requireNativeLoad
    };

    try {
      const response = isEmbedding
        ? await api.loadEmbeddingEndpointModel(endpoint.Id, request)
        : await api.loadCompletionEndpointModel(endpoint.Id, request);
      setResult(response);
      if (!response.Success) {
        setError(response.Message || response.ErrorMessage || `Model load returned HTTP ${response.HttpStatusCode || response.StatusCode || '-'}`);
      }
    } catch (err) {
      setError(err.message || 'Model load failed');
      setResult(null);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      title={`Load Model: ${titlePrefix}${endpoint?.Name ? ` - ${endpoint.Name}` : ''}`}
      onClose={onClose}
      extraWide
      footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Close</button>
          <button className="btn btn-primary" onClick={loadModel} disabled={submitting}>
            {submitting ? 'Loading...' : 'Load Model'}
          </button>
        </>
      }
    >
      <div className="endpoint-test-modal">
        <p className="endpoint-test-description">
          Load or warm the configured model through AssistantHub and Partio. Ollama endpoints can use native preload; OpenAI-compatible and Gemini endpoints use a minimal warm request.
        </p>

        <div className="endpoint-test-meta">
          <div><span>Name</span><strong>{endpoint?.Name || '(unnamed)'}</strong></div>
          <div><span>Model</span><strong>{endpoint?.Model || '-'}</strong></div>
          <div><span>Format</span><strong>{endpoint?.ApiFormat || '-'}</strong></div>
          <div><span>Endpoint</span><code>{endpoint?.Endpoint || '-'}</code></div>
        </div>

        <div className="endpoint-test-grid two-col">
          <div className="form-group">
            <label>Strategy</label>
            <select value={strategy} onChange={(event) => setStrategy(event.target.value)}>
              <option value="Auto">Auto</option>
              <option value="NativeProviderLoad">Native provider load</option>
              <option value="WarmRequest">Warm request</option>
            </select>
          </div>
          <div className="form-group">
            <label>Timeout (ms)</label>
            <input
              type="number"
              min="0"
              step="1000"
              value={timeoutMs}
              onChange={(event) => setTimeoutMs(event.target.value)}
            />
          </div>
        </div>

        <div className="endpoint-test-grid two-col">
          <div className="form-group">
            <label>Keep Alive</label>
            <input
              type="text"
              value={keepAlive}
              onChange={(event) => setKeepAlive(event.target.value)}
              placeholder="30m"
            />
          </div>
          <div className="form-group">
            <label>Max Tokens</label>
            <input
              type="number"
              min="1"
              max="16"
              value={maxTokens}
              onChange={(event) => setMaxTokens(event.target.value)}
            />
          </div>
        </div>

        <div className="form-group">
          <label>Sample Input</label>
          <textarea
            className="endpoint-test-textarea"
            rows={4}
            value={sampleInput}
            onChange={(event) => setSampleInput(event.target.value)}
            placeholder="Short non-sensitive warm request input"
          />
        </div>

        <div className="endpoint-test-grid two-col">
          <label className="endpoint-test-checkbox">
            <input
              type="checkbox"
              checked={recordRequestHistory}
              onChange={(event) => setRecordRequestHistory(event.target.checked)}
            />
            <span>Record request history</span>
          </label>
          <label className="endpoint-test-checkbox">
            <input
              type="checkbox"
              checked={requireNativeLoad}
              onChange={(event) => setRequireNativeLoad(event.target.checked)}
            />
            <span>Require native load</span>
          </label>
        </div>

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
                <label>Outcome</label>
                <strong>{result.Outcome || '-'}</strong>
              </div>
              <div className="endpoint-test-card">
                <label>Status Code</label>
                <strong>{result.StatusCode ?? result.HttpStatusCode ?? '-'}</strong>
              </div>
              <div className="endpoint-test-card">
                <label>Response Time</label>
                <strong>{result.ResponseTimeMs != null ? `${result.ResponseTimeMs} ms` : '-'}</strong>
              </div>
              <div className="endpoint-test-card">
                <label>Strategy</label>
                <strong>{result.Strategy || '-'}</strong>
              </div>
              <div className="endpoint-test-card">
                <label>History Entry</label>
                {result.RequestHistoryId ? (
                  <div className="endpoint-test-copyable">
                    <code className="endpoint-test-copyable-value" title={result.RequestHistoryId}>
                      {result.RequestHistoryId}
                    </code>
                    <CopyButton text={result.RequestHistoryId} />
                  </div>
                ) : (
                  <strong>Not recorded</strong>
                )}
              </div>
            </div>

            {result.Message && (
              <div className="endpoint-test-card">
                <label>Message</label>
                <strong>{result.Message}</strong>
              </div>
            )}

            <div className="endpoint-test-grid two-col">
              <div className="endpoint-test-card">
                <label>Started</label>
                <strong>{result.StartedUtc ? new Date(result.StartedUtc).toLocaleString() : '-'}</strong>
              </div>
              <div className="endpoint-test-card">
                <label>Completed</label>
                <strong>{result.CompletedUtc ? new Date(result.CompletedUtc).toLocaleString() : '-'}</strong>
              </div>
            </div>

            <div className="endpoint-test-actions">
              <span className="endpoint-test-actions-label">Response JSON</span>
              <CopyButton text={resultJson} />
            </div>

            {upstreamCalls.length > 0 && (
              <div>
                <h4>Upstream Calls</h4>
                <div className="endpoint-test-call-list">
                  {upstreamCalls.map((call, index) => (
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
  );
}

export default EndpointModelLoadModal;
