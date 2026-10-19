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

function EmbeddingEndpointFormModal({ endpoint, initialData, onSave, onClose, onRevealEndpoint }) {
  const isEdit = !!endpoint;
  const source = endpoint || initialData;
  const initialApiFormat = source?.ApiFormat || 'Ollama';
  const initialDefaults = getApiFormatDefaults(initialApiFormat, source?.Endpoint || getDefaultEndpoint(initialApiFormat));

  const initialProviderSettings = providerSettingsToForm(initialApiFormat, source?.ProviderSettings);
  const [form, setForm] = useState({
    Name: source?.Name || '',
    Model: source?.Model || getDefaultModel(initialApiFormat, 'embedding'),
    Endpoint: resolveProviderEndpoint(initialApiFormat, source?.Endpoint),
    ApiFormat: initialApiFormat,
    ProviderSettings: initialProviderSettings,
    Auth: authFromEndpoint(source || {}),
    EndpointOverride: hasEndpointOverride(initialApiFormat, source?.Endpoint, initialProviderSettings),
    ApiKey: source?.ApiKey || '',
    Active: source?.Active !== undefined ? source.Active : true,
    MaxConcurrentRequests: source?.MaxConcurrentRequests !== undefined ? source.MaxConcurrentRequests : 4,
    MaxQueueDepth: source?.MaxQueueDepth !== undefined ? source.MaxQueueDepth : 16,
    ContextSize: source?.ContextSize !== undefined ? source.ContextSize : 0,
    MaximumTimeoutMs: source?.MaximumTimeoutMs !== undefined ? source.MaximumTimeoutMs : initialDefaults.MaximumTimeoutMs,
    RequestTimeoutMs: source?.RequestTimeoutMs ?? '',
    HealthCheckEnabled: source?.HealthCheckEnabled !== undefined ? source.HealthCheckEnabled : initialDefaults.HealthCheckEnabled,
    HealthCheckUrl: source?.HealthCheckUrl || initialDefaults.HealthCheckUrl,
    HealthCheckMethod: source?.HealthCheckMethod || initialDefaults.HealthCheckMethod,
    HealthCheckIntervalMs: source?.HealthCheckIntervalMs !== undefined ? source.HealthCheckIntervalMs : initialDefaults.HealthCheckIntervalMs,
    HealthCheckTimeoutMs: source?.HealthCheckTimeoutMs !== undefined ? source.HealthCheckTimeoutMs : initialDefaults.HealthCheckTimeoutMs,
    HealthCheckExpectedStatusCode: source?.HealthCheckExpectedStatusCode !== undefined ? source.HealthCheckExpectedStatusCode : initialDefaults.HealthCheckExpectedStatusCode,
    HealthyThreshold: source?.HealthyThreshold !== undefined ? source.HealthyThreshold : initialDefaults.HealthyThreshold,
    UnhealthyThreshold: source?.UnhealthyThreshold !== undefined ? source.UnhealthyThreshold : initialDefaults.UnhealthyThreshold,
    HealthCheckUseAuth: source?.HealthCheckUseAuth !== undefined ? source.HealthCheckUseAuth : initialDefaults.HealthCheckUseAuth
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [connectionErrors, setConnectionErrors] = useState({});
  const [revealing, setRevealing] = useState(false);
  const credentialLabel = apiKeyLabel(form.ApiFormat, form.ProviderSettings, form.Auth);
  // The health check URL auto-follows the endpoint until the user edits it manually.
  const [healthCheckUrlTouched, setHealthCheckUrlTouched] = useState(
    !isDefaultHealthCheckUrl(
      source?.HealthCheckUrl || '',
      resolveProviderEndpoint(initialApiFormat, source?.Endpoint),
      initialApiFormat
    )
  );
  const handleChange = (field, value) => {
    setError('');
    if (field === 'ApiFormat') {
      setForm(previous => applyProviderChange(previous, value, 'embedding', healthCheckUrlTouched));
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
    const errors = validateConnection(form, 'embedding');
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
        MaxConcurrentRequests: parseInt(form.MaxConcurrentRequests) || 4,
        MaxQueueDepth: Number.isNaN(parseInt(form.MaxQueueDepth)) ? 16 : Math.max(0, parseInt(form.MaxQueueDepth)),
        ContextSize: Number.isNaN(parseInt(form.ContextSize)) ? 0 : Math.max(0, parseInt(form.ContextSize)),
        MaximumTimeoutMs: parseInt(form.MaximumTimeoutMs) || getApiFormatDefaults(form.ApiFormat, form.Endpoint).MaximumTimeoutMs,
        RequestTimeoutMs: toTimeoutValue(form.RequestTimeoutMs),
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
      title={isEdit ? 'Edit Embedding Endpoint' : 'Create Embedding Endpoint'}
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
            <label><Tooltip text="Optional display name for the embedding endpoint">Name</Tooltip></label>
            <input
              type="text"
              value={form.Name}
              onChange={(e) => handleChange('Name', e.target.value)}
              placeholder="Optional"
            />
          </div>

          <ProviderPicker kind="embedding" value={form.ApiFormat} onChange={value => handleChange('ApiFormat', value)} confirmChange={isEdit} />
          <ProviderConnectionFields kind="embedding" form={form} setForm={setForm} errors={connectionErrors}
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
              <span><Tooltip text="Whether this endpoint is active and available for embedding requests">Active</Tooltip></span>
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label><Tooltip text="Maximum number of concurrent requests Partio will allow for this embedding endpoint">Max Concurrent Requests</Tooltip></label>
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
              <label><Tooltip text="Partio's limit, in milliseconds, for its own call to the upstream embedding model, and for how long a queued request waits for a slot (e.g. 300000 = 5 minutes). AssistantHub's own wait is the AssistantHub timeout below.">Partio Timeout (ms)</Tooltip></label>
              <input
                type="number"
                value={form.MaximumTimeoutMs}
                onChange={(e) => handleChange('MaximumTimeoutMs', e.target.value)}
                min="1000"
                step="1000"
              />
            </div>

            <div className="form-group">
              <label><Tooltip text="How long AssistantHub waits for a query embedding through this endpoint before retrieval falls back to keyword search (1,000 to 3,600,000 ms). Empty uses the server default (Chunking.QueryEmbeddingTimeoutMs, 30,000).">AssistantHub Query Timeout (ms)</Tooltip></label>
              <input
                type="number"
                title="How long AssistantHub waits for a query embedding through this endpoint before retrieval falls back to keyword search (1,000 to 3,600,000 ms). Empty uses the server default (Chunking.QueryEmbeddingTimeoutMs, 30,000)."
                value={form.RequestTimeoutMs}
                onChange={(e) => handleChange('RequestTimeoutMs', e.target.value)}
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

        {/* Health Check */}
        <div className="form-section">
          <div className="form-section-title">Health Check</div>
          {usesConnectivityProbe(form.ApiFormat) && <p>{probeDescription(form.ApiFormat, 'embedding')}</p>}

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

export default EmbeddingEndpointFormModal;
