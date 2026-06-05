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

function EmbeddingEndpointFormModal({ endpoint, initialData, onSave, onClose }) {
  const isEdit = !!endpoint;
  const source = endpoint || initialData;
  const initialApiFormat = source?.ApiFormat || 'Ollama';
  const initialDefaults = getApiFormatDefaults(initialApiFormat, source?.Endpoint || getDefaultEndpoint(initialApiFormat));

  const [form, setForm] = useState({
    Name: source?.Name || '',
    Model: source?.Model || getDefaultModel(initialApiFormat, 'embedding'),
    Endpoint: source?.Endpoint || initialDefaults.Endpoint,
    ApiFormat: initialApiFormat,
    ApiKey: source?.ApiKey || '',
    Active: source?.Active !== undefined ? source.Active : true,
    MaxConcurrentRequests: source?.MaxConcurrentRequests !== undefined ? source.MaxConcurrentRequests : 2,
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
  const handleChange = (field, value) => {
    setForm(prev => {
      const updated = { ...prev, [field]: value };
      if (field === 'ApiFormat') {
        const oldDefaults = getApiFormatDefaults(prev.ApiFormat, prev.Endpoint || getDefaultEndpoint(prev.ApiFormat));
        const nextEndpoint = !prev.Endpoint || prev.Endpoint === getDefaultEndpoint(prev.ApiFormat)
          ? getDefaultEndpoint(value)
          : prev.Endpoint;
        const newDefaults = getApiFormatDefaults(value, nextEndpoint);
        const oldDefaultModel = getDefaultModel(prev.ApiFormat, 'embedding');
        const newDefaultModel = getDefaultModel(value, 'embedding');

        updated.Endpoint = nextEndpoint;
        if (!prev.Model || prev.Model === oldDefaultModel) {
          updated.Model = newDefaultModel;
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
      title={isEdit ? 'Edit Embedding Endpoint' : 'Create Embedding Endpoint'}
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
          <label><Tooltip text="Optional display name for the embedding endpoint">Name</Tooltip></label>
          <input
            type="text"
            value={form.Name}
            onChange={(e) => handleChange('Name', e.target.value)}
            placeholder="Optional"
          />
        </div>

        {/* ApiFormat */}
        <div className="form-group">
          <label><Tooltip text="API format used by the embedding endpoint (Ollama, OpenAI, or Gemini)">Format</Tooltip></label>
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
          <label><Tooltip text="Name of the embedding model to use (e.g. nomic-embed-text, text-embedding-3-small, gemini-embedding-001)">Model</Tooltip></label>
          <input
            type="text"
            value={form.Model}
            onChange={(e) => handleChange('Model', e.target.value)}
            required
          />
        </div>

        {/* Endpoint */}
        <div className="form-group">
          <label><Tooltip text="Base URL of the embedding API server (e.g. http://ollama:11434 or https://generativelanguage.googleapis.com)">Endpoint</Tooltip></label>
          <input
            type="text"
            value={form.Endpoint}
            onChange={(e) => handleChange('Endpoint', e.target.value)}
            required
          />
        </div>

        {/* ApiKey */}
        <div className="form-group">
          <label><Tooltip text="Optional API key for authenticating with the embedding endpoint">API Key</Tooltip></label>
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
            <span><Tooltip text="Whether this endpoint is active and available for embedding requests">Active</Tooltip></span>
          </div>
        </div>

        <div className="form-group">
          <label><Tooltip text="Maximum number of concurrent requests Partio will allow for this embedding endpoint">Max Concurrent Requests</Tooltip></label>
          <input
            type="number"
            value={form.MaxConcurrentRequests}
            onChange={(e) => handleChange('MaxConcurrentRequests', e.target.value)}
            min="1"
          />
        </div>

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

export default EmbeddingEndpointFormModal;
