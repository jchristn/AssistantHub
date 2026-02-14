import React, { useState, useRef } from 'react';
import Modal from '../Modal';

function DocumentUploadModal({ assistantId, onUpload, onClose }) {
  const [file, setFile] = useState(null);
  const [name, setName] = useState('');
  const [uploading, setUploading] = useState(false);
  const fileRef = useRef(null);

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) {
      setFile(selectedFile);
      if (!name) setName(selectedFile.name);
    }
  };

  const handleSubmit = async () => {
    if (!file) return;
    setUploading(true);
    try {
      await onUpload({
        AssistantId: assistantId,
        Name: name || file.name,
        OriginalFilename: file.name,
        ContentType: file.type || 'application/octet-stream',
        SizeBytes: file.size,
        file: file
      });
    } finally {
      setUploading(false);
    }
  };

  return (
    <Modal title="Upload Document" onClose={onClose} footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={!file || uploading}>
          {uploading ? 'Uploading...' : 'Upload'}
        </button>
      </>
    }>
      <div className="form-group">
        <label>Document Name</label>
        <input type="text" value={name} onChange={(e) => setName(e.target.value)} placeholder="Enter document name" />
      </div>
      <div className="form-group">
        <label>File</label>
        <input ref={fileRef} type="file" onChange={handleFileChange} />
      </div>
      {file && (
        <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
          <p>File: {file.name}</p>
          <p>Size: {(file.size / 1024).toFixed(1)} KB</p>
          <p>Type: {file.type || 'unknown'}</p>
        </div>
      )}
    </Modal>
  );
}

export default DocumentUploadModal;
