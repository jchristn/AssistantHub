import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyableId from '../components/CopyableId';

const RANGE_OPTIONS = [
  { id: 'hour', label: 'Last hour', hours: 1, buckets: 12 },
  { id: 'day', label: 'Last day', hours: 24, buckets: 24 },
  { id: 'week', label: 'Last week', hours: 168, buckets: 14 },
  { id: 'month', label: 'Last month', hours: 720, buckets: 30 },
];
const DEFAULT_RANGE = 'day';

const STAGE_COLORS = {
  filedownloadfroms3: '#4dabf7',
  typedetection: '#3bc9db',
  atomextraction: '#38d9a9',
  summarization: '#ffa94d',
  chunking: '#ff922b',
  embeddingstorage: '#da77f2',
  verbexreindex: '#ff6b6b',
};
const PALETTE = ['#4dabf7', '#38d9a9', '#a9e34b', '#ffd43b', '#ffa94d', '#ff6b6b', '#da77f2', '#845ef7', '#20c997'];

function normalizeKey(v) { return String(v ?? '').toLowerCase().replace(/[^a-z0-9]/g, ''); }
function colorFor(stage, idx) { return STAGE_COLORS[normalizeKey(stage)] || PALETTE[idx % PALETTE.length]; }

function formatDuration(ms) {
  if (ms == null || Number.isNaN(ms)) return '—';
  if (ms < 1000) return `${Math.round(ms)} ms`;
  const s = ms / 1000;
  if (s < 60) return `${s.toFixed(s < 10 ? 2 : 1)} s`;
  const m = Math.floor(s / 60);
  return `${m}m ${Math.round(s % 60)}s`;
}

function formatWhen(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleString();
}

// Aggregate raw per-stage events into per-document rollups, per-stage averages, and a time series.
function aggregate(events, windowStartUtc, windowEndUtc, buckets) {
  const byDoc = new Map();
  const byStage = new Map();

  for (const e of events) {
    const ms = Number(e.DurationMs) || 0;
    // per-document rollup
    const doc = byDoc.get(e.DocumentId) || {
      documentId: e.DocumentId, ingestionRuleId: e.IngestionRuleId,
      stageCount: 0, totalMs: 0, success: true, completedUtc: null,
    };
    doc.stageCount += 1;
    doc.totalMs += ms;
    if (e.Success === false) doc.success = false;
    const fin = e.FinishedUtc || e.CreatedUtc;
    if (fin && (!doc.completedUtc || new Date(fin) > new Date(doc.completedUtc))) doc.completedUtc = fin;
    byDoc.set(e.DocumentId, doc);

    // per-stage average
    const key = e.Stage || 'Stage';
    const st = byStage.get(key) || { stage: key, totalMs: 0, count: 0 };
    st.totalMs += ms;
    st.count += 1;
    byStage.set(key, st);
  }

  const docs = Array.from(byDoc.values()).sort((a, b) => new Date(b.completedUtc || 0) - new Date(a.completedUtc || 0));
  const stages = Array.from(byStage.values())
    .map((s) => ({ ...s, avgMs: s.count ? s.totalMs / s.count : 0 }))
    .sort((a, b) => b.avgMs - a.avgMs);

  // time series: documents completed per bucket
  const start = new Date(windowStartUtc).getTime();
  const end = new Date(windowEndUtc).getTime();
  const span = Math.max(1, end - start);
  const series = Array.from({ length: buckets }, () => 0);
  for (const doc of docs) {
    if (!doc.completedUtc) continue;
    const t = new Date(doc.completedUtc).getTime();
    let idx = Math.floor(((t - start) / span) * buckets);
    if (idx < 0) idx = 0;
    if (idx >= buckets) idx = buckets - 1;
    series[idx] += 1;
  }

  const totalDocs = docs.length;
  const failedDocs = docs.filter((d) => !d.success).length;
  const avgTotalMs = totalDocs ? docs.reduce((sum, d) => sum + d.totalMs, 0) / totalDocs : 0;
  const successRate = totalDocs ? ((totalDocs - failedDocs) / totalDocs) * 100 : 0;

  return { docs, stages, series, totalDocs, failedDocs, avgTotalMs, successRate };
}

