import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';
import ProviderConnectionFields from '../ProviderConnectionFields';
import ProviderPicker from '../ProviderPicker';
import { applyProviderChange, authFromEndpoint, providerSettingsToForm, hasEndpointOverride, validateConnection, buildConnectionPayload, apiKeyRequired, getProvider, mergeRevealedSecrets, hasMaskedSecrets, usesConnectivityProbe, probeDescription, apiKeyLabel } from '../../utils/providers';
import {
  API_FORMAT_OPTIONS,
  HEALTH_CHECK_METHOD_OPTIONS,
  getApiFormatDefaults,
  getEndpointProviderFields,
  resolveProviderEndpoint,
  getDefaultEndpoint,
  getDefaultHealthCheckUrl,
  getDefaultModel,
  getDefaultToolCallingApiFormat,
  getHealthCheckUrlForEndpointChange,
  isDefaultHealthCheckUrl
} from '../../utils/endpointDefaults';

// Empty means the server default (0 clears a stored value).
const toTimeoutValue = (value) => {
  const parsed = parseInt(value);
  return Number.isNaN(parsed) || parsed <= 0 ? 0 : Math.min(3600000, Math.max(1000, parsed));
};

function isAbsoluteUrl(url) {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
}

const TOOL_CALLING_LABEL = 'assistanthub:tool-calling';
const TOOL_TAG_SUPPORTS = 'AssistantHub.SupportsToolCalling';
const TOOL_TAG_FORMAT = 'AssistantHub.ToolCallingApiFormat';
const TOOL_TAG_PARALLEL = 'AssistantHub.SupportsParallelToolCalls';
const TOOL_TAG_STREAMING = 'AssistantHub.SupportsStreamingToolCalls';

function getSourceField(source, ...keys) {
  if (!source) return undefined;
  for (const key of keys) {
    if (source[key] !== undefined && source[key] !== null) return source[key];
  }
  return undefined;
}

function coerceBoolean(value) {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'number') return value !== 0;
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase();
    if (normalized === 'true' || normalized === 'yes' || normalized === '1') return true;
    if (normalized === 'false' || normalized === 'no' || normalized === '0' || normalized === '') return false;
  }
  return !!value;
}

function getSourceBoolean(source, ...keys) {
  return coerceBoolean(getSourceField(source, ...keys));
}

function getSourceText(source, ...keys) {
  const value = getSourceField(source, ...keys);
  return value === undefined || value === null ? '' : String(value);
}

function getSourceLabels(source) {
  const value = getSourceField(source, 'Labels', 'labels');
  return Array.isArray(value) ? value.filter(label => typeof label === 'string' && label.trim()).map(label => label.trim()) : [];
}

function getSourceTags(source) {
  const value = getSourceField(source, 'Tags', 'tags');
  return value && typeof value === 'object' && !Array.isArray(value) ? { ...value } : {};
}

function getTagValue(tags, key) {
  if (!tags || !key) return undefined;
  const match = Object.keys(tags).find(candidate => candidate.toLowerCase() === key.toLowerCase());
  return match ? tags[match] : undefined;
}

function getSourceToolBoolean(source, keys, tagKey, labels) {
  const direct = getSourceField(source, ...keys);
  if (direct !== undefined && direct !== null) return coerceBoolean(direct);
  const tagValue = getTagValue(getSourceTags(source), tagKey);
  if (tagValue !== undefined && tagValue !== null) return coerceBoolean(tagValue);
  return Array.isArray(labels) && labels.some(label => label.toLowerCase() === TOOL_CALLING_LABEL.toLowerCase());
}

