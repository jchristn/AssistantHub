import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { createPortal } from 'react-dom';
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

const SUMMARY_RANGE_OPTIONS = [
  { id: 'lastHour', label: 'Last Hour', bucketSeconds: 30, sliceCount: 120 },
  { id: 'lastDay', label: 'Last Day', bucketSeconds: 10 * 60, sliceCount: 144 },
  { id: 'lastWeek', label: 'Last Week', bucketSeconds: 60 * 60, sliceCount: 168 },
  { id: 'lastMonth', label: 'Last Month', bucketSeconds: 6 * 60 * 60, sliceCount: 120 },
];

const MAX_SUMMARY_LABELS = 8;
const MAX_SUMMARY_Y_AXIS_LABELS = 5;
const SUMMARY_COUNT_FORMATTER = new Intl.NumberFormat(undefined, {
  notation: 'compact',
  maximumFractionDigits: 1,
});

function getSummaryRangeConfig(rangeId) {
  return SUMMARY_RANGE_OPTIONS.find((option) => option.id === rangeId) || SUMMARY_RANGE_OPTIONS[1];
}

function getSummaryRangeWindow(rangeId, now = new Date()) {
  const config = getSummaryRangeConfig(rangeId);
  const bucketMs = config.bucketSeconds * 1000;
  const endExclusiveMs = Math.floor(now.getTime() / bucketMs) * bucketMs + bucketMs;
  const startMs = endExclusiveMs - (config.sliceCount * bucketMs);

  return {
    ...config,
    bucketMs,
    startMs,
    endExclusiveMs,
    startUtc: new Date(startMs),
    endUtc: new Date(endExclusiveMs - 1),
  };
}

function formatSummaryBucketLabel(bucketStartUtc, rangeId) {
  const date = new Date(bucketStartUtc);

  if (rangeId === 'lastHour' || rangeId === 'lastDay') {
    return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
  }

  if (rangeId === 'lastWeek') {
    return date.toLocaleString([], { weekday: 'short', hour: 'numeric' });
  }

  return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}

function formatSummaryTooltipWindow(bucket) {
  if (!bucket?.BucketStartUtc) return '';

  const start = new Date(bucket.BucketStartUtc).toLocaleString();
  const endValue = bucket.BucketEndUtc
    ? new Date(new Date(bucket.BucketEndUtc).getTime() - 1).toLocaleString()
    : '';

  return endValue ? `${start} to ${endValue}` : start;
}

function formatBucketSizeLabel(bucketSeconds) {
  if (bucketSeconds < 60) {
    return `${bucketSeconds} second bucket${bucketSeconds === 1 ? '' : 's'}`;
  }

  if (bucketSeconds % 3600 === 0) {
    const hours = bucketSeconds / 3600;
    return `${hours} hour bucket${hours === 1 ? '' : 's'}`;
  }

  const minutes = bucketSeconds / 60;
  return `${minutes} minute bucket${minutes === 1 ? '' : 's'}`;
}

function formatSummaryCountLabel(value) {
  return value >= 1000 ? SUMMARY_COUNT_FORMATTER.format(value) : value.toLocaleString();
}

function floorToBucketTimestamp(value, bucketMs) {
  return Math.floor(new Date(value).getTime() / bucketMs) * bucketMs;
}

function buildSummaryChartBuckets(summary, range) {
  const apiBuckets = new Map(
    (summary?.Buckets || []).map((bucket) => [
      floorToBucketTimestamp(bucket.BucketStartUtc, range.bucketMs),
      bucket,
    ])
  );

  return Array.from({ length: range.sliceCount }, (_, index) => {
    const bucketStartMs = range.startMs + (index * range.bucketMs);
    const apiBucket = apiBuckets.get(bucketStartMs);

    return {
      BucketStartUtc: new Date(bucketStartMs).toISOString(),
      BucketEndUtc: new Date(bucketStartMs + range.bucketMs).toISOString(),
      RequestCount: apiBucket?.RequestCount || 0,
      SuccessCount: apiBucket?.SuccessCount || 0,
      FailureCount: apiBucket?.FailureCount || 0,
      AverageDurationMs: apiBucket?.AverageDurationMs || 0,
    };
  });
}

