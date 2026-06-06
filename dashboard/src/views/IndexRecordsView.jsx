import React, { useState, useEffect, useCallback } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyableId from '../components/CopyableId';
import ActionMenu from '../components/ActionMenu';
import Modal from '../components/Modal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import {
  BadgeList,
  TagBadges,
  formatJson,
  getContentSha256,
  getCustomMetadata,
  getDocumentLength,
  getIndexId,
  getIndexedDate,
  getIndexingRuntimeMs,
  getIsDeleted,
  getLabels,
  getLastModified,
  getOriginalFileName,
  getRecordContent,
  getRecordId,
  getRecordPath,
  getRecordTerms,
  getTags,
  parseJsonInput,
  parseListInput,
  parseTagsInput,
  tagsToInput,
  unwrapObjects,
} from '../utils/artifactSearch.jsx';

const emptyRecordForm = { Id: '', Name: '', Content: '', Labels: '', Tags: '', CustomMetadata: '{}' };
const numberFormatter = new Intl.NumberFormat();

const formatDateTime = (value) => {
  if (!value) return '';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
};

const formatInteger = (value) => {
  if (value == null || value === '') return '';
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numberFormatter.format(numeric) : String(value);
};

const formatRuntime = (value) => {
  if (value == null || value === '') return '';
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) return String(value);
  return `${numeric.toFixed(numeric >= 10 ? 0 : 1)} ms`;
};

const getSourceDocumentId = (record) => {
  const metadata = getCustomMetadata(record);
  return metadata.AssistantHubDocumentId || metadata.AssistantDocumentId || metadata.DocumentId || metadata.ObjectId || '';
};

function IndexRecordsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const requestedIndex = searchParams.get('indexId') || location.state?.indexId || '';
  const [indices, setIndices] = useState([]);
  const [selectedIndex, setSelectedIndex] = useState('');
  const [records, setRecords] = useState([]);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [filters, setFilters] = useState({ label: '', tags: '', recordId: '', maxResults: 100 });
  const [recordForm, setRecordForm] = useState(emptyRecordForm);
  const [batchJson, setBatchJson] = useState('{\n  "Documents": []\n}');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [showBatchCreate, setShowBatchCreate] = useState(false);
  const [showJson, setShowJson] = useState(null);
  const [detailTarget, setDetailTarget] = useState(null);
  const [editTarget, setEditTarget] = useState(null);
  const [editForm, setEditForm] = useState({ Labels: '', Tags: '', CustomMetadata: '{}' });
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false);
  const [alert, setAlert] = useState(null);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getIndices({ maxResults: 1000 });
        const items = unwrapObjects(result);
        setIndices(items);
        setSelectedIndex((current) => {
          if (requestedIndex && items.some((item) => getIndexId(item) === requestedIndex)) return requestedIndex;
          if (!current && items.length === 1) return getIndexId(items[0]);
          return current;
        });
      } catch (err) {
        setAlert({ title: 'Error', message: err.message || 'Failed to load indices' });
      }
    })();
  }, [serverUrl, credential, requestedIndex]);

  const loadRecords = useCallback(async () => {
    setSelectedIds(new Set());
    if (!selectedIndex) {
      setRecords([]);
      return;
    }

    const query = {
      maxResults: Number(filters.maxResults) || 100,
    };
    const labels = parseListInput(filters.label);
    if (labels.length > 0) query.labels = labels;
    Object.entries(parseTagsInput(filters.tags)).forEach(([key, value]) => {
      query[`tag.${key}`] = value;
    });

    setLoading(true);
    setError('');
    try {
      const result = await api.getIndexRecords(selectedIndex, query);
      let items = unwrapObjects(result);
      if (filters.recordId.trim()) {
        const needle = filters.recordId.trim().toLowerCase();
        items = items.filter((record) => [
          getRecordId(record),
          getRecordPath(record),
          getOriginalFileName(record),
          getContentSha256(record),
        ].some((value) => String(value || '').toLowerCase().includes(needle)));
      }
      setRecords(items);
    } catch (err) {
      setError(err.message || 'Failed to load records');
      setRecords([]);
    } finally {
      setLoading(false);
    }
  }, [selectedIndex, filters, serverUrl, credential]);

  useEffect(() => { loadRecords(); }, [loadRecords]);

  const buildRecordBody = (source) => {
    const body = {
      Name: source.Name || source.DocumentPath || source.Id || 'record',
      Content: source.Content,
    };
    if (source.Id) body.Id = source.Id;
    const labels = parseListInput(source.Labels);
    const tags = parseTagsInput(source.Tags);
    const metadata = parseJsonInput(source.CustomMetadata, {});
    if (labels.length > 0) body.Labels = labels;
    if (Object.keys(tags).length > 0) body.Tags = tags;
    if (Object.keys(metadata).length > 0) body.CustomMetadata = metadata;
    return body;
  };

  const createRecord = async (e) => {
    e.preventDefault();
    try {
      await api.createIndexRecord(selectedIndex, buildRecordBody(recordForm));
      setRecordForm(emptyRecordForm);
      setShowCreate(false);
      await loadRecords();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to create record' });
    }
  };

  const createBatch = async (e) => {
    e.preventDefault();
    try {
      await api.createIndexRecordsBatch(selectedIndex, parseJsonInput(batchJson, { Documents: [] }));
      setShowBatchCreate(false);
      await loadRecords();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to create records' });
    }
  };

  const openEdit = (record) => {
    setEditTarget(record);
    setEditForm({
      Labels: getLabels(record).join(', '),
      Tags: tagsToInput(getTags(record)),
      CustomMetadata: formatJson(getCustomMetadata(record)),
    });
  };

  const saveEdit = async (e) => {
    e.preventDefault();
    const recordId = getRecordId(editTarget);
    try {
      await api.updateIndexRecordLabels(selectedIndex, recordId, parseListInput(editForm.Labels));
      await api.updateIndexRecordTags(selectedIndex, recordId, parseTagsInput(editForm.Tags));
      await api.updateIndexRecordCustomMetadata(selectedIndex, recordId, parseJsonInput(editForm.CustomMetadata, {}));
      setEditTarget(null);
      await loadRecords();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to update record metadata' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteIndexRecord(selectedIndex, getRecordId(deleteTarget));
      setDeleteTarget(null);
      await loadRecords();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete record' });
    }
  };

  const handleBulkDelete = async () => {
    try {
      await api.deleteIndexRecords(selectedIndex, Array.from(selectedIds));
      setBulkDeleteConfirm(false);
      setSelectedIds(new Set());
      await loadRecords();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete records' });
    }
  };

  const toggleSelected = (id) => {
    setSelectedIds((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleAll = () => {
    if (records.length > 0 && records.every((record) => selectedIds.has(getRecordId(record)))) {
      setSelectedIds(new Set());
      return;
    }
    setSelectedIds(new Set(records.map(getRecordId).filter(Boolean)));
  };

  const selectedAll = records.length > 0 && records.every((record) => selectedIds.has(getRecordId(record)));

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Index Records</h1>
          <p className="content-subtitle">Browse records stored in Verbex inverted indices.</p>
        </div>
        <div className="artifact-header-actions">
          <button className="btn btn-secondary" onClick={() => setShowBatchCreate(true)} disabled={!selectedIndex}>Batch Create</button>
          <button className="btn btn-primary" onClick={() => setShowCreate(true)} disabled={!selectedIndex}>Create Record</button>
        </div>
      </div>

      <div className="filter-bar artifact-filter-bar">
        <label className="filter-label">
          Index
          <select value={selectedIndex} onChange={(e) => setSelectedIndex(e.target.value)}>
            <option value="">Select an index...</option>
            {indices.map((index) => <option key={getIndexId(index)} value={getIndexId(index)}>{index.Name || getIndexId(index)}</option>)}
          </select>
        </label>
        <label className="filter-label">
          Record
          <input value={filters.recordId} onChange={(e) => setFilters({ ...filters, recordId: e.target.value })} />
        </label>
        <label className="filter-label">
          Labels
          <input value={filters.label} onChange={(e) => setFilters({ ...filters, label: e.target.value })} />
        </label>
        <label className="filter-label">
          Tags
          <input value={filters.tags} onChange={(e) => setFilters({ ...filters, tags: e.target.value })} />
        </label>
        <label className="filter-label">
          Max
          <select value={filters.maxResults} onChange={(e) => setFilters({ ...filters, maxResults: e.target.value })}>
            {[25, 50, 100, 250, 500].map((value) => <option key={value} value={value}>{value}</option>)}
          </select>
        </label>
        <button className="btn btn-secondary btn-sm" onClick={loadRecords} disabled={!selectedIndex || loading}>Refresh</button>
        {selectedIds.size > 0 && <button className="btn btn-danger btn-sm" onClick={() => setBulkDeleteConfirm(true)}>Delete Selected ({selectedIds.size})</button>}
      </div>

      <div className="data-table-container">
        {loading ? (
          <div className="loading"><div className="spinner" /></div>
        ) : error ? (
          <div className="empty-state error-state"><p>{error}</p></div>
        ) : !selectedIndex ? (
          <div className="empty-state"><p>Select an index.</p></div>
        ) : records.length === 0 ? (
          <div className="empty-state"><p>No records found in this index.</p></div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '2.5rem', textAlign: 'center' }}><input type="checkbox" checked={selectedAll} onChange={toggleAll} /></th>
                <th>ID</th>
                <th>Document Path</th>
                <th>Original File</th>
                <th>Length</th>
                <th>Indexed</th>
                <th>Source Document</th>
                <th className="actions-cell"></th>
              </tr>
            </thead>
            <tbody>
              {records.map((record, idx) => {
                const recordId = getRecordId(record);
                const sourceDocumentId = getSourceDocumentId(record);
                return (
                  <tr key={recordId || idx} onClick={() => setDetailTarget(record)} style={{ cursor: 'pointer' }}>
                    <td style={{ width: '2.5rem', textAlign: 'center' }} onClick={(e) => e.stopPropagation()}>
                      <input type="checkbox" checked={selectedIds.has(recordId)} onChange={() => toggleSelected(recordId)} />
                    </td>
                    <td><CopyableId id={recordId} /></td>
                    <td className="artifact-content-cell">{getRecordPath(record)}</td>
                    <td>{getOriginalFileName(record)}</td>
                    <td>{formatInteger(getDocumentLength(record))}</td>
                    <td>{formatDateTime(getIndexedDate(record))}</td>
                    <td>{sourceDocumentId ? <CopyableId id={sourceDocumentId} /> : ''}</td>
                    <td className="actions-cell" onClick={(e) => e.stopPropagation()}>
                      <ActionMenu items={[
                        { label: 'View Details', onClick: () => setDetailTarget(record) },
                        { label: 'Edit Metadata', onClick: () => openEdit(record) },
                        { label: 'View JSON', onClick: () => setShowJson(record) },
                        { label: 'Delete', danger: true, onClick: () => setDeleteTarget(record) },
                      ]} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <Modal title="Create Index Record" onClose={() => setShowCreate(false)} wide footer={<><button className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancel</button><button className="btn btn-primary" onClick={createRecord}>Save</button></>}>
          <form onSubmit={createRecord}>
            <div className="form-row">
              <div className="form-group"><label>Record ID</label><input value={recordForm.Id} onChange={(e) => setRecordForm({ ...recordForm, Id: e.target.value })} /></div>
              <div className="form-group"><label>Document Path</label><input value={recordForm.Name} onChange={(e) => setRecordForm({ ...recordForm, Name: e.target.value })} /></div>
            </div>
            <div className="form-group"><label>Content</label><textarea value={recordForm.Content} onChange={(e) => setRecordForm({ ...recordForm, Content: e.target.value })} /></div>
            <div className="form-row">
              <div className="form-group"><label>Labels</label><input value={recordForm.Labels} onChange={(e) => setRecordForm({ ...recordForm, Labels: e.target.value })} /></div>
              <div className="form-group"><label>Tags</label><input value={recordForm.Tags} onChange={(e) => setRecordForm({ ...recordForm, Tags: e.target.value })} /></div>
            </div>
            <div className="form-group"><label>Custom Metadata</label><textarea value={recordForm.CustomMetadata} onChange={(e) => setRecordForm({ ...recordForm, CustomMetadata: e.target.value })} /></div>
          </form>
        </Modal>
      )}

      {showBatchCreate && (
        <Modal title="Batch Create Records" onClose={() => setShowBatchCreate(false)} wide footer={<><button className="btn btn-secondary" onClick={() => setShowBatchCreate(false)}>Cancel</button><button className="btn btn-primary" onClick={createBatch}>Create</button></>}>
          <form onSubmit={createBatch}>
            <div className="form-group"><label>Payload</label><textarea className="artifact-json-input" value={batchJson} onChange={(e) => setBatchJson(e.target.value)} /></div>
          </form>
        </Modal>
      )}

      {detailTarget && (
        <Modal title="Index Record Details" onClose={() => setDetailTarget(null)} extraWide>
          <div className="artifact-modal-field-rows">
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Record ID</div>
                <div className="artifact-field-value"><CopyableId id={getRecordId(detailTarget)} /></div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Document Path</div>
                <div className="artifact-field-value">{getRecordPath(detailTarget)}</div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Content SHA-256</div>
                <div className="artifact-field-value">{getContentSha256(detailTarget) ? <CopyableId id={getContentSha256(detailTarget)} /> : ''}</div>
              </div>
            </div>
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Document Length</div>
                <div className="artifact-field-value">{formatInteger(getDocumentLength(detailTarget))}</div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Indexed</div>
                <div className="artifact-field-value">{formatDateTime(getIndexedDate(detailTarget))}</div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Last Modified</div>
                <div className="artifact-field-value">{formatDateTime(getLastModified(detailTarget))}</div>
              </div>
            </div>
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Indexing Runtime</div>
                <div className="artifact-field-value">{formatRuntime(getIndexingRuntimeMs(detailTarget))}</div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Deleted</div>
                <div className="artifact-field-value">{getIsDeleted(detailTarget) ? 'Yes' : 'No'}</div>
              </div>
              <div className="artifact-field artifact-field-empty" aria-hidden="true"></div>
            </div>
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Labels</div>
                <div className="artifact-field-value"><BadgeList values={getLabels(detailTarget)} /></div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Tags</div>
                <div className="artifact-field-value"><TagBadges tags={getTags(detailTarget)} /></div>
              </div>
              <div className="artifact-field artifact-field-empty" aria-hidden="true"></div>
            </div>
          </div>
          {getRecordTerms(detailTarget).length > 0 && (
            <div className="artifact-detail-section">
              <h4>Terms</h4>
              <BadgeList values={getRecordTerms(detailTarget).slice(0, 100)} />
            </div>
          )}
          {getRecordContent(detailTarget) && (
            <div className="artifact-detail-section">
              <h4>Content</h4>
              <pre className="artifact-content-preview">{getRecordContent(detailTarget)}</pre>
            </div>
          )}
          <div className="artifact-detail-section">
            <h4>Custom Metadata</h4>
            <pre className="json-view compact">{formatJson(getCustomMetadata(detailTarget))}</pre>
          </div>
        </Modal>
      )}

      {editTarget && (
        <Modal title="Edit Record Metadata" onClose={() => setEditTarget(null)} wide footer={<><button className="btn btn-secondary" onClick={() => setEditTarget(null)}>Cancel</button><button className="btn btn-primary" onClick={saveEdit}>Save</button></>}>
          <form onSubmit={saveEdit}>
            <div className="form-row">
              <div className="form-group"><label>Labels</label><input value={editForm.Labels} onChange={(e) => setEditForm({ ...editForm, Labels: e.target.value })} /></div>
              <div className="form-group"><label>Tags</label><input value={editForm.Tags} onChange={(e) => setEditForm({ ...editForm, Tags: e.target.value })} /></div>
            </div>
            <div className="form-group"><label>Custom Metadata</label><textarea value={editForm.CustomMetadata} onChange={(e) => setEditForm({ ...editForm, CustomMetadata: e.target.value })} /></div>
          </form>
        </Modal>
      )}

      {showJson && <JsonViewModal title="Index Record JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Index Record" message={`Are you sure you want to delete record "${getRecordId(deleteTarget)}"?`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {bulkDeleteConfirm && <ConfirmModal title="Delete Index Records" message={`Delete ${selectedIds.size} selected record(s)?`} confirmLabel="Delete" danger onConfirm={handleBulkDelete} onClose={() => setBulkDeleteConfirm(false)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default IndexRecordsView;
