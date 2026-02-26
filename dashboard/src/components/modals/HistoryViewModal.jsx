import React, { useState, useEffect, useMemo } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';
import { ApiClient } from '../../utils/api';
import { useAuth } from '../../context/AuthContext';

function formatTps(tps) {
  if (!tps || tps <= 0 || !isFinite(tps)) return 'N/A';
  return tps.toFixed(1) + ' tok/s';
}

function formatMs(ms) {
  if (!ms || ms <= 0) return 'N/A';
  if (ms < 1000) return ms.toFixed(1) + ' ms';
  return (ms / 1000).toFixed(2) + ' s';
}

function formatTimestamp(utc) {
  if (!utc) return 'N/A';
  return new Date(utc).toLocaleString();
}

function TimingBar({ label, tooltip, durationMs, totalMs, color }) {
  const pct = totalMs > 0 && durationMs > 0 ? Math.max(1, (durationMs / totalMs) * 100) : 0;
  return (
    <div className="history-timing-row">
      <div className="history-timing-label">
        <Tooltip text={tooltip}>{label}</Tooltip>
      </div>
      <div className="history-timing-bar-track">
        {pct > 0 && (
          <div
            className="history-timing-bar-fill"
            style={{ width: `${Math.min(pct, 100)}%`, background: color }}
          />
        )}
      </div>
      <div className="history-timing-value">{formatMs(durationMs)}</div>
    </div>
  );
}

