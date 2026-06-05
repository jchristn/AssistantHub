import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';

const RANGE_OPTIONS = [
  { id: 'lastHour', label: 'Last hour' },
  { id: 'lastDay', label: 'Last day' },
  { id: 'lastWeek', label: 'Last week' },
  { id: 'lastMonth', label: 'Last month' },
];

const MAX_X_AXIS_LABELS = 7;
const MIN_Y_AXIS_LABELS = 2;
const MAX_Y_AXIS_LABELS = 5;

const CHART_COLORS = [
  '#2563eb',
  '#16a34a',
  '#dc2626',
  '#d97706',
  '#0891b2',
  '#7c3aed',
  '#be123c',
  '#4f46e5',
  '#059669',
  '#9333ea',
];

const DEFAULT_RANGE = 'lastDay';

function formatNumber(value, maximumFractionDigits = 0) {
  if (value == null || Number.isNaN(Number(value))) return '-';
  return Number(value).toLocaleString(undefined, { maximumFractionDigits });
}

function formatDuration(value) {
  if (value == null || Number.isNaN(Number(value))) return '-';
  const numeric = Number(value);
  if (numeric >= 1000) return `${formatNumber(numeric / 1000, 2)}s`;
  return `${formatNumber(numeric, 0)}ms`;
}

function formatRate(value) {
  if (value == null || Number.isNaN(Number(value))) return '-';
  return `${formatNumber(Number(value) * 100, 1)}%`;
}

function formatDateTime(value) {
  if (!value) return '-';
  return new Date(value).toLocaleString();
}

function formatBucketLabel(value, rangeId) {
  const date = new Date(value);
  if (rangeId === 'lastHour' || rangeId === 'lastDay') {
    return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
  }
  if (rangeId === 'lastWeek') {
    return date.toLocaleString([], { weekday: 'short', hour: 'numeric' });
  }
  return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}

function roundAxisValue(value) {
  if (Math.abs(value) < 0.000000001) return 0;
  return Number(value.toPrecision(12));
}

function niceAxisStep(span) {
  if (!Number.isFinite(span) || span <= 0) return 1;

  const roughStep = span / (MAX_Y_AXIS_LABELS - 1);
  const power = Math.pow(10, Math.floor(Math.log10(roughStep)));
  const fraction = roughStep / power;

  if (fraction <= 1) return power;
  if (fraction <= 2) return 2 * power;
  if (fraction <= 2.5) return 2.5 * power;
  if (fraction <= 5) return 5 * power;
  return 10 * power;
}

function nextNiceAxisStep(step) {
  if (!Number.isFinite(step) || step <= 0) return 1;

  const power = Math.pow(10, Math.floor(Math.log10(step)));
  const fraction = step / power;

  if (fraction < 2) return 2 * power;
  if (fraction < 2.5) return 2.5 * power;
  if (fraction < 5) return 5 * power;
  if (fraction < 10) return 10 * power;
  return 20 * power;
}

function buildAxisTicks(axisMin, axisMax, step) {
  const ticks = [];
  for (let value = axisMin; value <= axisMax + (step / 2); value += step) {
    ticks.push(roundAxisValue(value));
  }

  return ticks;
}

