import React from 'react';

export const unwrapObjects = (result) =>
  result?.Objects ||
  result?.Data?.Objects ||
  result?.Indices ||
  result?.Data?.Indices ||
  result?.Documents ||
  result?.Data?.Documents ||
  result?.Records ||
  result?.Data?.Records ||
  result?.Items ||
  result?.Data?.Items ||
  result?.Terms ||
  result?.Data?.Terms ||
  [];

export const unwrapSearchResults = (result) =>
  result?.Results ||
  result?.Data?.Results ||
  result?.Documents ||
  result?.Data?.Documents ||
  result?.Objects ||
  result?.Data?.Objects ||
  [];

export const getIndexId = (row) => row?.Identifier || row?.Id || row?.GUID || row?.Name || '';
export const getRecordId = (row) => row?.Id || row?.DocumentId || row?.DocumentKey || row?.GUID || row?.Identifier || '';
export const getCollectionId = (row) => row?.GUID || row?.Id || row?.Name || '';

export const getRecordEnvelope = (row) => row?.Document || row?.Record || row || {};

export const getRecordPath = (row) => {
  const record = getRecordEnvelope(row);
  return record?.DocumentPath || record?.DocumentKey || record?.Path || record?.Name || row?.DocumentPath || '';
};

export const getOriginalFileName = (row) => {
  const record = getRecordEnvelope(row);
  return record?.OriginalFileName || record?.OriginalFilename || record?.Filename || row?.OriginalFileName || '';
};

export const getDocumentLength = (row) => {
  const record = getRecordEnvelope(row);
  return record?.DocumentLength ?? record?.Length ?? row?.DocumentLength ?? null;
};

export const getContentLength = (row) => {
  const record = getRecordEnvelope(row);
  return record?.ContentLength ?? record?.ContentSize ?? record?.Length ?? row?.ContentLength ?? null;
};

export const getContentType = (row) => {
  const record = getRecordEnvelope(row);
  const metadata = getCustomMetadata(row);
  return record?.ContentType || record?.Type || row?.ContentType || metadata?.ContentType || '';
};

export const getIndexedDate = (row) => {
  const record = getRecordEnvelope(row);
  return record?.IndexedDate || record?.IndexedUtc || row?.IndexedDate || '';
};

export const getLastModified = (row) => {
  const record = getRecordEnvelope(row);
  return record?.LastModified || record?.LastModifiedUtc || row?.LastModified || '';
};

export const getContentSha256 = (row) => {
  const record = getRecordEnvelope(row);
  return record?.ContentSha256 || record?.ContentSHA256 || row?.ContentSha256 || '';
};

export const getRecordTerms = (row) => {
  const record = getRecordEnvelope(row);
  const value = record?.Terms || row?.Terms;
  return Array.isArray(value)
    ? value.map((term) => typeof term === 'string' ? term : term?.Term || term?.Text || JSON.stringify(term)).filter(Boolean)
    : [];
};

export const getTotalTerms = (row) => {
  const record = getRecordEnvelope(row);
  const stats = row?.DocumentTermStats || row?.Document?.DocumentTermStats || row?.Record?.DocumentTermStats;
  const value =
    stats?.UniqueTermCount ??
    record?.TermCount ??
    record?.TotalTerms ??
    record?.TotalTermCount ??
    row?.DocumentTermStats?.UniqueTermCount ??
    row?.TermCount ??
    row?.TotalTerms ??
    row?.TotalTermCount;
  if (value != null && value !== '') return value;
  const terms = getRecordTerms(row);
  return terms.length > 0 ? terms.length : null;
};

export const getTotalTermOccurrences = (row) => {
  const record = getRecordEnvelope(row);
  const stats = row?.DocumentTermStats || row?.Document?.DocumentTermStats || row?.Record?.DocumentTermStats;
  return stats?.TotalTermOccurrences ??
    record?.TotalTermOccurrences ??
    record?.TermOccurrences ??
    row?.DocumentTermStats?.TotalTermOccurrences ??
    row?.TotalTermOccurrences ??
    row?.TermOccurrences ??
    null;
};

export const getTermMatchCount = (row) =>
  row?.TotalTermMatches ??
  row?.MatchedTermCount ??
  row?.Document?.TotalTermMatches ??
  row?.Record?.TotalTermMatches ??
  row?.Document?.MatchedTermCount ??
  row?.Record?.MatchedTermCount ??
  null;

export const getIsDeleted = (row) => {
  const record = getRecordEnvelope(row);
  return Boolean(record?.IsDeleted ?? row?.IsDeleted);
};

export const getIndexingRuntimeMs = (row) => {
  const record = getRecordEnvelope(row);
  return record?.IndexingRuntimeMs ?? row?.IndexingRuntimeMs ?? null;
};

