import React, { useState } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';

function HistoryViewModal({ history, onClose }) {
  if (!history) return null;

  const [retrievalOpen, setRetrievalOpen] = useState(false);

  const metricStyle = {
    display: 'inline-block',
    padding: '0.25rem 0.625rem',
    background: 'var(--bg-tertiary, var(--bg-secondary))',
    borderRadius: 'var(--radius-sm, 4px)',
    fontSize: '0.85rem',
    fontWeight: 500,
    color: 'var(--text-primary)',
    fontFamily: 'monospace',
  };

  const timestampStyle = {
    fontSize: '0.85rem',
    fontWeight: 500,
    color: 'var(--text-primary)',
  };

  const metricLabelStyle = {
    fontSize: '0.75rem',
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.03em',
    color: 'var(--text-secondary)',
    marginBottom: '0.15rem',
  };

  return (
    <Modal title="History Details" onClose={onClose} extraWide footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      <div className="form-group">
        <label><Tooltip text="The conversation thread this message belongs to">Thread ID</Tooltip></label>
        <CopyableId id={history.ThreadId} />
      </div>
      <div className="form-group">
        <label><Tooltip text="The assistant that handled this conversation">Assistant ID</Tooltip></label>
        <CopyableId id={history.AssistantId} />
      </div>
      {history.CollectionId && (
        <div className="form-group">
          <label><Tooltip text="The document collection used for retrieval-augmented generation">Collection ID</Tooltip></label>
          <CopyableId id={history.CollectionId} />
        </div>
      )}

      <div className="form-group">
        <label><Tooltip text="The message sent by the user in this conversation turn">User Message</Tooltip></label>
        <div style={{ marginBottom: '0.35rem' }}>
          <span style={timestampStyle}>
            {history.UserMessageUtc ? new Date(history.UserMessageUtc).toLocaleString() : ''}
          </span>
        </div>
        <div className="json-view" style={{ maxHeight: '150px' }}>{history.UserMessage || '(none)'}</div>
      </div>

      <div className="form-group">
        <label
          style={{ cursor: 'pointer', userSelect: 'none' }}
          onClick={() => setRetrievalOpen(!retrievalOpen)}
        >
          <Tooltip text="Document retrieval phase where relevant context is fetched from the collection">Retrieval</Tooltip> {retrievalOpen ? '\u25BC' : '\u25B6'}
        </label>
        <div style={{ display: 'flex', gap: '1rem', marginTop: '0.25rem' }}>
          <div>
            <div style={metricLabelStyle}><Tooltip text="Total time spent retrieving documents from the collection">Duration</Tooltip></div>
            <span style={metricStyle}>
              {history.RetrievalDurationMs > 0 ? `${history.RetrievalDurationMs.toFixed(2)} ms` : 'N/A'}
            </span>
          </div>
        </div>
        {retrievalOpen && (
          <>
            <div style={{ marginTop: '0.5rem', marginBottom: '0.25rem' }}>
              <span style={metricLabelStyle}><Tooltip text="When the retrieval phase began">Started:</Tooltip> </span>
              <span style={timestampStyle}>
                {history.RetrievalStartUtc ? new Date(history.RetrievalStartUtc).toLocaleString() : 'N/A'}
              </span>
            </div>
            {(() => {
              if (!history.RetrievalContext) return <div className="json-view">(no context retrieved)</div>;
              let chunks;
              try { chunks = JSON.parse(history.RetrievalContext); } catch { chunks = null; }
              if (!Array.isArray(chunks)) {
                return <div className="json-view" style={{ maxHeight: '200px' }}>{history.RetrievalContext}</div>;
              }
              return chunks.map((chunk, idx) => (
                <div key={idx} style={{
                  border: '1px solid var(--border-color, #e0e0e0)',
                  borderRadius: 'var(--radius-sm, 4px)',
                  padding: '0.75rem',
                  marginBottom: '0.5rem',
                  background: 'var(--bg-secondary)',
                }}>
                  <div style={{ display: 'flex', gap: '1rem', marginBottom: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}>
                    <div>
                      <span style={metricLabelStyle}>Chunk {idx + 1}</span>
                    </div>
                    {chunk.score != null && (
                      <div>
                        <span style={metricLabelStyle}>Score </span>
                        <span style={metricStyle}>{chunk.score.toFixed(4)}</span>
                      </div>
                    )}
                    {chunk.document_id && (
                      <div>
                        <span style={metricLabelStyle}>Source </span>
                        <CopyableId id={chunk.document_id} />
                      </div>
                    )}
                  </div>
                  <div className="json-view" style={{ maxHeight: '150px', fontSize: '0.8rem' }}>
                    {chunk.content || '(empty)'}
                  </div>
                </div>
              ));
            })()}
          </>
        )}
      </div>

      <div className="form-group">
        <label><Tooltip text="The inference phase where the prompt is sent to the language model">Inference</Tooltip></label>
        <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap', marginTop: '0.25rem' }}>
          <div>
            <div style={metricLabelStyle}><Tooltip text="Timestamp when the assembled prompt was sent to the model">Prompt Sent</Tooltip></div>
            <span style={timestampStyle}>
              {history.PromptSentUtc ? new Date(history.PromptSentUtc).toLocaleString() : 'N/A'}
            </span>
          </div>
          <div>
            <div style={metricLabelStyle}><Tooltip text="Estimated number of tokens in the prompt sent to the model (system message + RAG context + conversation history)">Prompt Tokens</Tooltip></div>
            <span style={metricStyle}>
              {history.PromptTokens > 0 ? `~${history.PromptTokens.toLocaleString()}` : 'N/A'}
            </span>
          </div>
          <div>
            <div style={metricLabelStyle}><Tooltip text="Time to first token — how long before the model began streaming its response">TTFT</Tooltip></div>
            <span style={metricStyle}>
              {history.TimeToFirstTokenMs > 0 ? `${history.TimeToFirstTokenMs.toFixed(2)} ms` : 'N/A'}
            </span>
          </div>
          <div>
            <div style={metricLabelStyle}><Tooltip text="Time to last token — total time from prompt sent to the final token received">TTLT</Tooltip></div>
            <span style={metricStyle}>
              {history.TimeToLastTokenMs > 0 ? `${history.TimeToLastTokenMs.toFixed(2)} ms` : 'N/A'}
            </span>
          </div>
        </div>
      </div>

      <div className="form-group">
        <label><Tooltip text="The response generated by the assistant for this conversation turn">Assistant Response</Tooltip></label>
        <div className="json-view" style={{ maxHeight: '200px' }}>{history.AssistantResponse || '(none)'}</div>
      </div>
    </Modal>
  );
}

export default HistoryViewModal;
