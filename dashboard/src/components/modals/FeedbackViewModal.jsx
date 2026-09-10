import React, { useMemo } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

const markdownComponents = {
  a: ({ href, children }) => (
    <a href={href} target="_blank" rel="noopener noreferrer">{children}</a>
  ),
  table: ({ children }) => (
    <div className="feedback-md-table-wrap"><table className="feedback-md-table">{children}</table></div>
  ),
};

function Markdown({ text }) {
  if (text === null || text === undefined || text === '') return <span className="feedback-md-empty">(none)</span>;
  return (
    <div className="feedback-md">
      <ReactMarkdown remarkPlugins={[remarkGfm]} components={markdownComponents}>{String(text)}</ReactMarkdown>
    </div>
  );
}

function FeedbackViewModal({ feedback, onClose }) {
  if (!feedback) return null;

  const messageHistory = useMemo(() => {
    if (!feedback.MessageHistory) return null;
    try {
      return JSON.parse(feedback.MessageHistory);
    } catch {
      return null;
    }
  }, [feedback.MessageHistory]);

  return (
    <Modal title="Feedback Details" onClose={onClose} className="feedback-detail-modal" footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      <div className="form-group">
        <label><Tooltip text="The user's rating for this assistant response">Rating</Tooltip></label>
        <p>{feedback.Rating === 'ThumbsUp' ? '👍 Thumbs Up' : '👎 Thumbs Down'}</p>
      </div>
      {feedback.FeedbackText && (
        <div className="form-group">
          <label><Tooltip text="Optional comments the user provided with their rating">Feedback Text</Tooltip></label>
          <div className="feedback-scroll" style={{ maxHeight: '160px' }}><Markdown text={feedback.FeedbackText} /></div>
        </div>
      )}
      <div className="form-group">
        <label><Tooltip text="The message the user sent that prompted this response">User Message</Tooltip></label>
        <div className="feedback-scroll" style={{ maxHeight: '220px' }}><Markdown text={feedback.UserMessage} /></div>
      </div>
      <div className="form-group">
        <label><Tooltip text="The assistant's response that was rated">Assistant Response</Tooltip></label>
        <div className="feedback-scroll" style={{ maxHeight: '320px' }}><Markdown text={feedback.AssistantResponse} /></div>
      </div>
      {messageHistory && messageHistory.length > 0 && (
        <div className="form-group">
          <label><Tooltip text="The full conversation history leading up to this feedback">Message History</Tooltip></label>
          <div className="feedback-scroll" style={{ maxHeight: '360px' }}>
            {messageHistory.map((msg, idx) => (
              <div key={idx} style={{ marginBottom: '12px' }}>
                <strong style={{ textTransform: 'capitalize' }}>{msg.role}:</strong>
                <div style={{ marginLeft: '8px' }}><Markdown text={msg.content} /></div>
              </div>
            ))}
          </div>
        </div>
      )}
    </Modal>
  );
}

export default FeedbackViewModal;
