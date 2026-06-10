import React, { useEffect, useMemo, useState } from 'react';
import Modal from '../Modal';
import CopyButton from '../CopyButton';
import CopyableId from '../CopyableId';
import AssistantAttachedDocumentsSection from './AssistantAttachedDocumentsSection';
import AssistantToolCallTraceSection from './AssistantToolCallTraceSection';
import { ApiClient } from '../../utils/api';
import { useAuth } from '../../context/AuthContext';

function formatJsonBlock(value) {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  return JSON.stringify(value, null, 2);
}

function normalizeNumber(value) {
  if (value == null || value === '') return null;
  const number = Number(value);
  return isFinite(number) ? number : null;
}

function formatMs(ms) {
  const number = normalizeNumber(ms);
  if (number == null) return 'N/A';
  if (number <= 0) return '0 ms';
  if (number < 1000) return number.toFixed(1) + ' ms';
  return (number / 1000).toFixed(2) + ' s';
}

function formatNumber(value) {
  if (value == null || value === '') return 'N/A';
  const number = Number(value);
  if (!isFinite(number)) return 'N/A';
  return number.toLocaleString();
}

function formatStageLabel(name) {
  if (!name) return 'Unknown';
  if (name === 'tool_iteration_model') return 'Tool Model Check';
  if (name === 'tools') return 'Tool Execution';
  return String(name)
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

function parsePerformanceTelemetry(history) {
  if (!history?.PerformanceJson) return null;
  try {
    const parsed = JSON.parse(history.PerformanceJson);
    return parsed && Array.isArray(parsed.Stages) ? parsed : null;
  } catch {
    return null;
  }
}

function getClientTiming(stage, key) {
  return normalizeNumber(stage?.ClientTimings?.[key]);
}

function getProviderMetric(stage, key) {
  return normalizeNumber(stage?.ProviderMetrics?.[key]);
}

function getStageMetadata(stage, key) {
  const metadata = stage?.Metadata || {};
  return Object.prototype.hasOwnProperty.call(metadata, key) ? metadata[key] : undefined;
}

function getStageDuration(stage) {
  return normalizeNumber(stage?.DurationMs) ?? 0;
}

function getMetadataNumber(stage, key) {
  return normalizeNumber(getStageMetadata(stage, key));
}

function getStageSpanDuration(stage) {
  const metadataSpan = getMetadataNumber(stage, 'tool_call_span_duration_ms');
  if (metadataSpan != null) return metadataSpan;

  const started = stage?.StartedUtc ? Date.parse(stage.StartedUtc) : null;
  const finished = stage?.FinishedUtc ? Date.parse(stage.FinishedUtc) : null;
  if (started == null || finished == null || !isFinite(started) || !isFinite(finished) || finished < started) return null;
  return finished - started;
}

function findStage(stages, name) {
  return (stages || []).find((stage) => stage?.Name === name) || null;
}

function sumStageDuration(stages, name) {
  const matches = (stages || []).filter((stage) => stage?.Name === name);
  if (matches.length < 1) return null;
  return matches.reduce((sum, stage) => sum + getStageDuration(stage), 0);
}

function timingValue(primary, fallback) {
  const primaryNumber = normalizeNumber(primary);
  if (primaryNumber != null) return primaryNumber;
  const fallbackNumber = normalizeNumber(fallback);
  return fallbackNumber ?? 0;
}

function withEstimatedToolModelStage(stages, wallTimeMs) {
  const source = Array.isArray(stages) ? stages : [];
  if (source.some((stage) => stage?.Name === 'tool_iteration_model')) return source;

  const toolsStage = findStage(source, 'tools');
  const finalStage = findStage(source, 'final_inference');
  const wall = normalizeNumber(wallTimeMs);
  if (!toolsStage || !finalStage || wall == null) return source;

  const toolExecutionMs = getMetadataNumber(toolsStage, 'tool_call_duration_ms')
    ?? sumStageDuration(source, 'tools')
    ?? getStageDuration(toolsStage);
  const finalInferenceMs = getStageDuration(finalStage);
  const estimatedMs = Math.round(Math.max(0, wall - finalInferenceMs - toolExecutionMs) * 100) / 100;
  if (estimatedMs <= 0) return source;

  const estimatedStage = {
    Name: 'tool_iteration_model',
    Kind: 'inference',
    Sequence: 64,
    EndpointId: finalStage.EndpointId,
    EndpointName: finalStage.EndpointName,
    EndpointType: finalStage.EndpointType,
    Provider: finalStage.Provider,
    ApiFormat: finalStage.ApiFormat,
    Model: finalStage.Model,
    DurationMs: estimatedMs,
    Success: true,
    ClientTimings: { TotalMs: estimatedMs },
    Metadata: {
      phase: 'assistant_tool_model_legacy_estimate',
      estimated: true,
      summary: 'Estimated model time spent checking whether tools were needed.',
      source: 'wall_time_minus_final_inference_minus_tool_execution',
      tool_call_count: getMetadataNumber(toolsStage, 'tool_call_count')
    }
  };

  const insertIndex = source.findIndex((stage) => stage?.Name === 'tools');
  if (insertIndex < 0) return [...source, estimatedStage];
  return [
    ...source.slice(0, insertIndex),
    estimatedStage,
    ...source.slice(insertIndex)
  ];
}

function hasEndpointDetails(stage) {
  return Boolean(stage?.EndpointId || stage?.EndpointName || stage?.Provider || stage?.Model);
}

function getStageNoopReason(stage, stages) {
  if (!stage || getStageDuration(stage) > 0 || hasEndpointDetails(stage)) return null;

  const gateStage = stages?.find((candidate) => candidate?.Name === 'retrieval_gate');
  const gateDecision = String(getStageMetadata(gateStage, 'decision') || '').toUpperCase();

  if (stage.Name === 'query_rewrite') {
    if (gateDecision === 'SKIP') return 'Retrieval gate skipped query rewrite';
    return 'Query rewrite was not run';
  }

  if (stage.Name === 'retrieval') {
    if (gateDecision === 'SKIP') return 'Retrieval gate skipped retrieval';
    return 'Retrieval was not run';
  }

  if (stage.Name === 'rerank') {
    const chunksInput = Number(getStageMetadata(stage, 'chunks_input') || 0);
    if (chunksInput < 1) return 'No chunks to rerank';
    return 'Rerank was not run';
  }

  return null;
}

function RequestPerformanceTable({ stages }) {
  if (!stages || stages.length < 1) return null;

  return (
    <div className="history-stage-table-wrap">
      <table className="history-stage-table">
        <thead>
          <tr>
            <th>Stage</th>
            <th>Endpoint</th>
            <th>Duration</th>
            <th>Queue</th>
            <th>Headers</th>
            <th>First Token</th>
            <th>Generation</th>
            <th>Provider Load</th>
            <th>Prompt Eval</th>
            <th>Tokens</th>
          </tr>
        </thead>
        <tbody>
          {stages.map((stage, idx) => {
            const tokens = stage.Tokens || {};
            const inputTokens = tokens.Input ?? tokens.PromptEvalCount;
            const outputTokens = tokens.Output ?? tokens.EvalCount;
            const noopReason = getStageNoopReason(stage, stages);
            const endpointText = stage.EndpointId || stage.EndpointName || (noopReason ? 'Not run' : '-');
            const generationMs = getClientTiming(stage, 'FirstTokenToLastTokenMs') ?? getProviderMetric(stage, 'GenerationMs');
            return (
              <tr key={`${stage.Name || 'stage'}-${idx}`}>
                <td>
                  <div className="history-stage-name">{formatStageLabel(stage.Name)}</div>
                  <div className="history-stage-kind">{stage.Kind || 'stage'}</div>
                </td>
                <td>
                  <div
                    className={['history-stage-endpoint', noopReason ? 'history-stage-muted' : ''].filter(Boolean).join(' ')}
                    title={noopReason || undefined}
                  >
                    {endpointText}
                  </div>
                  {(stage.Provider || stage.Model) && (
                    <div className="history-stage-kind">{[stage.Provider, stage.Model].filter(Boolean).join(' / ')}</div>
                  )}
                </td>
                <td>{formatMs(getStageDuration(stage))}</td>
                <td>{formatMs(getClientTiming(stage, 'EndpointLimiterWaitMs'))}</td>
                <td>{formatMs(getClientTiming(stage, 'RequestToHeadersMs'))}</td>
                <td>{formatMs(getClientTiming(stage, 'HeadersToFirstTokenMs'))}</td>
                <td>{formatMs(generationMs)}</td>
                <td>{formatMs(getProviderMetric(stage, 'LoadMs'))}</td>
                <td>{formatMs(getProviderMetric(stage, 'PromptEvalMs'))}</td>
                <td>{inputTokens || outputTokens ? `${formatNumber(inputTokens)} / ${formatNumber(outputTokens)}` : 'N/A'}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function RequestHistoryDetailModal({ entry, onClose, onReplay }) {
  const { serverUrl, credential } = useAuth();
  const [linkedHistory, setLinkedHistory] = useState(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState(null);

  const performanceTelemetry = useMemo(() => parsePerformanceTelemetry(linkedHistory), [linkedHistory]);
  const performanceStages = withEstimatedToolModelStage(
    performanceTelemetry?.Stages || [],
    performanceTelemetry?.WallTimeMs ?? linkedHistory?.TimeToLastTokenMs);
  const finalInferenceStage = performanceStages.find((stage) => stage.Name === 'final_inference')
    || performanceStages.find((stage) => stage.Kind === 'inference' && getStageDuration(stage) > 0);
  const toolExecutionStage = findStage(performanceStages, 'tools');
  const toolExecutionMs = timingValue(getMetadataNumber(toolExecutionStage, 'tool_call_duration_ms'), sumStageDuration(performanceStages, 'tools'));
  const recordedToolModelChecksMs = sumStageDuration(performanceStages, 'tool_iteration_model');
  const toolSpanMs = getStageSpanDuration(toolExecutionStage);
  const inferenceWallMs = normalizeNumber(performanceTelemetry?.WallTimeMs) ?? normalizeNumber(linkedHistory?.TimeToLastTokenMs);
  const finalInferenceMs = finalInferenceStage ? getStageDuration(finalInferenceStage) : null;
  const legacyToolModelChecksFromWallMs = recordedToolModelChecksMs == null && toolExecutionStage && inferenceWallMs != null && finalInferenceMs != null
    ? Math.max(0, inferenceWallMs - finalInferenceMs - toolExecutionMs)
    : null;
  const legacyToolModelChecksFromSpanMs = recordedToolModelChecksMs == null && toolSpanMs != null
    ? Math.max(0, toolSpanMs - toolExecutionMs)
    : null;
  const toolModelChecksMs = recordedToolModelChecksMs ?? legacyToolModelChecksFromWallMs ?? legacyToolModelChecksFromSpanMs;
  const hasToolTiming = toolModelChecksMs != null || toolExecutionMs > 0;
  const finalPromptProcessingMs = getClientTiming(finalInferenceStage, 'HeadersToFirstTokenMs')
    ?? getProviderMetric(finalInferenceStage, 'PromptEvalMs');
  const finalGenerationMs = getClientTiming(finalInferenceStage, 'FirstTokenToLastTokenMs')
    ?? getProviderMetric(finalInferenceStage, 'GenerationMs');

  useEffect(() => {
    if (!entry?.ChatHistoryId) {
      setLinkedHistory(null);
      setHistoryError(null);
      setHistoryLoading(false);
      return;
    }

    let cancelled = false;
    const api = new ApiClient(serverUrl, credential?.BearerToken);
    setHistoryLoading(true);
    setHistoryError(null);

    (async () => {
      try {
        const history = await api.getHistory(entry.ChatHistoryId);
        if (!cancelled) setLinkedHistory(history || null);
      } catch (err) {
        if (!cancelled) setHistoryError(err?.message || 'Failed to load linked chat history.');
      } finally {
        if (!cancelled) setHistoryLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [entry?.ChatHistoryId, serverUrl, credential]);

  if (!entry) return null;

  const requestHeaders = formatJsonBlock(entry.RequestHeaders);
  const responseHeaders = formatJsonBlock(entry.ResponseHeaders);
  const routeParameters = formatJsonBlock(entry.RouteParameters);
  const queryParameters = formatJsonBlock(entry.QueryParameters);
  const requestBody = entry.RequestBody || '';
  const responseBody = entry.ResponseBody || '';

  const footer = (
    <>
      {onReplay && (
        <button className="btn btn-primary" onClick={() => onReplay(entry)}>
          Replay In Explorer
        </button>
      )}
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    </>
  );

  return (
    <Modal title={`Request History: ${entry.Id}`} onClose={onClose} fullscreen footer={footer}>
      <div className="request-history-detail">
        <div className="request-history-detail-hero">
          <div className="request-history-detail-status">
            <span className={`request-history-method method-${(entry.HttpMethod || 'GET').toLowerCase()}`}>
              {entry.HttpMethod || 'GET'}
            </span>
            <span className={`status-badge ${entry.Success ? 'active' : 'failed'}`}>
              HTTP {entry.StatusCode}
            </span>
            <span className="request-history-detail-pill">{entry.RequestType || 'SystemApi'}</span>
            <span className="request-history-detail-pill">{entry.SourceType || 'api'}</span>
          </div>
          <div className="request-history-detail-path">{entry.RequestUrl || entry.RequestPath}</div>
          <div className="request-history-detail-stats">
            <div className="stat-card">
              <span className="stat-card-label">Duration</span>
              <span className="stat-card-value">{Math.round(entry.DurationMs || 0)} ms</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Request Size</span>
              <span className="stat-card-value">{Number(entry.RequestSizeBytes || 0).toLocaleString()} B</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Response Size</span>
              <span className="stat-card-value">{Number(entry.ResponseSizeBytes || 0).toLocaleString()} B</span>
            </div>
            <div className="stat-card">
              <span className="stat-card-label">Created</span>
              <span className="stat-card-value request-history-timestamp">{entry.CreatedUtc ? new Date(entry.CreatedUtc).toLocaleString() : '-'}</span>
            </div>
          </div>
        </div>

        {entry.ChatHistoryId && (
          <section className="request-history-detail-section request-history-performance-section">
            <div className="request-history-detail-section-title">
              Performance Timing
              {performanceTelemetry && (
                <span className="history-section-badge">Telemetry v{performanceTelemetry.SchemaVersion || 1}</span>
              )}
            </div>
            {historyLoading ? (
              <div className="empty-state compact"><p>Loading linked assistant timing...</p></div>
            ) : historyError ? (
              <div className="empty-state compact"><p>{historyError}</p></div>
            ) : performanceTelemetry ? (
              <>
                <div className="request-history-performance-summary">
                  <div className="stat-card">
                    <span className="stat-card-label">Pipeline Wall Time</span>
                    <span className="stat-card-value">{formatMs(performanceTelemetry.WallTimeMs)}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Stages</span>
                    <span className="stat-card-value">{performanceStages.length}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Request to Headers</span>
                    <span className="stat-card-value">{formatMs(getClientTiming(finalInferenceStage, 'RequestToHeadersMs'))}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Prompt Processing</span>
                    <span className="stat-card-value">{formatMs(finalPromptProcessingMs)}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Generation</span>
                    <span className="stat-card-value">{formatMs(finalGenerationMs)}</span>
                  </div>
                  <div className="stat-card">
                    <span className="stat-card-label">Provider Load</span>
                    <span className="stat-card-value">{formatMs(getProviderMetric(finalInferenceStage, 'LoadMs'))}</span>
                  </div>
                  {hasToolTiming && (
                    <>
                      <div className="stat-card">
                        <span className="stat-card-label">Tool Model Checks</span>
                        <span className="stat-card-value">{formatMs(toolModelChecksMs)}</span>
                      </div>
                      <div className="stat-card">
                        <span className="stat-card-label">Tool Execution</span>
                        <span className="stat-card-value">{formatMs(toolExecutionMs)}</span>
                      </div>
                    </>
                  )}
                </div>
                <RequestPerformanceTable stages={performanceStages} />
              </>
            ) : (
              <div className="empty-state compact"><p>No v0.12.0 performance telemetry was recorded for the linked chat history row.</p></div>
            )}
          </section>
        )}

        {linkedHistory && (
          <AssistantAttachedDocumentsSection history={linkedHistory} />
        )}

        <AssistantToolCallTraceSection
          assistantId={entry.AssistantId}
          requestHistoryId={entry.Id}
          traceId={entry.TraceId}
        />

        <div className="request-history-detail-grid">
          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Identifiers</div>
            <div className="request-history-detail-meta">
              <div className="request-history-detail-meta-row">
                <span>ID</span>
                <CopyableId id={entry.Id} />
              </div>
              <div className="request-history-detail-meta-row">
                <span>Trace</span>
                {entry.TraceId ? <CopyableId id={entry.TraceId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Chat History</span>
                {entry.ChatHistoryId ? <CopyableId id={entry.ChatHistoryId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Tenant</span>
                {entry.TenantId ? <CopyableId id={entry.TenantId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>User</span>
                {entry.UserId ? <CopyableId id={entry.UserId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Credential</span>
                {entry.CredentialId ? <CopyableId id={entry.CredentialId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Assistant</span>
                {entry.AssistantId ? <CopyableId id={entry.AssistantId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Thread</span>
                {entry.ThreadId ? <CopyableId id={entry.ThreadId} /> : <strong>-</strong>}
              </div>
              <div className="request-history-detail-meta-row">
                <span>Principal</span>
                <strong>{entry.PrincipalName || '-'}</strong>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Source IP</span>
                <strong>{entry.SourceIp || '-'}</strong>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Route Template</span>
                <code>{entry.RouteTemplate || '-'}</code>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Request Content-Type</span>
                <code>{entry.RequestContentType || '-'}</code>
              </div>
              <div className="request-history-detail-meta-row">
                <span>Response Content-Type</span>
                <code>{entry.ResponseContentType || '-'}</code>
              </div>
            </div>
          </section>

          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Request Parameters</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Route Parameters</span>
                <CopyButton text={routeParameters} />
              </div>
              <pre className="json-view request-history-block-body">{routeParameters || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Query Parameters</span>
                <CopyButton text={queryParameters} />
              </div>
              <pre className="json-view request-history-block-body">{queryParameters || '{}'}</pre>
            </div>
          </section>
        </div>

        <div className="request-history-panels">
          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Request</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Headers</span>
                <CopyButton text={requestHeaders} />
              </div>
              <pre className="json-view request-history-block-body">{requestHeaders || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Body</span>
                <div className="request-history-block-flags">
                  {entry.RequestBodyTruncated && <span className="request-history-detail-pill warning">Truncated</span>}
                  {entry.RequestBodyIsBinary && <span className="request-history-detail-pill">Binary</span>}
                  <CopyButton text={requestBody} />
                </div>
              </div>
              <pre className="json-view request-history-block-body">{requestBody || '(empty)'}</pre>
            </div>
          </section>

          <section className="request-history-detail-section">
            <div className="request-history-detail-section-title">Response</div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Headers</span>
                <CopyButton text={responseHeaders} />
              </div>
              <pre className="json-view request-history-block-body">{responseHeaders || '{}'}</pre>
            </div>
            <div className="request-history-block">
              <div className="request-history-block-header">
                <span>Body</span>
                <div className="request-history-block-flags">
                  {entry.ResponseBodyTruncated && <span className="request-history-detail-pill warning">Truncated</span>}
                  {entry.ResponseBodyIsBinary && <span className="request-history-detail-pill">Binary</span>}
                  <CopyButton text={responseBody} />
                </div>
              </div>
              <pre className="json-view request-history-block-body">{responseBody || '(empty)'}</pre>
            </div>
          </section>
        </div>
      </div>
    </Modal>
  );
}

export default RequestHistoryDetailModal;
