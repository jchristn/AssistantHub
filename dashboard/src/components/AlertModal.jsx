import React from 'react';
import Modal from './Modal';

function AlertModal({ title, message, onClose }) {
  return (
    <div className="alert-modal">
      <Modal title={title || 'Alert'} onClose={onClose} footer={<button className="btn btn-primary" onClick={onClose}>OK</button>}>
        <p>{message}</p>
      </Modal>
    </div>
  );
}

export default AlertModal;