export const getRecordContent = (row) => {
  const record = getRecordEnvelope(row);
  return record?.Content || record?.Text || row?.Content || '';
};

export const getLabels = (row) => {
  const value = row?.Labels || row?.Document?.Labels || row?.Record?.Labels;
  return Array.isArray(value) ? value.filter(Boolean) : [];
};

export const getTags = (row) => {
  const value = row?.Tags || row?.Document?.Tags || row?.Record?.Tags;
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {};
};

export const getCustomMetadata = (row) =>
  row?.CustomMetadata || row?.Document?.CustomMetadata || row?.Record?.CustomMetadata || {};

export const parseListInput = (value) =>
  String(value || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);

export const parseTagsInput = (value) => {
  const tags = {};
  parseListInput(value).forEach((item) => {
    const [key, ...rest] = item.split('=');
    if (key && rest.length > 0) tags[key.trim()] = rest.join('=').trim();
  });
  return tags;
};

export const tagsToInput = (tags) =>
  Object.entries(tags || {})
    .map(([key, value]) => `${key}=${value}`)
    .join(', ');

const normalizeLabelRows = (value) => {
  const rows = Array.isArray(value) ? value.map((item) => String(item ?? '')) : [];
  return rows.length > 0 ? rows : [''];
};

const normalizeTagRows = (value) => {
  const rows = Array.isArray(value)
    ? value.map((item) => ({
      key: String(item?.key ?? ''),
      value: String(item?.value ?? ''),
    }))
    : [];
  return rows.length > 0 ? rows : [{ key: '', value: '' }];
};

export const labelRowsToList = (value) =>
  normalizeLabelRows(value)
    .map((item) => item.trim())
    .filter(Boolean);

export const tagRowsToObject = (value) => {
  const tags = {};
  normalizeTagRows(value).forEach((item) => {
    const key = item.key.trim();
    const tagValue = item.value.trim();
    if (key && tagValue) tags[key] = tagValue;
  });
  return tags;
};

export const parseJsonInput = (value, fallback) => {
  const trimmed = String(value || '').trim();
  if (!trimmed) return fallback;
  return JSON.parse(trimmed);
};

export const formatJson = (value) => JSON.stringify(value || {}, null, 2);

export const formatNumber = (value, digits = 4) =>
  typeof value === 'number' && Number.isFinite(value) ? value.toFixed(digits) : value ?? '';

export const formatDuration = (value) => {
  const ms = value ?? null;
  if (ms == null || ms === '') return '';
  if (typeof ms === 'number') return `${ms.toFixed(ms >= 10 ? 0 : 1)} ms`;
  return String(ms);
};

export const getScore = (result) =>
  result?.Score ??
  result?.TextScore ??
  result?.TfIdfScore ??
  result?.SimilarityScore ??
  result?.Document?.Score ??
  null;

export const getMatchedTerms = (result) => {
  const value = result?.MatchedTerms || result?.Terms || result?.TermMatches || result?.Data?.MatchedTerms;
  if (Array.isArray(value)) return value.map((term) => typeof term === 'string' ? term : term?.Term || term?.Text || JSON.stringify(term)).filter(Boolean);
  if (typeof value === 'string') return parseListInput(value);
  const termScores = result?.TermScores || result?.Document?.TermScores || result?.Record?.TermScores;
  if (termScores && typeof termScores === 'object' && !Array.isArray(termScores)) return Object.keys(termScores).filter(Boolean);
  const termFrequencies = result?.TermFrequencies || result?.Document?.TermFrequencies || result?.Record?.TermFrequencies;
  if (termFrequencies && typeof termFrequencies === 'object' && !Array.isArray(termFrequencies)) return Object.keys(termFrequencies).filter(Boolean);
  return [];
};

export const getTermDetails = (result) => {
  const explicitDetails = result?.TermDetails || result?.Document?.TermDetails || result?.Record?.TermDetails;
  if (Array.isArray(explicitDetails)) return explicitDetails.filter((item) => item && typeof item === 'object');

  const candidates = result?.TermScores || result?.TermFrequencies || result?.Terms || result?.MatchedTerms || [];
  if (Array.isArray(candidates)) return candidates.filter((item) => typeof item === 'object');

  const scores = result?.TermScores && typeof result.TermScores === 'object' && !Array.isArray(result.TermScores) ? result.TermScores : {};
  const frequencies = result?.TermFrequencies && typeof result.TermFrequencies === 'object' && !Array.isArray(result.TermFrequencies) ? result.TermFrequencies : {};
  const terms = Array.from(new Set([...Object.keys(scores), ...Object.keys(frequencies)])).filter(Boolean);
  return terms.map((term) => ({
    Term: term,
    Score: scores[term] ?? '',
    Frequency: frequencies[term] ?? '',
  }));
};

