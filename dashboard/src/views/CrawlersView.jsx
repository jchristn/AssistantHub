import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import JsonViewModal from '../components/modals/JsonViewModal';
import CrawlPlanFormModal from '../components/modals/CrawlPlanFormModal';
import CrawlOperationsModal from '../components/modals/CrawlOperationsModal';
import CrawlEnumerationModal from '../components/modals/CrawlEnumerationModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';
import Tooltip from '../components/Tooltip';

function CrawlersView() {
  const { serverUrl, credential, isGlobalAdmin } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [showForm, setShowForm] = useState(null); // null | 'create' | crawlPlan object
  const [showJson, setShowJson] = useState(null);
  const [showOperations, setShowOperations] = useState(null);
  const [showEnumeration, setShowEnumeration] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [ingestionRules, setIngestionRules] = useState([]);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getIngestionRules({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setIngestionRules(items);
      } catch (err) {
        console.error('Failed to load ingestion rules', err);
      }
    })();
  }, [serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier for this crawl plan', filterable: true, render: (row) => <CopyableId id={row.Id || row.GUID} /> },
    ...(isGlobalAdmin ? [{ key: 'TenantId', label: 'Tenant', tooltip: 'Owning tenant ID', filterable: true, render: (row) => <CopyableId id={row.TenantId} /> }] : []),
    { key: 'Name', label: 'Name', tooltip: 'Display name for this crawl plan', filterable: true },
    { key: 'RepositoryType', label: 'Type', tooltip: 'Repository type (e.g. Web)', filterable: true },
    { key: 'StartUrl', label: 'URL', tooltip: 'Start URL for this crawler', filterable: true, render: (row) => {
      const url = row.Repository?.StartUrl || row.StartUrl || '';
      return url.length > 50 ? <span title={url}>{url.slice(0, 47)}...</span> : url;
    }},
    { key: 'State', label: 'State', tooltip: 'Current state of the crawler', render: (row) => {
      const state = row.State || 'Stopped';
      const cls = state.toLowerCase() === 'running' ? 'running' : 'stopped';
      return <span className={`status-badge badge-${cls}`}>{state}</span>;
    }},
    { key: 'IntervalType', label: 'Interval', tooltip: 'Schedule interval type', render: (row) => {
      const schedule = row.Schedule;
      if (!schedule) return '';
      return `${schedule.IntervalValue || ''} ${schedule.IntervalType || ''}`.trim();
    }},
    { key: 'LastCrawl', label: 'Last Crawl', tooltip: 'Date and time of the last crawl', render: (row) => row.LastCrawlUtc ? new Date(row.LastCrawlUtc).toLocaleString() : '' },
    { key: 'LastResult', label: 'Result', tooltip: 'Result of the last crawl operation', render: (row) => {
      const result = row.LastResult || row.LastCrawlResult;
      if (!result) return <span className="status-badge badge-na">N/A</span>;
      const cls = result.toLowerCase() === 'success' ? 'success' : result.toLowerCase() === 'failed' ? 'failed' : 'na';
      return <span className={`status-badge badge-${cls}`}>{result}</span>;
    }},
  ];

  const fetchData = useCallback(async (params) => {
    return await api.getCrawlPlans(params);
  }, [serverUrl, credential]);

  const handleSave = async (data) => {
    try {
      if (showForm && showForm !== 'create' && (showForm.Id || showForm.GUID)) {
        await api.updateCrawlPlan(showForm.Id || showForm.GUID, data);
      } else {
        await api.createCrawlPlan(data);
      }
      setShowForm(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save crawl plan' });
    }
  };

  const handleDelete = async () => {
    try {
      await api.deleteCrawlPlan(deleteTarget.Id || deleteTarget.GUID);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete crawl plan' });
    }
  };

  const handleStartStop = async (row) => {
    try {
      const state = (row.State || '').toLowerCase();
      if (state === 'running') {
        await api.stopCrawl(row.Id || row.GUID);
      } else {
        await api.startCrawl(row.Id || row.GUID);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to start/stop crawler' });
    }
  };

  const handleTestConnectivity = async (row) => {
    try {
      const result = await api.testCrawlConnectivity(row.Id || row.GUID);
      setAlert({ title: 'Connectivity Test', message: result?.Message || result?.Success ? 'Connectivity test succeeded.' : 'Connectivity test completed.' });
    } catch (err) {
      setAlert({ title: 'Connectivity Test Failed', message: err.message || 'Failed to test connectivity' });
    }
  };

  const handleEnumerate = async (row) => {
    setShowEnumeration({ planId: row.Id || row.GUID, mode: 'plan' });
  };

  const getRowActions = (row) => {
    const state = (row.State || '').toLowerCase();
    return [
      { label: 'Edit', onClick: () => setShowForm(row) },
      { label: state === 'running' ? 'Stop' : 'Start', onClick: () => handleStartStop(row) },
      { label: 'View Operations', onClick: () => setShowOperations(row) },
      { label: 'View JSON', onClick: () => setShowJson(row) },
      { label: 'Verify Connectivity', onClick: () => handleTestConnectivity(row) },
      { label: 'Enumerate Contents', onClick: () => handleEnumerate(row) },
      { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
    ];
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteCrawlPlan(id);
      }
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some crawl plans' });
    }
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Crawlers</h1>
          <p className="content-subtitle">Manage crawl plans for automated content ingestion from web and other sources.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowForm('create')}>Create Crawler</button>
      </div>
      <DataTable columns={columns} fetchData={fetchData} getRowActions={getRowActions} refreshTrigger={refresh} onBulkDelete={handleBulkDelete} />
      {showForm && (
        <CrawlPlanFormModal
          plan={showForm !== 'create' ? showForm : null}
          ingestionRules={ingestionRules}
          onSave={handleSave}
          onClose={() => setShowForm(null)}
        />
      )}
      {showJson && <JsonViewModal title="Crawl Plan JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {showOperations && (
        <CrawlOperationsModal
          api={api}
          plan={showOperations}
          onClose={() => setShowOperations(null)}
        />
      )}
      {showEnumeration && (
        <CrawlEnumerationModal
          api={api}
          planId={showEnumeration.planId}
          operationId={showEnumeration.operationId}
          onClose={() => setShowEnumeration(null)}
        />
      )}
      {deleteTarget && <ConfirmModal title="Delete Crawl Plan" message={`Are you sure you want to delete crawl plan "${deleteTarget.Name || deleteTarget.Id}"? This action cannot be undone.`} confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default CrawlersView;
