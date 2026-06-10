import React, { useState, useEffect, useMemo } from 'react';
import Modal from '../Modal';
import CopyableId from '../CopyableId';
import Tooltip from '../Tooltip';
import AssistantAttachedDocumentsSection from './AssistantAttachedDocumentsSection';
import AssistantToolCallTraceSection from './AssistantToolCallTraceSection';
import { ApiClient } from '../../utils/api';
import { useAuth } from '../../context/AuthContext';

function formatTps(tps) {
  if (!tps || tps <= 0 || !isFinite(tps)) return 'N/A';
  return tps.toFixed(1) + ' tok/s';
}

function formatMs(ms) {
  if (!ms || ms <= 0) return 'N/A';
  if (ms < 1000) return ms.toFixed(1) + ' ms';
  return (ms / 1000).toFixed(2) + ' s';
}

function formatTimestamp(utc) {
  if (!utc) return 'N/A';
  return new Date(utc).toLocaleString();
}

function formatNumber(value) {
  if (value == null || value === '') return 'N/A';
  const number = Number(value);
  if (!isFinite(number)) return 'N/A';
  return number.toLocaleString();
}

function formatStageLabel(name) {
  if (!name) return 'Unknown';
  return String(name)
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

const stageColumnTooltips = {
  Stage: 'The assistant pipeline step that was measured for this chat turn.',
  Endpoint: 'The configured inference endpoint, provider, and model used by this stage when an upstream model call was made.',
  Duration: 'Total elapsed time for this stage as captured by AssistantHub.',
  Queue: 'Time spent waiting for AssistantHub endpoint concurrency limits before the provider request was allowed to run.',
  Headers: 'Client-observed time from sending the provider HTTP request to receiving response headers.',
  'First Token': 'Client-observed time from response headers to the first streamed token.',
  Generation: 'Time spent generating output after the first token, or the provider-reported generation duration when available.',
  'Provider Load': 'Provider-reported model-load time. Ollama can report this; OpenAI-compatible providers often cannot.',
  'Prompt Eval': 'Provider-reported prompt-evaluation time, when the provider exposes it.',
  Tokens: 'Input and output token counts for this stage, shown as input / output when known.'
};

const identifierTooltips = {
  History: 'Unique identifier for this assistant history turn.',
  Thread: 'Conversation thread identifier for this chat turn.',
  Assistant: 'Assistant identifier that handled this chat turn.',
  Trace: 'Correlation identifier shared by chat history, request history, telemetry rows, and logs.',
  Request: 'Captured request-history identifier associated with this chat turn.',
  Collection: 'Vector collection used for retrieval by this assistant turn.'
};

function valueTooltip(label, description, value) {
  return `${label}: ${description} Current value: ${value || 'N/A'}.`;
}

function stageSummary(stage, stages = []) {
  const noopReason = getStageNoopReason(stage, stages);
  const parts = [
    `${formatStageLabel(stage?.Name)} stage.`,
    stage?.Kind ? `Kind: ${stage.Kind}.` : null,
    stage?.EndpointId ? `Endpoint: ${stage.EndpointId}.` : null,
    stage?.Provider ? `Provider: ${stage.Provider}.` : null,
    stage?.Model ? `Model: ${stage.Model}.` : null,
    noopReason,
    `Duration: ${formatMs(getStageDuration(stage))}.`
  ];
  return parts.filter(Boolean).join(' ');
}

function IdentifierItem({ label, id }) {
  if (!id) return null;
  return (
    <div className="history-id-item">
      <Tooltip className="history-id-label" text={identifierTooltips[label]}>{label}</Tooltip>
      <Tooltip text={`${identifierTooltips[label]} Value: ${id}.`}>
        <CopyableId id={id} />
      </Tooltip>
    </div>
  );
}

function Metric({ label, tooltip, value, valueClassName = '' }) {
  return (
    <div className="history-metric">
      <Tooltip className="history-metric-label" text={tooltip}>{label}</Tooltip>
      <Tooltip
        className={['history-metric-value', valueClassName].filter(Boolean).join(' ')}
        text={valueTooltip(label, tooltip, value)}
      >
        {value}
      </Tooltip>
    </div>
  );
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

function getStageDuration(stage) {
  return Number(stage?.DurationMs || 0);
}

function getClientTiming(stage, key) {
  return Number(stage?.ClientTimings?.[key] || 0);
}

function getProviderMetric(stage, key) {
  return Number(stage?.ProviderMetrics?.[key] || 0);
}

function getStageMetadata(stage, key) {
  const metadata = stage?.Metadata || {};
  return Object.prototype.hasOwnProperty.call(metadata, key) ? metadata[key] : undefined;
}

function hasEndpointDetails(stage) {
  return Boolean(stage?.EndpointId || stage?.EndpointName || stage?.Provider || stage?.Model);
}

function getStageNoopReason(stage, stages) {
  if (!stage || getStageDuration(stage) > 0 || hasEndpointDetails(stage)) return null;

  const gateStage = stages?.find((candidate) => candidate?.Name === 'retrieval_gate');
  const gateDecision = String(getStageMetadata(gateStage, 'decision') || '').toUpperCase();

  if (stage.Name === 'query_rewrite') {
    if (gateDecision === 'SKIP') return 'Not run: retrieval gate returned SKIP, so query rewrite was bypassed.';
    return 'Not run: no query rewrite model call was made for this turn.';
  }

  if (stage.Name === 'retrieval') {
    if (gateDecision === 'SKIP') return 'Not run: retrieval gate returned SKIP.';
    return 'Not run: no retrieval search was performed for this turn.';
  }

  if (stage.Name === 'rerank') {
    const chunksInput = Number(getStageMetadata(stage, 'chunks_input') || 0);
    if (chunksInput < 1) return 'Not run: no retrieved chunks were available to rerank.';
    return 'Not run: no rerank model call was made for this turn.';
  }

  return null;
}

function TimingBar({ label, tooltip, durationMs, totalMs, color }) {
  const pct = totalMs > 0 && durationMs > 0 ? Math.max(1, (durationMs / totalMs) * 100) : 0;
  const formattedDuration = formatMs(durationMs);
  const share = totalMs > 0 && durationMs > 0 ? `${Math.min(100, (durationMs / totalMs) * 100).toFixed(1)}% of measured stage time.` : 'No measurable share for this request.';
  return (
    <Tooltip
      as="div"
      className="history-timing-row"
      text={`${label}: ${tooltip}. Current value: ${formattedDuration}. ${share}`}
    >
      <div className="history-timing-label">
        {label}
      </div>
      <div className="history-timing-bar-track">
        {pct > 0 && (
          <div
            className="history-timing-bar-fill"
            style={{ width: `${Math.min(pct, 100)}%`, background: color }}
          />
        )}
      </div>
      <div className="history-timing-value">{formattedDuration}</div>
    </Tooltip>
  );
}

function StageTable({ stages }) {
  if (!stages || stages.length < 1) return null;

  return (
    <div className="history-stage-table-wrap">
      <table className="history-stage-table">
        <thead>
          <tr>
            {Object.keys(stageColumnTooltips).map((column) => (
              <th key={column}>
                <Tooltip text={stageColumnTooltips[column]}>{column}</Tooltip>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {stages.map((stage, idx) => {
            const tokens = stage.Tokens || {};
            const inputTokens = tokens.Input ?? tokens.PromptEvalCount;
            const outputTokens = tokens.Output ?? tokens.EvalCount;
            const noopReason = getStageNoopReason(stage, stages);
            const stageText = stageSummary(stage, stages);
            const endpointText = stage.EndpointId || stage.EndpointName || (noopReason ? 'Not run' : '-');
            const providerText = [stage.Provider, stage.Model].filter(Boolean).join(' / ');
            const generationMs = getClientTiming(stage, 'FirstTokenToLastTokenMs') || getProviderMetric(stage, 'GenerationMs');
            const tokenText = inputTokens || outputTokens ? `${formatNumber(inputTokens)} / ${formatNumber(outputTokens)}` : 'N/A';
            return (
              <tr key={`${stage.Name || 'stage'}-${idx}`}>
                <td>
                  <Tooltip as="div" className="history-stage-name" text={stageText}>{formatStageLabel(stage.Name)}</Tooltip>
                  <Tooltip as="div" className="history-stage-kind" text={`Stage kind. ${stageText}`}>{stage.Kind || 'stage'}</Tooltip>
                </td>
                <td>
                  <Tooltip
                    as="div"
                    className={['history-stage-endpoint', noopReason ? 'history-stage-muted' : ''].filter(Boolean).join(' ')}
                    text={`Endpoint used for this stage. Value: ${endpointText}. ${noopReason || (providerText ? `Provider/model: ${providerText}.` : 'No provider/model was recorded for this stage.')}`}
                  >
                    {endpointText}
                  </Tooltip>
                  {(stage.Provider || stage.Model) && (
                    <Tooltip as="div" className="history-stage-kind" text="Provider and model reported for this stage.">
                      {providerText}
                    </Tooltip>
                  )}
                </td>
                <td><Tooltip text={valueTooltip('Duration', stageColumnTooltips.Duration, formatMs(getStageDuration(stage)))}>{formatMs(getStageDuration(stage))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Queue', stageColumnTooltips.Queue, formatMs(getClientTiming(stage, 'EndpointLimiterWaitMs')))}>{formatMs(getClientTiming(stage, 'EndpointLimiterWaitMs'))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Headers', stageColumnTooltips.Headers, formatMs(getClientTiming(stage, 'RequestToHeadersMs')))}>{formatMs(getClientTiming(stage, 'RequestToHeadersMs'))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('First Token', stageColumnTooltips['First Token'], formatMs(getClientTiming(stage, 'HeadersToFirstTokenMs')))}>{formatMs(getClientTiming(stage, 'HeadersToFirstTokenMs'))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Generation', stageColumnTooltips.Generation, formatMs(generationMs))}>{formatMs(generationMs)}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Provider Load', stageColumnTooltips['Provider Load'], formatMs(getProviderMetric(stage, 'LoadMs')))}>{formatMs(getProviderMetric(stage, 'LoadMs'))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Prompt Eval', stageColumnTooltips['Prompt Eval'], formatMs(getProviderMetric(stage, 'PromptEvalMs')))}>{formatMs(getProviderMetric(stage, 'PromptEvalMs'))}</Tooltip></td>
                <td><Tooltip text={valueTooltip('Tokens', stageColumnTooltips.Tokens, tokenText)}>{tokenText}</Tooltip></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function HistoryViewModal({ history, onClose }) {
  if (!history) return null;

  const { serverUrl, credential } = useAuth();
  const [retrievalOpen, setRetrievalOpen] = useState(false);
  const [messagesOpen, setMessagesOpen] = useState(true);
  const [queryRewriteOpen, setQueryRewriteOpen] = useState(false);
  const [docNames, setDocNames] = useState({});

  // Parse retrieval chunks once
  const retrievalChunks = useMemo(() => {
    if (!history.RetrievalContext) return null;
    try {
      const parsed = JSON.parse(history.RetrievalContext);
      return Array.isArray(parsed) ? parsed : null;
    } catch { return null; }
  }, [history.RetrievalContext]);

  // Compute retrieval summary stats
  const retrievalStats = useMemo(() => {
    if (!retrievalChunks) return null;
    const docIds = new Set();
    let totalChunks = 0;
    for (const chunk of retrievalChunks) {
      totalChunks++;
      if (chunk.document_id) docIds.add(chunk.document_id);
      if (chunk.neighbors) {
        totalChunks += chunk.neighbors.length;
      }
    }
    return { uniqueDocIds: [...docIds], totalChunks };
  }, [retrievalChunks]);

  // Fetch document names for unique document IDs
  useEffect(() => {
    if (!retrievalStats || retrievalStats.uniqueDocIds.length === 0) return;
    const api = new ApiClient(serverUrl, credential?.BearerToken);
    let cancelled = false;
    (async () => {
      const names = {};
      await Promise.all(retrievalStats.uniqueDocIds.map(async (id) => {
        try {
          const doc = await api.getDocument(id);
          if (!cancelled && doc) names[id] = doc.OriginalFilename || doc.Name || null;
        } catch { /* document may have been deleted */ }
      }));
      if (!cancelled) setDocNames(names);
    })();
    return () => { cancelled = true; };
  }, [retrievalStats, serverUrl, credential]);

  const performanceTelemetry = useMemo(() => parsePerformanceTelemetry(history), [history]);
  const performanceStages = performanceTelemetry?.Stages || [];

  const legacyStages = useMemo(() => ([
    { Name: 'retrieval_gate', Kind: 'inference', DurationMs: history.RetrievalGateDurationMs, Metadata: { decision: history.RetrievalGateDecision } },
    { Name: 'query_rewrite', Kind: 'inference', DurationMs: history.QueryRewriteDurationMs },
    { Name: 'retrieval', Kind: 'retrieval', DurationMs: history.RetrievalDurationMs },
    { Name: 'rerank', Kind: 'inference', DurationMs: history.RerankDurationMs, Metadata: { chunks_input: history.RerankInputCount, chunks_output: history.RerankOutputCount } },
    { Name: 'endpoint_resolution', Kind: 'network', DurationMs: history.EndpointResolutionDurationMs },
    { Name: 'context_compaction', Kind: 'inference', DurationMs: history.CompactionDurationMs },
    {
      Name: 'final_inference',
      Kind: 'inference',
      DurationMs: history.TimeToLastTokenMs,
      ClientTimings: {
        RequestToHeadersMs: history.InferenceConnectionDurationMs,
        HeadersToFirstTokenMs: history.TimeToFirstTokenMs > 0 && history.InferenceConnectionDurationMs > 0
          ? Math.max(0, history.TimeToFirstTokenMs - history.InferenceConnectionDurationMs)
          : 0,
        FirstTokenToLastTokenMs: history.TimeToLastTokenMs > 0 && history.TimeToFirstTokenMs > 0
          ? Math.max(0, history.TimeToLastTokenMs - history.TimeToFirstTokenMs)
          : 0,
        TotalMs: history.TimeToLastTokenMs
      },
      Tokens: {
        Input: history.PromptTokens,
        Output: history.CompletionTokens,
        Total: (history.PromptTokens || 0) + (history.CompletionTokens || 0)
      }
    }
  ]).filter((stage) => getStageDuration(stage) > 0 || Object.keys(stage.Metadata || {}).length > 0), [history]);

  const displayStages = performanceStages.length > 0 ? performanceStages : legacyStages;
  const finalInferenceStage = displayStages.find((stage) => stage.Name === 'final_inference')
    || displayStages.find((stage) => stage.Name === 'streaming_inference')
    || displayStages.find((stage) => stage.Kind === 'inference' && getStageDuration(stage) > 0);

  const totalPipelineMs = displayStages.reduce((sum, stage) => sum + getStageDuration(stage), 0);

  // Infer prompt processing time: TTFT minus connection time (if both available)
  const promptProcessingMs =
    history.TimeToFirstTokenMs > 0 && history.InferenceConnectionDurationMs > 0
      ? Math.max(0, history.TimeToFirstTokenMs - history.InferenceConnectionDurationMs)
      : 0;

  // Token generation: TTLT minus TTFT
  const tokenGenMs =
    history.TimeToLastTokenMs > 0 && history.TimeToFirstTokenMs > 0
      ? Math.max(0, history.TimeToLastTokenMs - history.TimeToFirstTokenMs)
      : 0;

  // Use backend-stored TPS if available, otherwise compute from raw fields
  const overallTps = history.TokensPerSecondOverall > 0
    ? history.TokensPerSecondOverall
    : (history.CompletionTokens > 0 && history.TimeToLastTokenMs > 0
        ? (history.CompletionTokens / (history.TimeToLastTokenMs / 1000))
        : 0);

  const generationTps = history.TokensPerSecondGeneration > 0
    ? history.TokensPerSecondGeneration
    : (history.CompletionTokens > 0 && tokenGenMs > 0
        ? (history.CompletionTokens / (tokenGenMs / 1000))
        : 0);

  return (
    <Modal title="History Details" onClose={onClose} fullscreen footer={
      <button className="btn btn-secondary" onClick={onClose}>Close</button>
    }>
      {/* === Identifiers row === */}
      <div className="history-ids-row">
        <IdentifierItem label="History" id={history.Id} />
        <IdentifierItem label="Thread" id={history.ThreadId} />
        <IdentifierItem label="Assistant" id={history.AssistantId} />
        <IdentifierItem label="Trace" id={history.TraceId} />
        <IdentifierItem label="Request" id={history.RequestHistoryId} />
        <IdentifierItem label="Collection" id={history.CollectionId} />
      </div>

      {/* === Metadata Filters === */}
      {history.MetadataFilter && (() => {
        let filter = null;
        try { filter = JSON.parse(history.MetadataFilter); } catch {}
        if (!filter) return null;
        return (
          <div className="history-section" style={{ marginTop: '1rem' }}>
            <h4 style={{ margin: '0 0 0.5rem 0', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              <Tooltip text="Metadata constraints that were applied to retrieval for this chat turn.">Metadata Filters Applied</Tooltip>
            </h4>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
              {filter.required_labels && filter.required_labels.length > 0 && (
                <div>
                  <Tooltip text="Documents had to include these labels to be eligible for retrieval.">
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Required Labels: </span>
                  </Tooltip>
                  {filter.required_labels.map((l, i) => (
                    <Tooltip key={i} text={`Required label applied during retrieval: ${l}.`}>
                      <span style={{ display: 'inline-block', padding: '0.1rem 0.4rem', margin: '0.1rem', fontSize: '0.75rem', background: 'var(--bg-tertiary, #e8f5e9)', borderRadius: '4px' }}>{l}</span>
                    </Tooltip>
                  ))}
                </div>
              )}
              {filter.excluded_labels && filter.excluded_labels.length > 0 && (
                <div>
                  <Tooltip text="Documents with these labels were excluded from retrieval.">
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Excluded Labels: </span>
                  </Tooltip>
                  {filter.excluded_labels.map((l, i) => (
                    <Tooltip key={i} text={`Excluded label applied during retrieval: ${l}.`}>
                      <span style={{ display: 'inline-block', padding: '0.1rem 0.4rem', margin: '0.1rem', fontSize: '0.75rem', background: 'var(--bg-tertiary, #ffebee)', borderRadius: '4px', textDecoration: 'line-through' }}>{l}</span>
                    </Tooltip>
                  ))}
                </div>
              )}
              {filter.required_tags && filter.required_tags.length > 0 && (
                <div style={{ width: '100%' }}>
                  <Tooltip text="Tag predicates that documents had to satisfy to be eligible for retrieval.">
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Required Tags:</span>
                  </Tooltip>
                  <div style={{ marginTop: '0.25rem', fontSize: '0.75rem' }}>
                    {filter.required_tags.map((t, i) => (
                      <Tooltip key={i} as="div" text={`Required tag filter: ${t.key} ${t.condition} ${t.value || ''}.`} style={{ fontFamily: 'monospace' }}>
                        {t.key} {t.condition} {t.value || ''}
                      </Tooltip>
                    ))}
                  </div>
                </div>
              )}
              {filter.excluded_tags && filter.excluded_tags.length > 0 && (
                <div style={{ width: '100%' }}>
                  <Tooltip text="Tag predicates that caused documents to be excluded from retrieval.">
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Excluded Tags:</span>
                  </Tooltip>
                  <div style={{ marginTop: '0.25rem', fontSize: '0.75rem' }}>
                    {filter.excluded_tags.map((t, i) => (
                      <Tooltip key={i} as="div" text={`Excluded tag filter: ${t.key} ${t.condition} ${t.value || ''}.`} style={{ fontFamily: 'monospace', textDecoration: 'line-through' }}>
                        {t.key} {t.condition} {t.value || ''}
                      </Tooltip>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        );
      })()}

      <AssistantAttachedDocumentsSection history={history} />

      {/* === Performance Timing === */}
      <div className="history-section">
        <div className="history-section-header">
          <Tooltip text="End-to-end timing breakdown for this chat turn">Performance Timing</Tooltip>
          {performanceTelemetry && (
            <Tooltip className="history-section-badge" text="This history entry includes the v0.12 provider-agnostic telemetry payload.">
              Telemetry v{performanceTelemetry.SchemaVersion || 1}
            </Tooltip>
          )}
        </div>
        <div className="history-timing-container">
          <TimingBar
            label="Retrieval Gate"
            tooltip={'LLM-based retrieval gate - classifies whether retrieval is needed or can be skipped' + (history.RetrievalGateDecision ? '. Decision: ' + history.RetrievalGateDecision : '')}
            durationMs={history.RetrievalGateDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-gate, #a9e34b)"
          />
          <TimingBar
            label="Query Rewrite"
            tooltip="LLM-based query rewrite - rewrites the user prompt into multiple semantically varied queries to improve retrieval recall"
            durationMs={history.QueryRewriteDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-rewrite, #74c0fc)"
          />
          <TimingBar
            label="Retrieval"
            tooltip="Time spent searching the document collection for relevant context"
            durationMs={history.RetrievalDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-retrieval, #4dabf7)"
          />
          <TimingBar
            label="Re-Ranking"
            tooltip="LLM-based re-ranking - scores each retrieved chunk for relevance and filters low-scoring chunks"
            durationMs={history.RerankDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-rerank, #ffa94d)"
          />
          <TimingBar
            label="Endpoint Resolution"
            tooltip="HTTP request to Partio to resolve the configured inference endpoint details"
            durationMs={history.EndpointResolutionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-endpoint, #69db7c)"
          />
          <TimingBar
            label="Compaction"
            tooltip="Conversation history compaction when context window is exceeded (may involve an LLM call)"
            durationMs={history.CompactionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-compaction, #ffd43b)"
          />
          <TimingBar
            label="Request to Headers"
            tooltip="Time from HTTP request sent to response headers received - includes network latency and model loading"
            durationMs={history.InferenceConnectionDurationMs}
            totalMs={totalPipelineMs}
            color="var(--timing-connection, #ff922b)"
          />
          <TimingBar
            label="Prompt Processing"
            tooltip="Estimated time the model spent processing the prompt before generating the first token (TTFT minus connection time)"
            durationMs={promptProcessingMs}
            totalMs={totalPipelineMs}
            color="var(--timing-prompt, #da77f2)"
          />
          <TimingBar
            label="Token Generation"
            tooltip="Time from the first token to the last token - the streaming generation phase"
            durationMs={tokenGenMs}
            totalMs={totalPipelineMs}
            color="var(--timing-generation, #ff6b6b)"
          />
        </div>

        {/* Summary metrics row */}
        <div className="history-metrics-row">
          <Metric label="TTFT" tooltip="Time to first token, measured from when the prompt was sent to when the first token was received." value={formatMs(history.TimeToFirstTokenMs)} />
          <Metric label="TTLT" tooltip="Time to last token, measured from when the prompt was sent to when the final token was received." value={formatMs(history.TimeToLastTokenMs)} />
          <Metric label="Prompt Tokens" tooltip="Estimated number of tokens in the assembled prompt, including system instructions, RAG context, and conversation." value={history.PromptTokens > 0 ? `~${history.PromptTokens.toLocaleString()}` : 'N/A'} />
          <Metric label="Completion Tokens" tooltip="Estimated number of tokens in the assistant response." value={history.CompletionTokens > 0 ? `~${history.CompletionTokens.toLocaleString()}` : 'N/A'} />
          <Metric label="TPS (Overall)" tooltip="Completion tokens per second using total time from prompt sent to final token." value={formatTps(overallTps)} />
          <Metric label="TPS (Generation)" tooltip="Completion tokens per second using only the first-token-to-last-token generation window." value={formatTps(generationTps)} />
          <Metric label="Prompt Sent" tooltip="Timestamp when AssistantHub sent the assembled prompt to the inference endpoint." value={formatTimestamp(history.PromptSentUtc)} valueClassName="history-metric-timestamp" />
        </div>

        <div className="history-stage-summary">
          <div className="history-stage-summary-heading">
            <Tooltip text="Detailed per-stage telemetry used to explain where this assistant turn spent time.">Stage Details</Tooltip>
            {finalInferenceStage?.Provider && (
              <Tooltip className="history-section-badge" text="Provider and model used by the final inference stage.">
                {finalInferenceStage.Provider}{finalInferenceStage.Model ? ` / ${finalInferenceStage.Model}` : ''}
              </Tooltip>
            )}
          </div>
          <StageTable stages={displayStages} />
        </div>
      </div>

      <AssistantToolCallTraceSection
        assistantId={history.AssistantId}
        chatHistoryId={history.Id}
        traceId={history.TraceId}
      />

      {/* === Messages === */}
      <div className="history-section">
        <div
          className="history-section-header history-section-toggle"
          onClick={() => setMessagesOpen(!messagesOpen)}
        >
          <Tooltip text="User message and assistant response for this conversation turn">Messages</Tooltip>
          <Tooltip className="history-toggle-icon" text={messagesOpen ? 'Collapse the message section.' : 'Expand the message section.'}>{messagesOpen ? '\u25BC' : '\u25B6'}</Tooltip>
        </div>
        {messagesOpen && (
          <div className="history-messages-grid">
            <div className="history-message-panel">
              <div className="history-message-heading">
                <Tooltip text="The user prompt that started this assistant turn.">User Message</Tooltip>
                <Tooltip className="history-message-ts" text="UTC timestamp when AssistantHub received the user message.">
                  {formatTimestamp(history.UserMessageUtc)}
                </Tooltip>
              </div>
              <Tooltip as="div" className="json-view history-message-body" text="Raw user message content recorded for this assistant history item.">
                {history.UserMessage || '(none)'}
              </Tooltip>
            </div>
            <div className="history-message-panel">
              <div className="history-message-heading">
                <Tooltip text="The final assistant response returned to the user.">Assistant Response</Tooltip>
              </div>
              <Tooltip as="div" className="json-view history-message-body" text="Raw assistant response content recorded for this assistant history item.">
                {history.AssistantResponse || '(none)'}
              </Tooltip>
            </div>
          </div>
        )}
      </div>

      {/* === Query Rewrite === */}
      {history.QueryRewriteResult && (
        <div className="history-section">
          <div
            className="history-section-header history-section-toggle"
            onClick={() => setQueryRewriteOpen(!queryRewriteOpen)}
          >
            <Tooltip text="LLM-based query rewrite - the original prompt was rewritten into multiple queries for broader retrieval">Query Rewrite</Tooltip>
            <Tooltip className="history-toggle-icon" text={queryRewriteOpen ? 'Collapse the query rewrite details.' : 'Expand the query rewrite details.'}>{queryRewriteOpen ? '\u25BC' : '\u25B6'}</Tooltip>
            <Tooltip className="history-section-badge" text="Elapsed time spent generating query variants before retrieval.">
              {formatMs(history.QueryRewriteDurationMs)}
            </Tooltip>
          </div>
          {queryRewriteOpen && (
            <div className="history-retrieval-body">
              {history.QueryRewriteResult.split('\n').filter(q => q.trim()).map((query, idx) => (
                <div key={idx} className="history-chunk-card">
                  <div className="history-chunk-header">
                    <Tooltip className="history-chunk-num" text={idx === 0 ? 'Original query text used as part of retrieval.' : `Query rewrite variant ${idx} generated to improve retrieval recall.`}>
                      {idx === 0 ? 'Original' : `Variant ${idx}`}
                    </Tooltip>
                  </div>
                  <Tooltip as="div" className="json-view history-chunk-content" text={idx === 0 ? 'Original retrieval query text.' : `Generated retrieval query variant ${idx}.`}>
                    {query.trim()}
                  </Tooltip>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* === Retrieval Context === */}
      <div className="history-section">
        <div
          className="history-section-header history-section-toggle"
          onClick={() => setRetrievalOpen(!retrievalOpen)}
        >
          <Tooltip text="Document retrieval phase - context fetched from the collection for RAG">Retrieval Context</Tooltip>
          <Tooltip className="history-toggle-icon" text={retrievalOpen ? 'Collapse the retrieval context details.' : 'Expand the retrieval context details.'}>{retrievalOpen ? '\u25BC' : '\u25B6'}</Tooltip>
          <Tooltip className="history-section-badge" text="Elapsed time spent retrieving context from the configured collection.">
            {formatMs(history.RetrievalDurationMs)}
          </Tooltip>
          {history.RerankInputCount > 0 && (
            <Tooltip className="history-section-badge" style={{ background: 'var(--timing-rerank, #ffa94d)', color: '#000' }} text="Number of chunks sent to reranking, number retained after reranking, and elapsed rerank time.">
              Re-ranked: {history.RerankInputCount} to {history.RerankOutputCount} chunks in {formatMs(history.RerankDurationMs)}
            </Tooltip>
          )}
          {history.RetrievalStartUtc && (
            <Tooltip className="history-section-meta" text="UTC timestamp when retrieval started.">
              started {formatTimestamp(history.RetrievalStartUtc)}
            </Tooltip>
          )}
        </div>
        {retrievalOpen && (
          <div className="history-retrieval-body">
            {!history.RetrievalContext ? (
              <Tooltip as="div" className="json-view" text="No retrieval context was stored for this assistant turn.">
                (no context retrieved)
              </Tooltip>
            ) : !retrievalChunks ? (
              <Tooltip as="div" className="json-view" style={{ maxHeight: '300px' }} text="Raw retrieval context payload. It could not be parsed as the structured chunk array.">
                {history.RetrievalContext}
              </Tooltip>
            ) : (
              <>
                {/* Retrieval summary stats */}
                {retrievalStats && (
                  <div className="history-retrieval-summary">
                    <div className="history-retrieval-summary-stats">
                      <Metric label="Documents" tooltip="Number of unique source documents represented in the retrieved context." value={retrievalStats.uniqueDocIds.length.toLocaleString()} />
                      <Metric label="Chunks" tooltip="Number of retrieved chunks and neighbor chunks represented in the context payload." value={retrievalStats.totalChunks.toLocaleString()} />
                    </div>
                    {retrievalStats.uniqueDocIds.length > 0 && (
                      <div className="history-retrieval-doc-list">
                        <Tooltip as="div" className="history-retrieval-doc-list-label" text="Documents that supplied retrieved context for this assistant response.">
                          Source Documents
                        </Tooltip>
                        <ul className="history-retrieval-doc-items">
                          {retrievalStats.uniqueDocIds.map((docId) => (
                            <li key={docId} className="history-retrieval-doc-item">
                              <Tooltip text={`Source document identifier used during retrieval: ${docId}.`}>
                                <CopyableId id={docId} />
                              </Tooltip>
                              {docNames[docId] && (
                                <Tooltip className="history-retrieval-doc-name" text="Original filename or display name for this source document.">
                                  {docNames[docId]}
                                </Tooltip>
                              )}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}
                {/* Auto-populated citation notice */}
                {(() => {
                  const hasBracketCitations = history.AssistantResponse && /\[\d+\]/.test(history.AssistantResponse);
                  return retrievalChunks.length > 0 && !hasBracketCitations ? (
                    <Tooltip as="div" className="history-auto-populated-tag" text="The assistant response did not include inline bracket citations, so the dashboard associated citations from retrieved context automatically.">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>
                      </svg>
                      Citations auto-populated - model did not produce inline [N] references
                    </Tooltip>
                  ) : null;
                })()}
                {/* Individual chunks */}
                {retrievalChunks.map((chunk, idx) => (
                  <div key={idx} className="history-chunk-card">
                    <div className="history-chunk-header">
                      <Tooltip className="history-chunk-num" text={`Retrieved context chunk ${idx + 1}.`}>
                        Chunk {idx + 1}
                      </Tooltip>
                      {chunk.score != null && (
                        <Tooltip className="history-chunk-score" text="Vector retrieval similarity score reported for this chunk. Higher usually means a closer semantic match.">
                          Score: <strong>{chunk.score.toFixed(4)}</strong>
                        </Tooltip>
                      )}
                      {chunk.rerank_score != null && (
                        <Tooltip className="history-chunk-score" text="LLM reranker relevance score for this chunk on a 1 to 10 scale.">
                          Relevance: <strong>{chunk.rerank_score.toFixed(1)}/10</strong>
                        </Tooltip>
                      )}
                      {chunk.document_id && (
                        <Tooltip className="history-chunk-source" text={`Source document for this retrieved chunk: ${chunk.document_id}.`}>
                          Source: <CopyableId id={chunk.document_id} />
                          {docNames[chunk.document_id] && (
                            <span className="history-retrieval-doc-name">{docNames[chunk.document_id]}</span>
                          )}
                        </Tooltip>
                      )}
                    </div>
                    <Tooltip as="div" className="json-view history-chunk-content" text="Text content from this retrieved chunk that was available to the assistant as context.">
                      {chunk.content || '(empty)'}
                    </Tooltip>
                  </div>
                ))}
              </>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}

export default HistoryViewModal;
