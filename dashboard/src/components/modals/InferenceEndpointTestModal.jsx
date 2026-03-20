import React, { useState } from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';
import './EndpointTestModal.css';

function InferenceEndpointTestModal({ api, endpoint, onClose }) {
  const [systemPrompt, setSystemPrompt] = useState('You are a concise and accurate assistant.');
  const [prompt, setPrompt] = useState('Respond with a one-sentence smoke test confirmation.');
  const [maxTokens, setMaxTokens] = useState(512);
  const [timeoutMs, setTimeoutMs] = useState(60000);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const resultJson = result ? JSON.stringify(result, null, 2) : '';

  const runTest = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const response = await api.testCompletionEndpoint(endpoint.Id, {
        SystemPrompt: systemPrompt || null,
        Prompt: prompt,
        MaxTokens: parseInt(maxTokens, 10) || 512,
        TimeoutMs: parseInt(timeoutMs, 10) || 60000
      });
      setResult(response);
    } catch (err) {
      setError(err.message || 'Inference test failed');
      setResult(null);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Modal
        title={`Test Inference Endpoint${endpoint?.Name ? `: ${endpoint.Name}` : ''}`}
        onClose={onClose}
        extraWide
        footer={
          <>
            <button className="btn btn-secondary" onClick={onClose}>Close</button>
            <button className="btn btn-primary" onClick={runTest} disabled={submitting || !prompt.trim()}>
              {submitting ? 'Running...' : 'Run Test'}
            </button>
          </>
        }
      >
        <div className="endpoint-test-modal">
          <p className="endpoint-test-description">
            Run a smoke test through AssistantHub to verify this inference endpoint works end-to-end via Partio.
          </p>

          <div className="endpoint-test-meta">
            <div><span>Name</span><strong>{endpoint?.Name || '(unnamed)'}</strong></div>
            <div><span>Model</span><strong>{endpoint?.Model || '-'}</strong></div>
            <div><span>Format</span><strong>{endpoint?.ApiFormat || '-'}</strong></div>
            <div><span>Endpoint</span><code>{endpoint?.Endpoint || '-'}</code></div>
          </div>

          <div className="endpoint-test-grid">
            <div className="form-group">
              <label>System Prompt</label>
              <textarea
                className="endpoint-test-textarea"
                rows={4}
                value={systemPrompt}
                onChange={(e) => setSystemPrompt(e.target.value)}
                placeholder="Optional system prompt"
              />
            </div>
            <div className="form-group">
              <label>User Prompt</label>
              <textarea
                className="endpoint-test-textarea"
                rows={8}
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder="Enter the user prompt"
              />
            </div>
          </div>

          <div className="endpoint-test-grid two-col">
            <div className="form-group">
              <label>Max Tokens</label>
              <input
                type="number"
                min="1"
                value={maxTokens}
                onChange={(e) => setMaxTokens(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label>Timeout (ms)</label>
              <input
                type="number"
                min="1000"
                step="1000"
                value={timeoutMs}
                onChange={(e) => setTimeoutMs(e.target.value)}
              />
            </div>
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
                  <label>Status Code</label>
                  <strong>{result.StatusCode ?? '-'}</strong>
                </div>
                <div className="endpoint-test-card">
                  <label>Response Time</label>
                  <strong>{result.ResponseTimeMs != null ? `${result.ResponseTimeMs} ms` : '-'}</strong>
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

              {result.Error && <div className="endpoint-test-error">{result.Error}</div>}

              <div>
                <h4>Model Output</h4>
                <pre className="endpoint-test-output">{result.Output || '(empty)'}</pre>
              </div>

              <div className="endpoint-test-actions">
                <span className="endpoint-test-actions-label">Response JSON</span>
                <CopyButton text={resultJson} />
              </div>

              {Array.isArray(result.CompletionCalls) && result.CompletionCalls.length > 0 && (
                <div>
                  <h4>Upstream Calls</h4>
                  <div className="endpoint-test-call-list">
                    {result.CompletionCalls.map((call, index) => (
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

export default InferenceEndpointTestModal;