function buildYAxisScale(minValue, maxValue, unit = '') {
  let min = Number(minValue);
  let max = Number(maxValue);

  if (!Number.isFinite(min)) min = 0;
  if (!Number.isFinite(max)) max = 1;
  if (min > max) {
    const previousMin = min;
    min = max;
    max = previousMin;
  }

  if (min === max) {
    if (min === 0) {
      max = 1;
    } else {
      const padding = Math.abs(min) * 0.1;
      min -= padding;
      max += padding;
    }
  }

  if (min > 0) min = 0;

  if (unit === 'count' && min >= 0 && Number.isInteger(max) && max <= MAX_Y_AXIS_LABELS - 1) {
    const axisMax = Math.max(1, max);
    const ticks = [];
    for (let index = 0; index <= axisMax; index += 1) {
      ticks.push(index);
    }

    return { min: 0, max: axisMax, ticks };
  }

  let step = niceAxisStep(max - min);
  let axisMin = roundAxisValue(Math.floor(min / step) * step);
  let axisMax = roundAxisValue(Math.ceil(max / step) * step);

  if (axisMax <= axisMin) axisMax = roundAxisValue(axisMin + step);

  let ticks = buildAxisTicks(axisMin, axisMax, step);
  while (ticks.length > MAX_Y_AXIS_LABELS) {
    step = nextNiceAxisStep(step);
    axisMin = roundAxisValue(Math.floor(min / step) * step);
    axisMax = roundAxisValue(Math.ceil(max / step) * step);
    ticks = buildAxisTicks(axisMin, axisMax, step);
  }

  if (ticks.length < MIN_Y_AXIS_LABELS) {
    ticks.push(axisMax);
  }

  return { min: axisMin, max: axisMax, ticks };
}

function formatAxisTickValue(value, unit) {
  if (unit === 'ms') return formatDuration(value);
  if (unit === 'ratio') return formatRate(value);
  if (unit === 'tokens/s') return formatNumber(value, Math.abs(Number(value)) < 10 ? 1 : 0);
  if (unit === 'count') return formatNumber(value, Number.isInteger(Number(value)) ? 0 : 2);
  return formatNumber(value, Number.isInteger(Number(value)) ? 0 : 1);
}

function pointsFor(result, metric) {
  return result?.Series?.find((series) => series.Metric === metric)?.Points || [];
}

function buildSeriesFromMetrics(result, definitions) {
  return definitions.map((definition, index) => ({
    ...definition,
    color: definition.color || CHART_COLORS[index % CHART_COLORS.length],
    points: pointsFor(result, definition.metric),
  }));
}