export function BadgeList({ values, empty = '' }) {
  if (!values || values.length < 1) return empty;
  return (
    <div className="artifact-chip-list">
      {values.map((value) => <span key={value} className="artifact-chip">{value}</span>)}
    </div>
  );
}

export function TagBadges({ tags }) {
  const entries = Object.entries(tags || {});
  if (entries.length < 1) return '';
  return (
    <div className="artifact-chip-list">
      {entries.map(([key, value]) => <span key={key} className="artifact-chip">{key}={String(value)}</span>)}
    </div>
  );
}

export function LabelConstraintInput({
  value,
  onChange,
  inputTitle = 'Label value. Enter one label per row.',
  addTitle = 'Add another label row.',
  deleteTitle = 'Delete this label row.',
}) {
  const rows = normalizeLabelRows(value);
  const updateRow = (index, nextValue) => onChange(rows.map((item, itemIndex) => itemIndex === index ? nextValue : item));
  const addRow = () => onChange([...rows, '']);
  const deleteRow = (index) => onChange(rows.length > 1 ? rows.filter((_, itemIndex) => itemIndex !== index) : ['']);

  return (
    <div className="artifact-constraint-list">
      {rows.map((item, index) => {
        const isLast = index === rows.length - 1;
        return (
          <div key={index} className="artifact-label-constraint-row">
            <input title={inputTitle} value={item} onChange={(e) => updateRow(index, e.target.value)} />
            {isLast ? (
              <button type="button" className="artifact-constraint-icon" title={addTitle} aria-label={addTitle} onClick={addRow}>+</button>
            ) : (
              <span className="artifact-constraint-spacer" aria-hidden="true" />
            )}
            <button type="button" className="artifact-constraint-icon danger" title={deleteTitle} aria-label={deleteTitle} onClick={() => deleteRow(index)}>&times;</button>
          </div>
        );
      })}
    </div>
  );
}

export function TagConstraintInput({
  value,
  onChange,
  keyTitle = 'Tag key.',
  valueTitle = 'Tag value.',
  addTitle = 'Add another tag row.',
  deleteTitle = 'Delete this tag row.',
}) {
  const rows = normalizeTagRows(value);
  const updateRow = (index, field, nextValue) => onChange(rows.map((item, itemIndex) => (
    itemIndex === index ? { ...item, [field]: nextValue } : item
  )));
  const addRow = () => onChange([...rows, { key: '', value: '' }]);
  const deleteRow = (index) => onChange(rows.length > 1 ? rows.filter((_, itemIndex) => itemIndex !== index) : [{ key: '', value: '' }]);

  return (
    <div className="artifact-constraint-list">
      {rows.map((item, index) => {
        const isLast = index === rows.length - 1;
        return (
          <div key={index} className="artifact-tag-constraint-row">
            <input title={keyTitle} value={item.key} onChange={(e) => updateRow(index, 'key', e.target.value)} placeholder="Key" />
            <input title={valueTitle} value={item.value} onChange={(e) => updateRow(index, 'value', e.target.value)} placeholder="Value" />
            {isLast ? (
              <button type="button" className="artifact-constraint-icon" title={addTitle} aria-label={addTitle} onClick={addRow}>+</button>
            ) : (
              <span className="artifact-constraint-spacer" aria-hidden="true" />
            )}
            <button type="button" className="artifact-constraint-icon danger" title={deleteTitle} aria-label={deleteTitle} onClick={() => deleteRow(index)}>&times;</button>
          </div>
        );
      })}
    </div>
  );
}

export function ScoreBar({ score }) {
  if (score == null || score === '') return '';
  const numeric = Number(score);
  const normalized = Number.isFinite(numeric) ? Math.max(0, Math.min(1, numeric > 1 ? numeric / 100 : numeric)) : 0;
  return (
    <div className="artifact-score">
      <span className="artifact-score-value">{formatNumber(score)}</span>
      <span className="artifact-score-track"><span style={{ width: `${Math.max(4, normalized * 100)}%` }} /></span>
    </div>
  );
}

export function ScoreBadge({ score }) {
  if (score == null || score === '') return '';
  return <span className="artifact-score-badge">{formatNumber(score)}</span>;
}

export function FieldGrid({ rows }) {
  const visibleRows = rows.filter((row) => row.value != null && row.value !== '');
  if (visibleRows.length < 1) return null;
  return (
    <div className="artifact-field-grid">
      {visibleRows.map((row) => (
        <div key={row.label} className="artifact-field">
          <div className="artifact-field-label">{row.label}</div>
          <div className="artifact-field-value">{row.value}</div>
        </div>
      ))}
    </div>
  );
}
