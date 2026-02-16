import React, { useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import CopyButton from '../components/CopyButton';
import AssistantFormModal from '../components/modals/AssistantFormModal';
import AssistantSettingsFormModal from '../components/modals/AssistantSettingsFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function AssistantsView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editAssistant, setEditAssistant] = useState(null);
  const [showSettings, setShowSettings] = useState(null);
  const [settingsData, setSettingsData] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const chatUrl = (id) => `${window.location.origin}/chat/${id}`;

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this assistant', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Name', label: 'Name', tooltip: 'Display name used to identify this assistant', filterable: true },
    { key: 'Description', label: 'Description', tooltip: "Brief summary of the assistant's purpose", filterable: true, render: (row) => row.Description ? (row.Description.length > 50 ? row.Description.substring(0, 50) + '...' : row.Description) : '' },
    { key: 'Active', label: 'Status', tooltip: 'Whether this assistant is currently active and available', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
    { key: 'ChatLink', label: 'Chat Link', tooltip: 'URL to open the chat interface for this assistant', render: (row) => (
      <span className="copyable-id">
        <a href={`/chat/${row.Id}`} target="_blank" rel="noopener noreferrer" style={{ fontSize: '0.8125rem', color: 'var(--accent-color)' }}>Open</a>
        <CopyButton text={chatUrl(row.Id)} />
      </span>
    )},
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getAssistants(params);
  }, [serverUrl, credential]);

  const handleOpenSettings = async (row) => {
    try {
      const result = await api.getAssistantSettings(row.Id);
      setSettingsData(result);
      setShowSettings(row);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load assistant settings' });
    }
  };

  const getRowActions = (row) => [
    { label: 'Edit', onClick: () => { setEditAssistant(row); setShowForm(true); } },
    { label: 'Settings', onClick: () => handleOpenSettings(row) },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleSave = async (data) => {
    try {
      if (editAssistant) {
        await api.updateAssistant(editAssistant.Id, data);
      } else {
        await api.createAssistant(data);
      }
      setShowForm(false);
      setEditAssistant(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save assistant' });
    }
  };

  const handleSaveSettings = async (settings) => {
    try {
      await api.updateAssistantSettings(showSettings.Id, settings);
      setShowSettings(null);
      setSettingsData(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save settings' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteAssistant(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete assistant' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Assistants</h1>
          <p className="content-subtitle">Create and manage AI assistants with custom configurations.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditAssistant(null); setShowForm(true); }}>Create Assistant</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} />
      {showForm && <AssistantFormModal assistant={editAssistant} onSave={handleSave} onClose={() => { setShowForm(false); setEditAssistant(null); }} />}
      {showSettings && settingsData && <AssistantSettingsFormModal settings={settingsData} onSave={handleSaveSettings} onClose={() => { setShowSettings(null); setSettingsData(null); }} />}
      {showJson && <JsonViewModal title="Assistant JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Assistant" message={`Are you sure you want to delete assistant "${deleteTarget.Name}"? This will also delete all associated documents and settings.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default AssistantsView;
