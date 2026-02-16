import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Tooltip from '../components/Tooltip';
import ConfigurationFormModal from '../components/modals/ConfigurationFormModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import AlertModal from '../components/AlertModal';

const configTooltips = {
  DnsHostname: 'DNS hostname the server listens on',
  Port: 'TCP port the server listens on',
  Ssl: 'Whether SSL/TLS is enabled',
  HeaderApiKey: 'HTTP header name used for API key authentication',
  AdminApiKey: 'API key granting admin-level access',
  Type: 'Database or provider type',
  Filename: 'Path to the database file',
  Endpoint: 'Service endpoint URL',
  BaseUrl: 'Base URL for the service',
  Region: 'Cloud provider region',
  Bucket: 'Default S3-compatible storage bucket',
  AccessKey: 'Access key for authentication',
  EnableSsl: 'Whether SSL is enabled for this connection',
  MaxChunkSize: 'Maximum size of each document chunk in characters',
  ShiftSize: 'Overlap between consecutive chunks',
  TopK: 'Number of results to retrieve per query',
  BatchSize: 'Number of items processed per batch',
  Model: 'Model used by this service',
  ApiKey: 'API key for this service',
  MaxTokens: 'Maximum tokens per request',
  Temperature: 'Sampling temperature for generation',
  MinimumScore: 'Minimum relevance score threshold',
  MaxResults: 'Maximum number of results to return',
  LogDirectory: 'Directory where log files are stored',
  LogFilename: 'Name of the log file',
  MinimumSeverity: 'Minimum severity level for logging',
  ConsoleLogging: 'Whether to output logs to the console',
  EnableColors: 'Whether to use colored console output',
};

function ConfigurationView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [config, setConfig] = useState(null);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [showJson, setShowJson] = useState(false);
  const [alert, setAlert] = useState(null);

  const loadConfig = async () => {
    setLoading(true);
    try {
      const data = await api.getConfiguration();
      setConfig(data);
    } catch (err) {
      setAlert({ title: 'Error', message: 'Failed to load configuration.' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadConfig(); }, []);

  const handleSave = async (data) => {
    try {
      await api.updateConfiguration(data);
      setShowForm(false);
      setAlert({ title: 'Success', message: 'Configuration saved successfully.' });
      loadConfig();
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save configuration.' });
    }
  };

  const renderSummarySection = (title, obj) => {
    if (!obj) return null;
    return (
      <div className="config-summary-section">
        <h4>{title}</h4>
        <div className="config-summary-grid">
          {Object.entries(obj).filter(([k]) => k !== 'statusCode').map(([key, value]) => (
            <React.Fragment key={key}>
              <span className="config-summary-label">{configTooltips[key] ? <Tooltip text={configTooltips[key]}>{key}</Tooltip> : key}</span>
              <span className="config-summary-value">
                {typeof value === 'boolean' ? (
                  <span className={`status-badge ${value ? 'active' : 'inactive'}`}>{value ? 'Yes' : 'No'}</span>
                ) : typeof value === 'object' && value !== null ? (
                  JSON.stringify(value)
                ) : (
                  String(value ?? '')
                )}
              </span>
            </React.Fragment>
          ))}
        </div>
      </div>
    );
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Configuration</h1>
          <p className="content-subtitle">View and modify server configuration settings.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <button className="btn btn-secondary" onClick={() => setShowJson(true)} disabled={!config}>
            View JSON
          </button>
          <button className="btn btn-primary" onClick={() => setShowForm(true)}>
            Edit Configuration
          </button>
        </div>
      </div>

      {loading && <p>Loading configuration...</p>}

      {!loading && config && (
        <div className="config-summary">
          {renderSummarySection('Webserver', config.Webserver)}
          {renderSummarySection('Database', config.Database)}
          {renderSummarySection('S3 Storage', config.S3)}
          {renderSummarySection('DocumentAtom', config.DocumentAtom)}
          {renderSummarySection('Chunking', config.Chunking)}
          {renderSummarySection('Inference', config.Inference)}
          {renderSummarySection('RecallDb', config.RecallDb)}
          {renderSummarySection('Logging', config.Logging)}
        </div>
      )}

      {showForm && (
        <ConfigurationFormModal
          api={api}
          onSave={handleSave}
          onClose={() => setShowForm(false)}
        />
      )}

      {showJson && config && (
        <JsonViewModal data={config} title="Configuration JSON" onClose={() => setShowJson(false)} />
      )}

      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default ConfigurationView;
