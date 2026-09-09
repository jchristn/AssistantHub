import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyableId from '../components/CopyableId';
import DataTable from '../components/DataTable';
import IngestionPerformanceModal from '../components/modals/IngestionPerformanceModal';

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

// Responsive SVG bar chart with X (time) and Y (count) axes.
function TimeSeriesChart({ series, windowStartUtc, windowEndUtc, hours }) {
  const W = 1000;
  const H = 320;
  const mLeft = 46;
  const mRight = 16;
  const mTop = 16;
  const mBottom = 36;
  const plotW = W - mLeft - mRight;
  const plotH = H - mTop - mBottom;
  const n = Math.max(1, series.length);

  const rawMax = Math.max(1, ...series);
  const tickCount = Math.min(5, Math.max(2, rawMax));
  const step = Math.max(1, Math.ceil(rawMax / tickCount));
  const niceMax = step * tickCount;
  const yTicks = [];
  for (let v = 0; v <= niceMax; v += step) yTicks.push(v);

  const start = new Date(windowStartUtc).getTime();
  const end = new Date(windowEndUtc).getTime();
  const span = Math.max(1, end - start);
  const fmtTime = (t) => {
    const d = new Date(t);
    return hours <= 24
      ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      : d.toLocaleDateString([], { month: 'numeric', day: 'numeric' });
  };
  const labelCount = Math.min(6, n);
  const xLabels = [];
  for (let i = 0; i < labelCount; i++) {
    const frac = labelCount === 1 ? 0 : i / (labelCount - 1);
    xLabels.push({ x: mLeft + frac * plotW, label: fmtTime(start + frac * span) });
  }

  const slot = plotW / n;
  const barW = Math.max(1, slot - 3);

  return (
    <svg className="ing-ts-chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Documents ingested over time">
      {yTicks.map((v) => {
        const y = mTop + plotH - (v / niceMax) * plotH;
        return (
          <g key={v}>
            <line x1={mLeft} y1={y} x2={W - mRight} y2={y} className="ing-ts-grid" />
            <text x={mLeft - 8} y={y + 4} className="ing-ts-label" fontSize="11" textAnchor="end">{v}</text>
          </g>
        );
      })}
      {series.map((v, i) => {
        const h = (v / niceMax) * plotH;
        const x = mLeft + i * slot + (slot - barW) / 2;
        const y = mTop + plotH - h;
        return <rect key={i} x={x} y={y} width={barW} height={Math.max(0, h)} className="ing-ts-bar" rx="2" />;
      })}
      <line x1={mLeft} y1={mTop + plotH} x2={W - mRight} y2={mTop + plotH} className="ing-ts-axis" />
      {xLabels.map((l, i) => (
        <text
          key={i}
          x={l.x}
          y={H - 12}
          className="ing-ts-label"
          fontSize="11"
          textAnchor={i === 0 ? 'start' : i === xLabels.length - 1 ? 'end' : 'middle'}
        >
          {l.label}
        </text>
      ))}
    </svg>
  );
}

function IngestionPerformanceView() {
  const { serverUrl, credential } = useAuth();
  const [rangeId, setRangeId] = useState(DEFAULT_RANGE);
  const [data, setData] = useState(undefined);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [nonce, setNonce] = useState(0);
  const [perfDoc, setPerfDoc] = useState(null);

  const range = RANGE_OPTIONS.find((r) => r.id === rangeId) || RANGE_OPTIONS[1];
  const api = useMemo(() => new ApiClient(serverUrl, credential?.BearerToken), [serverUrl, credential]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      const result = await api.getIngestionAnalytics({ hours: range.hours, maxResults: 20000 });
      setData(result || null);
    } catch (err) {
      setError(err.message || 'Failed to load ingestion analytics');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [api, range.hours]);

  useEffect(() => { load(); }, [load, nonce]);

  const agg = useMemo(() => {
    if (!data || !Array.isArray(data.Events)) return null;
    return aggregate(data.Events, data.WindowStartUtc, data.WindowEndUtc, range.buckets);
  }, [data, range.buckets]);

  const maxStageMs = agg ? Math.max(1, ...agg.stages.map((s) => s.avgMs)) : 1;
  const tableRows = useMemo(() => (agg ? agg.docs.map((d) => ({ ...d, Id: d.documentId })) : []), [agg]);
  const fetchTableData = useCallback(async () => ({ Objects: tableRows }), [tableRows]);

  const columns = [
    { key: 'documentId', label: 'Document', tooltip: 'Ingested document identifier', filterable: true, render: (row) => <CopyableId id={row.documentId} /> },
    { key: 'ingestionRuleId', label: 'Ingestion Rule', tooltip: 'Ingestion rule applied', filterable: true, render: (row) => (row.ingestionRuleId ? <CopyableId id={row.ingestionRuleId} /> : '—') },
    { key: 'stageCount', label: 'Stages', tooltip: 'Number of timed pipeline stages', render: (row) => row.stageCount },
    { key: 'totalMs', label: 'Total Duration', tooltip: 'Sum of stage durations', render: (row) => formatDuration(row.totalMs) },
    { key: 'success', label: 'Status', tooltip: 'Whether all stages succeeded', render: (row) => <span className={`status-badge ${row.success ? 'active' : 'failed'}`}>{row.success ? 'OK' : 'Failed'}</span> },
    { key: 'completedUtc', label: 'Completed', tooltip: 'When the last stage finished', render: (row) => formatWhen(row.completedUtc) },
  ];

  const getRowActions = (row) => [
    { label: 'View Performance', onClick: () => setPerfDoc({ Id: row.documentId }) },
  ];

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
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setNonce((v) => v + 1)} disabled={loading}>
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

          <section className="analytics-panel" style={{ marginBottom: '1rem' }}>
            <div className="analytics-panel-header"><div><h2>Documents Ingested Over Time</h2></div></div>
            <TimeSeriesChart series={agg.series} windowStartUtc={data.WindowStartUtc} windowEndUtc={data.WindowEndUtc} hours={range.hours} />
          </section>

          <section className="analytics-panel" style={{ marginBottom: '1rem' }}>
            <div className="analytics-panel-header"><div><h2>Average Duration Per Stage</h2></div></div>
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
          </section>

          <section className="analytics-panel">
            <div className="analytics-panel-header"><div><h2>Recent Ingestions</h2></div></div>
            <DataTable columns={columns} fetchData={fetchTableData} getRowActions={getRowActions} refreshTrigger={nonce} />
          </section>
        </>
      )}

      {perfDoc && <IngestionPerformanceModal api={api} doc={perfDoc} onClose={() => setPerfDoc(null)} />}
    </div>
  );
}

export default IngestionPerformanceView;
