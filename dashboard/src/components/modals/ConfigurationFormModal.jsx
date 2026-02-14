import React, { useState, useEffect } from 'react';
import Modal from '../Modal';

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

  const renderTextField = (section, field, label, type = 'text') => (
    <div className="form-group" key={field}>
      <label>{label}</label>
      <input
        type={type}
        value={form[section]?.[field] ?? ''}
        onChange={(e) => handleChange(section, field, type === 'number' ? parseInt(e.target.value) || 0 : e.target.value)}
      />
    </div>
  );

  const renderToggle = (section, field, label) => (
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
        <span>{label}</span>
      </div>
    </div>
  );

  const renderSelect = (section, field, label, options) => (
    <div className="form-group" key={field}>
      <label>{label}</label>
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
            {renderTextField('Webserver', 'Hostname', 'Hostname')}
            {renderTextField('Webserver', 'Port', 'Port', 'number')}
          </div>
          {renderToggle('Webserver', 'Ssl', 'SSL')}
        </>)}

        {renderSection('Database', 'Database', <>
          {renderSelect('Database', 'Type', 'Type', ['Sqlite', 'Postgresql', 'SqlServer', 'Mysql'])}
          {renderTextField('Database', 'Filename', 'Filename')}
          <div className="form-row">
            {renderTextField('Database', 'Hostname', 'Hostname')}
            {renderTextField('Database', 'Port', 'Port', 'number')}
          </div>
          {renderTextField('Database', 'DatabaseName', 'Database Name')}
          <div className="form-row">
            {renderTextField('Database', 'Username', 'Username')}
            {renderTextField('Database', 'Password', 'Password')}
          </div>
          {renderTextField('Database', 'Schema', 'Schema')}
          {renderToggle('Database', 'RequireEncryption', 'Require Encryption')}
          {renderToggle('Database', 'LogQueries', 'Log Queries')}
        </>)}

        {renderSection('S3 Storage', 'S3', <>
          <div className="form-row">
            {renderTextField('S3', 'Region', 'Region')}
            {renderTextField('S3', 'BucketName', 'Bucket Name')}
          </div>
          <div className="form-row">
            {renderTextField('S3', 'AccessKey', 'Access Key')}
            {renderTextField('S3', 'SecretKey', 'Secret Key')}
          </div>
          {renderTextField('S3', 'EndpointUrl', 'Endpoint URL')}
          {renderToggle('S3', 'UseSsl', 'Use SSL')}
          {renderTextField('S3', 'BaseUrl', 'Base URL')}
        </>)}

        {renderSection('DocumentAtom', 'DocumentAtom', <>
          {renderTextField('DocumentAtom', 'Endpoint', 'Endpoint')}
          {renderTextField('DocumentAtom', 'AccessKey', 'Access Key')}
        </>)}

        {renderSection('Chunking', 'Chunking', <>
          {renderTextField('Chunking', 'Endpoint', 'Endpoint')}
          {renderTextField('Chunking', 'AccessKey', 'Access Key')}
          {renderTextField('Chunking', 'EndpointId', 'Endpoint ID')}
        </>)}

        {renderSection('Inference', 'Inference', <>
          {renderSelect('Inference', 'Provider', 'Provider', ['OpenAI', 'Ollama'])}
          {renderTextField('Inference', 'Endpoint', 'Endpoint')}
          {renderTextField('Inference', 'ApiKey', 'API Key')}
          {renderTextField('Inference', 'DefaultModel', 'Default Model')}
        </>)}

        {renderSection('RecallDb', 'RecallDb', <>
          {renderTextField('RecallDb', 'Endpoint', 'Endpoint')}
          {renderTextField('RecallDb', 'TenantId', 'Tenant ID')}
          {renderTextField('RecallDb', 'AccessKey', 'Access Key')}
        </>)}

        {renderSection('Logging', 'Logging', <>
          {renderToggle('Logging', 'ConsoleLogging', 'Console Logging')}
          {renderToggle('Logging', 'EnableColors', 'Enable Colors')}
          {renderToggle('Logging', 'FileLogging', 'File Logging')}
          {renderTextField('Logging', 'LogDirectory', 'Log Directory')}
          {renderTextField('Logging', 'LogFilename', 'Log Filename')}
          {renderToggle('Logging', 'IncludeDateInFilename', 'Include Date in Filename')}
          {renderTextField('Logging', 'MinimumSeverity', 'Minimum Severity (0-7)', 'number')}
        </>)}
      </form>
    </Modal>
  );
}

export default ConfigurationFormModal;
