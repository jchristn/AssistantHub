import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

const REPOSITORY_TYPES = ['Web'];
const AUTH_TYPES = ['None', 'Basic', 'Bearer', 'ApiKey'];
const INTERVAL_TYPES = ['Minutes', 'Hours', 'Days', 'Weeks'];

const defaultRepository = {
  StartUrl: '',
  AuthType: 'None',
  Username: '',
  Password: '',
  BearerToken: '',
  ApiKey: '',
  UserAgent: '',
  FollowLinks: true,
  FollowRedirects: true,
  ExtractSitemapLinks: false,
  RestrictToChildUrls: true,
  RestrictToSubdomain: false,
  RestrictToRootDomain: false,
  IgnoreRobotsTxt: false,
  UseHeadlessBrowser: false,
  MaxDepth: 5,
  MaxParallelTasks: 4,
  CrawlDelayMs: 1000,
};

const defaultSchedule = {
  IntervalType: 'Hours',
  IntervalValue: 24,
};

const defaultFilter = {
  ObjectPrefix: '',
  ObjectSuffix: '',
  AllowedContentTypes: '',
  MinimumSize: '',
  MaximumSize: '',
};

const defaultProcessing = {
  ProcessAdditions: true,
  ProcessUpdates: true,
  ProcessDeletions: true,
  MaxDrainTasks: 4,
};

