import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

const API_FORMAT_OPTIONS = ['Ollama', 'OpenAI', 'Bedrock', 'Cohere'];
const HEALTH_CHECK_METHOD_OPTIONS = ['GET', 'POST', 'HEAD'];

const defaultHealthCheck = {
  HealthCheckEnabled: true,
  HealthCheckUrl: '',
  HealthCheckMethod: 'GET',
  HealthCheckIntervalMs: 30000,
  HealthCheckTimeoutMs: 5000,
  HealthCheckExpectedStatusCode: 200,
  HealthyThreshold: 3,
  UnhealthyThreshold: 3,
  HealthCheckUseAuth: false
};

function InferenceEndpointFormModal({ endpoint, onSave, onClose }) {
  const isEdit = !!endpoint;

  const [form, setForm] = useState({
    Name: endpoint?.Name || '',
    Model: endpoint?.Model || '',
    Endpoint: endpoint?.Endpoint || '',
    ApiFormat: endpoint?.ApiFormat || '',
    ApiKey: endpoint?.ApiKey || '',
    Active: endpoint?.Active !== undefined ? endpoint.Active : true,
    HealthCheckEnabled: endpoint?.HealthCheckEnabled !== undefined ? endpoint.HealthCheckEnabled : defaultHealthCheck.HealthCheckEnabled,
    HealthCheckUrl: endpoint?.HealthCheckUrl || defaultHealthCheck.HealthCheckUrl,
    HealthCheckMethod: endpoint?.HealthCheckMethod || defaultHealthCheck.HealthCheckMethod,
    HealthCheckIntervalMs: endpoint?.HealthCheckIntervalMs !== undefined ? endpoint.HealthCheckIntervalMs : defaultHealthCheck.HealthCheckIntervalMs,
    HealthCheckTimeoutMs: endpoint?.HealthCheckTimeoutMs !== undefined ? endpoint.HealthCheckTimeoutMs : defaultHealthCheck.HealthCheckTimeoutMs,
    HealthCheckExpectedStatusCode: endpoint?.HealthCheckExpectedStatusCode !== undefined ? endpoint.HealthCheckExpectedStatusCode : defaultHealthCheck.HealthCheckExpectedStatusCode,
    HealthyThreshold: endpoint?.HealthyThreshold !== undefined ? endpoint.HealthyThreshold : defaultHealthCheck.HealthyThreshold,
    UnhealthyThreshold: endpoint?.UnhealthyThreshold !== undefined ? endpoint.UnhealthyThreshold : defaultHealthCheck.UnhealthyThreshold,
    HealthCheckUseAuth: endpoint?.HealthCheckUseAuth !== undefined ? endpoint.HealthCheckUseAuth : defaultHealthCheck.HealthCheckUseAuth
  });

  const [saving, setSaving] = useState(false);
  const [healthCheckOpen, setHealthCheckOpen] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

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
        HealthCheckEnabled: form.HealthCheckEnabled,
        HealthCheckUrl: form.HealthCheckUrl,
        HealthCheckMethod: form.HealthCheckMethod,
        HealthCheckIntervalMs: parseInt(form.HealthCheckIntervalMs) || defaultHealthCheck.HealthCheckIntervalMs,
        HealthCheckTimeoutMs: parseInt(form.HealthCheckTimeoutMs) || defaultHealthCheck.HealthCheckTimeoutMs,
        HealthCheckExpectedStatusCode: parseInt(form.HealthCheckExpectedStatusCode) || defaultHealthCheck.HealthCheckExpectedStatusCode,
        HealthyThreshold: parseInt(form.HealthyThreshold) || defaultHealthCheck.HealthyThreshold,
        UnhealthyThreshold: parseInt(form.UnhealthyThreshold) || defaultHealthCheck.UnhealthyThreshold,
        HealthCheckUseAuth: form.HealthCheckUseAuth
      };
      if (isEdit && endpoint.GUID) data.GUID = endpoint.GUID;
      await onSave(data);
    } finally {
      setSaving(false);
    }
  };

  const collapsibleButtonStyle = {
    background: 'none',
    border: 'none',
    padding: 0,
    cursor: 'pointer',
    fontSize: '0.95rem',
    fontWeight: 600,
    color: 'var(--text-primary)'
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
            disabled={saving || !form.Model.trim() || !form.Endpoint.trim() || !form.ApiFormat}
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

        {/* Model */}
        <div className="form-group">
          <label><Tooltip text="Name of the language model to use for inference (e.g. llama3, gpt-4)">Model</Tooltip></label>
          <input
            type="text"
            value={form.Model}
            onChange={(e) => handleChange('Model', e.target.value)}
            required
          />
        </div>

        {/* Endpoint */}
        <div className="form-group">
          <label><Tooltip text="Base URL of the inference API server (e.g. http://ollama:11434)">Endpoint</Tooltip></label>
          <input
            type="text"
            value={form.Endpoint}
            onChange={(e) => handleChange('Endpoint', e.target.value)}
            required
          />
        </div>

        {/* ApiFormat */}
        <div className="form-group">
          <label><Tooltip text="API format used by the inference endpoint (Ollama, OpenAI, Bedrock, or Cohere)">Format</Tooltip></label>
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

        {/* ApiKey */}
        <div className="form-group">
          <label><Tooltip text="Optional API key for authenticating with the inference endpoint">API Key</Tooltip></label>
          <input
            type="password"
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

        {/* Health Check (collapsible) */}
        <div className="form-group">
          <button
            type="button"
            style={collapsibleButtonStyle}
            onClick={() => setHealthCheckOpen(prev => !prev)}
          >
            {healthCheckOpen ? '\u25BE' : '\u25B8'} Health Check
          </button>
          {healthCheckOpen && (
            <div style={{ marginTop: '0.5rem' }}>
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
                <label><Tooltip text="URL to send health check requests to. Defaults to the endpoint URL if not specified">Health Check URL</Tooltip></label>
                <input
                  type="text"
                  value={form.HealthCheckUrl}
                  onChange={(e) => handleChange('HealthCheckUrl', e.target.value)}
                  placeholder="Optional"
                />
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
          )}
        </div>
      </form>
    </Modal>
  );
}

export default InferenceEndpointFormModal;
