import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import CopyableId from '../components/CopyableId';
import JsonViewModal from '../components/modals/JsonViewModal';
import RequestHistoryDetailModal from '../components/modals/RequestHistoryDetailModal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function toLocalDateTimeInputValue(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function buildUtcIso(value) {
  if (!value) return null;
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return null;
  return parsed.toISOString();
}

function RequestHistorySummaryChart({ buckets }) {
  const maxCount = useMemo(() => Math.max(...(buckets || []).map((bucket) => bucket.RequestCount || 0), 1), [buckets]);

  if (!buckets || buckets.length < 1) {
    return <div className="empty-state"><p>No request-history activity in the selected range.</p></div>;
  }

  return (
    <div className="request-history-chart">
      {buckets.map((bucket, index) => {
        const totalHeight = Math.max(4, Math.round(((bucket.RequestCount || 0) / maxCount) * 100));
        const successRatio = bucket.RequestCount > 0 ? (bucket.SuccessCount || 0) / bucket.RequestCount : 0;
        const failureRatio = bucket.RequestCount > 0 ? (bucket.FailureCount || 0) / bucket.RequestCount : 0;

        return (
          <div
            key={`${bucket.BucketStartUtc || 'bucket'}-${index}`}
            className="request-history-chart-column"
            title={`${new Date(bucket.BucketStartUtc).toLocaleString()} - ${bucket.RequestCount || 0} requests (${bucket.SuccessCount || 0} success, ${bucket.FailureCount || 0} failure)`}
          >
            <div className="request-history-chart-track">
              <div className="request-history-chart-bar" style={{ height: `${totalHeight}%` }}>
                <div className="request-history-chart-success" style={{ height: `${Math.round(successRatio * 100)}%` }} />
                <div className="request-history-chart-failure" style={{ height: `${Math.round(failureRatio * 100)}%` }} />
              </div>
            </div>
            <div className="request-history-chart-label">
              {new Date(bucket.BucketStartUtc).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function RequestHistoryView() {
  const { serverUrl, credential, isGlobalAdmin } = useAuth();
  const navigate = useNavigate();
  const api = new ApiClient(serverUrl, credential?.BearerToken);

  const [summary, setSummary] = useState(null);
  const [assistants, setAssistants] = useState([]);
  const [detail, setDetail] = useState(null);
  const [showJson, setShowJson] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleteFilteredConfirm, setDeleteFilteredConfirm] = useState(false);
  const [alert, setAlert] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [methodFilter, setMethodFilter] = useState('');
  const [requestTypeFilter, setRequestTypeFilter] = useState('');
  const [sourceTypeFilter, setSourceTypeFilter] = useState('');
  const [successFilter, setSuccessFilter] = useState('');
  const [statusCodeFilter, setStatusCodeFilter] = useState('');
  const [pathFilter, setPathFilter] = useState('');
  const [searchFilter, setSearchFilter] = useState('');
  const [assistantFilter, setAssistantFilter] = useState('');
  const [threadFilter, setThreadFilter] = useState('');
  const [tenantFilter, setTenantFilter] = useState('');
  const [startUtcFilter, setStartUtcFilter] = useState(() => toLocalDateTimeInputValue(new Date(Date.now() - 24 * 60 * 60 * 1000)));
  const [endUtcFilter, setEndUtcFilter] = useState(() => toLocalDateTimeInputValue(new Date()));

  const buildFilterParams = useCallback((overrides = {}) => {
    const params = {
      maxResults: 1000,
      bucketMinutes: 60,
      method: methodFilter,
      requestType: requestTypeFilter,
      sourceType: sourceTypeFilter,
      path: pathFilter,
      search: searchFilter,
      assistantId: assistantFilter,
      threadId: threadFilter,
      tenantId: isGlobalAdmin ? tenantFilter : null,
      startUtc: buildUtcIso(startUtcFilter),
      endUtc: buildUtcIso(endUtcFilter),
      ...overrides,
    };

    if (successFilter === 'true') params.success = true;
    if (successFilter === 'false') params.success = false;
    if (statusCodeFilter) params.statusCode = statusCodeFilter;

    return params;
  }, [
    assistantFilter,
    endUtcFilter,
    isGlobalAdmin,
    methodFilter,
    pathFilter,
    requestTypeFilter,
    searchFilter,
    sourceTypeFilter,
    startUtcFilter,
    statusCodeFilter,
    successFilter,
    tenantFilter,
    threadFilter,
  ]);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getAssistants({ maxResults: 1000 });
        const items = result?.Objects || (Array.isArray(result) ? result : []);
        setAssistants(items);
      } catch (err) {
        console.error('Failed to load assistants', err);
      }
    })();
  }, [serverUrl, credential]);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getRequestHistorySummary(buildFilterParams());
        setSummary(result);
      } catch (err) {
        setSummary(null);
        console.error('Failed to load request-history summary', err);
      }
    })();
  }, [buildFilterParams, refresh]);

  const loadDetail = useCallback(async (requestId) => {
    return await api.getRequestHistoryEntryDetail(requestId);
  }, [serverUrl, credential]);

  const openDetail = useCallback(async (row) => {
    try {
      const full = await loadDetail(row.Id);
      setDetail(full);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load request history detail' });
    }
  }, [loadDetail]);

  const openJson = useCallback(async (row) => {
    try {
      const full = await loadDetail(row.Id);
      setShowJson(full);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load request history detail' });
    }
  }, [loadDetail]);

  const replayRequest = useCallback(async (row) => {
    try {
      const full = row.RequestBody != null ? row : await loadDetail(row.Id);
      navigate('/api-explorer', {
        state: {
          preset: {
            type: 'requestHistoryReplay',
            entry: full,
          },
        },
      });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to prepare request replay' });
    }
  }, [loadDetail, navigate]);

  const fetchData = useCallback(async (params) => {
    return await api.getRequestHistory(buildFilterParams(params));
  }, [buildFilterParams, serverUrl, credential]);

  const columns = [
    { key: 'Id', label: 'ID', tooltip: 'Request-history identifier', render: (row) => <CopyableId id={row.Id} /> },
    ...(isGlobalAdmin ? [{ key: 'TenantId', label: 'Tenant', tooltip: 'Owning tenant identifier', render: (row) => row.TenantId ? <CopyableId id={row.TenantId} /> : '-' }] : []),
    {
      key: 'HttpMethod',
      label: 'Method',
      tooltip: 'HTTP method',
      render: (row) => <span className={`request-history-method method-${(row.HttpMethod || 'GET').toLowerCase()}`}>{row.HttpMethod || 'GET'}</span>,
    },
    { key: 'RequestPath', label: 'Path', tooltip: 'Request path', render: (row) => <code className="request-history-path-cell">{row.RequestPath}</code> },
    {
      key: 'StatusCode',
      label: 'Status',
      tooltip: 'HTTP response status code',
      render: (row) => <span className={`status-badge ${row.Success ? 'active' : 'failed'}`}>{row.StatusCode}</span>,
    },
    { key: 'RequestType', label: 'Type', tooltip: 'System or assistant request', render: (row) => row.RequestType || '-' },
    { key: 'SourceType', label: 'Source', tooltip: 'Dashboard, API, or public traffic', render: (row) => row.SourceType || '-' },
    { key: 'AssistantId', label: 'Assistant', tooltip: 'Associated assistant identifier', render: (row) => row.AssistantId ? <CopyableId id={row.AssistantId} /> : '-' },
    { key: 'ThreadId', label: 'Thread', tooltip: 'Associated thread identifier', render: (row) => row.ThreadId ? <CopyableId id={row.ThreadId} /> : '-' },
    { key: 'DurationMs', label: 'Duration', tooltip: 'End-to-end duration in milliseconds', render: (row) => `${Math.round(row.DurationMs || 0)} ms` },
    { key: 'CreatedUtc', label: 'Created', tooltip: 'When the request completed', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const getRowActions = (row) => [
    { label: 'View', onClick: () => openDetail(row) },
    { label: 'Replay In Explorer', onClick: () => replayRequest(row) },
    { label: 'View JSON', onClick: () => openJson(row) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleDelete = async () => {
    try {
      await api.deleteRequestHistoryEntry(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete request-history entry' });
    }
  };

  const handleBulkDelete = async (ids) => {
    try {
      for (const id of ids) {
        await api.deleteRequestHistoryEntry(id);
      }
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some request-history entries' });
    }
  };

  const handleDeleteFiltered = async () => {
    try {
      await api.deleteRequestHistoryBulk(buildFilterParams({ maxResults: null }));
      setDeleteFilteredConfirm(false);
      setRefresh((value) => value + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete filtered request-history entries' });
    }
  };

  const resetFilters = () => {
    setMethodFilter('');
    setRequestTypeFilter('');
    setSourceTypeFilter('');
    setSuccessFilter('');
    setStatusCodeFilter('');
    setPathFilter('');
    setSearchFilter('');
    setAssistantFilter('');
    setThreadFilter('');
    setTenantFilter('');
    setStartUtcFilter(toLocalDateTimeInputValue(new Date(Date.now() - 24 * 60 * 60 * 1000)));
    setEndUtcFilter(toLocalDateTimeInputValue(new Date()));
    setRefresh((value) => value + 1);
  };

  const deleteFilteredDisabled = !summary || summary.TotalCount < 1;

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Request History</h1>
          <p className="content-subtitle">Search and replay HTTP traffic across system APIs and assistant-facing APIs.</p>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button className="btn btn-secondary" onClick={resetFilters}>Reset Filters</button>
          <button className="btn btn-danger" onClick={() => setDeleteFilteredConfirm(true)} disabled={deleteFilteredDisabled}>Delete Filtered</button>
        </div>
      </div>

      <div className="filter-bar request-history-filter-bar">
        <label className="filter-label">
          Search
          <input className="request-history-filter-input" type="text" value={searchFilter} onChange={(e) => setSearchFilter(e.target.value)} placeholder="Search path, URL, principal..." />
        </label>
        <label className="filter-label">
          Path
          <input className="request-history-filter-input" type="text" value={pathFilter} onChange={(e) => setPathFilter(e.target.value)} placeholder="/v1.0/assistants" />
        </label>
        <label className="filter-label">
          Method
          <select value={methodFilter} onChange={(e) => setMethodFilter(e.target.value)}>
            <option value="">All</option>
            <option value="GET">GET</option>
            <option value="POST">POST</option>
            <option value="PUT">PUT</option>
            <option value="DELETE">DELETE</option>
            <option value="HEAD">HEAD</option>
          </select>
        </label>
        <label className="filter-label">
          Type
          <select value={requestTypeFilter} onChange={(e) => setRequestTypeFilter(e.target.value)}>
            <option value="">All</option>
            <option value="SystemApi">System API</option>
            <option value="AssistantApi">Assistant API</option>
          </select>
        </label>
        <label className="filter-label">
          Source
          <select value={sourceTypeFilter} onChange={(e) => setSourceTypeFilter(e.target.value)}>
            <option value="">All</option>
            <option value="dashboard">Dashboard</option>
            <option value="api">API</option>
            <option value="public">Public</option>
            <option value="public-assistant">Public Assistant</option>
          </select>
        </label>
        <label className="filter-label">
          Success
          <select value={successFilter} onChange={(e) => setSuccessFilter(e.target.value)}>
            <option value="">All</option>
            <option value="true">Success</option>
            <option value="false">Failure</option>
          </select>
        </label>
        <label className="filter-label">
          Status
          <input className="request-history-filter-input request-history-filter-narrow" type="number" value={statusCodeFilter} onChange={(e) => setStatusCodeFilter(e.target.value)} placeholder="500" />
        </label>
        <label className="filter-label">
          Assistant
          <select value={assistantFilter} onChange={(e) => setAssistantFilter(e.target.value)}>
            <option value="">All</option>
            {assistants.map((assistant) => (
              <option key={assistant.Id} value={assistant.Id}>
                {assistant.Name} ({assistant.Id.slice(0, 8)}...)
              </option>
            ))}
          </select>
        </label>
        <label className="filter-label">
          Thread
          <input className="request-history-filter-input" type="text" value={threadFilter} onChange={(e) => setThreadFilter(e.target.value)} placeholder="thread_..." />
        </label>
        {isGlobalAdmin && (
          <label className="filter-label">
            Tenant
            <input className="request-history-filter-input" type="text" value={tenantFilter} onChange={(e) => setTenantFilter(e.target.value)} placeholder="tenant_..." />
          </label>
        )}
        <label className="filter-label">
          From
          <input className="request-history-filter-input" type="datetime-local" value={startUtcFilter} onChange={(e) => setStartUtcFilter(e.target.value)} />
        </label>
        <label className="filter-label">
          To
          <input className="request-history-filter-input" type="datetime-local" value={endUtcFilter} onChange={(e) => setEndUtcFilter(e.target.value)} />
        </label>
        <button className="btn btn-primary btn-sm" onClick={() => setRefresh((value) => value + 1)}>Apply</button>
      </div>

      <div className="request-history-summary-grid">
        <div className="stat-card">
          <span className="stat-card-label">Requests</span>
          <span className="stat-card-value">{Number(summary?.TotalCount || 0).toLocaleString()}</span>
        </div>
        <div className="stat-card">
          <span className="stat-card-label">Success</span>
          <span className="stat-card-value request-history-success-value">{Number(summary?.TotalSuccess || 0).toLocaleString()}</span>
        </div>
        <div className="stat-card">
          <span className="stat-card-label">Failure</span>
          <span className="stat-card-value request-history-failure-value">{Number(summary?.TotalFailure || 0).toLocaleString()}</span>
        </div>
        <div className="stat-card">
          <span className="stat-card-label">Avg Duration</span>
          <span className="stat-card-value">{Math.round(summary?.AverageDurationMs || 0)} ms</span>
        </div>
      </div>

      <div className="request-history-chart-card">
        <div className="request-history-chart-header">
          <h3>Traffic Summary</h3>
          <span>Last {summary?.Buckets?.length || 0} buckets</span>
        </div>
        <RequestHistorySummaryChart buckets={summary?.Buckets || []} />
      </div>

      <DataTable
        columns={columns}
        fetchData={fetchData}
        getRowActions={getRowActions}
        refreshTrigger={refresh}
        onBulkDelete={handleBulkDelete}
        onRowClick={(row) => openDetail(row)}
      />

      {detail && <RequestHistoryDetailModal entry={detail} onClose={() => setDetail(null)} onReplay={replayRequest} />}
      {showJson && <JsonViewModal title="Request History JSON" data={showJson} onClose={() => setShowJson(null)} />}
      {deleteTarget && <ConfirmModal title="Delete Request History Entry" message="Are you sure you want to delete this request-history entry? This action cannot be undone." confirmLabel="Delete" danger onConfirm={handleDelete} onClose={() => setDeleteTarget(null)} />}
      {deleteFilteredConfirm && <ConfirmModal title="Delete Filtered Request History" message="Delete all request-history entries matching the current filters? This action cannot be undone." confirmLabel="Delete Filtered" danger onConfirm={handleDeleteFiltered} onClose={() => setDeleteFilteredConfirm(false)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default RequestHistoryView;
