import React from 'react';
import Modal from './Modal';

function ConfirmModal({ title, message, onConfirm, onClose, confirmLabel, danger, isLoading, loadingLabel }) {
  const effectiveConfirmLabel = isLoading ? (loadingLabel || 'Working...') : (confirmLabel || 'Confirm');
  const handleClose = () => {
    if (!isLoading) onClose?.();
  };

  return (
    <div className="confirm-modal">
      <Modal title={title || 'Confirm'} onClose={handleClose} footer={
        <>
          <button className="btn btn-secondary" onClick={handleClose} disabled={isLoading}>Cancel</button>
          <button className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`} onClick={onConfirm} disabled={isLoading}>{effectiveConfirmLabel}</button>
        </>
      }>
        <p>{message}</p>
      </Modal>
    </div>
  );
}

export default ConfirmModal;
