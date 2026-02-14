import React from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';

function JsonViewModal({ title, data, onClose }) {
  const json = JSON.stringify(data, null, 2);

  return (
    <Modal title={title || 'JSON View'} onClose={onClose} wide footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '0.5rem' }}>
        <CopyButton text={json} />
      </div>
      <div className="json-view">{json}</div>
    </Modal>
  );
}

export default JsonViewModal;