function CrawlPlanFormModal({ plan, ingestionRules, onSave, onClose }) {
  const isEdit = !!plan;

  const [form, setForm] = useState({
    Name: plan?.Name || '',
    RepositoryType: plan?.RepositoryType || 'Web',
    IngestionRuleId: plan?.IngestionRuleId || '',
    StoreInS3: plan?.StoreInS3 ?? false,
    S3BucketName: plan?.S3BucketName || '',
    Repository: plan?.Repository ? { ...defaultRepository, ...plan.Repository } : { ...defaultRepository },
    Schedule: plan?.Schedule ? { ...defaultSchedule, ...plan.Schedule } : { ...defaultSchedule },
    Filter: plan?.Filter ? {
      ObjectPrefix: plan.Filter.ObjectPrefix || '',
      ObjectSuffix: plan.Filter.ObjectSuffix || '',
      AllowedContentTypes: (plan.Filter.AllowedContentTypes || []).join(', '),
      MinimumSize: plan.Filter.MinimumSize ?? '',
      MaximumSize: plan.Filter.MaximumSize ?? '',
    } : { ...defaultFilter },
    Processing: plan?.Processing ? { ...defaultProcessing, ...plan.Processing } : { ...defaultProcessing },
    RetentionDays: plan?.RetentionDays ?? '',
  });

  const [saving, setSaving] = useState(false);
  const [generalOpen, setGeneralOpen] = useState(true);
  const [ingestionOpen, setIngestionOpen] = useState(false);
  const [repoOpen, setRepoOpen] = useState(false);
  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [filterOpen, setFilterOpen] = useState(false);
  const [processingOpen, setProcessingOpen] = useState(false);
  const [retentionOpen, setRetentionOpen] = useState(false);

  const handleChange = (field, value) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleRepoChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Repository: { ...prev.Repository, [field]: value }
    }));
  };

  const handleScheduleChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Schedule: { ...prev.Schedule, [field]: value }
    }));
  };

  const handleFilterChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Filter: { ...prev.Filter, [field]: value }
    }));
  };

  const handleProcessingChange = (field, value) => {
    setForm(prev => ({
      ...prev,
      Processing: { ...prev.Processing, [field]: value }
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const data = {
        Name: form.Name,
        RepositoryType: form.RepositoryType,
        IngestionRuleId: form.IngestionRuleId || undefined,
        StoreInS3: form.StoreInS3,
        S3BucketName: form.StoreInS3 ? form.S3BucketName : undefined,
        Repository: {
          StartUrl: form.Repository.StartUrl,
          AuthType: form.Repository.AuthType,
          ...(form.Repository.AuthType === 'Basic' ? { Username: form.Repository.Username, Password: form.Repository.Password } : {}),
          ...(form.Repository.AuthType === 'Bearer' ? { BearerToken: form.Repository.BearerToken } : {}),
          ...(form.Repository.AuthType === 'ApiKey' ? { ApiKey: form.Repository.ApiKey } : {}),
          UserAgent: form.Repository.UserAgent || undefined,
          FollowLinks: form.Repository.FollowLinks,
          FollowRedirects: form.Repository.FollowRedirects,
          ExtractSitemapLinks: form.Repository.ExtractSitemapLinks,
          RestrictToChildUrls: form.Repository.RestrictToChildUrls,
          RestrictToSubdomain: form.Repository.RestrictToSubdomain,
          RestrictToRootDomain: form.Repository.RestrictToRootDomain,
          IgnoreRobotsTxt: form.Repository.IgnoreRobotsTxt,
          UseHeadlessBrowser: form.Repository.UseHeadlessBrowser,
          MaxDepth: parseInt(form.Repository.MaxDepth) || 5,
          MaxParallelTasks: parseInt(form.Repository.MaxParallelTasks) || 4,
          CrawlDelayMs: parseInt(form.Repository.CrawlDelayMs) || 1000,
        },
        Schedule: {
          IntervalType: form.Schedule.IntervalType,
          IntervalValue: parseInt(form.Schedule.IntervalValue) || 24,
        },
        Filter: {
          ObjectPrefix: form.Filter.ObjectPrefix || undefined,
          ObjectSuffix: form.Filter.ObjectSuffix || undefined,
          AllowedContentTypes: form.Filter.AllowedContentTypes
            ? form.Filter.AllowedContentTypes.split(',').map(s => s.trim()).filter(Boolean)
            : undefined,
          MinimumSize: form.Filter.MinimumSize !== '' ? parseInt(form.Filter.MinimumSize) : undefined,
          MaximumSize: form.Filter.MaximumSize !== '' ? parseInt(form.Filter.MaximumSize) : undefined,
        },
        Processing: {
          ProcessAdditions: form.Processing.ProcessAdditions,
          ProcessUpdates: form.Processing.ProcessUpdates,
          ProcessDeletions: form.Processing.ProcessDeletions,
          MaxDrainTasks: parseInt(form.Processing.MaxDrainTasks) || 4,
        },
        RetentionDays: form.RetentionDays !== '' ? parseInt(form.RetentionDays) : undefined,
      };
      if (isEdit && (plan.Id || plan.GUID)) {
        data.Id = plan.Id || plan.GUID;
        data.GUID = plan.GUID || plan.Id;
      }
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
    color: 'var(--text-primary)',
  };

  return (
    <Modal
      title={isEdit ? 'Edit Crawl Plan' : 'Create Crawl Plan'}
      onClose={onClose}
      wide
      footer={
        <>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button
            className="btn btn-primary"
            onClick={handleSubmit}
            disabled={saving || !form.Name.trim()}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </>
      }
    >
      <form onSubmit={handleSubmit}>
        {/* General (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setGeneralOpen(prev => !prev)}>
            {generalOpen ? '\u25BE' : '\u25B8'} General
          </button>
          {generalOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Display name for this crawl plan">Name</Tooltip></label>
                <input type="text" value={form.Name} onChange={(e) => handleChange('Name', e.target.value)} required />
              </div>
              <div className="form-group">
                <label><Tooltip text="Repository type to crawl">Repository Type</Tooltip></label>
                <select value={form.RepositoryType} onChange={(e) => handleChange('RepositoryType', e.target.value)}>
                  {REPOSITORY_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
            </div>
          )}
        </div>

        {/* Ingestion (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setIngestionOpen(prev => !prev)}>
            {ingestionOpen ? '\u25BE' : '\u25B8'} Ingestion
          </button>
          {ingestionOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Ingestion rule used to process crawled documents">Ingestion Rule</Tooltip></label>
                <select value={form.IngestionRuleId} onChange={(e) => handleChange('IngestionRuleId', e.target.value)}>
                  <option value="">-- Select Ingestion Rule --</option>
                  {(ingestionRules || []).map(r => (
                    <option key={r.GUID || r.Id} value={r.GUID || r.Id}>{r.Name || r.GUID || r.Id}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.StoreInS3} onChange={(e) => handleChange('StoreInS3', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Store crawled content in an S3 bucket">Store in S3</Tooltip></span>
                </div>
              </div>
              {form.StoreInS3 && (
                <div className="form-group">
                  <label><Tooltip text="S3 bucket name to store crawled content">S3 Bucket Name</Tooltip></label>
                  <input type="text" value={form.S3BucketName} onChange={(e) => handleChange('S3BucketName', e.target.value)} />
                </div>
              )}
            </div>
          )}
        </div>

        {/* Repository Settings (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setRepoOpen(prev => !prev)}>
            {repoOpen ? '\u25BE' : '\u25B8'} Repository Settings
          </button>
          {repoOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Starting URL for the web crawl">Start URL</Tooltip></label>
                <input type="text" value={form.Repository.StartUrl} onChange={(e) => handleRepoChange('StartUrl', e.target.value)} placeholder="https://example.com" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Authentication type for accessing the repository">Auth Type</Tooltip></label>
                <select value={form.Repository.AuthType} onChange={(e) => handleRepoChange('AuthType', e.target.value)}>
                  {AUTH_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              {form.Repository.AuthType === 'Basic' && (
                <>
                  <div className="form-group">
                    <label><Tooltip text="Username for basic authentication">Username</Tooltip></label>
                    <input type="text" value={form.Repository.Username} onChange={(e) => handleRepoChange('Username', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label><Tooltip text="Password for basic authentication">Password</Tooltip></label>
                    <input type="password" value={form.Repository.Password} onChange={(e) => handleRepoChange('Password', e.target.value)} />
                  </div>
                </>
              )}
              {form.Repository.AuthType === 'Bearer' && (
                <div className="form-group">
                  <label><Tooltip text="Bearer token for authentication">Bearer Token</Tooltip></label>
                  <input type="text" value={form.Repository.BearerToken} onChange={(e) => handleRepoChange('BearerToken', e.target.value)} />
                </div>
              )}
              {form.Repository.AuthType === 'ApiKey' && (
                <div className="form-group">
                  <label><Tooltip text="API key for authentication">API Key</Tooltip></label>
                  <input type="text" value={form.Repository.ApiKey} onChange={(e) => handleRepoChange('ApiKey', e.target.value)} />
                </div>
              )}
              <div className="form-group">
                <label><Tooltip text="User agent string sent with HTTP requests">User Agent</Tooltip></label>
                <input type="text" value={form.Repository.UserAgent} onChange={(e) => handleRepoChange('UserAgent', e.target.value)} placeholder="Optional" />
              </div>

              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.FollowLinks} onChange={(e) => handleRepoChange('FollowLinks', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Follow hyperlinks discovered on crawled pages">Follow Links</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.FollowRedirects} onChange={(e) => handleRepoChange('FollowRedirects', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Follow HTTP redirects (301, 302, etc.)">Follow Redirects</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.ExtractSitemapLinks} onChange={(e) => handleRepoChange('ExtractSitemapLinks', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Extract and follow URLs found in sitemap.xml">Extract Sitemap Links</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.RestrictToChildUrls} onChange={(e) => handleRepoChange('RestrictToChildUrls', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Only crawl URLs that are children of the start URL path">Restrict to Child URLs</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.RestrictToSubdomain} onChange={(e) => handleRepoChange('RestrictToSubdomain', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Only crawl URLs within the same subdomain as the start URL">Restrict to Subdomain</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.RestrictToRootDomain} onChange={(e) => handleRepoChange('RestrictToRootDomain', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Only crawl URLs within the same root domain">Restrict to Root Domain</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.IgnoreRobotsTxt} onChange={(e) => handleRepoChange('IgnoreRobotsTxt', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Ignore robots.txt rules when crawling">Ignore robots.txt</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Repository.UseHeadlessBrowser} onChange={(e) => handleRepoChange('UseHeadlessBrowser', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Use a headless browser to render JavaScript-heavy pages">Use Headless Browser</Tooltip></span>
                </div>
              </div>

              <div className="form-group">
                <label><Tooltip text="Maximum depth to follow links from the start URL">Max Depth</Tooltip></label>
                <input type="number" value={form.Repository.MaxDepth} onChange={(e) => handleRepoChange('MaxDepth', e.target.value)} min="1" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Maximum number of pages to crawl concurrently">Max Parallel Tasks</Tooltip></label>
                <input type="number" value={form.Repository.MaxParallelTasks} onChange={(e) => handleRepoChange('MaxParallelTasks', e.target.value)} min="1" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Delay in milliseconds between consecutive requests to the same host">Crawl Delay (ms)</Tooltip></label>
                <input type="number" value={form.Repository.CrawlDelayMs} onChange={(e) => handleRepoChange('CrawlDelayMs', e.target.value)} min="0" step="100" />
              </div>
            </div>
          )}
        </div>

        {/* Schedule (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setScheduleOpen(prev => !prev)}>
            {scheduleOpen ? '\u25BE' : '\u25B8'} Schedule
          </button>
          {scheduleOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Unit of time for the crawl interval">Interval Type</Tooltip></label>
                <select value={form.Schedule.IntervalType} onChange={(e) => handleScheduleChange('IntervalType', e.target.value)}>
                  {INTERVAL_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label><Tooltip text="Number of interval units between crawls">Interval Value</Tooltip></label>
                <input type="number" value={form.Schedule.IntervalValue} onChange={(e) => handleScheduleChange('IntervalValue', e.target.value)} min="1" />
              </div>
            </div>
          )}
        </div>

        {/* Filter (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setFilterOpen(prev => !prev)}>
            {filterOpen ? '\u25BE' : '\u25B8'} Filter
          </button>
          {filterOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Only process objects whose key starts with this prefix">Object Prefix</Tooltip></label>
                <input type="text" value={form.Filter.ObjectPrefix} onChange={(e) => handleFilterChange('ObjectPrefix', e.target.value)} placeholder="Optional" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Only process objects whose key ends with this suffix">Object Suffix</Tooltip></label>
                <input type="text" value={form.Filter.ObjectSuffix} onChange={(e) => handleFilterChange('ObjectSuffix', e.target.value)} placeholder="Optional" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Comma-separated list of allowed MIME types (e.g. text/html, application/pdf)">Allowed Content Types</Tooltip></label>
                <input type="text" value={form.Filter.AllowedContentTypes} onChange={(e) => handleFilterChange('AllowedContentTypes', e.target.value)} placeholder="e.g. text/html, application/pdf" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Minimum file size in bytes to process">Minimum Size (bytes)</Tooltip></label>
                <input type="number" value={form.Filter.MinimumSize} onChange={(e) => handleFilterChange('MinimumSize', e.target.value)} min="0" placeholder="Optional" />
              </div>
              <div className="form-group">
                <label><Tooltip text="Maximum file size in bytes to process">Maximum Size (bytes)</Tooltip></label>
                <input type="number" value={form.Filter.MaximumSize} onChange={(e) => handleFilterChange('MaximumSize', e.target.value)} min="0" placeholder="Optional" />
              </div>
            </div>
          )}
        </div>

        {/* Processing (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setProcessingOpen(prev => !prev)}>
            {processingOpen ? '\u25BE' : '\u25B8'} Processing
          </button>
          {processingOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Processing.ProcessAdditions} onChange={(e) => handleProcessingChange('ProcessAdditions', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Process new files discovered during crawl">Process Additions</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Processing.ProcessUpdates} onChange={(e) => handleProcessingChange('ProcessUpdates', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Process files that have been modified since the last crawl">Process Updates</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <div className="form-toggle">
                  <label className="toggle-switch">
                    <input type="checkbox" checked={form.Processing.ProcessDeletions} onChange={(e) => handleProcessingChange('ProcessDeletions', e.target.checked)} />
                    <span className="toggle-slider"></span>
                  </label>
                  <span><Tooltip text="Remove documents that no longer exist at the source">Process Deletions</Tooltip></span>
                </div>
              </div>
              <div className="form-group">
                <label><Tooltip text="Maximum number of concurrent document processing tasks">Max Concurrent Tasks</Tooltip></label>
                <input type="number" value={form.Processing.MaxDrainTasks} onChange={(e) => handleProcessingChange('MaxDrainTasks', e.target.value)} min="1" />
              </div>
            </div>
          )}
        </div>

        {/* Retention (collapsible) */}
        <div className="form-group">
          <button type="button" style={collapsibleButtonStyle} onClick={() => setRetentionOpen(prev => !prev)}>
            {retentionOpen ? '\u25BE' : '\u25B8'} Retention
          </button>
          {retentionOpen && (
            <div style={{ marginTop: '0.5rem' }}>
              <div className="form-group">
                <label><Tooltip text="Number of days to retain crawl operation history. Leave empty for no limit.">Retention Days</Tooltip></label>
                <input type="number" value={form.RetentionDays} onChange={(e) => handleChange('RetentionDays', e.target.value)} min="1" placeholder="Optional" />
              </div>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
}

export default CrawlPlanFormModal;
