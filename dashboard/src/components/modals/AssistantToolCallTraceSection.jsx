import React, { useCallback, useEffect, useMemo, useState } from 'react';
import CopyButton from '../CopyButton';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';
import { ApiClient } from '../../utils/api';
import { useAuth } from '../../context/AuthContext';

function formatMs(value) {
  const number = Number(value || 0);
  if (!number || !isFinite(number)) return 'N/A';
  if (number < 1000) return `${number.toFixed(1)} ms`;
  return `${(number / 1000).toFixed(2)} s`;
}

function formatTimestamp(value) {
  if (!value) return 'N/A';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'N/A';
  return date.toLocaleString();
}

function formatCount(value) {
  const number = Number(value || 0);
  if (!isFinite(number)) return '0';
  return number.toLocaleString();
}

function formatJsonText(value) {
  if (value == null || value === '') return '';
  if (typeof value !== 'string') return JSON.stringify(value, null, 2);
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function toUtcIsoInput(value) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

function getToolCallStatus(record) {
  if (record.Denied) return { label: 'Denied', className: 'failed' };
  if (record.Success) return { label: 'Success', className: 'active' };
  return { label: 'Failed', className: 'failed' };
}

function getRecordSortTime(record) {
  const value = record?.StartedUtc || record?.CreatedUtc || record?.FinishedUtc;
  if (!value) return 0;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 0 : date.getTime();
}

function sortToolRecords(records) {
  return [...(records || [])].sort((left, right) => {
    const leftIteration = Number(left?.Iteration || 0);
    const rightIteration = Number(right?.Iteration || 0);
    if (leftIteration !== rightIteration) return leftIteration - rightIteration;

    const leftSequence = Number(left?.SequenceNumber || 0);
    const rightSequence = Number(right?.SequenceNumber || 0);
    if (leftSequence !== rightSequence) return leftSequence - rightSequence;

    return getRecordSortTime(left) - getRecordSortTime(right);
  });
}

function ToolCallTimeline({ records }) {
  const orderedRecords = sortToolRecords(records);
  if (orderedRecords.length < 2) return null;

  return (
    <div className="tool-call-timeline" aria-label="Tool call progress timeline">
      {orderedRecords.map((record, index) => {
        const status = getToolCallStatus(record);
        return (
          <div key={record.Id || `${record.ToolName}-${index}`} className={`tool-call-timeline-item ${status.className}`} title={`${record.ToolName || 'tool'} - ${status.label}`}>
            <span className="tool-call-timeline-index">{index + 1}</span>
            <span className="tool-call-timeline-name">{record.ToolName || 'tool'}</span>
            <span className="tool-call-timeline-duration">{formatMs(record.DurationMs)}</span>
          </div>
        );
      })}
    </div>
  );
}

function AssistantToolCallTraceSection({
  assistantId,
  requestHistoryId = null,
  chatHistoryId = null,
  traceId = null,
  title = 'Tool Calls',
}) {
  const { serverUrl, credential } = useAuth();
  const api = useMemo(() => new ApiClient(serverUrl, credential?.BearerToken), [serverUrl, credential]);
  const [records, setRecords] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [filters, setFilters] = useState({
    toolName: '',
    success: '',
    denied: '',
    traceId: traceId || '',
    startUtc: '',
    endUtc: '',
  });

  useEffect(() => {
    setFilters((current) => ({
      ...current,
      traceId: traceId || '',
    }));
  }, [traceId]);

  const scopeParams = useMemo(() => {
    if (requestHistoryId) return { requestHistoryId };
    if (chatHistoryId) return { chatHistoryId };
    return {};
  }, [requestHistoryId, chatHistoryId]);

  const loadRecords = useCallback(async () => {
    if (!assistantId) {
      setRecords([]);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await api.getAssistantToolCalls(assistantId, {
        maxResults: 100,
        ...scopeParams,
        traceId: filters.traceId,
        toolName: filters.toolName,
        success: filters.success === '' ? null : filters.success,
        denied: filters.denied === '' ? null : filters.denied,
        startUtc: toUtcIsoInput(filters.startUtc),
        endUtc: toUtcIsoInput(filters.endUtc),
      });
      setRecords(result?.Objects || []);
    } catch (err) {
      setError(err?.message || 'Failed to load tool-call traces.');
      setRecords([]);
    } finally {
      setLoading(false);
    }
  }, [api, assistantId, filters, scopeParams]);

  useEffect(() => {
    loadRecords();
  }, [loadRecords, refresh]);

  if (!assistantId) return null;

  return (
    <section className="history-section tool-call-trace-section">
      <div className="history-section-header">
        <Tooltip text="Redacted model-directed tool calls associated with this assistant turn.">{title}</Tooltip>
        <Tooltip className="history-section-badge" text="Number of tool-call records matching the current scope and filters.">
          {loading ? 'Loading' : `${records.length} record${records.length === 1 ? '' : 's'}`}
        </Tooltip>
      </div>

      <div className="tool-call-trace-filters">
        <label className="filter-label">
          Tool
          <input type="text" value={filters.toolName} onChange={(e) => setFilters((current) => ({ ...current, toolName: e.target.value }))} placeholder="collection_search" />
        </label>
        <label className="filter-label">
          Success
          <select value={filters.success} onChange={(e) => setFilters((current) => ({ ...current, success: e.target.value }))}>
            <option value="">All</option>
            <option value="true">Success</option>
            <option value="false">Failure</option>
          </select>
        </label>
        <label className="filter-label">
          Denied
          <select value={filters.denied} onChange={(e) => setFilters((current) => ({ ...current, denied: e.target.value }))}>
            <option value="">All</option>
            <option value="true">Denied</option>
            <option value="false">Allowed</option>
          </select>
        </label>
        <label className="filter-label">
          Trace
          <input type="text" value={filters.traceId} onChange={(e) => setFilters((current) => ({ ...current, traceId: e.target.value }))} placeholder="trace_..." />
        </label>
        <label className="filter-label">
          From
          <input type="datetime-local" value={filters.startUtc} onChange={(e) => setFilters((current) => ({ ...current, startUtc: e.target.value }))} />
        </label>
        <label className="filter-label">
          To
          <input type="datetime-local" value={filters.endUtc} onChange={(e) => setFilters((current) => ({ ...current, endUtc: e.target.value }))} />
        </label>
        <button className="btn btn-secondary btn-sm" type="button" onClick={() => setRefresh((value) => value + 1)}>Apply</button>
      </div>

      {error ? (
        <div className="empty-state compact"><p>{error}</p></div>
      ) : loading ? (
        <div className="empty-state compact"><p>Loading tool-call traces...</p></div>
      ) : records.length < 1 ? (
        <div className="empty-state compact"><p>No tool calls recorded for this scope.</p></div>
      ) : (
        <>
        <ToolCallTimeline records={records} />
        <div className="tool-call-trace-list">
          {sortToolRecords(records).map((record) => {
            const status = getToolCallStatus(record);
            const argumentsJson = formatJsonText(record.ArgumentsJson);
            const summaryJson = formatJsonText(record.ResultSummaryJson);
            const outputJson = formatJsonText(record.OutputJson);
            const runtimeLabel = [record.Provider, record.Model].filter(Boolean).join(' / ');

            return (
              <article key={record.Id} className="tool-call-trace-card">
                <div className="tool-call-trace-card-header">
                  <div className="tool-call-trace-title">
                    <span className="tool-call-trace-tool">{record.ToolName || 'tool'}</span>
                    <span className={`status-badge ${status.className}`}>{status.label}</span>
                    {record.Truncated && <span className="request-history-detail-pill warning">Truncated</span>}
                  </div>
                  <div className="tool-call-trace-meta">
                    <span>{formatMs(record.DurationMs)}</span>
                    <span>{formatTimestamp(record.CreatedUtc || record.FinishedUtc)}</span>
                  </div>
                </div>

                <div className="tool-call-trace-identifiers">
                  <div><span>Record</span><CopyableId id={record.Id} /></div>
                  {record.RequestHistoryId && <div><span>Request</span><CopyableId id={record.RequestHistoryId} /></div>}
                  {record.ChatHistoryId && <div><span>History</span><CopyableId id={record.ChatHistoryId} /></div>}
                  {record.TraceId && <div><span>Trace</span><CopyableId id={record.TraceId} /></div>}
                  {record.ProviderToolCallId && <div><span>Provider</span><CopyableId id={record.ProviderToolCallId} /></div>}
                  {runtimeLabel && <div><span>Runtime</span><span className="tool-call-trace-value" title={runtimeLabel}>{runtimeLabel}</span></div>}
                  <div><span>Bytes</span><span className="tool-call-trace-value">{formatCount(record.InputBytes)} in / {formatCount(record.OutputBytes)} out</span></div>
                  {record.LastUpdateUtc && <div><span>Updated</span><span className="tool-call-trace-value">{formatTimestamp(record.LastUpdateUtc)}</span></div>}
                </div>

                {record.ErrorMessage && (
                  <div className="tool-call-trace-error">{record.ErrorMessage}</div>
                )}

                <div className="tool-call-trace-json-grid">
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Arguments</span>
                      <CopyButton text={argumentsJson} />
                    </div>
                    <pre className="json-view request-history-block-body">{argumentsJson || '{}'}</pre>
                  </div>
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Summary</span>
                      <CopyButton text={summaryJson} />
                    </div>
                    <pre className="json-view request-history-block-body">{summaryJson || '{}'}</pre>
                  </div>
                  <div className="request-history-block">
                    <div className="request-history-block-header">
                      <span>Output</span>
                      <CopyButton text={outputJson} />
                    </div>
                    <pre className="json-view request-history-block-body">{outputJson || '{}'}</pre>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
        </>
      )}
    </section>
  );
}

export default AssistantToolCallTraceSection;