function HistoryViewModal({ history, onClose }) {
  if (!history) return null;

  const { serverUrl, credential } = useAuth();
  const [retrievalOpen, setRetrievalOpen] = useState(false);
  const [messagesOpen, setMessagesOpen] = useState(true);
  const [queryRewriteOpen, setQueryRewriteOpen] = useState(false);
  const [docNames, setDocNames] = useState({});

  // Parse retrieval chunks once
  const retrievalChunks = useMemo(() => {
    if (!history.RetrievalContext) return null;
    try {
      const parsed = JSON.parse(history.RetrievalContext);
      return Array.isArray(parsed) ? parsed : null;
    } catch { return null; }
  }, [history.RetrievalContext]);

  // Compute retrieval summary stats
  const retrievalStats = useMemo(() => {
    if (!retrievalChunks) return null;
    const docIds = new Set();
    let totalChunks = 0;
    for (const chunk of retrievalChunks) {
      totalChunks++;
      if (chunk.document_id) docIds.add(chunk.document_id);
      if (chunk.neighbors) {
        totalChunks += chunk.neighbors.length;
      }
    }
    return { uniqueDocIds: [...docIds], totalChunks };
  }, [retrievalChunks]);

  // Fetch document names for unique document IDs
  useEffect(() => {
    if (!retrievalStats || retrievalStats.uniqueDocIds.length === 0) return;
    const api = new ApiClient(serverUrl, credential?.BearerToken);
    let cancelled = false;
    (async () => {
      const names = {};
      await Promise.all(retrievalStats.uniqueDocIds.map(async (id) => {
        try {
          const doc = await api.getDocument(id);
          if (!cancelled && doc) names[id] = doc.OriginalFilename || doc.Name || null;
        } catch { /* document may have been deleted */ }
      }));
      if (!cancelled) setDocNames(names);
    })();
    return () => { cancelled = true; };
  }, [retrievalStats, serverUrl, credential]);

  // Compute total pipeline duration for proportional bars
  const totalPipelineMs =
    (history.RetrievalGateDurationMs || 0) +
    (history.QueryRewriteDurationMs || 0) +
    (history.RetrievalDurationMs || 0) +
    (history.EndpointResolutionDurationMs || 0) +
    (history.CompactionDurationMs || 0) +
    (history.TimeToLastTokenMs || 0);

  // Infer prompt processing time: TTFT minus connection time (if both available)
  const promptProcessingMs =
    history.TimeToFirstTokenMs > 0 && history.InferenceConnectionDurationMs > 0
      ? Math.max(0, history.TimeToFirstTokenMs - history.InferenceConnectionDurationMs)
      : 0;

  // Token generation: TTLT minus TTFT
  const tokenGenMs =
    history.TimeToLastTokenMs > 0 && history.TimeToFirstTokenMs > 0
      ? Math.max(0, history.TimeToLastTokenMs - history.TimeToFirstTokenMs)
      : 0;

  // Use backend-stored TPS if available, otherwise compute from raw fields
  const overallTps = history.TokensPerSecondOverall > 0
    ? history.TokensPerSecondOverall
    : (history.CompletionTokens > 0 && history.TimeToLastTokenMs > 0
        ? (history.CompletionTokens / (history.TimeToLastTokenMs / 1000))
        : 0);

  const generationTps = history.TokensPerSecondGeneration > 0
    ? history.TokensPerSecondGeneration
    : (history.CompletionTokens > 0 && tokenGenMs > 0
        ? (history.CompletionTokens / (tokenGenMs / 1000))
        : 0);

  return (
    <Modal title="History Details" onClose={onClose} fullscreen footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      {/* === Identifiers row === */}
      <div className="history-ids-row">
        <div className="history-id-item">
          <span className="history-id-label">History</span>
          <CopyableId id={history.Id} />
        </div>
        <div className="history-id-item">
          <span className="history-id-label">Thread</span>
          <CopyableId id={history.ThreadId} />
        </div>
        <div className="history-id-item">
          <span className="history-id-label">Assistant</span>
          <CopyableId id={history.AssistantId} />
        </div>
        {history.CollectionId && (
          <div className="history-id-item">
            <span className="history-id-label">Collection</span>
            <CopyableId id={history.CollectionId} />
          </div>
        )}
      </div>

      {/* === Performance Timing === */}
      <div className="history-section">
        <div className="history-section-header">
          <Tooltip text="End-to-end timing breakdown for this chat turn">Performance Timing</Tooltip>
        </div>
        <div className="history-timing-container">
          <TimingBar
            label="Retrieval Gate"
            tooltip={'LLM-based retrieval gate — classifies whether retrieval is needed or can be skipped' + (history.RetrievalGateDecision ? '. Decision: ' + history.RetrievalGateDecision : '')}
            durationMs={history.RetrievalGateDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-gate, #a9e34b)"
          />
          <TimingBar
            label="Query Rewrite"
            tooltip="LLM-based query rewrite — rewrites the user prompt into multiple semantically varied queries to improve retrieval recall"
            durationMs={history.QueryRewriteDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-rewrite, #74c0fc)"
          />
          <TimingBar
            label="Retrieval"
            tooltip="Time spent searching the document collection for relevant context"
            durationMs={history.RetrievalDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-retrieval, #4dabf7)"
          />
          <TimingBar
            label="Endpoint Resolution"
            tooltip="HTTP request to Partio to resolve the configured inference endpoint details"
            durationMs={history.EndpointResolutionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-endpoint, #69db7c)"
          />
          <TimingBar
            label="Compaction"
            tooltip="Conversation history compaction when context window is exceeded (may involve an LLM call)"
            durationMs={history.CompactionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-compaction, #ffd43b)"
          />
          <TimingBar
            label="Connection"
            tooltip="Time from HTTP request sent to response headers received — includes network latency and model loading"
            durationMs={history.InferenceConnectionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-connection, #ff922b)"
          />
          <TimingBar
            label="Prompt Processing"
            tooltip="Estimated time the model spent processing the prompt before generating the first token (TTFT minus connection time)"
            durationMs={promptProcessingMs}
            totalMs={totalPipelineMs}
            color="var(--timing-prompt, #da77f2)"
          />
          <TimingBar
            label="Token Generation"
            tooltip="Time from the first token to the last token — the streaming generation phase"
            durationMs={tokenGenMs}
            totalMs={totalPipelineMs}
            color="var(--timing-generation, #ff6b6b)"
          />
        </div>

        {/* Summary metrics row */}
        <div className="history-metrics-row">
          <div className="history-metric">
            <span className="history-metric-label"><Tooltip text="Time to first token — measured from when the prompt was sent to when the first token was received">TTFT</Tooltip></span>
            <span className="history-metric-value">{formatMs(history.TimeToFirstTokenMs)}</span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label"><Tooltip text="Time to last token — total time from prompt sent to the final token received">TTLT</Tooltip></span>
            <span className="history-metric-value">{formatMs(history.TimeToLastTokenMs)}</span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label"><Tooltip text="Estimated number of tokens in the prompt (system + RAG context + conversation)">Prompt Tokens</Tooltip></span>
            <span className="history-metric-value">{history.PromptTokens > 0 ? `~${history.PromptTokens.toLocaleString()}` : 'N/A'}</span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label">
              <Tooltip text="Estimated completion tokens in the assistant's response">Completion Tokens</Tooltip>
            </span>
            <span className="history-metric-value">
              {history.CompletionTokens > 0 ? `~${history.CompletionTokens.toLocaleString()}` : 'N/A'}
            </span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label">
              <Tooltip text="Tokens per second — completion tokens divided by total time from prompt sent to last token (TTLT)">TPS (Overall)</Tooltip>
            </span>
            <span className="history-metric-value">{formatTps(overallTps)}</span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label">
              <Tooltip text="Tokens per second — completion tokens divided by generation time (first token to last token)">TPS (Generation)</Tooltip>
            </span>
            <span className="history-metric-value">{formatTps(generationTps)}</span>
          </div>
          <div className="history-metric">
            <span className="history-metric-label"><Tooltip text="Timestamp when the assembled prompt was sent to the inference endpoint">Prompt Sent</Tooltip></span>
            <span className="history-metric-value history-metric-timestamp">{formatTimestamp(history.PromptSentUtc)}</span>
          </div>
        </div>
      </div>

      {/* === Messages === */}
      <div className="history-section">
        <div
          className="history-section-header history-section-toggle"
          onClick={() => setMessagesOpen(!messagesOpen)}
        >
          <Tooltip text="User message and assistant response for this conversation turn">Messages</Tooltip>
          <span className="history-toggle-icon">{messagesOpen ? '\u25BC' : '\u25B6'}</span>
        </div>
        {messagesOpen && (
          <div className="history-messages-grid">
            <div className="history-message-panel">
              <div className="history-message-heading">
                User Message
                <span className="history-message-ts">{formatTimestamp(history.UserMessageUtc)}</span>
              </div>
              <div className="json-view history-message-body">{history.UserMessage || '(none)'}</div>
            </div>
            <div className="history-message-panel">
              <div className="history-message-heading">Assistant Response</div>
              <div className="json-view history-message-body">{history.AssistantResponse || '(none)'}</div>
            </div>
          </div>
        )}
      </div>

      {/* === Query Rewrite === */}
      {history.QueryRewriteResult && (
        <div className="history-section">
          <div
            className="history-section-header history-section-toggle"
            onClick={() => setQueryRewriteOpen(!queryRewriteOpen)}
          >
            <Tooltip text="LLM-based query rewrite — the original prompt was rewritten into multiple queries for broader retrieval">Query Rewrite</Tooltip>
            <span className="history-toggle-icon">{queryRewriteOpen ? '\u25BC' : '\u25B6'}</span>
            <span className="history-section-badge">{formatMs(history.QueryRewriteDurationMs)}</span>
          </div>
          {queryRewriteOpen && (
            <div className="history-retrieval-body">
              {history.QueryRewriteResult.split('\n').filter(q => q.trim()).map((query, idx) => (
                <div key={idx} className="history-chunk-card">
                  <div className="history-chunk-header">
                    <span className="history-chunk-num">{idx === 0 ? 'Original' : `Variant ${idx}`}</span>
                  </div>
                  <div className="json-view history-chunk-content">{query.trim()}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* === Retrieval Context === */}
      <div className="history-section">
        <div
          className="history-section-header history-section-toggle"
          onClick={() => setRetrievalOpen(!retrievalOpen)}
        >
          <Tooltip text="Document retrieval phase — context fetched from the collection for RAG">Retrieval Context</Tooltip>
          <span className="history-toggle-icon">{retrievalOpen ? '\u25BC' : '\u25B6'}</span>
          <span className="history-section-badge">{formatMs(history.RetrievalDurationMs)}</span>
          {history.RetrievalStartUtc && (
            <span className="history-section-meta">started {formatTimestamp(history.RetrievalStartUtc)}</span>
          )}
        </div>
        {retrievalOpen && (
          <div className="history-retrieval-body">
            {!history.RetrievalContext ? (
              <div className="json-view">(no context retrieved)</div>
            ) : !retrievalChunks ? (
              <div className="json-view" style={{ maxHeight: '300px' }}>{history.RetrievalContext}</div>
            ) : (
              <>
                {/* Retrieval summary stats */}
                {retrievalStats && (
                  <div className="history-retrieval-summary">
                    <div className="history-retrieval-summary-stats">
                      <div className="history-metric">
                        <span className="history-metric-label">Documents</span>
                        <span className="history-metric-value">{retrievalStats.uniqueDocIds.length}</span>
                      </div>
                      <div className="history-metric">
                        <span className="history-metric-label">Chunks</span>
                        <span className="history-metric-value">{retrievalStats.totalChunks}</span>
                      </div>
                    </div>
                    {retrievalStats.uniqueDocIds.length > 0 && (
                      <div className="history-retrieval-doc-list">
                        <div className="history-retrieval-doc-list-label">Source Documents</div>
                        <ul className="history-retrieval-doc-items">
                          {retrievalStats.uniqueDocIds.map((docId) => (
                            <li key={docId} className="history-retrieval-doc-item">
                              <CopyableId id={docId} />
                              {docNames[docId] && (
                                <span className="history-retrieval-doc-name">{docNames[docId]}</span>
                              )}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}
                {/* Auto-populated citation notice */}
                {(() => {
                  const hasBracketCitations = history.AssistantResponse && /\[\d+\]/.test(history.AssistantResponse);
                  return retrievalChunks.length > 0 && !hasBracketCitations ? (
                    <div className="history-auto-populated-tag">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>
                      </svg>
                      Citations auto-populated — model did not produce inline [N] references
                    </div>
                  ) : null;
                })()}
                {/* Individual chunks */}
                {retrievalChunks.map((chunk, idx) => (
                  <div key={idx} className="history-chunk-card">
                    <div className="history-chunk-header">
                      <span className="history-chunk-num">Chunk {idx + 1}</span>
                      {chunk.score != null && (
                        <span className="history-chunk-score">Score: <strong>{chunk.score.toFixed(4)}</strong></span>
                      )}
                      {chunk.document_id && (
                        <span className="history-chunk-source">
                          Source: <CopyableId id={chunk.document_id} />
                          {docNames[chunk.document_id] && (
                            <span className="history-retrieval-doc-name">{docNames[chunk.document_id]}</span>
                          )}
                        </span>
                      )}
                    </div>
                    <div className="json-view history-chunk-content">
                      {chunk.content || '(empty)'}
                    </div>
                  </div>
                ))}
              </>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}

export default HistoryViewModal;
