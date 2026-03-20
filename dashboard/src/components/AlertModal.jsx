import React from 'react';
import Modal from './Modal';

function AlertModal({ title, message, content, loading, onClose, wide, extraWide }) {
  return (
    <div className="alert-modal">
      <Modal title={title || 'Alert'} onClose={loading ? undefined : onClose} wide={wide} extraWide={extraWide} footer={!loading && <button className="btn btn-primary" onClick={onClose}>OK</button>}>
        {loading && <div className="loading" style={{ marginBottom: '0.5rem' }}><div className="spinner" /></div>}
        {content || <p style={{ whiteSpace: 'pre-wrap' }}>{message}</p>}
      </Modal>
    </div>
  );
}

export default AlertModal;