function InferenceEndpointFormModal({ endpoint, initialData, onSave, onClose, onRevealEndpoint }) {
  const isEdit = !!endpoint;
  const source = endpoint || initialData;
  const initialApiFormat = getSourceText(source, 'ApiFormat', 'apiFormat') || 'Ollama';
  const initialEndpoint = resolveProviderEndpoint(initialApiFormat, getSourceText(source, 'Endpoint', 'endpoint'));
  const initialLabels = getSourceLabels(source);
  const initialTags = getSourceTags(source);
  const initialDefaults = getApiFormatDefaults(initialApiFormat, initialEndpoint || getDefaultEndpoint(initialApiFormat));

  const initialProviderSettings = providerSettingsToForm(initialApiFormat, source?.ProviderSettings);
  const [form, setForm] = useState({
    Name: getSourceText(source, 'Name', 'name'),
    Model: getSourceText(source, 'Model', 'model') || getDefaultModel(initialApiFormat, 'inference'),
    Endpoint: initialEndpoint || initialDefaults.Endpoint,
    ApiFormat: initialApiFormat,
    ProviderSettings: initialProviderSettings,
    Auth: authFromEndpoint(source || {}),
    EndpointOverride: hasEndpointOverride(initialApiFormat, source?.Endpoint, initialProviderSettings),
    ApiKey: getSourceText(source, 'ApiKey', 'apiKey'),
    Active: getSourceField(source, 'Active', 'active') !== undefined ? getSourceBoolean(source, 'Active', 'active') : true,
    MaxConcurrentRequests: getSourceField(source, 'MaxConcurrentRequests', 'maxConcurrentRequests') !== undefined ? getSourceField(source, 'MaxConcurrentRequests', 'maxConcurrentRequests') : 2,
    MaxQueueDepth: getSourceField(source, 'MaxQueueDepth', 'maxQueueDepth') !== undefined ? getSourceField(source, 'MaxQueueDepth', 'maxQueueDepth') : 16,
    ContextSize: getSourceField(source, 'ContextSize', 'contextSize') !== undefined ? getSourceField(source, 'ContextSize', 'contextSize') : 0,
    MaximumTimeoutMs: getSourceField(source, 'MaximumTimeoutMs', 'maximumTimeoutMs') !== undefined ? getSourceField(source, 'MaximumTimeoutMs', 'maximumTimeoutMs') : initialDefaults.MaximumTimeoutMs,
    RequestTimeoutMs: getSourceField(source, 'RequestTimeoutMs', 'requestTimeoutMs') ?? '',
    UtilityTimeoutMs: getSourceField(source, 'UtilityTimeoutMs', 'utilityTimeoutMs') ?? '',
    SupportsToolCalling: getSourceToolBoolean(source, ['SupportsToolCalling', 'supportsToolCalling'], TOOL_TAG_SUPPORTS, initialLabels),
    ToolCallingApiFormat: getSourceText(source, 'ToolCallingApiFormat', 'toolCallingApiFormat') || getTagValue(initialTags, TOOL_TAG_FORMAT) || getDefaultToolCallingApiFormat(initialApiFormat),
    SupportsParallelToolCalls: getSourceToolBoolean(source, ['SupportsParallelToolCalls', 'supportsParallelToolCalls'], TOOL_TAG_PARALLEL, []),
    SupportsStreamingToolCalls: getSourceToolBoolean(source, ['SupportsStreamingToolCalls', 'supportsStreamingToolCalls'], TOOL_TAG_STREAMING, []),
    Labels: initialLabels,
    Tags: initialTags,
    HealthCheckEnabled: getSourceField(source, 'HealthCheckEnabled', 'healthCheckEnabled') !== undefined ? getSourceBoolean(source, 'HealthCheckEnabled', 'healthCheckEnabled') : initialDefaults.HealthCheckEnabled,
    HealthCheckUrl: getSourceText(source, 'HealthCheckUrl', 'healthCheckUrl') || initialDefaults.HealthCheckUrl,
    HealthCheckMethod: getSourceText(source, 'HealthCheckMethod', 'healthCheckMethod') || initialDefaults.HealthCheckMethod,
    HealthCheckIntervalMs: getSourceField(source, 'HealthCheckIntervalMs', 'healthCheckIntervalMs') !== undefined ? getSourceField(source, 'HealthCheckIntervalMs', 'healthCheckIntervalMs') : initialDefaults.HealthCheckIntervalMs,
    HealthCheckTimeoutMs: getSourceField(source, 'HealthCheckTimeoutMs', 'healthCheckTimeoutMs') !== undefined ? getSourceField(source, 'HealthCheckTimeoutMs', 'healthCheckTimeoutMs') : initialDefaults.HealthCheckTimeoutMs,
    HealthCheckExpectedStatusCode: getSourceField(source, 'HealthCheckExpectedStatusCode', 'healthCheckExpectedStatusCode') !== undefined ? getSourceField(source, 'HealthCheckExpectedStatusCode', 'healthCheckExpectedStatusCode') : initialDefaults.HealthCheckExpectedStatusCode,
    HealthyThreshold: getSourceField(source, 'HealthyThreshold', 'healthyThreshold') !== undefined ? getSourceField(source, 'HealthyThreshold', 'healthyThreshold') : initialDefaults.HealthyThreshold,
    UnhealthyThreshold: getSourceField(source, 'UnhealthyThreshold', 'unhealthyThreshold') !== undefined ? getSourceField(source, 'UnhealthyThreshold', 'unhealthyThreshold') : initialDefaults.UnhealthyThreshold,
    HealthCheckUseAuth: getSourceField(source, 'HealthCheckUseAuth', 'healthCheckUseAuth') !== undefined ? getSourceBoolean(source, 'HealthCheckUseAuth', 'healthCheckUseAuth') : initialDefaults.HealthCheckUseAuth
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [connectionErrors, setConnectionErrors] = useState({});
  const [revealing, setRevealing] = useState(false);
  const credentialLabel = apiKeyLabel(form.ApiFormat, form.ProviderSettings, form.Auth);
  // The health check URL auto-follows the endpoint until the user edits it manually.
  const [healthCheckUrlTouched, setHealthCheckUrlTouched] = useState(
    !isDefaultHealthCheckUrl(
      getSourceText(source, 'HealthCheckUrl', 'healthCheckUrl'),
      initialEndpoint || initialDefaults.Endpoint,
      initialApiFormat
    )
  );
  const handleChange = (field, value) => {
    setError('');
    if (field === 'ApiFormat') {
      setForm(previous => ({ ...applyProviderChange(previous, value, 'completion', healthCheckUrlTouched), ToolCallingApiFormat: getDefaultToolCallingApiFormat(value) }));
      setConnectionErrors({});
      return;
    }
    if (field === 'HealthCheckUrl') setHealthCheckUrlTouched(true);
    setForm(prev => {
      const updated = { ...prev, [field]: value };
      if (field === 'Endpoint') {
        updated.HealthCheckUrl = healthCheckUrlTouched
          ? getHealthCheckUrlForEndpointChange(prev.HealthCheckUrl, prev.Endpoint, value, prev.ApiFormat)
          : getDefaultHealthCheckUrl(value, prev.ApiFormat);
      }
      return updated;
    });
  };

  const healthCheckUrlInvalid = !usesConnectivityProbe(form.ApiFormat) && form.HealthCheckUrl && !isAbsoluteUrl(form.HealthCheckUrl);

  const handleReveal = async () => {
    if (!endpoint?.Id || !onRevealEndpoint) return;
    setRevealing(true);
    try {
      const revealed = await onRevealEndpoint(endpoint.Id);
      setForm(current => mergeRevealedSecrets(current, revealed));
    } catch (err) { setError(err.message || 'Unable to reveal endpoint credentials.'); }
    finally { setRevealing(false); }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errors = validateConnection(form, 'completion');
    setConnectionErrors(errors);
    if (Object.keys(errors).length) { setError(Object.values(errors)[0]); return; }
    setError('');
    setSaving(true);
    try {
      const data = {
        Name: form.Name,
        Model: form.Model,
        Endpoint: resolveProviderEndpoint(form.ApiFormat, form.Endpoint),
        ApiFormat: form.ApiFormat,
        ApiKey: form.ApiKey,
        Active: form.Active,
        MaxConcurrentRequests: parseInt(form.MaxConcurrentRequests) || 2,
        MaxQueueDepth: Number.isNaN(parseInt(form.MaxQueueDepth)) ? 16 : Math.max(0, parseInt(form.MaxQueueDepth)),
        ContextSize: Number.isNaN(parseInt(form.ContextSize)) ? 0 : Math.max(0, parseInt(form.ContextSize)),
        MaximumTimeoutMs: parseInt(form.MaximumTimeoutMs) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).MaximumTimeoutMs,
        RequestTimeoutMs: toTimeoutValue(form.RequestTimeoutMs),
        UtilityTimeoutMs: toTimeoutValue(form.UtilityTimeoutMs),
        SupportsToolCalling: form.SupportsToolCalling,
        ToolCallingApiFormat: form.SupportsToolCalling ? form.ToolCallingApiFormat : null,
        SupportsParallelToolCalls: form.SupportsToolCalling && form.SupportsParallelToolCalls,
        SupportsStreamingToolCalls: form.SupportsToolCalling && form.SupportsStreamingToolCalls,
        Labels: form.Labels,
        Tags: form.Tags,
        HealthCheckEnabled: form.HealthCheckEnabled,
        HealthCheckUrl: form.HealthCheckUrl,
        HealthCheckMethod: form.HealthCheckMethod,
        HealthCheckIntervalMs: parseInt(form.HealthCheckIntervalMs) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).HealthCheckIntervalMs,
        HealthCheckTimeoutMs: parseInt(form.HealthCheckTimeoutMs) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).HealthCheckTimeoutMs,
        HealthCheckExpectedStatusCode: parseInt(form.HealthCheckExpectedStatusCode) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).HealthCheckExpectedStatusCode,
        HealthyThreshold: parseInt(form.HealthyThreshold) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).HealthyThreshold,
        UnhealthyThreshold: parseInt(form.UnhealthyThreshold) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).UnhealthyThreshold,
        HealthCheckUseAuth: form.HealthCheckUseAuth
      };
      Object.assign(data, buildConnectionPayload(form));
      if (isEdit && endpoint.GUID) data.GUID = endpoint.GUID;
      await onSave(data);
    } catch (err) {
      setError(err.message || 'Unable to save endpoint.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={isEdit ? 'Edit Inference Endpoint' : 'Create Inference Endpoint'}
      onClose={onClose}
      wide
      footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button
            className="btn btn-primary"
            onClick={handleSubmit}
            disabled={saving || !form.ApiFormat || healthCheckUrlInvalid}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </>
      }
    >
      <form onSubmit={handleSubmit}>
        {error && <div style={{ color: 'var(--danger-color)' }} role="alert">{error}</div>}
        {/* General */}
        <div className="form-section">
          <div className="form-section-title">General</div>

          <div className="form-group">
            <label><Tooltip text="Optional display name for the inference endpoint">Name</Tooltip></label>
            <input
              type="text"
              value={form.Name}
              onChange={(e) => handleChange('Name', e.target.value)}
              placeholder="Optional"
            />
          </div>

          <ProviderPicker kind="completion" value={form.ApiFormat} onChange={value => handleChange('ApiFormat', value)} confirmChange={isEdit} />
          <ProviderConnectionFields kind="completion" form={form} setForm={setForm} errors={connectionErrors}
            healthFieldsEdited={healthCheckUrlTouched} editing={isEdit && hasMaskedSecrets(form)} onReveal={handleReveal} revealing={revealing} />
        </div>

        {/* Request Handling */}
        <div className="form-section">
          <div className="form-section-title">Request Handling</div>

          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={form.Active}
                  onChange={(e) => handleChange('Active', e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text="Whether this endpoint is active and available for inference requests">Active</Tooltip></span>
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label><Tooltip text="Maximum number of concurrent requests Partio will allow for this inference endpoint">Max Concurrent Requests</Tooltip></label>
              <input
                type="number"
                value={form.MaxConcurrentRequests}
                onChange={(e) => handleChange('MaxConcurrentRequests', e.target.value)}
                min="1"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="How many requests Partio queues once the concurrent-request limit is reached. 0 rejects extra requests immediately with 429; a positive value lets that many requests wait for a slot (a queued request that waits past the request timeout returns 504).">Max Queue Depth</Tooltip></label>
              <input
                type="number"
                value={form.MaxQueueDepth}
                onChange={(e) => handleChange('MaxQueueDepth', e.target.value)}
                min="0"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="Partio's limit, in milliseconds, for requests it proxies to the upstream model, and for how long a queued request waits for a slot (e.g. 300000 = 5 minutes). AssistantHub calls the model directly and uses the AssistantHub timeouts below.">Partio Timeout (ms)</Tooltip></label>
              <input
                type="number"
                value={form.MaximumTimeoutMs}
                onChange={(e) => handleChange('MaximumTimeoutMs', e.target.value)}
                min="1000"
                step="1000"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="How long AssistantHub waits for an answer from this endpoint (1,000 to 3,600,000 ms). Empty uses the server default (Inference.RequestTimeoutMs, 300,000).">AssistantHub Answer Timeout (ms)</Tooltip></label>
              <input
                type="number"
                title="How long AssistantHub waits for an answer from this endpoint (1,000 to 3,600,000 ms). Empty uses the server default (Inference.RequestTimeoutMs, 300,000)."
                value={form.RequestTimeoutMs}
                onChange={(e) => handleChange('RequestTimeoutMs', e.target.value)}
                min="1000"
                max="3600000"
                step="1000"
                placeholder="Server default"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="How long AssistantHub waits for a utility step on this endpoint (retrieval gate, query or conversation rewrite, LLM re-rank) before falling back (1,000 to 3,600,000 ms). Empty uses the server default (Inference.UtilityTimeoutMs, 30,000).">AssistantHub Utility Timeout (ms)</Tooltip></label>
              <input
                type="number"
                title="How long AssistantHub waits for a utility step on this endpoint (retrieval gate, query or conversation rewrite, LLM re-rank) before falling back (1,000 to 3,600,000 ms). Empty uses the server default (Inference.UtilityTimeoutMs, 30,000)."
                value={form.UtilityTimeoutMs}
                onChange={(e) => handleChange('UtilityTimeoutMs', e.target.value)}
                min="1000"
                max="3600000"
                step="1000"
                placeholder="Server default"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="Context window size (maximum tokens) for the model served by this endpoint. 0 means unspecified.">Context Size</Tooltip></label>
              <input
                type="number"
                value={form.ContextSize}
                onChange={(e) => handleChange('ContextSize', e.target.value)}
                min="0"
              />
            </div>
          </div>
        </div>

        {/* Tool Calling */}
        <div className="form-section">
          <div className="form-section-title">Tool Calling</div>

          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={form.SupportsToolCalling}
                  onChange={(e) => handleChange('SupportsToolCalling', e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text="Mark this endpoint as explicitly supporting model tool calls. Tool calls stay disabled for assistants unless this endpoint and the assistant policy both allow them.">Supports Tool Calling</Tooltip></span>
            </div>
          </div>

          {form.SupportsToolCalling && (
            <>
              <div className="form-group">
                <label><Tooltip text="Wire format used for tool definitions and model tool calls. Use Ollama Chat for Ollama, OpenAI Chat Completions for OpenAI/vLLM/xAI, and Provider Native for Azure OpenAI, Anthropic, Gemini, Cohere, Bedrock or Vertex AI.">Tool Calling Format</Tooltip></label>
                <select
                  value={form.ToolCallingApiFormat}
                  onChange={(e) => handleChange('ToolCallingApiFormat', e.target.value)}
                >
                  <option value="Native">Provider Native</option>
                  <option value="OllamaChat">Ollama Chat</option>
                  <option value="OpenAIChatCompletions">OpenAI Chat Completions</option>
                </select>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={form.SupportsParallelToolCalls}
                        onChange={(e) => handleChange('SupportsParallelToolCalls', e.target.checked)}
                      />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Endpoint can return multiple tool calls in a single assistant response. Leave disabled unless the endpoint is known to support it.">Parallel Tool Calls</Tooltip></span>
                  </div>
                </div>

                <div className="form-group">
                  <div className="form-toggle">
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={form.SupportsStreamingToolCalls}
                        onChange={(e) => handleChange('SupportsStreamingToolCalls', e.target.checked)}
                      />
                      <span className="toggle-slider"></span>
                    </label>
                    <span><Tooltip text="Endpoint can stream responses that include tool-call deltas. Leave disabled unless the endpoint is known to support it.">Streaming Tool Calls</Tooltip></span>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>

        {/* Health Check */}
        <div className="form-section">
          <div className="form-section-title">Health Check</div>
          {usesConnectivityProbe(form.ApiFormat) && <p>{probeDescription(form.ApiFormat, 'completion')}</p>}

          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={form.HealthCheckEnabled}
                  onChange={(e) => handleChange('HealthCheckEnabled', e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text="Enable periodic health checks to monitor endpoint availability">Health Check Enabled</Tooltip></span>
            </div>
          </div>

{!usesConnectivityProbe(form.ApiFormat) && (
          <div className="form-group">
            <label><Tooltip text="Full URL to send health check requests to (e.g. http://ollama:11434/api/tags). Must be an absolute URL, not a relative path.">Health Check URL</Tooltip></label>
            <input
              type="text"
              value={form.HealthCheckUrl}
              onChange={(e) => handleChange('HealthCheckUrl', e.target.value)}
              placeholder="e.g. http://ollama:11434/api/tags"
            />
            {healthCheckUrlInvalid && (
              <small style={{ color: 'var(--danger, #e74c3c)', marginTop: '0.25rem', display: 'block' }}>
                Must be a full URL starting with http:// or https://
              </small>
            )}
          </div>
)}

          <div className="form-row">
{!usesConnectivityProbe(form.ApiFormat) && (
            <div className="form-group">
              <label><Tooltip text="HTTP method used for health check requests">Health Check Method</Tooltip></label>
              <select
                value={form.HealthCheckMethod}
                onChange={(e) => handleChange('HealthCheckMethod', e.target.value)}
              >
                {HEALTH_CHECK_METHOD_OPTIONS.map(opt => (
                  <option key={opt} value={opt}>{opt}</option>
                ))}
              </select>
            </div>
)}

            <div className="form-group">
              <label><Tooltip text="Time in milliseconds between consecutive health check requests">Interval (ms)</Tooltip></label>
              <input
                type="number"
                value={form.HealthCheckIntervalMs}
                onChange={(e) => handleChange('HealthCheckIntervalMs', e.target.value)}
                min="1000"
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label><Tooltip text="Maximum time in milliseconds to wait for a health check response">Health Check Timeout (ms)</Tooltip></label>
              <input
                type="number"
                value={form.HealthCheckTimeoutMs}
                onChange={(e) => handleChange('HealthCheckTimeoutMs', e.target.value)}
                min="100"
              />
            </div>

{!usesConnectivityProbe(form.ApiFormat) && (
            <div className="form-group">
              <label><Tooltip text="HTTP status code expected from a successful health check response">Expected Status Code</Tooltip></label>
              <input
                type="number"
                value={form.HealthCheckExpectedStatusCode}
                onChange={(e) => handleChange('HealthCheckExpectedStatusCode', e.target.value)}
              />
            </div>
)}
          </div>

          <div className="form-row">
            <div className="form-group">
              <label><Tooltip text="Number of consecutive successful health checks required before the endpoint is considered healthy">Healthy Threshold</Tooltip></label>
              <input
                type="number"
                value={form.HealthyThreshold}
                onChange={(e) => handleChange('HealthyThreshold', e.target.value)}
                min="1"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="Number of consecutive failed health checks required before the endpoint is considered unhealthy">Unhealthy Threshold</Tooltip></label>
              <input
                type="number"
                value={form.UnhealthyThreshold}
                onChange={(e) => handleChange('UnhealthyThreshold', e.target.value)}
                min="1"
              />
            </div>
          </div>

{!usesConnectivityProbe(form.ApiFormat) && (
          <div className="form-group">
            <div className="form-toggle">
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={form.HealthCheckUseAuth}
                  onChange={(e) => handleChange('HealthCheckUseAuth', e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
              <span><Tooltip text={`Authenticate health checks using the configured ${credentialLabel.toLowerCase()}.`}>Use Auth for Health Check</Tooltip></span>
            </div>
          </div>
)}
        </div>
      </form>
    </Modal>
  );
}

export default InferenceEndpointFormModal;
