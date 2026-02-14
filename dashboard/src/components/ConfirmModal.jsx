import React from 'react';
import Modal from './Modal';

function ConfirmModal({ title, message, onConfirm, onClose, confirmLabel, danger }) {
  return (
    <div className="confirm-modal">
      <Modal title={title || 'Confirm'} onClose={onClose} footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`} onClick={onConfirm}>{confirmLabel || 'Confirm'}</button>
        </>
      }>
        <p>{message}</p>
      </Modal>
    </div>
  );
}

export default ConfirmModal;
