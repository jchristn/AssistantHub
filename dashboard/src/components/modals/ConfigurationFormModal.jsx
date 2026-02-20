import React, { useState, useEffect } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

function ConfigurationFormModal({ api, onSave, onClose }) {
  const [form, setForm] = useState(null);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [expandedSections, setExpandedSections] = useState({
    Webserver: true,
    Database: false,
    S3: false,
    DocumentAtom: false,
    Chunking: false,
    Inference: false,
    RecallDb: false,
    Logging: false,
  });

  useEffect(() => {
    (async () => {
      try {
        const data = await api.getConfiguration();
        setForm(data);
      } catch (err) {
        console.error('Failed to load configuration', err);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const handleChange = (section, field, value) => {
    setForm(prev => ({
      ...prev,
      [section]: { ...prev[section], [field]: value }
    }));
  };

  const toggleSection = (section) => {
    setExpandedSections(prev => ({ ...prev, [section]: !prev[section] }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSave(form);
    } finally {
      setSaving(false);
    }
  };

  const renderTextField = (section, field, label, type = 'text', tooltip = '') => (
    <div className="form-group" key={field}>
      <label>{tooltip ? <Tooltip text={tooltip}>{label}</Tooltip> : label}</label>
      <input
        type={type}
        value={form[section]?.[field] ?? ''}
        onChange={(e) => handleChange(section, field, type === 'number' ? parseInt(e.target.value) || 0 : e.target.value)}
      />
    </div>
  );

  const renderToggle = (section, field, label, tooltip = '') => (
    <div className="form-group" key={field}>
      <div className="form-toggle">
        <label className="toggle-switch">
          <input
            type="checkbox"
            checked={form[section]?.[field] ?? false}
            onChange={(e) => handleChange(section, field, e.target.checked)}
          />
          <span className="toggle-slider"></span>
        </label>
        <span>{tooltip ? <Tooltip text={tooltip}>{label}</Tooltip> : label}</span>
      </div>
    </div>
  );

  const renderSelect = (section, field, label, options, tooltip = '') => (
    <div className="form-group" key={field}>
      <label>{tooltip ? <Tooltip text={tooltip}>{label}</Tooltip> : label}</label>
      <select
        value={form[section]?.[field] ?? ''}
        onChange={(e) => handleChange(section, field, e.target.value)}
      >
        {options.map(opt => <option key={opt} value={opt}>{opt}</option>)}
      </select>
    </div>
  );

  const renderSection = (title, sectionKey, children) => (
    <div className="config-section" key={sectionKey}>
      <button
        type="button"
        className="config-section-header"
        onClick={() => toggleSection(sectionKey)}
      >
        <span className={`config-section-arrow ${expandedSections[sectionKey] ? 'expanded' : ''}`}>&#9654;</span>
        <span>{title}</span>
      </button>
      {expandedSections[sectionKey] && (
        <div className="config-section-body">{children}</div>
      )}
    </div>
  );

  if (loading) {
    return (
      <Modal title="Edit Configuration" onClose={onClose} wide>
        <p>Loading configuration...</p>
      </Modal>
    );
  }

  if (!form) {
    return (
      <Modal title="Edit Configuration" onClose={onClose} wide>
        <p>Failed to load configuration.</p>
      </Modal>
    );
  }

  return (
    <Modal title="Edit Configuration" onClose={onClose} wide footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <form onSubmit={handleSubmit}>
        {renderSection('Webserver', 'Webserver', <>
          <div className="form-row">
            {renderTextField('Webserver', 'Hostname', 'Hostname', 'text', 'Hostname or IP address the webserver listens on')}
            {renderTextField('Webserver', 'Port', 'Port', 'number', 'TCP port number for the webserver')}
          </div>
          {renderToggle('Webserver', 'Ssl', 'SSL', 'Enable HTTPS/SSL encryption for the webserver')}
        </>)}

        {renderSection('Database', 'Database', <>
          {renderSelect('Database', 'Type', 'Type', ['Sqlite', 'Postgresql', 'SqlServer', 'Mysql'], 'Database engine type to use for persistent storage')}
          {renderTextField('Database', 'Filename', 'Filename', 'text', 'File path for the SQLite database file')}
          <div className="form-row">
            {renderTextField('Database', 'Hostname', 'Hostname', 'text', 'Hostname or IP address of the database server')}
            {renderTextField('Database', 'Port', 'Port', 'number', 'TCP port number for the database server')}
          </div>
          {renderTextField('Database', 'DatabaseName', 'Database Name', 'text', 'Name of the database to connect to')}
          <div className="form-row">
            {renderTextField('Database', 'Username', 'Username', 'text', 'Username for database authentication')}
            {renderTextField('Database', 'Password', 'Password', 'text', 'Password for database authentication')}
          </div>
          {renderTextField('Database', 'Schema', 'Schema', 'text', 'Database schema to use for tables')}
          {renderToggle('Database', 'RequireEncryption', 'Require Encryption', 'Require encrypted connections to the database server')}
          {renderToggle('Database', 'LogQueries', 'Log Queries', 'Log all SQL queries for debugging purposes')}
        </>)}

        {renderSection('S3 Storage', 'S3', <>
          <div className="form-row">
            {renderTextField('S3', 'Region', 'Region', 'text', 'AWS region or S3-compatible region identifier')}
            {renderTextField('S3', 'BucketName', 'Bucket Name', 'text', 'Default S3 bucket name for document storage')}
          </div>
          <div className="form-row">
            {renderTextField('S3', 'AccessKey', 'Access Key', 'text', 'Access key ID for S3 authentication')}
            {renderTextField('S3', 'SecretKey', 'Secret Key', 'text', 'Secret access key for S3 authentication')}
          </div>
          {renderTextField('S3', 'EndpointUrl', 'Endpoint URL', 'text', 'Custom S3-compatible endpoint URL (e.g. MinIO)')}
          {renderToggle('S3', 'UseSsl', 'Use SSL', 'Use HTTPS for S3 connections')}
          {renderTextField('S3', 'BaseUrl', 'Base URL', 'text', 'Public base URL for accessing stored objects')}
        </>)}

        {renderSection('DocumentAtom', 'DocumentAtom', <>
          {renderTextField('DocumentAtom', 'Endpoint', 'Endpoint', 'text', 'URL of the DocumentAtom service for document parsing')}
          {renderTextField('DocumentAtom', 'AccessKey', 'Access Key', 'text', 'Authentication key for the DocumentAtom service')}
        </>)}

        {renderSection('Chunking', 'Chunking', <>
          {renderTextField('Chunking', 'Endpoint', 'Endpoint', 'text', 'URL of the chunking service endpoint')}
          {renderTextField('Chunking', 'AccessKey', 'Access Key', 'text', 'Authentication key for the chunking service')}
          {renderTextField('Chunking', 'EndpointId', 'Endpoint ID', 'text', 'Identifier for the specific chunking endpoint to use')}
        </>)}

        {renderSection('Inference', 'Inference', <>
          {renderSelect('Inference', 'Provider', 'Provider', ['OpenAI', 'Ollama'], 'Default AI inference provider for the system')}
          {renderTextField('Inference', 'Endpoint', 'Endpoint', 'text', 'Default inference API endpoint URL')}
          {renderTextField('Inference', 'ApiKey', 'API Key', 'text', 'Default API key for inference authentication')}
          {renderTextField('Inference', 'DefaultModel', 'Default Model', 'text', 'Default model name used for inference requests')}
        </>)}

        {renderSection('RecallDb', 'RecallDb', <>
          {renderTextField('RecallDb', 'Endpoint', 'Endpoint', 'text', 'URL of the RecallDB vector database endpoint')}
          {renderTextField('RecallDb', 'TenantId', 'Tenant ID', 'text', 'Tenant identifier for multi-tenant RecallDB deployments')}
          {renderTextField('RecallDb', 'AccessKey', 'Access Key', 'text', 'Authentication key for RecallDB access')}
        </>)}

        {renderSection('Logging', 'Logging', <>
          {renderToggle('Logging', 'ConsoleLogging', 'Console Logging', 'Output log messages to the console')}
          {renderToggle('Logging', 'EnableColors', 'Enable Colors', 'Use colored output for console log messages')}
          {renderToggle('Logging', 'FileLogging', 'File Logging', 'Write log messages to a file on disk')}
          {renderTextField('Logging', 'LogDirectory', 'Log Directory', 'text', 'Directory path where log files are stored')}
          {renderTextField('Logging', 'LogFilename', 'Log Filename', 'text', 'Base filename for log files')}
          {renderToggle('Logging', 'IncludeDateInFilename', 'Include Date in Filename', 'Append the current date to log filenames for daily rotation')}
          {renderTextField('Logging', 'MinimumSeverity', 'Minimum Severity (0-7)', 'number', 'Minimum log level to record (0=Trace, 7=Fatal)')}
        </>)}
      </form>
    </Modal>
  );
}

export default ConfigurationFormModal;
