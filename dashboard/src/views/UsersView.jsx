import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import UserFormModal from '../components/modals/UserFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function UsersView() {
  const navigate = useNavigate();
  const { serverUrl, credential, tenantId, isGlobalAdmin } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(false);
  const [editUser, setEditUser] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this user account', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    ...(isGlobalAdmin ? [{ key: 'TenantId', label: 'Tenant', tooltip: 'Tenant this user belongs to', filterable: true, render: (row) => <CopyableId id={row.TenantId} /> }] : []),
    { key: 'Email', label: 'Email', tooltip: 'Email address used for login and notifications', filterable: true },
    { key: 'FirstName', label: 'First Name', tooltip: "User's given name", filterable: true },
    { key: 'LastName', label: 'Last Name', tooltip: "User's family name", filterable: true },
    { key: 'IsAdmin', label: 'Admin', tooltip: 'Whether this user has administrator privileges', render: (row) => row.IsAdmin ? <span className="status-badge active">Yes</span> : <span className="status-badge inactive">No</span> },
    { key: 'IsTenantAdmin', label: 'Tenant Admin', tooltip: 'Whether this user has tenant administrator privileges', render: (row) => row.IsTenantAdmin ? <span className="status-badge active">Yes</span> : <span className="status-badge inactive">No</span> },
    { key: 'Active', label: 'Status', tooltip: 'Whether this user account is currently active', render: (row) => row.Active ? <span className="status-badge active">Active</span> : <span className="status-badge inactive">Inactive</span> },
    { key: 'IsProtected', label: 'Protected', tooltip: 'Protected records cannot be deleted', render: (row) => row.IsProtected ? <span className="status-badge active">Yes</span> : <span className="status-badge inactive">No</span> },
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getUsers(tenantId, params);
  }, [serverUrl, credential, tenantId]);

  const getRowActions = (row) => [
    { label: 'View Credentials', onClick: () => navigate('/credentials', { state: { initialFilters: { UserId: row.Id } } }) },
    { label: 'Edit', onClick: () => { setEditUser(row); setShowForm(true); } },
    { label: 'View JSON', onClick: () => setShowJson(row) },
    ...(!row.IsProtected ? [{ label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) }] : []),
  ];

  const handleSave = async (data) => {
    try {
      const tid = data.TenantId || tenantId;
      if (editUser) {
        await api.updateUser(tid, editUser.Id, data);
      } else {
        await api.createUser(data, tid);
      }
      setShowForm(false);
      setEditUser(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save user' });
    }
  };

  const handleDelete = async () => {
    try {
      const tid = deleteTarget.TenantId || tenantId;
      await api.deleteUser(tid, deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete user' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteUser(tenantId, id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some users' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Users</h1>
          <p className="content-subtitle">Manage user accounts and admin privileges.</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditUser(null); setShowForm(true); }}>Create User</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && <UserFormModal user={editUser} onSave={handleSave} onClose={() => { setShowForm(false); setEditUser(null); }} />}
      {showJson && <JsonViewModal title="User JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete User" message={`Are you sure you want to delete user "${deleteTarget.Email}"? This will also delete all their credentials.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default UsersView;
