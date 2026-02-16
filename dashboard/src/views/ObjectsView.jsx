import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import ActionMenu from '../components/ActionMenu';
import Tooltip from '../components/Tooltip';
import JsonViewModal from '../components/modals/JsonViewModal';
import DirectoryFormModal from '../components/modals/DirectoryFormModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function ObjectsView() {
  const { serverUrl, credential } = useAuth();
  const location = useLocation();
  const api = new ApiClient(serverUrl, credential?.BearerToken);

  const [buckets, setBuckets] = useState([]);
  const [selectedBucket, setSelectedBucket] = useState('');
  const [prefix, setPrefix] = useState('');
  const [objects, setObjects] = useState([]);
  const [prefixes, setPrefixes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refreshState, setRefreshState] = useState('idle');
  const [showCreateDir, setShowCreateDir] = useState(false);
  const [uploading, setUploading] = useState(false);
  const refreshTimerRef = useRef(null);
  const fileInputRef = useRef(null);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getBuckets();
        if (result && result.Objects) {
          setBuckets(result.Objects);
          const navBucket = location.state?.bucket;
          if (navBucket && result.Objects.some(b => b.Name === navBucket)) {
            setSelectedBucket(navBucket);
          } else if (result.Objects.length === 1) {
            setSelectedBucket(result.Objects[0].Name);
          }
        }
      } catch (err) { console.error('Failed to load buckets', err); }
    })();
  }, [serverUrl, credential]);

  useEffect(() => {
    return () => { if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current); };
  }, []);

  const loadObjects = useCallback(async () => {
    if (!selectedBucket) { setObjects([]); setPrefixes([]); return; }
    setLoading(true);
    try {
      const result = await api.getObjects(selectedBucket, prefix);
      setPrefixes(result.CommonPrefixes || []);
      setObjects(result.Objects || []);
    } catch (err) {
      console.error('Failed to load objects', err);
    } finally {
      setLoading(false);
    }
  }, [selectedBucket, prefix, serverUrl, credential]);

  useEffect(() => { loadObjects(); }, [loadObjects]);

  const handleRefresh = async () => {
    if (refreshState === 'spinning') return;
    setRefreshState('spinning');
    await loadObjects();
    setRefreshState('done');
    refreshTimerRef.current = setTimeout(() => setRefreshState('idle'), 1500);
  };

  const navigateTo = (newPrefix) => setPrefix(newPrefix);

  const navigateUp = () => {
    if (!prefix) return;
    const parts = prefix.replace(/\/$/, '').split('/');
    parts.pop();
    setPrefix(parts.length > 0 ? parts.join('/') + '/' : '');
  };

  const breadcrumbs = () => {
    const parts = prefix.replace(/\/$/, '').split('/').filter(Boolean);
    const crumbs = [{ label: selectedBucket, prefix: '' }];
    let accumulated = '';
    for (const part of parts) {
      accumulated += part + '/';
      crumbs.push({ label: part, prefix: accumulated });
    }
    return crumbs;
  };

  const formatSize = (bytes) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  const handleViewMetadata = async (key) => {
    try {
      const result = await api.getObjectMetadata(selectedBucket, key);
      setShowJson(result);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load metadata' });
    }
  };

  const handleDownload = (key) => {
    const url = api.getObjectDownloadUrl(selectedBucket, key);
    window.open(url, '_blank');
  };

  const handleDelete = async () => {
    try {
      await api.deleteObject(selectedBucket, deleteTarget);
      setDeleteTarget(null);
      loadObjects();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete object' });
    }
  };

  const getObjectName = (key) => {
    const withoutPrefix = key.startsWith(prefix) ? key.slice(prefix.length) : key;
    return withoutPrefix;
  };

  const handleUploadFiles = async (e) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;
    setUploading(true);
    try {
      for (const file of files) {
        const key = prefix + file.name;
        await api.uploadObject(selectedBucket, key, file);
      }
      loadObjects();
    } catch (err) {
      setAlert({ title: 'Upload Error', message: err.message || 'Failed to upload file(s)' });
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Objects</h1>
          <p className="content-subtitle">Browse objects in S3-compatible storage buckets.</p>
        </div>
      </div>

      <div style={{ marginBottom: '1rem' }}>
        <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)', marginRight: '0.5rem' }}><Tooltip text="S3-compatible storage bucket to browse">Bucket:</Tooltip></label>
        <select
          value={selectedBucket}
          onChange={(e) => { setSelectedBucket(e.target.value); setPrefix(''); }}
          style={{ padding: '0.5rem 0.75rem', border: '1px solid var(--input-border)', borderRadius: 'var(--radius-sm)', background: 'var(--input-bg)', color: 'var(--text-primary)', fontSize: '0.875rem', minWidth: '300px' }}
        >
          <option value="">Select a bucket...</option>
          {buckets.map(b => <option key={b.Name} value={b.Name}>{b.Name}</option>)}
        </select>
      </div>

      {selectedBucket && (
        <div className="data-table-container">
          <div className="data-table-toolbar">
            <div className="data-table-toolbar-left">
              <button
                className={`refresh-btn ${refreshState}`}
                onClick={handleRefresh}
                disabled={refreshState === 'spinning'}
                title="Refresh"
              >
                {refreshState === 'done' ? (
                  <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="3,8 7,12 13,4" />
                  </svg>
                ) : (
                  <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M14 8A6 6 0 1 1 10 2.5" />
                    <polyline points="14,2 14,6 10,6" />
                  </svg>
                )}
              </button>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', flexWrap: 'wrap' }}>
                {breadcrumbs().map((crumb, i) => (
                  <span key={i} style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                    {i > 0 && <span style={{ color: 'var(--text-secondary)' }}>/</span>}
                    <button
                      className="btn btn-ghost btn-sm"
                      style={{ padding: '0.125rem 0.375rem', fontSize: '0.8125rem' }}
                      onClick={() => navigateTo(crumb.prefix)}
                    >{crumb.label}</button>
                  </span>
                ))}
              </div>
            </div>
            <div className="data-table-toolbar-right">
              <input type="file" ref={fileInputRef} style={{ display: 'none' }} multiple onChange={handleUploadFiles} />
              <button className="btn btn-secondary btn-sm" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
                {uploading ? 'Uploading...' : 'Upload Files'}
              </button>
              <button className="btn btn-primary btn-sm" onClick={() => setShowCreateDir(true)}>Create Directory</button>
            </div>
          </div>

          {loading ? (
            <div className="loading"><div className="spinner" /></div>
          ) : (prefixes.length === 0 && objects.length === 0) ? (
            <div className="empty-state"><p>{prefix ? 'This prefix is empty.' : 'This bucket is empty.'}</p></div>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th><Tooltip text="File or folder name within the bucket">Name</Tooltip></th>
                  <th><Tooltip text="File size of the object">Size</Tooltip></th>
                  <th><Tooltip text="Date and time the object was last updated">Last Modified</Tooltip></th>
                  <th className="actions-cell"></th>
                </tr>
              </thead>
              <tbody>
                {prefix && (
                  <tr style={{ cursor: 'pointer' }} onClick={navigateUp}>
                    <td style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="var(--text-secondary)" strokeWidth="2"><polyline points="15 18 9 12 15 6"/></svg>
                      <span>..</span>
                    </td>
                    <td></td>
                    <td></td>
                    <td></td>
                  </tr>
                )}
                {prefixes.map((p, idx) => (
                  <tr key={'p-' + idx} style={{ cursor: 'pointer' }} onClick={() => navigateTo(p.Prefix)}>
                    <td style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="var(--warning-color)" stroke="var(--warning-color)" strokeWidth="1"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                      <span>{p.Prefix.slice(prefix.length).replace(/\/$/, '')}</span>
                    </td>
                    <td>--</td>
                    <td>--</td>
                    <td className="actions-cell" onClick={(e) => e.stopPropagation()}>
                      <ActionMenu items={[
                        { label: 'Delete', danger: true, onClick: () => setDeleteTarget(p.Prefix) },
                      ]} />
                    </td>
                  </tr>
                ))}
                {objects.map((obj, idx) => (
                  <tr key={'o-' + idx}>
                    <td style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="var(--text-secondary)" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                      <span>{getObjectName(obj.Key)}</span>
                    </td>
                    <td>{formatSize(obj.Size)}</td>
                    <td>{obj.LastModified ? new Date(obj.LastModified).toLocaleString() : ''}</td>
                    <td className="actions-cell">
                      <ActionMenu items={[
                        { label: 'View JSON', onClick: () => setShowJson(obj) },
                        { label: 'View Metadata', onClick: () => handleViewMetadata(obj.Key) },
                        { label: 'Download', onClick: () => handleDownload(obj.Key) },
                        { label: 'Delete', danger: true, onClick: () => setDeleteTarget(obj.Key) },
                      ]} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {showCreateDir && <DirectoryFormModal onSave={async (name) => { await api.createDirectory(selectedBucket, prefix + name + '/'); setShowCreateDir(false); loadObjects(); }} onClose={() => setShowCreateDir(false)} />}
      {showJson && <JsonViewModal title="Object Details" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Object" message={`Are you sure you want to delete "${deleteTarget}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default ObjectsView;
