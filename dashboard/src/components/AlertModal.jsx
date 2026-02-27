import React from 'react';
import Modal from './Modal';

function AlertModal({ title, message, loading, onClose }) {
  return (
    <div className="alert-modal">
      <Modal title={title || 'Alert'} onClose={loading ? undefined : onClose} footer={!loading && <button className="btn btn-primary" onClick={onClose}>OK</button>}>
        {loading && <div className="loading" style={{ marginBottom: '0.5rem' }}><div className="spinner" /></div>}
        <p>{message}</p>
      </Modal>
    </div>
  );
}

export default AlertModal;