function RangeSelector({ value, onChange, className = '' }) {
  return (
    <div className={`analytics-range-selector ${className}`.trim()} role="group" aria-label="Analytics range">
      {RANGE_OPTIONS.map((option) => (
        <button
          key={option.id}
          type="button"
          className={value === option.id ? 'active' : ''}
          onClick={() => onChange(option.id)}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}

function RefreshIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M14 8A6 6 0 1 1 10 2.5" />
      <polyline points="14,2 14,6 10,6" />
    </svg>
  );
}

function ChartShell({ title, subtitle, loading, error, children }) {
  return (
    <section className="analytics-panel">
      <div className="analytics-panel-header">
        <div>
          <h2>{title}</h2>
          {subtitle && <p>{subtitle}</p>}
        </div>
      </div>
      {loading ? (
        <div className="analytics-state">Loading...</div>
      ) : error ? (
        <div className="analytics-state analytics-state-error">{error}</div>
      ) : (
        children
      )}
    </section>
  );
}

function MetricTile({ label, value, detail }) {
  return (
    <div className="analytics-metric-tile" title={detail || label}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function StackedBarChart({ series, rangeId, unit = '', emptyMessage = 'No data in this range.' }) {
  const [tooltip, setTooltip] = useState(null);
  const points = series?.[0]?.points || [];
  const maxTotal = Math.max(
    ...points.map((_, index) => series.reduce((total, item) => total + Math.max(0, Number(item.points?.[index]?.Value || 0)), 0)),
    0
  );

  if (!points.length || maxTotal <= 0) {
    return <div className="analytics-state">{emptyMessage}</div>;
  }

  const yAxisScale = buildYAxisScale(0, maxTotal, unit);
  const yAxisSpan = yAxisScale.max - yAxisScale.min;

  return (
    <div className="analytics-chart-wrap">
      <div className="analytics-legend">
        {series.map((item) => (
          <span key={item.metric || item.label}>
            <i style={{ background: item.color }} />
            {item.label}
          </span>
        ))}
      </div>
      <div className="analytics-chart-body">
        <YAxisLabels scale={yAxisScale} unit={unit} />
        <div className="analytics-bar-chart" style={{ gridTemplateColumns: `repeat(${points.length}, minmax(0, 1fr))` }}>
          {points.map((point, index) => {
            const total = series.reduce((sum, item) => sum + Math.max(0, Number(item.points?.[index]?.Value || 0)), 0);
            const scaledTotal = yAxisSpan > 0 ? (total - yAxisScale.min) / yAxisSpan : 0;
            const height = total > 0 ? Math.max(3, Math.round(Math.min(1, Math.max(0, scaledTotal)) * 100)) : 2;
            const tooltipLines = series.map((item) => `${item.label}: ${formatNumber(item.points?.[index]?.Value || 0, unit === 'ratio' ? 2 : 0)}${unit && unit !== 'count' ? ` ${unit}` : ''}`);

            return (
              <div
                key={`${point.BucketStartUtc}-${index}`}
                className="analytics-bar-column"
                onMouseEnter={(event) => setTooltip({ point, total, tooltipLines, clientX: event.clientX, clientY: event.clientY })}
                onMouseMove={(event) => setTooltip((current) => current ? { ...current, clientX: event.clientX, clientY: event.clientY } : current)}
                onMouseLeave={() => setTooltip(null)}
              >
                <div className="analytics-bar-track">
                  <div className="analytics-bar-stack" style={{ height: `${height}%` }}>
                    {series.map((item) => {
                      const value = Math.max(0, Number(item.points?.[index]?.Value || 0));
                      const segmentHeight = total > 0 ? (value / total) * 100 : 0;
                      return (
                        <span
                          key={item.metric || item.label}
                          style={{ height: `${segmentHeight}%`, background: item.color }}
                        />
                      );
                    })}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <XAxisLabels points={points} rangeId={rangeId} />
      {tooltip && <ChartTooltip tooltip={tooltip} rangeId={rangeId} unit={unit} />}
    </div>
  );
}

function LineChart({ series, rangeId, unit = '', emptyMessage = 'No data in this range.' }) {
  const [tooltip, setTooltip] = useState(null);
  const points = series?.[0]?.points || [];
  const values = series.flatMap((item) => item.points.map((point) => Number(point.Value)).filter((value) => Number.isFinite(value)));
  const max = Math.max(...values, 0);
  const min = Math.min(...values, 0);
  const width = 1000;
  const height = 220;
  const padX = 24;
  const padY = 18;

  if (!points.length || values.length < 1) {
    return <div className="analytics-state">{emptyMessage}</div>;
  }

  const yAxisScale = buildYAxisScale(min, max, unit);
  const yAxisSpan = yAxisScale.max - yAxisScale.min;
  const xFor = (index) => points.length === 1
    ? width / 2
    : padX + ((index / (points.length - 1)) * (width - (padX * 2)));
  const yFor = (value) => {
    if (yAxisSpan <= 0) return height / 2;
    return height - padY - (((value - yAxisScale.min) / yAxisSpan) * (height - (padY * 2)));
  };

  return (
    <div className="analytics-chart-wrap">
      <div className="analytics-legend">
        {series.map((item) => (
          <span key={item.metric || item.label}>
            <i style={{ background: item.color }} />
            {item.label}
          </span>
        ))}
      </div>
      <div className="analytics-chart-body">
        <YAxisLabels scale={yAxisScale} unit={unit} />
        <div className="analytics-line-chart">
          <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Analytics line chart">
            <line x1={padX} y1={height - padY} x2={width - padX} y2={height - padY} className="analytics-axis-line" />
            <line x1={padX} y1={padY} x2={padX} y2={height - padY} className="analytics-axis-line" />
            {series.map((item) => {
              const path = item.points
                .map((point, index) => {
                  const value = Number(point.Value);
                  if (!Number.isFinite(value)) return null;
                  return `${xFor(index)},${yFor(value)}`;
                })
                .filter(Boolean)
                .join(' ');
              return <polyline key={item.metric || item.label} points={path} fill="none" stroke={item.color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />;
            })}
            {points.map((point, index) => (
              <rect
                key={`${point.BucketStartUtc}-${index}`}
                x={xFor(index) - 8}
                y={0}
                width="16"
                height={height}
                fill="transparent"
                onMouseEnter={(event) => setTooltip({ point, index, clientX: event.clientX, clientY: event.clientY })}
                onMouseMove={(event) => setTooltip((current) => current ? { ...current, clientX: event.clientX, clientY: event.clientY } : current)}
                onMouseLeave={() => setTooltip(null)}
              />
            ))}
          </svg>
        </div>
      </div>
      <XAxisLabels points={points} rangeId={rangeId} />
      {tooltip && (
        <ChartTooltip
          tooltip={{
            ...tooltip,
            tooltipLines: series.map((item) => `${item.label}: ${formatMetricValue(item.points?.[tooltip.index]?.Value, unit)}`),
          }}
          rangeId={rangeId}
          unit={unit}
        />
      )}
    </div>
  );
}

function XAxisLabels({ points, rangeId }) {
  const labelCount = Math.min(MAX_X_AXIS_LABELS, points.length);
  const indices = new Set();
  for (let index = 0; index < labelCount; index += 1) {
    indices.add(Math.round((index * (points.length - 1)) / Math.max(1, labelCount - 1)));
  }

  return (
    <div className="analytics-chart-x-axis">
      <div />
      <div className="analytics-axis-labels">
        {Array.from(indices).map((index) => (
          <span key={index}>{formatBucketLabel(points[index]?.BucketStartUtc, rangeId)}</span>
        ))}
      </div>
    </div>
  );
}

function YAxisLabels({ scale, unit }) {
  return (
    <div className="analytics-y-axis-labels" aria-hidden="true">
      {scale.ticks.map((tick) => {
        const position = scale.max === scale.min
          ? 50
          : 100 - (((tick - scale.min) / (scale.max - scale.min)) * 100);
        return (
          <span key={tick} style={{ top: `${Math.min(100, Math.max(0, position))}%` }}>
            {formatAxisTickValue(tick, unit)}
          </span>
        );
      })}
    </div>
  );
}

function ChartTooltip({ tooltip, rangeId }) {
  const style = {
    left: Math.min(window.innerWidth - 340, tooltip.clientX + 14),
    top: Math.min(window.innerHeight - 180, tooltip.clientY + 14),
  };

  return (
    <div className="analytics-tooltip" style={style}>
      <strong>{formatBucketLabel(tooltip.point?.BucketStartUtc, rangeId)}</strong>
      <span>{formatDateTime(tooltip.point?.BucketStartUtc)} to {formatDateTime(tooltip.point?.BucketEndUtc)}</span>
      {(tooltip.tooltipLines || []).map((line) => <span key={line}>{line}</span>)}
    </div>
  );
}

function formatMetricValue(value, unit) {
  if (unit === 'ms') return formatDuration(value);
  if (unit === 'ratio') return formatRate(value);
  if (unit === 'tokens/s') return `${formatNumber(value, 1)} tokens/s`;
  if (unit === 'tokens') return `${formatNumber(value)} tokens`;
  return formatNumber(value);
}

function buildStageSeries(stageResult) {
  const buckets = stageResult?.Buckets || [];
  const range = stageResult?.Range;
  if (!range) return [];

  const stageTotals = new Map();
  buckets.forEach((bucket) => {
    const value = Number(bucket.AverageDurationMs || 0);
    stageTotals.set(bucket.Stage, (stageTotals.get(bucket.Stage) || 0) + value);
  });

  const topStages = Array.from(stageTotals.entries())
    .sort((left, right) => right[1] - left[1])
    .slice(0, 8)
    .map(([stage]) => stage);

  return topStages.map((stage, stageIndex) => ({
    metric: stage,
    label: stage.replace(/_/g, ' '),
    color: CHART_COLORS[stageIndex % CHART_COLORS.length],
    points: Array.from({ length: range.BucketCount }, (_, index) => {
      const bucketStart = new Date(new Date(range.StartUtc).getTime() + (index * range.BucketSeconds * 1000));
      const bucketEnd = new Date(bucketStart.getTime() + (range.BucketSeconds * 1000));
      const match = buckets.find((bucket) => bucket.Stage === stage && new Date(bucket.BucketStartUtc).getTime() === bucketStart.getTime());
      return {
        BucketStartUtc: bucketStart.toISOString(),
        BucketEndUtc: bucketEnd.toISOString(),
        Value: match?.AverageDurationMs || 0,
      };
    }),
  }));
}

function EndpointTable({ result }) {
  const rows = result?.Endpoints || [];
  if (!rows.length) return <div className="analytics-state">No endpoint activity in this range.</div>;

  return (
    <div className="analytics-table-wrap">
      <table className="analytics-table">
        <thead>
          <tr>
            <th>Endpoint</th>
            <th>Stage</th>
            <th>Model</th>
            <th>Calls</th>
            <th>Failures</th>
            <th>Avg</th>
            <th>P95</th>
            <th>Limiter</th>
            <th>Load</th>
            <th>Throughput</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={`${row.EndpointId || row.Model || row.Provider || 'endpoint'}-${index}`}>
              <td title={`${row.EndpointName || row.EndpointId || 'Unspecified'} ${row.Provider || ''}`.trim()}>
                <strong>{row.EndpointName || row.EndpointId || 'Unspecified'}</strong>
                <span>{row.EndpointType || row.Provider || row.ApiFormat || ''}</span>
              </td>
              <td>{row.Stage || '-'}</td>
              <td title={row.Model || ''}>{row.Model || '-'}</td>
              <td>{formatNumber(row.Calls)}</td>
              <td>{formatNumber(row.Failures)}</td>
              <td>{formatDuration(row.AverageDurationMs)}</td>
              <td>{formatDuration(row.P95DurationMs)}</td>
              <td>{formatDuration(row.AverageLimiterWaitMs)}</td>
              <td>{formatDuration(row.AverageProviderLoadMs)}</td>
              <td>{row.AverageTokensPerSecond == null ? '-' : `${formatNumber(row.AverageTokensPerSecond, 1)}/s`}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SlowestRequestsTable({ result, onOpenHistory, onOpenRequestHistory, canOpenRequestHistory }) {
  const rows = result?.Requests || [];
  if (!rows.length) return <div className="analytics-state">No slow request rows in this range.</div>;

  return (
    <div className="analytics-table-wrap">
      <table className="analytics-table">
        <thead>
          <tr>
            <th>Created</th>
            <th>Total</th>
            <th>Status</th>
            <th>Dominant stage</th>
            <th>Endpoint/model</th>
            <th>Request</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.RequestHistoryId || row.ChatHistoryId || row.TraceId}>
              <td>{formatDateTime(row.CreatedUtc)}</td>
              <td>{formatDuration(row.DurationMs)}</td>
              <td>{row.Success ? 'Success' : `Failed ${row.StatusCode || ''}`}</td>
              <td>
                <strong>{row.DominantStage || '-'}</strong>
                <span>{formatDuration(row.DominantStageDurationMs)}</span>
              </td>
              <td title={`${row.EndpointName || row.EndpointId || ''} ${row.Model || ''}`.trim()}>
                <strong>{row.EndpointName || row.EndpointId || row.Provider || '-'}</strong>
                <span>{row.Model || ''}</span>
              </td>
              <td title={row.RequestPath || ''}>{row.RequestPath || '-'}</td>
              <td className="analytics-actions">
                {row.ChatHistoryId && <button type="button" onClick={() => onOpenHistory(row.ChatHistoryId)}>History</button>}
                {canOpenRequestHistory && row.RequestHistoryId && <button type="button" onClick={() => onOpenRequestHistory(row.RequestHistoryId)}>Request</button>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function FeedbackChart({ result, rangeId }) {
  const points = (result?.Buckets || []).map((bucket) => ({
    BucketStartUtc: bucket.BucketStartUtc,
    BucketEndUtc: bucket.BucketEndUtc,
    Value: bucket.ThumbsUpCount,
    ThumbsUpCount: bucket.ThumbsUpCount,
    ThumbsDownCount: bucket.ThumbsDownCount,
  }));

  return (
    <StackedBarChart
      rangeId={rangeId}
      unit="count"
      emptyMessage="No feedback in this range."
      series={[
        { metric: 'thumbs_up', label: 'Thumbs up', color: '#16a34a', points: points.map((point) => ({ ...point, Value: point.ThumbsUpCount })) },
        { metric: 'thumbs_down', label: 'Thumbs down', color: '#dc2626', points: points.map((point) => ({ ...point, Value: point.ThumbsDownCount })) },
      ]}
    />
  );
}

function AssistantAnalyticsView() {
  const { serverUrl, credential, isGlobalAdmin, isTenantAdmin } = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const api = useMemo(() => new ApiClient(serverUrl, credential?.BearerToken), [serverUrl, credential]);
  const [assistants, setAssistants] = useState([]);
  const [assistantId, setAssistantId] = useState(searchParams.get('assistantId') || localStorage.getItem('ah_analytics_assistant') || '');
  const [range, setRange] = useState(DEFAULT_RANGE);
  const [data, setData] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [refreshNonce, setRefreshNonce] = useState(0);
  const canOpenRequestHistory = isGlobalAdmin || isTenantAdmin;

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const result = await api.getAssistants({ maxResults: 1000 });
        if (cancelled) return;
        const items = result?.Objects || (Array.isArray(result) ? result : []);
        setAssistants(items);

        const requestedAssistantId = searchParams.get('assistantId') || localStorage.getItem('ah_analytics_assistant') || '';
        const selectedExists = requestedAssistantId && items.some((assistant) => assistant.Id === requestedAssistantId);
        const nextAssistantId = selectedExists ? requestedAssistantId : items[0]?.Id || '';
        if (nextAssistantId && nextAssistantId !== assistantId) setAssistantId(nextAssistantId);
      } catch (err) {
        if (!cancelled) setError(err.message || 'Failed to load assistants.');
      }
    })();

    return () => { cancelled = true; };
  }, [api, serverUrl, credential]);

  useEffect(() => {
    if (!assistantId) return;
    localStorage.setItem('ah_analytics_assistant', assistantId);
    setSearchParams({ assistantId });
  }, [assistantId, setSearchParams]);

  useEffect(() => {
    if (!assistantId) return;
    let cancelled = false;

    (async () => {
      setLoading(true);
      setError('');
      try {
        const [
          overview,
          volume,
          latency,
          stages,
          provider,
          endpoints,
          activity,
          slowest,
          feedback,
        ] = await Promise.all([
          api.getAssistantAnalyticsOverview(assistantId, { range }),
          api.getAssistantAnalyticsTimeSeries(assistantId, { range, metrics: 'success_count,failure_count,request_count' }),
          api.getAssistantAnalyticsTimeSeries(assistantId, { range, metrics: 'avg_duration_ms,p95_duration_ms,p99_duration_ms,max_duration_ms' }),
          api.getAssistantAnalyticsStages(assistantId, { range }),
          api.getAssistantAnalyticsTimeSeries(assistantId, { range, metrics: 'provider_load_avg_ms,provider_generation_avg_ms,provider_tokens_per_second_avg,endpoint_limiter_wait_avg_ms' }),
          api.getAssistantAnalyticsEndpoints(assistantId, { range, limit: 25 }),
          api.getAssistantAnalyticsTimeSeries(assistantId, { range, metrics: 'query_rewrite_calls,rerank_calls,final_inference_calls,retrieval_query_count_avg,chunks_output_avg' }),
          api.getAssistantAnalyticsSlowest(assistantId, { range, limit: 20 }),
          api.getAssistantAnalyticsFeedback(assistantId, { range }),
        ]);

        if (!cancelled) {
          setData({ overview, volume, latency, stages, provider, endpoints, activity, slowest, feedback });
        }
      } catch (err) {
        if (!cancelled) setError(err.message || 'Failed to load analytics.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [api, assistantId, range, refreshNonce]);

  const selectedAssistant = assistants.find((assistant) => assistant.Id === assistantId);

  const openHistory = (chatHistoryId) => {
    const params = new URLSearchParams();
    params.set('assistantId', assistantId);
    if (chatHistoryId) params.set('historyId', chatHistoryId);
    navigate(`/history?${params.toString()}`);
  };

  const openRequestHistory = (requestHistoryId) => {
    navigate(`/request-history?requestId=${encodeURIComponent(requestHistoryId)}`);
  };

  const overview = data.overview;
  const volumeSeries = buildSeriesFromMetrics(data.volume, [
    { metric: 'success_count', label: 'Succeeded', color: '#16a34a' },
    { metric: 'failure_count', label: 'Failed', color: '#dc2626' },
  ]);
  const latencySeries = buildSeriesFromMetrics(data.latency, [
    { metric: 'avg_duration_ms', label: 'Average', color: '#2563eb' },
    { metric: 'p95_duration_ms', label: 'P95', color: '#d97706' },
    { metric: 'p99_duration_ms', label: 'P99', color: '#dc2626' },
    { metric: 'max_duration_ms', label: 'Max', color: '#7c3aed' },
  ]);
  const providerSeries = buildSeriesFromMetrics(data.provider, [
    { metric: 'provider_load_avg_ms', label: 'Provider load', color: '#d97706' },
    { metric: 'provider_generation_avg_ms', label: 'Generation', color: '#2563eb' },
    { metric: 'endpoint_limiter_wait_avg_ms', label: 'Limiter wait', color: '#dc2626' },
  ]);
  const throughputSeries = buildSeriesFromMetrics(data.provider, [
    { metric: 'provider_tokens_per_second_avg', label: 'Tokens/sec', color: '#16a34a' },
  ]);
  const activitySeries = buildSeriesFromMetrics(data.activity, [
    { metric: 'query_rewrite_calls', label: 'Rewrite calls', color: '#7c3aed' },
    { metric: 'rerank_calls', label: 'Rerank calls', color: '#0891b2' },
    { metric: 'final_inference_calls', label: 'Final inference', color: '#2563eb' },
  ]);
  const retrievalSeries = buildSeriesFromMetrics(data.activity, [
    { metric: 'retrieval_query_count_avg', label: 'Retrieval queries', color: '#d97706' },
    { metric: 'chunks_output_avg', label: 'Chunks returned', color: '#16a34a' },
  ]);
  const stageSeries = buildStageSeries(data.stages);

  return (
    <div className="assistant-analytics-view">
      <div className="content-header analytics-header">
        <div>
          <h1 className="content-title">Assistant Analytics</h1>
          <p className="content-subtitle">{selectedAssistant ? selectedAssistant.Name : 'Select an assistant'}</p>
        </div>
        <div className="analytics-header-controls">
          <div className="analytics-assistant-control-group">
            <select value={assistantId} onChange={(event) => setAssistantId(event.target.value)} disabled={!assistants.length}>
              {assistants.length < 1 && <option value="">No assistants</option>}
              {assistants.map((assistant) => (
                <option key={assistant.Id} value={assistant.Id}>{assistant.Name} ({assistant.Id.substring(0, 10)}...)</option>
              ))}
            </select>
            <button
              type="button"
              className="analytics-refresh-button"
              onClick={() => setRefreshNonce((value) => value + 1)}
              title="Refresh analytics"
              aria-label="Refresh analytics"
            >
              <RefreshIcon />
            </button>
          </div>
          <RangeSelector value={range} onChange={setRange} className="analytics-header-range-selector" />
        </div>
      </div>

      {!assistantId ? (
        <div className="empty-state"><p>No assistants are available.</p></div>
      ) : (
        <>
          <ChartShell
            title="Overview"
            subtitle={overview?.GeneratedUtc ? `Generated ${formatDateTime(overview.GeneratedUtc)}` : ''}
            loading={loading}
            error={error}
          >
            <div className="analytics-metric-grid">
              <MetricTile label="Requests" value={formatNumber(overview?.RequestCount)} detail="Total assistant request-history rows in this range." />
              <MetricTile label="Success rate" value={formatRate(overview?.SuccessRate)} detail="Successful requests divided by total requests." />
              <MetricTile label="P95 latency" value={formatDuration(overview?.P95DurationMs)} detail="95th percentile total HTTP request duration." />
              <MetricTile label="P99 latency" value={formatDuration(overview?.P99DurationMs)} detail="99th percentile total HTTP request duration." />
              <MetricTile label="Telemetry coverage" value={formatRate(overview?.TelemetryCoverageRate)} detail="Requests with linked performance-event telemetry." />
              <MetricTile label="Dominant stage" value={overview?.DominantStage || '-'} detail="Stage with the highest aggregate duration in this range." />
              <MetricTile label="Top model" value={overview?.TopEndpointModel || overview?.TopEndpointProvider || '-'} detail="Most frequently used provider/model among performance events." />
              <MetricTile label="Negative feedback" value={formatRate(overview?.NegativeFeedbackRate)} detail="Thumbs-down feedback divided by total feedback in this range." />
            </div>
          </ChartShell>

          <div className="analytics-grid analytics-grid-two">
            <ChartShell title="Request Volume and Outcome" loading={loading} error={error}>
              <StackedBarChart series={volumeSeries} rangeId={range} unit="count" />
            </ChartShell>

            <ChartShell title="End-to-End Latency Percentiles" loading={loading} error={error}>
              <LineChart series={latencySeries} rangeId={range} unit="ms" />
            </ChartShell>
          </div>

          <ChartShell title="Hot-Path Stage Duration Breakdown" loading={loading} error={error}>
            <StackedBarChart series={stageSeries} rangeId={range} unit="ms" emptyMessage="No stage telemetry in this range." />
          </ChartShell>

          <div className="analytics-grid analytics-grid-two">
            <ChartShell title="Inference Provider Timing" loading={loading} error={error}>
              <LineChart series={providerSeries} rangeId={range} unit="ms" emptyMessage="No provider timing telemetry in this range." />
            </ChartShell>

            <ChartShell title="Provider Throughput" loading={loading} error={error}>
              <LineChart series={throughputSeries} rangeId={range} unit="tokens/s" emptyMessage="No provider throughput telemetry in this range." />
            </ChartShell>
          </div>

          <div className="analytics-grid analytics-grid-two">
            <ChartShell title="Query Rewrite, Rerank, and Inference Activity" loading={loading} error={error}>
              <StackedBarChart series={activitySeries} rangeId={range} unit="count" emptyMessage="No rewrite, rerank, or inference activity in this range." />
            </ChartShell>

            <ChartShell title="Retrieval Fanout and Chunk Flow" loading={loading} error={error}>
              <LineChart series={retrievalSeries} rangeId={range} unit="count" emptyMessage="No retrieval fanout telemetry in this range." />
            </ChartShell>
          </div>

          <ChartShell title="Endpoint and Model Usage Mix" loading={loading} error={error}>
            <EndpointTable result={data.endpoints} />
          </ChartShell>

          <ChartShell title="Slowest Requests" loading={loading} error={error}>
            <SlowestRequestsTable
              result={data.slowest}
              onOpenHistory={openHistory}
              onOpenRequestHistory={openRequestHistory}
              canOpenRequestHistory={canOpenRequestHistory}
            />
          </ChartShell>

          <ChartShell title="Feedback Trend" loading={loading} error={error}>
            <FeedbackChart result={data.feedback} rangeId={range} />
          </ChartShell>
        </>
      )}
    </div>
  );
}

export default AssistantAnalyticsView;
