import React, { useState } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';

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

  const [retrievalOpen, setRetrievalOpen] = useState(false);
  const [messagesOpen, setMessagesOpen] = useState(true);

  // Compute total pipeline duration for proportional bars
  const totalPipelineMs =
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

  return (
    <Modal title="History Details" onClose={onClose} extraWide footer={
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
            {(() => {
              if (!history.RetrievalContext) return <div className="json-view">(no context retrieved)</div>;
              let chunks;
              try { chunks = JSON.parse(history.RetrievalContext); } catch { chunks = null; }
              if (!Array.isArray(chunks)) {
                return <div className="json-view" style={{ maxHeight: '300px' }}>{history.RetrievalContext}</div>;
              }
              return chunks.map((chunk, idx) => (
                <div key={idx} className="history-chunk-card">
                  <div className="history-chunk-header">
                    <span className="history-chunk-num">Chunk {idx + 1}</span>
                    {chunk.score != null && (
                      <span className="history-chunk-score">Score: <strong>{chunk.score.toFixed(4)}</strong></span>
                    )}
                    {chunk.document_id && (
                      <span className="history-chunk-source">
                        Source: <CopyableId id={chunk.document_id} />
                      </span>
                    )}
                  </div>
                  <div className="json-view history-chunk-content">
                    {chunk.content || '(empty)'}
                  </div>
                </div>
              ));
            })()}
          </div>
        )}
      </div>
    </Modal>
  );
}

export default HistoryViewModal;