function buildSummaryAxisLabels(buckets, rangeId) {
  if (!buckets?.length) return [];

  const labelCount = Math.min(MAX_SUMMARY_LABELS, buckets.length);
  const labelIndices = new Set();

  if (labelCount === 1) {
    labelIndices.add(0);
  } else {
    for (let index = 0; index < labelCount; index += 1) {
      labelIndices.add(Math.round((index * (buckets.length - 1)) / (labelCount - 1)));
    }
  }

  return Array.from(labelIndices)
    .sort((left, right) => left - right)
    .map((index, position, values) => ({
      bucketStartUtc: buckets[index].BucketStartUtc,
      label: formatSummaryBucketLabel(buckets[index].BucketStartUtc, rangeId),
      left: values.length === 1 ? 0 : (position / (values.length - 1)) * 100,
      align: position === 0 ? 'start' : position === values.length - 1 ? 'end' : 'center',
    }));
}

function buildSummaryYAxis(rawMaxCount) {
  const safeMaxCount = Math.max(0, Math.ceil(Number(rawMaxCount) || 0));

  if (safeMaxCount <= 0) {
    return {
      chartMax: 1,
      ticks: [
        { value: 1, label: '1' },
        { value: 0, label: '0' },
      ],
    };
  }

  if (safeMaxCount < MAX_SUMMARY_Y_AXIS_LABELS) {
    const tickValues = Array.from({ length: safeMaxCount + 1 }, (_, index) => safeMaxCount - index);

    return {
      chartMax: safeMaxCount,
      ticks: tickValues.map((value) => ({
        value,
        label: formatSummaryCountLabel(value),
      })),
    };
  }

  const step = Math.max(1, Math.ceil(safeMaxCount / (MAX_SUMMARY_Y_AXIS_LABELS - 1)));
  const chartMax = step * (MAX_SUMMARY_Y_AXIS_LABELS - 1);
  const tickValues = Array.from({ length: MAX_SUMMARY_Y_AXIS_LABELS }, (_, index) => chartMax - (index * step));

  return {
    chartMax,
    ticks: tickValues.map((value) => ({
      value,
      label: formatSummaryCountLabel(value),
    })),
  };
}

function getTooltipPosition(clientX, clientY) {
  const tooltipWidth = 300;
  const tooltipHeight = 110;
  const offset = 14;
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;

  let left = clientX + offset;
  let top = clientY + offset;

  if (left + tooltipWidth > viewportWidth - 12) left = clientX - tooltipWidth - offset;
  if (top + tooltipHeight > viewportHeight - 12) top = clientY - tooltipHeight - offset;

  return {
    left: Math.max(12, left),
    top: Math.max(12, top),
  };
}

