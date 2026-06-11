import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import Modal from '../components/Modal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import {
  BadgeList,
  FieldGrid,
  TagBadges,
  formatJson,
  getCustomMetadata,
  getIndexId,
  getLabels,
  getTags,
  parseJsonInput,
  parseListInput,
  parseTagsInput,
  tagsToInput,
  unwrapObjects,
} from '../utils/artifactSearch.jsx';
import { appendCopySuffix } from '../utils/duplicateObject';

const DEFAULT_INDEX_ID = 'default';
const DEFAULT_INDEX_FORM = { Identifier: '', Name: '', Description: '', Labels: '', Tags: '', CustomMetadata: '{}' };

function IndicesView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const navigate = useNavigate();
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState(DEFAULT_INDEX_FORM);
  const [editTarget, setEditTarget] = useState(null);
  const [editForm, setEditForm] = useState({ Name: '', Description: '', Labels: '', Tags: '', CustomMetadata: '{}' });
  const [detailTarget, setDetailTarget] = useState(null);
  const [topTerms, setTopTerms] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const fetchData = useCallback(async (params) => api.getIndices(params), [serverUrl, credential]);

  const columns = [
    { key: 'Identifier', label: 'ID', tooltip: 'Unique identifier for this inverted index', filterable: true, render: (row) => <CopyableId id={getIndexId(row)} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name for this index', filterable: true, render: (row) => row.Name || getIndexId(row) },
    { key: 'Description', label: 'Description', tooltip: 'Index description', filterable: true, render: (row) => row.Description ? String(row.Description).slice(0, 100) : '' },
    { key: 'DocumentCount', label: 'Records', tooltip: 'Number of indexed records', render: (row) => row.DocumentCount ?? row.Documents ?? row.RecordCount ?? row.Records ?? '' },
    { key: 'Labels', label: 'Labels', tooltip: 'Index labels', render: (row) => <BadgeList values={getLabels(row)} /> },
    { key: 'Tags', label: 'Tags', tooltip: 'Index tags', render: (row) => <TagBadges tags={getTags(row)} /> },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'Date and time the index was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const buildIndexBody = (source) => {
    const body = {
      Name: source.Name || source.Identifier || DEFAULT_INDEX_ID,
      Description: source.Description || undefined,
    };
    if (source.Identifier) body.Identifier = source.Identifier;
    const labels = parseListInput(source.Labels);
    const tags = parseTagsInput(source.Tags);
    const metadata = parseJsonInput(source.CustomMetadata, {});
    if (labels.length > 0) body.Labels = labels;
    if (Object.keys(tags).length > 0) body.Tags = tags;
    if (Object.keys(metadata).length > 0) body.CustomMetadata = metadata;
    return body;
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      await api.createIndex(buildIndexBody(form));
      setShowCreate(false);
      setForm(DEFAULT_INDEX_FORM);
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to create index' });
    }
  };

  const openEdit = (row) => {
    setEditTarget(row);
    setEditForm({
      Name: row.Name || getIndexId(row),
      Description: row.Description || '',
      Labels: getLabels(row).join(', '),
      Tags: tagsToInput(getTags(row)),
      CustomMetadata: formatJson(getCustomMetadata(row)),
    });
  };

  const openDuplicate = (row) => {
    setForm({
      Identifier: '',
      Name: appendCopySuffix(row.Name || getIndexId(row)),
      Description: row.Description || '',
      Labels: getLabels(row).join(', '),
      Tags: tagsToInput(getTags(row)),
      CustomMetadata: formatJson(getCustomMetadata(row)),
    });
    setShowCreate(true);
  };

  const handleEdit = async (e) => {
    e.preventDefault();
    const id = getIndexId(editTarget);
    try {
      await api.updateIndex(id, { Name: editForm.Name || id, Description: editForm.Description || undefined });
      await api.updateIndexLabels(id, parseListInput(editForm.Labels));
      await api.updateIndexTags(id, parseTagsInput(editForm.Tags));
      await api.updateIndexCustomMetadata(id, parseJsonInput(editForm.CustomMetadata, {}));
      setEditTarget(null);
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to update index' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteIndex(getIndexId(deleteTarget));
      setDeleteTarget(null);
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete index' });
    }
  };

  const loadTopTerms = async (row) => {
    try {
      const id = getIndexId(row);
      const result = await api.getIndexTopTerms(id, { maxResults: 25 });
      setTopTerms({ index: row, result, terms: unwrapObjects(result) });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load top terms' });
    }
  };

  const getRowActions = (row) => {
    const id = getIndexId(row);
    const actions = [
      { label: 'View Details', onClick: () => setDetailTarget(row) },
      { label: 'View Records', onClick: () => navigate(`/indices/records?indexId=${encodeURIComponent(id)}`, { state: { indexId: id } }) },
      { label: 'Search', onClick: () => navigate(`/indices/search?indexId=${encodeURIComponent(id)}`, { state: { indexId: id } }) },
      { label: 'Top Terms', onClick: () => loadTopTerms(row) },
      { label: 'Edit Metadata', onClick: () => openEdit(row) },
      { label: 'Duplicate', onClick: () => openDuplicate(row) },
      { label: 'View JSON', onClick: () => setShowJson(row) },
    ];
    if (id && id !== DEFAULT_INDEX_ID) actions.push({ label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) });
    return actions;
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Indices</h1>
          <p className="content-subtitle">Manage Verbex inverted indices used for text search.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setForm(DEFAULT_INDEX_FORM); setShowCreate((value) => !value); }}>Create Index</button>
      </div>

      {showCreate && (
        <form className="data-table-container artifact-form" onSubmit={handleCreate}>
          <div className="form-row">
            <div className="form-group">
              <label>Index ID</label>
              <input value={form.Identifier} onChange={(e) => setForm({ ...form, Identifier: e.target.value })} placeholder="default" />
            </div>
            <div className="form-group">
              <label>Name</label>
              <input value={form.Name} onChange={(e) => setForm({ ...form, Name: e.target.value })} placeholder="Default" />
            </div>
          </div>
          <div className="form-group">
            <label>Description</label>
            <input value={form.Description} onChange={(e) => setForm({ ...form, Description: e.target.value })} />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Labels</label>
              <input value={form.Labels} onChange={(e) => setForm({ ...form, Labels: e.target.value })} placeholder="default, documents" />
            </div>
            <div className="form-group">
              <label>Tags</label>
              <input value={form.Tags} onChange={(e) => setForm({ ...form, Tags: e.target.value })} placeholder="purpose=search" />
            </div>
          </div>
          <div className="form-group">
            <label>Custom Metadata</label>
            <textarea value={form.CustomMetadata} onChange={(e) => setForm({ ...form, CustomMetadata: e.target.value })} />
          </div>
          <div className="artifact-form-actions">
            <button className="btn btn-primary" type="submit">Save</button>
            <button className="btn btn-secondary" type="button" onClick={() => { setShowCreate(false); setForm(DEFAULT_INDEX_FORM); }}>Cancel</button>
          </div>
        </form>
      )}

      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onRowClick={setDetailTarget} />

      {detailTarget && (
        <Modal title="Index Details" onClose={() => setDetailTarget(null)} wide>
          <FieldGrid rows={[
            { label: 'ID', value: <CopyableId id={getIndexId(detailTarget)} /> },
            { label: 'Name', value: detailTarget.Name || getIndexId(detailTarget) },
            { label: 'Description', value: detailTarget.Description },
            { label: 'Records', value: detailTarget.DocumentCount ?? detailTarget.RecordCount ?? detailTarget.Documents },
            { label: 'Labels', value: <BadgeList values={getLabels(detailTarget)} /> },
            { label: 'Tags', value: <TagBadges tags={getTags(detailTarget)} /> },
            { label: 'Tokenization', value: detailTarget.Tokenizer || detailTarget.Tokenization || detailTarget.TokenizationStrategy },
            { label: 'Cache', value: detailTarget.Cache || detailTarget.CacheSettings || detailTarget.InMemory ? JSON.stringify(detailTarget.Cache || detailTarget.CacheSettings || { InMemory: detailTarget.InMemory }) : '' },
          ]} />
          <pre className="json-view compact">{formatJson(getCustomMetadata(detailTarget))}</pre>
        </Modal>
      )}

      {editTarget && (
        <Modal
          title="Edit Index Metadata"
          onClose={() => setEditTarget(null)}
          wide
          footer={
            <>
              <button className="btn btn-secondary" onClick={() => setEditTarget(null)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleEdit}>Save</button>
            </>
          }
        >
          <form onSubmit={handleEdit}>
            <div className="form-row">
              <div className="form-group">
                <label>Name</label>
                <input value={editForm.Name} onChange={(e) => setEditForm({ ...editForm, Name: e.target.value })} />
              </div>
              <div className="form-group">
                <label>Description</label>
                <input value={editForm.Description} onChange={(e) => setEditForm({ ...editForm, Description: e.target.value })} />
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label>Labels</label>
                <input value={editForm.Labels} onChange={(e) => setEditForm({ ...editForm, Labels: e.target.value })} />
              </div>
              <div className="form-group">
                <label>Tags</label>
                <input value={editForm.Tags} onChange={(e) => setEditForm({ ...editForm, Tags: e.target.value })} />
              </div>
            </div>
            <div className="form-group">
              <label>Custom Metadata</label>
              <textarea value={editForm.CustomMetadata} onChange={(e) => setEditForm({ ...editForm, CustomMetadata: e.target.value })} />
            </div>
          </form>
        </Modal>
      )}

      {topTerms && (
        <Modal title={`Top Terms: ${getIndexId(topTerms.index)}`} onClose={() => setTopTerms(null)} wide>
          {topTerms.terms.length < 1 ? (
            <pre className="json-view compact">{JSON.stringify(topTerms.result, null, 2)}</pre>
          ) : (
            <table className="data-table">
              <thead><tr><th>Term</th><th>Frequency</th><th>Score</th></tr></thead>
              <tbody>
                {topTerms.terms.map((term, index) => (
                  <tr key={term.Term || term.Text || index}>
                    <td>{term.Term || term.Text || term.Value || ''}</td>
                    <td>{term.Frequency ?? term.Count ?? ''}</td>
                    <td>{term.Score ?? term.Weight ?? ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Modal>
      )}

      {showJson && <JsonViewModal title="Index JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && (
        <ConfirmModal
          title="Delete Index"
          message={`Are you sure you want to delete index "${getIndexId(deleteTarget)}"? This action cannot be undone.`}
          confirmLabel="Delete"
          danger
          onConfirm={handleDelete}
          onClose={() => setDeleteTarget(null)}
        />
      )}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default IndicesView;
