import React, { useMemo } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

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
    <Modal title="Feedback Details" onClose={onClose} wide footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      <div className="form-group">
        <label><Tooltip text="The user's rating for this assistant response">Rating</Tooltip></label>
        <p>{feedback.Rating === 'ThumbsUp' ? '👍 Thumbs Up' : '👎 Thumbs Down'}</p>
      </div>
      {feedback.FeedbackText && (
        <div className="form-group">
          <label><Tooltip text="Optional comments the user provided with their rating">Feedback Text</Tooltip></label>
          <div className="json-view" style={{ maxHeight: '100px' }}>{feedback.FeedbackText}</div>
        </div>
      )}
      <div className="form-group">
        <label><Tooltip text="The message the user sent that prompted this response">User Message</Tooltip></label>
        <div className="json-view" style={{ maxHeight: '150px' }}>{feedback.UserMessage || '(none)'}</div>
      </div>
      <div className="form-group">
        <label><Tooltip text="The assistant's response that was rated">Assistant Response</Tooltip></label>
        <div className="json-view" style={{ maxHeight: '150px' }}>{feedback.AssistantResponse || '(none)'}</div>
      </div>
      {messageHistory && messageHistory.length > 0 && (
        <div className="form-group">
          <label><Tooltip text="The full conversation history leading up to this feedback">Message History</Tooltip></label>
          <div className="json-view" style={{ maxHeight: '300px' }}>
            {messageHistory.map((msg, idx) => (
              <div key={idx} style={{ marginBottom: '8px' }}>
                <strong style={{ textTransform: 'capitalize' }}>{msg.role}:</strong>
                <div style={{ whiteSpace: 'pre-wrap', marginLeft: '8px' }}>{msg.content}</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </Modal>
  );
}

export default FeedbackViewModal;
