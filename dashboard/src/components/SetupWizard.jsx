import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Modal from './Modal';

const INGESTION_STATUS_PERCENT = {
  uploading: 10, uploaded: 15, typedetecting: 25, typedetectionsuccess: 30,
  processing: 40, processingchunks: 55, summarizing: 60,
  storingembeddings: 80, completed: 100, indexed: 100, active: 100, failed: 100, error: 100,
};

function isIngestionFinalStatus(s) {
  if (!s) return false;
  const low = s.toLowerCase();
  return low === 'completed' || low === 'indexed' || low === 'active' || low === 'failed' || low === 'error';
}

const STEP_DEFS = [
  { key: 'embedding',   title: 'Embedding Endpoint' },
  { key: 'inference',    title: 'Inference Endpoint' },
  { key: 'bucket',       title: 'Storage Bucket' },
  { key: 'collection',   title: 'Vector Collection' },
  { key: 'assistant',    title: 'Assistant' },
  { key: 'settings',     title: 'Assistant Settings' },
  { key: 'upload',       title: 'Upload Documents' },
  { key: 'launch',       title: 'Launch Chat' },
];

function SetupWizard({ onClose }) {
  const { serverUrl, credential } = useAuth();
  const navigate = useNavigate();
  const api = useRef(new ApiClient(serverUrl, credential?.BearerToken)).current;

  const [step, setStep] = useState(0);
  const [createdItems, setCreatedItems] = useState({});
  const [existingItems, setExistingItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({});

  // For step 5 settings dropdowns
  const [collections, setCollections] = useState([]);
  const [inferenceEndpoints, setInferenceEndpoints] = useState([]);
  const [embeddingEndpoints, setEmbeddingEndpoints] = useState([]);

  // For step 6 file upload and ingestion tracking
  const [file, setFile] = useState(null);
  const [ingestionRules, setIngestionRules] = useState([]);
  const [uploadedDocId, setUploadedDocId] = useState(null);
  const [ingestionStatus, setIngestionStatus] = useState(null);
  const [ingestionPercent, setIngestionPercent] = useState(0);
  const [ingestionLog, setIngestionLog] = useState(null);
  const [ingestionDone, setIngestionDone] = useState(false);
  const [ingestionError, setIngestionError] = useState(null);
  const pollRef = useRef(null);

  const current = STEP_DEFS[step];

  const setField = (key, value) => setForm(prev => ({ ...prev, [key]: value }));

  const stopPolling = useCallback(() => {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }, []);

  const startPolling = useCallback((docId) => {
    stopPolling();
    const poll = async () => {
      try {
        const [doc, logResult] = await Promise.all([
          api.getDocument(docId),
          api.getDocumentProcessingLog(docId),
        ]);
        if (doc) {
          const status = doc.Status || '';
          const pct = INGESTION_STATUS_PERCENT[status.toLowerCase()] ?? 10;
          setIngestionStatus(status);
          setIngestionPercent(pct);
          if (isIngestionFinalStatus(status)) {
            stopPolling();
            setIngestionDone(true);
            setSaving(false);
            const low = status.toLowerCase();
            if (low === 'failed' || low === 'error') {
              setIngestionError(doc.StatusMessage || doc.ErrorMessage || 'Processing failed');
            }
          }
        }
        if (logResult?.Log) {
          setIngestionLog(logResult.Log);
        }
      } catch { /* polling error, retry next tick */ }
    };
    poll();
    pollRef.current = setInterval(poll, 3000);
  }, [api, stopPolling]);

  // Cleanup polling on unmount
  useEffect(() => {
    return () => stopPolling();
  }, [stopPolling]);

  const loadExisting = useCallback(async () => {
    setLoading(true);
    setExistingItems([]);
    setError('');
    try {
      let result;
      switch (step) {
        case 0: {
          result = await api.enumerateEmbeddingEndpoints({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setExistingItems(items);
          break;
        }
        case 1: {
          result = await api.enumerateCompletionEndpoints({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setExistingItems(items);
          break;
        }
        case 2: {
          result = await api.getBuckets({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setExistingItems(items);
          break;
        }
        case 3: {
          result = await api.getCollections({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setExistingItems(items);
          break;
        }
        case 4: {
          result = await api.getAssistants({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setExistingItems(items);
          break;
        }
        case 5: {
          const [colResult, infResult, embResult] = await Promise.all([
            api.getCollections({ maxResults: 1000 }),
            api.enumerateCompletionEndpoints({ maxResults: 1000 }),
            api.enumerateEmbeddingEndpoints({ maxResults: 1000 }),
          ]);
          setCollections((colResult?.Objects) || (Array.isArray(colResult) ? colResult : []));
          setInferenceEndpoints((infResult?.Objects) || (Array.isArray(infResult) ? infResult : []));
          setEmbeddingEndpoints((embResult?.Objects) || (Array.isArray(embResult) ? embResult : []));
          break;
        }
        case 6: {
          result = await api.getIngestionRules({ maxResults: 100 });
          const items = result?.Objects || (Array.isArray(result) ? result : []);
          setIngestionRules(items);
          if (items.length === 1) {
            setForm(prev => ({ ...prev, IngestionRuleId: items[0].Id }));
          }
          break;
        }
        default:
          break;
      }
    } catch (err) {
      setError('Failed to load existing items: ' + err.message);
    } finally {
      setLoading(false);
    }
  }, [step, api]);

  useEffect(() => {
    setForm({});
    setFile(null);
    setError('');
    setUploadedDocId(null);
    setIngestionStatus(null);
    setIngestionPercent(0);
    setIngestionLog(null);
    setIngestionDone(false);
    setIngestionError(null);
    stopPolling();
    if (step <= 6) loadExisting();
  }, [step, loadExisting, stopPolling]);

  // Pre-populate settings form when entering step 5
  useEffect(() => {
    if (step === 5) {
      setForm({
        Model: 'gemma3:4b',
        Temperature: 0.7,
        SystemPrompt: 'You are a helpful assistant. Use the provided context to answer questions accurately.',
        EnableRag: true,
        CollectionId: createdItems.collection || '',
        InferenceEndpointId: createdItems.inference || '',
        EmbeddingEndpointId: createdItems.embedding || '',
      });
    }
  }, [step, createdItems]);

  const handleUseExisting = (item) => {
    const id = item.Id || item.Name || item.GUID;
    setCreatedItems(prev => ({ ...prev, [current.key]: id }));
    setStep(step + 1);
  };

  const handleCreate = async () => {
    setSaving(true);
    setError('');
    try {
      let result;
      switch (step) {
        case 0:
          result = await api.createEmbeddingEndpoint({
            Model: form.Model || '',
            Endpoint: form.Endpoint || '',
            ApiFormat: form.ApiFormat || 'OpenAI',
            ApiKey: form.ApiKey || '',
          });
          setCreatedItems(prev => ({ ...prev, embedding: result.Id || result.GUID }));
          break;
        case 1:
          result = await api.createCompletionEndpoint({
            Name: form.Name || '',
            Model: form.Model || '',
            Endpoint: form.Endpoint || '',
            ApiFormat: form.ApiFormat || 'OpenAI',
            ApiKey: form.ApiKey || '',
          });
          setCreatedItems(prev => ({ ...prev, inference: result.Id || result.GUID }));
          break;
        case 2:
          result = await api.createBucket({ Name: form.Name || '' });
          setCreatedItems(prev => ({ ...prev, bucket: result.Name || form.Name }));
          break;
        case 3:
          result = await api.createCollection({
            Name: form.Name || '',
            Description: form.Description || '',
            Dimensionality: parseInt(form.Dimensionality) || 768,
          });
          setCreatedItems(prev => ({ ...prev, collection: result.Id || result.GUID }));
          break;
        case 4:
          result = await api.createAssistant({
            Name: form.Name || '',
            Description: form.Description || '',
          });
          setCreatedItems(prev => ({ ...prev, assistant: result.Id || result.GUID }));
          break;
        default:
          break;
      }
      setSaving(false);
      setStep(step + 1);
    } catch (err) {
      setError(err.message);
      setSaving(false);
    }
  };

  const handleSaveSettings = async () => {
    const assistantId = createdItems.assistant;
    if (!assistantId) {
      setError('No assistant selected');
      return;
    }
    setSaving(true);
    setError('');
    try {
      await api.updateAssistantSettings(assistantId, {
        Model: form.Model || 'gemma3:4b',
        Temperature: parseFloat(form.Temperature) || 0.7,
        SystemPrompt: form.SystemPrompt || '',
        EnableRag: form.EnableRag ?? true,
        CollectionId: form.CollectionId || '',
        InferenceEndpointId: form.InferenceEndpointId || '',
        EmbeddingEndpointId: form.EmbeddingEndpointId || '',
      });
      setSaving(false);
      setStep(step + 1);
    } catch (err) {
      setError(err.message);
      setSaving(false);
    }
  };

  const handleUpload = async () => {
    if (!file || !form.IngestionRuleId) return;
    setSaving(true);
    setError('');
    setUploadedDocId(null);
    setIngestionStatus(null);
    setIngestionPercent(0);
    setIngestionLog(null);
    setIngestionDone(false);
    setIngestionError(null);
    try {
      const reader = new FileReader();
      const base64Content = await new Promise((resolve, reject) => {
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
      });

      const result = await api.uploadDocument({
        IngestionRuleId: form.IngestionRuleId,
        Name: file.name,
        OriginalFilename: file.name,
        ContentType: file.type || 'application/octet-stream',
        Base64Content: base64Content,
      });
      const docId = result?.Id || result?.GUID || result?.id;
      if (docId) {
        setUploadedDocId(docId);
        setIngestionStatus('Uploaded');
        setIngestionPercent(15);
        startPolling(docId);
      } else {
        setSaving(false);
        setStep(step + 1);
      }
    } catch (err) {
      setError(err.message);
      setSaving(false);
    }
  };

  const handleLaunch = () => {
    localStorage.setItem('ah_wizardCompleted', 'true');
    onClose();
    const assistantId = createdItems.assistant;
    if (assistantId) {
      window.open(`/chat/${assistantId}`, '_blank');
    }
  };

  const handleSkip = () => {
    localStorage.setItem('ah_wizardCompleted', 'true');
    onClose();
  };

  const renderStepDots = () => (
    <div className="wizard-steps-indicator">
      {STEP_DEFS.map((s, i) => (
        <div
          key={s.key}
          className={`wizard-step-dot ${i === step ? 'active' : ''} ${i < step ? 'completed' : ''}`}
          title={s.title}
        />
      ))}
    </div>
  );

  const renderExistingItems = () => {
    if (existingItems.length === 0) return null;
    return (
      <div className="wizard-existing-items">
        <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
          Use an existing item:
        </p>
        {existingItems.map((item, i) => {
          const label = item.Name || item.Model || item.Id || item.GUID || `Item ${i + 1}`;
          const sub = item.Model && item.Name ? item.Model : item.Id || item.GUID || '';
          return (
            <button
              key={item.Id || item.GUID || item.Name || i}
              className="btn btn-secondary btn-sm"
              style={{ display: 'block', width: '100%', textAlign: 'left', marginBottom: '0.375rem' }}
              onClick={() => handleUseExisting(item)}
            >
              <span>{label}</span>
              {sub && sub !== label && (
                <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginLeft: '0.5rem' }}>
                  {sub.length > 40 ? sub.substring(0, 40) + '...' : sub}
                </span>
              )}
            </button>
          );
        })}
        <div className="wizard-separator">
          <span>or create new</span>
        </div>
      </div>
    );
  };

  const renderGenericForm = () => {
    switch (step) {
      case 0: // Embedding Endpoint
        return (
          <div className="wizard-step-form">
            <div className="form-group">
              <label>Model</label>
              <input type="text" value={form.Model || ''} onChange={e => setField('Model', e.target.value)} placeholder="e.g. nomic-embed-text" />
            </div>
            <div className="form-group">
              <label>Endpoint</label>
              <input type="text" value={form.Endpoint || ''} onChange={e => setField('Endpoint', e.target.value)} placeholder="e.g. http://localhost:11434" />
            </div>
            <div className="form-group">
              <label>API Format</label>
              <select value={form.ApiFormat || 'OpenAI'} onChange={e => setField('ApiFormat', e.target.value)}>
                <option value="OpenAI">OpenAI</option>
                <option value="Ollama">Ollama</option>
                <option value="VoyageAI">VoyageAI</option>
              </select>
            </div>
            <div className="form-group">
              <label>API Key (optional)</label>
              <input type="password" value={form.ApiKey || ''} onChange={e => setField('ApiKey', e.target.value)} placeholder="Leave blank if not required" />
            </div>
          </div>
        );
      case 1: // Inference Endpoint
        return (
          <div className="wizard-step-form">
            <div className="form-group">
              <label>Name</label>
              <input type="text" value={form.Name || ''} onChange={e => setField('Name', e.target.value)} placeholder="e.g. Local Ollama" />
            </div>
            <div className="form-group">
              <label>Model</label>
              <input type="text" value={form.Model || ''} onChange={e => setField('Model', e.target.value)} placeholder="e.g. gemma3:4b" />
            </div>
            <div className="form-group">
              <label>Endpoint</label>
              <input type="text" value={form.Endpoint || ''} onChange={e => setField('Endpoint', e.target.value)} placeholder="e.g. http://localhost:11434" />
            </div>
            <div className="form-group">
              <label>API Format</label>
              <select value={form.ApiFormat || 'OpenAI'} onChange={e => setField('ApiFormat', e.target.value)}>
                <option value="OpenAI">OpenAI</option>
                <option value="Ollama">Ollama</option>
              </select>
            </div>
            <div className="form-group">
              <label>API Key (optional)</label>
              <input type="password" value={form.ApiKey || ''} onChange={e => setField('ApiKey', e.target.value)} placeholder="Leave blank if not required" />
            </div>
          </div>
        );
      case 2: // Storage Bucket
        return (
          <div className="wizard-step-form">
            <div className="form-group">
              <label>Bucket Name</label>
              <input type="text" value={form.Name || ''} onChange={e => setField('Name', e.target.value)} placeholder="e.g. my-documents" />
            </div>
          </div>
        );
      case 3: // Vector Collection
        return (
          <div className="wizard-step-form">
            <div className="form-group">
              <label>Collection Name</label>
              <input type="text" value={form.Name || ''} onChange={e => setField('Name', e.target.value)} placeholder="e.g. knowledge-base" />
            </div>
            <div className="form-group">
              <label>Description</label>
              <input type="text" value={form.Description || ''} onChange={e => setField('Description', e.target.value)} placeholder="What this collection is for" />
            </div>
            <div className="form-group">
              <label>Dimensionality</label>
              <input type="number" value={form.Dimensionality || 768} onChange={e => setField('Dimensionality', e.target.value)} min="1" />
            </div>
          </div>
        );
      case 4: // Assistant
        return (
          <div className="wizard-step-form">
            <div className="form-group">
              <label>Assistant Name</label>
              <input type="text" value={form.Name || ''} onChange={e => setField('Name', e.target.value)} placeholder="e.g. My Assistant" />
            </div>
            <div className="form-group">
              <label>Description</label>
              <input type="text" value={form.Description || ''} onChange={e => setField('Description', e.target.value)} placeholder="What this assistant does" />
            </div>
          </div>
        );
      default:
        return null;
    }
  };

  const renderSettingsForm = () => (
    <div className="wizard-step-form">
      <div className="form-group">
        <label>Model</label>
        <input type="text" value={form.Model || ''} onChange={e => setField('Model', e.target.value)} />
      </div>
      <div className="form-group">
        <label>Temperature <span className="range-value">{form.Temperature}</span></label>
        <input type="range" min="0" max="2" step="0.1" value={form.Temperature || 0.7} onChange={e => setField('Temperature', parseFloat(e.target.value))} />
      </div>
      <div className="form-group">
        <label>System Prompt</label>
        <textarea value={form.SystemPrompt || ''} onChange={e => setField('SystemPrompt', e.target.value)} rows={3} />
      </div>
      <div className="form-group">
        <label>Collection</label>
        <select value={form.CollectionId || ''} onChange={e => setField('CollectionId', e.target.value)}>
          <option value="">-- Select a collection --</option>
          {collections.map(c => (
            <option key={c.Id} value={c.Id}>{c.Name || c.Id}</option>
          ))}
        </select>
      </div>
      <div className="form-group">
        <label>Inference Endpoint</label>
        <select value={form.InferenceEndpointId || ''} onChange={e => setField('InferenceEndpointId', e.target.value)}>
          <option value="">-- Select an inference endpoint --</option>
          {inferenceEndpoints.map(ep => (
            <option key={ep.Id} value={ep.Id}>{ep.Name || ep.Model || ep.Id}</option>
          ))}
        </select>
      </div>
      <div className="form-group">
        <label>Embedding Endpoint</label>
        <select value={form.EmbeddingEndpointId || ''} onChange={e => setField('EmbeddingEndpointId', e.target.value)}>
          <option value="">-- Select an embedding endpoint --</option>
          {embeddingEndpoints.map(ep => (
            <option key={ep.Id} value={ep.Id}>{ep.Model || ep.Id}</option>
          ))}
        </select>
      </div>
    </div>
  );

  const renderUploadForm = () => {
    // Show progress tracking after upload has started
    if (uploadedDocId) {
      const isError = ingestionError != null;
      const isComplete = ingestionDone && !isError;
      return (
        <div className="wizard-step-form">
          <div style={{ marginBottom: '1rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.375rem' }}>
              <span style={{ fontWeight: 500, fontSize: '0.875rem' }}>{file?.name || 'Document'}</span>
              <span style={{ fontSize: '0.8125rem', color: isError ? 'var(--danger-color)' : isComplete ? 'var(--success-color)' : 'var(--text-secondary)' }}>
                {isError ? 'Failed' : isComplete ? 'Completed' : (ingestionStatus || 'Processing...')} — {ingestionPercent}%
              </span>
            </div>
            <div style={{ width: '100%', height: '6px', background: 'var(--border-color)', borderRadius: '3px', overflow: 'hidden' }}>
              <div style={{
                width: `${ingestionPercent}%`,
                height: '100%',
                background: isError ? 'var(--danger-color)' : isComplete ? 'var(--success-color)' : 'var(--primary-color)',
                borderRadius: '3px',
                transition: 'width 0.5s ease',
              }} />
            </div>
          </div>
          {isError && (
            <div style={{ color: 'var(--danger-color)', fontSize: '0.8125rem', marginBottom: '0.75rem', padding: '0.5rem', background: 'rgba(220,53,69,0.1)', borderRadius: 'var(--radius-sm)' }}>
              {ingestionError}
            </div>
          )}
          {ingestionLog && (
            <div>
              <label style={{ fontSize: '0.8125rem', fontWeight: 500, marginBottom: '0.25rem', display: 'block' }}>Processing Log</label>
              <pre style={{
                background: 'var(--bg-secondary, #1a1a2e)',
                padding: '0.75rem',
                borderRadius: '6px',
                overflow: 'auto',
                maxHeight: '250px',
                fontSize: '0.75rem',
                lineHeight: '1.4',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              }}>{ingestionLog}</pre>
            </div>
          )}
          {!ingestionDone && (
            <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Please wait while the document is being processed...
            </p>
          )}
        </div>
      );
    }

    return (
      <div className="wizard-step-form">
        <div className="form-group">
          <label>Ingestion Rule</label>
          <select value={form.IngestionRuleId || ''} onChange={e => setField('IngestionRuleId', e.target.value)}>
            <option value="">Select an ingestion rule...</option>
            {ingestionRules.map(r => (
              <option key={r.Id} value={r.Id}>{r.Name} ({r.Id.substring(0, 12)}...)</option>
            ))}
          </select>
        </div>
        <div className="form-group">
          <label>File</label>
          <input type="file" onChange={e => setFile(e.target.files[0] || null)} />
        </div>
        {file && (
          <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
            Selected: {file.name} ({(file.size / 1024).toFixed(1)} KB)
          </p>
        )}
        <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
          You can skip this step and upload documents later from the Documents page.
        </p>
      </div>
    );
  };

  const renderLaunchStep = () => (
    <div style={{ textAlign: 'center', padding: '2rem 1rem' }}>
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--success-color)" strokeWidth="2" style={{ width: 48, height: 48, marginBottom: '1rem' }}>
        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
        <polyline points="22 4 12 14.01 9 11.01" />
      </svg>
      <h3 style={{ marginBottom: '0.5rem' }}>Setup Complete!</h3>
      <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', marginBottom: '1.5rem' }}>
        Your assistant is ready. You can now open a chat session or close this wizard and explore the dashboard.
      </p>
      <button className="btn btn-primary" onClick={handleLaunch}>
        Open Chat
      </button>
    </div>
  );

  const renderBody = () => {
    if (loading) {
      return <div className="loading"><div className="spinner" /></div>;
    }
    if (step === 5) return renderSettingsForm();
    if (step === 6) return renderUploadForm();
    if (step === 7) return renderLaunchStep();
    return (
      <>
        {renderExistingItems()}
        {renderGenericForm()}
      </>
    );
  };

  const renderFooter = () => {
    if (step === 7) {
      return (
        <button className="btn btn-secondary" onClick={handleSkip}>Close</button>
      );
    }

    const canGoBack = step > 0;
    let primaryAction, primaryLabel, primaryDisabled;

    if (step <= 4) {
      primaryAction = handleCreate;
      primaryLabel = saving ? 'Creating...' : 'Create & Continue';
      primaryDisabled = saving;
    } else if (step === 5) {
      primaryAction = handleSaveSettings;
      primaryLabel = saving ? 'Saving...' : 'Save & Continue';
      primaryDisabled = saving;
    } else if (step === 6) {
      if (ingestionDone) {
        primaryAction = () => { stopPolling(); setStep(step + 1); };
        primaryLabel = 'Continue';
        primaryDisabled = false;
      } else if (uploadedDocId) {
        primaryAction = null;
        primaryLabel = 'Processing...';
        primaryDisabled = true;
      } else {
        primaryAction = handleUpload;
        primaryLabel = saving ? 'Uploading...' : 'Upload & Ingest';
        primaryDisabled = saving || (!file || !form.IngestionRuleId);
      }
    }

    return (
      <>
        <button className="btn btn-ghost btn-sm" onClick={handleSkip}>Skip Wizard</button>
        <div style={{ flex: 1 }} />
        {canGoBack && (
          <button className="btn btn-secondary" onClick={() => setStep(step - 1)} disabled={saving || (uploadedDocId && !ingestionDone)}>Previous</button>
        )}
        {step === 6 && !uploadedDocId && (
          <button className="btn btn-secondary" onClick={() => setStep(step + 1)}>Skip Step</button>
        )}
        <button className="btn btn-primary" onClick={primaryAction} disabled={primaryDisabled}>
          {primaryLabel}
        </button>
      </>
    );
  };

  return (
    <Modal
      title={
        <div>
          <span>Setup Wizard — {current.title}</span>
          {renderStepDots()}
        </div>
      }
      onClose={handleSkip}
      wide
      className={step === 6 ? 'wizard-upload' : undefined}
      footer={renderFooter()}
    >
      {error && (
        <div style={{ color: 'var(--danger-color)', fontSize: '0.875rem', marginBottom: '0.75rem', padding: '0.5rem', background: 'rgba(220,53,69,0.1)', borderRadius: 'var(--radius-sm)' }}>
          {error}
        </div>
      )}
      {renderBody()}
    </Modal>
  );
}

export default SetupWizard;
