import React, { useState } from 'react';
import Modal from '../Modal';
import Tooltip from '../Tooltip';

const CONDITIONS = [
  'Equals', 'NotEquals', 'Contains', 'StartsWith', 'EndsWith',
  'GreaterThan', 'LessThan', 'IsNull', 'IsNotNull'
];

function parsePage(value) {
  const parsed = parseInt(value);
  return Number.isNaN(parsed) || parsed < 1 ? null : parsed;
}

function MetadataFilterModal({ filter, availableLabels, availableTags, onApply, onClose }) {
  const [requiredLabels, setRequiredLabels] = useState(filter?.required_labels || []);
  const [excludedLabels, setExcludedLabels] = useState(filter?.excluded_labels || []);
  const [requiredTags, setRequiredTags] = useState(filter?.required_tags || []);
  const [excludedTags, setExcludedTags] = useState(filter?.excluded_tags || []);
  const [pageStart, setPageStart] = useState(filter?.page_start ?? '');
  const [pageEnd, setPageEnd] = useState(filter?.page_end ?? '');
  const [customLabel, setCustomLabel] = useState('');

  const toggleLabel = (label, list, setList) => {
    setList(prev => prev.includes(label) ? prev.filter(l => l !== label) : [...prev, label]);
  };

  const addCustomLabel = (list, setList) => {
    const val = customLabel.trim();
    if (val && !list.includes(val)) {
      setList(prev => [...prev, val]);
    }
    setCustomLabel('');
  };

  const updateTag = (list, setList, index, field, value) => {
    setList(prev => prev.map((t, i) => i === index ? { ...t, [field]: value } : t));
  };

  const addTag = (setList) => {
    setList(prev => [...prev, { Key: '', Condition: 'Equals', Value: '' }]);
  };

  const removeTag = (list, setList, index) => {
    setList(prev => prev.filter((_, i) => i !== index));
  };

  const clearAll = () => {
    setRequiredLabels([]);
    setExcludedLabels([]);
    setRequiredTags([]);
    setExcludedTags([]);
    setPageStart('');
    setPageEnd('');
  };

  const handleApply = () => {
    const hasLabels = requiredLabels.length > 0 || excludedLabels.length > 0;
    const hasReqTags = requiredTags.some(t => t.Key);
    const hasExclTags = excludedTags.some(t => t.Key);
    const firstPage = parsePage(pageStart);
    const lastPage = parsePage(pageEnd);
    const hasPages = firstPage !== null || lastPage !== null;

    if (!hasLabels && !hasReqTags && !hasExclTags && !hasPages) {
      onApply(null);
      return;
    }

    const result = {};
    if (requiredLabels.length > 0) result.required_labels = requiredLabels;
    if (excludedLabels.length > 0) result.excluded_labels = excludedLabels;
    if (hasReqTags) result.required_tags = requiredTags.filter(t => t.Key);
    if (hasExclTags) result.excluded_tags = excludedTags.filter(t => t.Key);
    if (firstPage !== null) result.page_start = firstPage;
    if (lastPage !== null) result.page_end = lastPage !== null && firstPage !== null && lastPage < firstPage ? firstPage : lastPage;
    onApply(result);
  };

  const allLabels = [...new Set([...availableLabels, ...requiredLabels, ...excludedLabels])];
  const knownTagKeys = [...new Set(availableTags)];

  const renderLabelSection = (title, selected, setSelected) => (
    <div className="form-group">
      <label>{title}</label>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.375rem', marginBottom: '0.5rem' }}>
        {allLabels.map(label => (
          <label key={label} style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.85rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={selected.includes(label)}
              onChange={() => toggleLabel(label, selected, setSelected)}
            />
            {label}
          </label>
        ))}
        {allLabels.length === 0 && <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No labels available</span>}
      </div>
      <div style={{ display: 'flex', gap: '0.375rem' }}>
        <input
          type="text"
          className="form-input"
          placeholder="Custom label..."
          value={customLabel}
          onChange={e => setCustomLabel(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addCustomLabel(selected, setSelected); } }}
          style={{ flex: 1, fontSize: '0.85rem' }}
        />
        <button type="button" className="btn btn-secondary" style={{ fontSize: '0.8rem', padding: '0.25rem 0.75rem' }}
          onClick={() => addCustomLabel(selected, setSelected)}>Add</button>
      </div>
    </div>
  );

  const renderTagSection = (title, tags, setTags) => (
    <div className="form-group">
      <label>{title}</label>
      {tags.map((tag, i) => (
        <div key={i} style={{ display: 'flex', gap: '0.375rem', marginBottom: '0.375rem', alignItems: 'center' }}>
          <input
            type="text"
            className="form-input"
            placeholder="Key"
            value={tag.Key}
            onChange={e => updateTag(tags, setTags, i, 'Key', e.target.value)}
            list={`tag-keys-${title}`}
            style={{ flex: 1, fontSize: '0.85rem' }}
          />
          <select
            className="form-input"
            value={tag.Condition}
            onChange={e => updateTag(tags, setTags, i, 'Condition', e.target.value)}
            style={{ flex: 1, fontSize: '0.85rem' }}
          >
            {CONDITIONS.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
          {tag.Condition !== 'IsNull' && tag.Condition !== 'IsNotNull' && (
            <input
              type="text"
              className="form-input"
              placeholder="Value"
              value={tag.Value || ''}
              onChange={e => updateTag(tags, setTags, i, 'Value', e.target.value)}
              style={{ flex: 1, fontSize: '0.85rem' }}
            />
          )}
          <button type="button" className="btn btn-danger" style={{ fontSize: '0.8rem', padding: '0.25rem 0.5rem', flexShrink: 0 }}
            onClick={() => removeTag(tags, setTags, i)}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>
      ))}
      {knownTagKeys.length > 0 && <datalist id={`tag-keys-${title}`}>{knownTagKeys.map(k => <option key={k} value={k}/>)}</datalist>}
      <button type="button" className="btn btn-secondary" style={{ fontSize: '0.8rem', padding: '0.25rem 0.75rem' }}
        onClick={() => addTag(setTags)}>+ Add Tag</button>
    </div>
  );

  return (
    <Modal title="Metadata Filters" onClose={onClose} footer={
      <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%' }}>
        <button className="btn btn-secondary" onClick={clearAll}>Clear All</button>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button className="btn btn-primary" onClick={handleApply}>Apply</button>
        </div>
      </div>
    }>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        <h4 style={{ margin: 0, fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Labels</h4>
        {renderLabelSection('Required Labels', requiredLabels, setRequiredLabels)}
        {renderLabelSection('Excluded Labels', excludedLabels, setExcludedLabels)}

        <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)', margin: '0.25rem 0' }} />

        <h4 style={{ margin: 0, fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Tags</h4>
        {renderTagSection('Required Tags', requiredTags, setRequiredTags)}
        {renderTagSection('Excluded Tags', excludedTags, setExcludedTags)}

        <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)', margin: '0.25rem 0' }} />

        <h4 style={{ margin: 0, fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Pages</h4>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text="Only use passages from this page onward. Needs documents with page numbers (for example PDF, Word or PowerPoint).">From Page</Tooltip></label>
            <input type="number" className="form-input" min="1" title="First page to search (inclusive)." value={pageStart} onChange={e => setPageStart(e.target.value)} placeholder="Any" />
          </div>
          <div className="form-group">
            <label><Tooltip text="Only use passages up to this page. Passages without page numbers are excluded when a page range is set.">To Page</Tooltip></label>
            <input type="number" className="form-input" min="1" title="Last page to search (inclusive)." value={pageEnd} onChange={e => setPageEnd(e.target.value)} placeholder="Any" />
          </div>
        </div>
      </div>
    </Modal>
  );
}

export default MetadataFilterModal;
