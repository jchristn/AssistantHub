import React, { useState, useEffect, useCallback } from 'react';
import Modal from '../Modal';

// Per-stage bar colors, keyed by normalized stage name (falls back to a rotating palette).
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

function normalizeKey(value) {
  return String(value ?? '').toLowerCase().replace(/[^a-z0-9]/g, '');
}

function colorFor(stage, idx) {
  return STAGE_COLORS[normalizeKey(stage)] || PALETTE[idx % PALETTE.length];
}

function formatDuration(ms) {
  if (ms == null || Number.isNaN(ms)) return '—';
  if (ms < 1000) return `${Math.round(ms)} ms`;
  const seconds = ms / 1000;
  if (seconds < 60) return `${seconds.toFixed(seconds < 10 ? 2 : 1)} s`;
  const minutes = Math.floor(seconds / 60);
  const remSeconds = Math.round(seconds % 60);
  return `${minutes}m ${remSeconds}s`;
}

function IngestionPerformanceModal({ api, doc, onClose }) {
  const documentId = doc?.Id || doc?.GUID;
  const [data, setData] = useState(undefined);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    try {
      setError(null);
      const result = await api.getDocumentPerformance(documentId);
      setData(result || null);
    } catch (err) {
      setError(err.message || 'Failed to load ingestion performance');
      setData(null);
    }
  }, [api, documentId]);

  useEffect(() => { load(); }, [load]);

  const stages = (data && Array.isArray(data.Stages)) ? data.Stages : [];
  const summedMs = stages.reduce((sum, s) => sum + (Number(s.DurationMs) || 0), 0);
  const totalMs = data && typeof data.TotalMs === 'number' && data.TotalMs > 0 ? data.TotalMs : summedMs;
  const maxMs = stages.reduce((max, s) => Math.max(max, Number(s.DurationMs) || 0), 0);

  return (
    <Modal
      title="Ingestion Performance"
      onClose={onClose}
      wide
      footer={<button className="btn btn-secondary" onClick={onClose}>Close</button>}
    >
      {data === undefined && !error && <p>Loading...</p>}
      {error && <p style={{ color: 'var(--danger-color, #e74c3c)' }}>{error}</p>}
      {data !== undefined && !error && stages.length === 0 && (
        <p className="ah-perf-empty">No timed stages recorded for this document yet.</p>
      )}

      {stages.length > 0 && (
        <>
          <div className="ah-perf-filename" title={doc?.OriginalFilename || documentId}>
            {doc?.OriginalFilename || documentId}
          </div>

          <div className="ah-perf-metrics">
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Total Runtime</span>
              <span className="ah-perf-metric-value">{formatDuration(totalMs)}</span>
            </div>
            <div className="ah-perf-metric">
              <span className="ah-perf-metric-label">Timed Stages</span>
              <span className="ah-perf-metric-value">{stages.length}</span>
            </div>
          </div>

          <div className="ah-perf-section">
            <div className="ah-perf-section-title">Time Per Stage</div>
            <div className="ah-perf-timing">
              {stages.map((s, i) => {
                const ms = Number(s.DurationMs) || 0;
                const pct = maxMs > 0 ? Math.max(2, (ms / maxMs) * 100) : 0;
                const share = totalMs > 0 ? ((ms / totalMs) * 100).toFixed(0) : '0';
                const failed = s.Success === false;
                const label = s.Stage || 'Stage';
                const barColor = failed ? 'var(--danger-color, #e74c3c)' : colorFor(label, i);
                const titleText = `${label}: ${formatDuration(ms)} (${share}% of total)`
                  + (s.Detail ? ` — ${s.Detail}` : '')
                  + (failed ? ' — failed' : '');
                return (
                  <div className="ah-perf-timing-row" key={s.Id || `${label}-${i}`} title={titleText}>
                    <span className="ah-perf-timing-label">{label}{failed ? ' ⚠' : ''}</span>
                    <span className="ah-perf-timing-track">
                      <span className="ah-perf-timing-fill" style={{ width: `${Math.min(pct, 100)}%`, background: barColor }} />
                    </span>
                    <span className="ah-perf-timing-value">{formatDuration(ms)} &middot; {share}%</span>
                  </div>
                );
              })}
            </div>
          </div>
        </>
      )}
    </Modal>
  );
}

export default IngestionPerformanceModal;
