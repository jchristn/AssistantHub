import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';
import PasswordInput from '../PasswordInput';
import {
  API_FORMAT_OPTIONS,
  HEALTH_CHECK_METHOD_OPTIONS,
  getApiFormatDefaults,
  getDefaultEndpoint,
  getDefaultModel,
  getHealthCheckUrlForEndpointChange
} from '../../utils/endpointDefaults';

function isAbsoluteUrl(url) {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
}

function getDefaultToolCallingApiFormat(apiFormat) {
  return apiFormat === 'Ollama' ? 'OllamaChat' : 'OpenAIChatCompletions';
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

function InferenceEndpointFormModal({ endpoint, initialData, onSave, onClose }) {
  const isEdit = !!endpoint;
  const source = endpoint || initialData;
  const initialApiFormat = getSourceText(source, 'ApiFormat', 'apiFormat') || 'Ollama';
  const initialEndpoint = getSourceText(source, 'Endpoint', 'endpoint');
  const initialLabels = getSourceLabels(source);
  const initialTags = getSourceTags(source);
  const initialDefaults = getApiFormatDefaults(initialApiFormat, initialEndpoint || getDefaultEndpoint(initialApiFormat));

  const [form, setForm] = useState({
    Name: getSourceText(source, 'Name', 'name'),
    Model: getSourceText(source, 'Model', 'model') || getDefaultModel(initialApiFormat, 'inference'),
    Endpoint: initialEndpoint || initialDefaults.Endpoint,
    ApiFormat: initialApiFormat,
    ApiKey: getSourceText(source, 'ApiKey', 'apiKey'),
    Active: getSourceField(source, 'Active', 'active') !== undefined ? getSourceBoolean(source, 'Active', 'active') : true,
    MaxConcurrentRequests: getSourceField(source, 'MaxConcurrentRequests', 'maxConcurrentRequests') !== undefined ? getSourceField(source, 'MaxConcurrentRequests', 'maxConcurrentRequests') : 2,
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
  const handleChange = (field, value) => {
    setForm(prev => {
      const updated = { ...prev, [field]: value };
      if (field === 'ApiFormat') {
        const oldDefaults = getApiFormatDefaults(prev.ApiFormat, prev.Endpoint || getDefaultEndpoint(prev.ApiFormat));
        const nextEndpoint = !prev.Endpoint || prev.Endpoint === getDefaultEndpoint(prev.ApiFormat)
          ? getDefaultEndpoint(value)
          : prev.Endpoint;
        const newDefaults = getApiFormatDefaults(value, nextEndpoint);
        const oldDefaultModel = getDefaultModel(prev.ApiFormat, 'inference');
        const newDefaultModel = getDefaultModel(value, 'inference');
        const oldDefaultToolFormat = getDefaultToolCallingApiFormat(prev.ApiFormat);
        const newDefaultToolFormat = getDefaultToolCallingApiFormat(value);

        updated.Endpoint = nextEndpoint;
        if (!prev.Model || prev.Model === oldDefaultModel) {
          updated.Model = newDefaultModel;
        }
        if (!prev.ToolCallingApiFormat || prev.ToolCallingApiFormat === oldDefaultToolFormat) {
          updated.ToolCallingApiFormat = newDefaultToolFormat;
        }

        if (!prev.HealthCheckUrl || prev.HealthCheckUrl === oldDefaults.HealthCheckUrl) {
          updated.HealthCheckUrl = newDefaults.HealthCheckUrl;
        }
        if (prev.HealthCheckUseAuth === oldDefaults.HealthCheckUseAuth) {
          updated.HealthCheckUseAuth = newDefaults.HealthCheckUseAuth;
        }
        if (prev.HealthCheckIntervalMs === oldDefaults.HealthCheckIntervalMs) {
          updated.HealthCheckIntervalMs = newDefaults.HealthCheckIntervalMs;
        }
        if (prev.HealthCheckTimeoutMs === oldDefaults.HealthCheckTimeoutMs) {
          updated.HealthCheckTimeoutMs = newDefaults.HealthCheckTimeoutMs;
        }
      } else if (field === 'Endpoint') {
        updated.HealthCheckUrl = getHealthCheckUrlForEndpointChange(prev.HealthCheckUrl, prev.Endpoint, value, prev.ApiFormat);
      }
      return updated;
    });
  };

  const healthCheckUrlInvalid = form.HealthCheckUrl && !isAbsoluteUrl(form.HealthCheckUrl);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const data = {
        Name: form.Name,
        Model: form.Model,
        Endpoint: form.Endpoint,
        ApiFormat: form.ApiFormat,
        ApiKey: form.ApiKey,
        Active: form.Active,
        MaxConcurrentRequests: parseInt(form.MaxConcurrentRequests) || 2,
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
      if (isEdit && endpoint.GUID) data.GUID = endpoint.GUID;
      await onSave(data);
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
            disabled={saving || !form.Model.trim() || !form.Endpoint.trim() || !form.ApiFormat || healthCheckUrlInvalid}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </>
      }
    >
      <form onSubmit={handleSubmit}>
        {/* Name */}
        <div className="form-group">
          <label><Tooltip text="Optional display name for the inference endpoint">Name</Tooltip></label>
          <input
            type="text"
            value={form.Name}
            onChange={(e) => handleChange('Name', e.target.value)}
            placeholder="Optional"
          />
        </div>

        {/* ApiFormat */}
        <div className="form-group">
          <label><Tooltip text="API format used by the inference endpoint (Ollama, OpenAI, or Gemini)">Format</Tooltip></label>
          <select
            value={form.ApiFormat}
            onChange={(e) => handleChange('ApiFormat', e.target.value)}
            required
          >
            <option value="">-- Select Format --</option>
            {API_FORMAT_OPTIONS.map(opt => (
              <option key={opt} value={opt}>{opt}</option>
            ))}
          </select>
        </div>

        {/* Model */}
        <div className="form-group">
          <label><Tooltip text="Name of the language model to use for inference (e.g. llama3, gpt-4o-mini, gemini-2.5-flash)">Model</Tooltip></label>
          <input
            type="text"
            value={form.Model}
            onChange={(e) => handleChange('Model', e.target.value)}
            required
          />
        </div>

        {/* Endpoint */}
        <div className="form-group">
          <label><Tooltip text="Base URL of the inference API server (e.g. http://ollama:11434 or https://generativelanguage.googleapis.com)">Endpoint</Tooltip></label>
          <input
            type="text"
            value={form.Endpoint}
            onChange={(e) => handleChange('Endpoint', e.target.value)}
            required
          />
        </div>

        {/* ApiKey */}
        <div className="form-group">
          <label><Tooltip text="Optional API key for authenticating with the inference endpoint">API Key</Tooltip></label>
          <PasswordInput
            value={form.ApiKey}
            onChange={(e) => handleChange('ApiKey', e.target.value)}
            placeholder="Optional"
          />
        </div>

        {/* Active */}
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
              <label><Tooltip text="Wire format used for tool definitions and model tool calls. Use Ollama Chat for native Ollama endpoints and OpenAI Chat Completions for OpenAI-compatible endpoints.">Tool Calling Format</Tooltip></label>
              <select
                value={form.ToolCallingApiFormat}
                onChange={(e) => handleChange('ToolCallingApiFormat', e.target.value)}
              >
                <option value="OllamaChat">Ollama Chat</option>
                <option value="OpenAIChatCompletions">OpenAI Chat Completions</option>
              </select>
            </div>

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
          </>
        )}

        {/* Health Check */}
        <div className="form-group">
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

          <div className="form-group">
            <label><Tooltip text="Time in milliseconds between consecutive health check requests">Interval (ms)</Tooltip></label>
            <input
              type="number"
              value={form.HealthCheckIntervalMs}
              onChange={(e) => handleChange('HealthCheckIntervalMs', e.target.value)}
              min="1000"
            />
          </div>

          <div className="form-group">
            <label><Tooltip text="Maximum time in milliseconds to wait for a health check response">Timeout (ms)</Tooltip></label>
            <input
              type="number"
              value={form.HealthCheckTimeoutMs}
              onChange={(e) => handleChange('HealthCheckTimeoutMs', e.target.value)}
              min="100"
            />
          </div>

          <div className="form-group">
            <label><Tooltip text="HTTP status code expected from a successful health check response">Expected Status Code</Tooltip></label>
            <input
              type="number"
              value={form.HealthCheckExpectedStatusCode}
              onChange={(e) => handleChange('HealthCheckExpectedStatusCode', e.target.value)}
            />
          </div>

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
              <span><Tooltip text="Include API key authentication in health check requests">Use Auth for Health Check</Tooltip></span>
            </div>
          </div>
        </div>
      </form>
    </Modal>
  );
}

export default InferenceEndpointFormModal;