function IngestionPerformanceView() {
  const { serverUrl, credential } = useAuth();
  const [rangeId, setRangeId] = useState(DEFAULT_RANGE);
  const [data, setData] = useState(undefined);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [nonce, setNonce] = useState(0);

  const range = RANGE_OPTIONS.find((r) => r.id === rangeId) || RANGE_OPTIONS[1];

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      const api = new ApiClient(serverUrl, credential?.BearerToken);
      const result = await api.getIngestionAnalytics({ hours: range.hours, maxResults: 20000 });
      setData(result || null);
    } catch (err) {
      setError(err.message || 'Failed to load ingestion analytics');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [serverUrl, credential, range.hours]);

  useEffect(() => { load(); }, [load, nonce]);

  const agg = useMemo(() => {
    if (!data || !Array.isArray(data.Events)) return null;
    return aggregate(data.Events, data.WindowStartUtc, data.WindowEndUtc, range.buckets);
  }, [data, range.buckets]);

  const maxSeries = agg ? Math.max(1, ...agg.series) : 1;
  const maxStageMs = agg ? Math.max(1, ...agg.stages.map((s) => s.avgMs)) : 1;

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Ingestion Performance</h1>
          <p className="content-subtitle">Document ingestion timing across the selected window.</p>
        </div>
        <div className="ing-range">
          {RANGE_OPTIONS.map((r) => (
            <button
              key={r.id}
              type="button"
              className={`ing-range-btn${r.id === rangeId ? ' active' : ''}`}
              onClick={() => setRangeId(r.id)}
            >
              {r.label}
            </button>
          ))}
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setNonce((n) => n + 1)} disabled={loading}>
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>
      </div>

      {error && <p style={{ color: 'var(--danger-color, #e74c3c)' }}>{error}</p>}
      {data === undefined && !error && <p>Loading…</p>}
      {agg && agg.totalDocs === 0 && !loading && (
        <p className="ah-perf-empty">No documents were ingested in this window.</p>
      )}

      {agg && agg.totalDocs > 0 && (
        <>
          <div className="ah-perf-metrics">
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Documents Ingested</span>
              <span className="ah-perf-metric-value">{agg.totalDocs}</span>
            </div>
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Avg Total Duration</span>
              <span className="ah-perf-metric-value">{formatDuration(agg.avgTotalMs)}</span>
            </div>
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Success Rate</span>
              <span className="ah-perf-metric-value">{agg.successRate.toFixed(0)}%</span>
            </div>
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Failed</span>
              <span className="ah-perf-metric-value">{agg.failedDocs}</span>
            </div>
          </div>

          <div className="ah-perf-section">
            <div className="ah-perf-section-title">Documents Ingested Over Time</div>
            <div className="ing-barchart" role="img" aria-label="Documents ingested over time">
              {agg.series.map((v, i) => (
                <div className="ing-bar-col" key={i} title={`${v} document${v === 1 ? '' : 's'}`}>
                  <div className="ing-bar" style={{ height: `${(v / maxSeries) * 100}%` }} />
                </div>
              ))}
            </div>
            <div className="ing-barchart-axis">
              <span>{formatWhen(data.WindowStartUtc)}</span>
              <span>{formatWhen(data.WindowEndUtc)}</span>
            </div>
          </div>

          <div className="ah-perf-section">
            <div className="ah-perf-section-title">Average Duration Per Stage</div>
            <div className="ah-perf-timing">
              {agg.stages.map((s, i) => {
                const pct = maxStageMs > 0 ? Math.max(2, (s.avgMs / maxStageMs) * 100) : 0;
                return (
                  <div className="ah-perf-timing-row" key={s.stage} title={`${s.stage}: avg ${formatDuration(s.avgMs)} over ${s.count} run(s)`}>
                    <span className="ah-perf-timing-label">{s.stage}</span>
                    <span className="ah-perf-timing-track">
                      <span className="ah-perf-timing-fill" style={{ width: `${Math.min(pct, 100)}%`, background: colorFor(s.stage, i) }} />
                    </span>
                    <span className="ah-perf-timing-value">{formatDuration(s.avgMs)} · {s.count}×</span>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="ah-perf-section">
            <div className="ah-perf-section-title">Recent Ingestions</div>
            <div className="data-table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Document</th>
                    <th>Ingestion Rule</th>
                    <th>Stages</th>
                    <th>Total Duration</th>
                    <th>Status</th>
                    <th>Completed</th>
                  </tr>
                </thead>
                <tbody>
                  {agg.docs.slice(0, 100).map((d) => (
                    <tr key={d.documentId}>
                      <td><CopyableId id={d.documentId} /></td>
                      <td>{d.ingestionRuleId ? <CopyableId id={d.ingestionRuleId} /> : '—'}</td>
                      <td>{d.stageCount}</td>
                      <td>{formatDuration(d.totalMs)}</td>
                      <td>
                        <span className={`status-badge ${d.success ? 'active' : 'failed'}`}>{d.success ? 'OK' : 'Failed'}</span>
                      </td>
                      <td>{formatWhen(d.completedUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default IngestionPerformanceView;