function RequestHistorySummaryChart({ buckets, rangeId, rangeWindow }) {
  const normalizedBuckets = useMemo(() => buildSummaryChartBuckets({ Buckets: buckets }, rangeWindow), [buckets, rangeWindow]);
  const [tooltipState, setTooltipState] = useState(null);
  const maxRequestCount = useMemo(() => Math.max(...normalizedBuckets.map((bucket) => bucket.RequestCount || 0), 0), [normalizedBuckets]);
  const axisLabels = useMemo(() => buildSummaryAxisLabels(normalizedBuckets, rangeId), [normalizedBuckets, rangeId]);
  const yAxis = useMemo(() => buildSummaryYAxis(maxRequestCount), [maxRequestCount]);
  const tooltipPosition = tooltipState ? getTooltipPosition(tooltipState.clientX, tooltipState.clientY) : null;

  if (!normalizedBuckets.length) {
    return <div className="empty-state"><p>No request-history activity in the selected range.</p></div>;
  }

  return (
    <>
      <div className="request-history-chart-shell">
        <div className="request-history-chart-frame">
          <div className="request-history-chart-y-axis" aria-hidden="true">
            {yAxis.ticks.map((tick) => (
              <span key={tick.value} className="request-history-chart-y-axis-label">
                {tick.label}
              </span>
            ))}
          </div>
          <div className="request-history-chart-plot-shell">
            <div className="request-history-chart-plot">
              <div className="request-history-chart-grid" aria-hidden="true">
                {yAxis.ticks.map((tick) => (
                  <span key={tick.value} className="request-history-chart-grid-line" />
                ))}
              </div>
              <div
                className="request-history-chart"
                style={{ gridTemplateColumns: `repeat(${normalizedBuckets.length}, minmax(0, 1fr))` }}
              >
              {normalizedBuckets.map((bucket, index) => {
                const requestCount = bucket.RequestCount || 0;
                const totalHeight = Math.max(requestCount > 0 ? 6 : 2, Math.round((requestCount / yAxis.chartMax) * 100));
                const successRatio = requestCount > 0 ? (bucket.SuccessCount || 0) / requestCount : 0;
                const failureRatio = requestCount > 0 ? (bucket.FailureCount || 0) / requestCount : 0;
                const bucketId = `${bucket.BucketStartUtc || 'bucket'}-${index}`;

                return (
                  <div
                    key={bucketId}
                    className={`request-history-chart-column ${tooltipState?.bucketId === bucketId ? 'active' : ''}`}
                    onMouseEnter={(event) => setTooltipState({ bucketId, bucket, clientX: event.clientX, clientY: event.clientY })}
                    onMouseMove={(event) => setTooltipState({
                      bucketId,
                      bucket,
                      clientX: event.clientX,
                      clientY: event.clientY,
                    })}
                    onMouseLeave={() => setTooltipState(null)}
                  >
                    <div className="request-history-chart-track">
                      <div className="request-history-chart-bar" style={{ height: `${totalHeight}%` }}>
                        <div className="request-history-chart-success" style={{ height: `${Math.round(successRatio * 100)}%` }} />
                        <div className="request-history-chart-failure" style={{ height: `${Math.round(failureRatio * 100)}%` }} />
                      </div>
                    </div>
                  </div>
                );
              })}
              </div>
            </div>
            <div className="request-history-chart-axis">
              {axisLabels.map((label) => (
                <span
                  key={label.bucketStartUtc}
                  className={`request-history-chart-axis-label request-history-chart-axis-label-${label.align}`}
                  style={{ left: `${label.left}%` }}
                >
                  {label.label}
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>
      {tooltipState && tooltipPosition && typeof document !== 'undefined' && createPortal(
        <div
          className="request-history-chart-tooltip"
          role="status"
          aria-live="polite"
          style={{ left: `${tooltipPosition.left}px`, top: `${tooltipPosition.top}px` }}
        >
          <strong>{formatSummaryTooltipWindow(tooltipState.bucket)}</strong>
          <span>{Number(tooltipState.bucket?.RequestCount || 0).toLocaleString()} requests</span>
          <span>{Number(tooltipState.bucket?.SuccessCount || 0).toLocaleString()} success</span>
          <span>{Number(tooltipState.bucket?.FailureCount || 0).toLocaleString()} failure</span>
        </div>,
        document.body
      )}
    </>
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
  const [summaryRangeId, setSummaryRangeId] = useState('lastDay');
  const summaryRangeWindow = useMemo(() => getSummaryRangeWindow(summaryRangeId), [summaryRangeId, refresh]);

  const buildFilterParams = useCallback((overrides = {}) => {
    const params = {
      maxResults: 1000,
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

  const buildSummaryParams = useCallback(() => {
    return buildFilterParams({
      maxResults: null,
      startUtc: summaryRangeWindow.startUtc.toISOString(),
      endUtc: summaryRangeWindow.endUtc.toISOString(),
      bucketSeconds: summaryRangeWindow.bucketSeconds,
    });
  }, [buildFilterParams, summaryRangeWindow]);

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
        const result = await api.getRequestHistorySummary(buildSummaryParams());
        setSummary(result);
      } catch (err) {
        setSummary(null);
        console.error('Failed to load request-history summary', err);
      }
    })();
  }, [buildSummaryParams, refresh]);

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
    setSummaryRangeId('lastDay');
    setRefresh((value) => value + 1);
  };

  const selectedSummaryRange = getSummaryRangeConfig(summaryRangeId);

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Request History</h1>
          <p className="content-subtitle">Search and replay HTTP traffic across system APIs and assistant-facing APIs.</p>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button className="btn btn-secondary" onClick={resetFilters}>Reset Filters</button>
          <button className="btn btn-danger" onClick={() => setDeleteFilteredConfirm(true)}>Delete Filtered</button>
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
          <div>
            <h3>Traffic Summary</h3>
            <span>{selectedSummaryRange.label} | {summaryRangeWindow.sliceCount} slices | {formatBucketSizeLabel(selectedSummaryRange.bucketSeconds)}</span>
          </div>
          <div className="request-history-range-group" role="tablist" aria-label="Traffic summary range">
            {SUMMARY_RANGE_OPTIONS.map((option) => (
              <button
                key={option.id}
                type="button"
                className={`request-history-range-btn ${summaryRangeId === option.id ? 'active' : ''}`}
                onClick={() => setSummaryRangeId(option.id)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
        <RequestHistorySummaryChart buckets={summary?.Buckets || []} rangeId={summaryRangeId} rangeWindow={summaryRangeWindow} />
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
