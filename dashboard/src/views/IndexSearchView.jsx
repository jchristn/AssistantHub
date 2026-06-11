import React, { useMemo, useState, useEffect } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyableId from '../components/CopyableId';
import CopyButton from '../components/CopyButton';
import Modal from '../components/Modal';
import JsonViewModal from '../components/modals/JsonViewModal';
import AlertModal from '../components/AlertModal';
import Tooltip from '../components/Tooltip';
import {
  BadgeList,
  LabelConstraintInput,
  ScoreBar,
  ScoreBadge,
  TagBadges,
  TagConstraintInput,
  getContentLength,
  getContentType,
  formatDuration,
  formatJson,
  formatNumber,
  getCustomMetadata,
  getDocumentLength,
  getContentSha256,
  getIndexedDate,
  getIndexingRuntimeMs,
  getIndexId,
  getLabels,
  getMatchedTerms,
  getRecordId,
  getScore,
  getTags,
  labelRowsToList,
  getTermDetails,
  getTermMatchCount,
  getTotalTermOccurrences,
  getTotalTerms,
  parseListInput,
  tagRowsToObject,
  unwrapObjects,
  unwrapSearchResults,
} from '../utils/artifactSearch.jsx';

const createDefaultFilters = () => ({
  query: '',
  useAndLogic: false,
  maxResults: 25,
  minScore: '',
  labels: [''],
  tags: [{ key: '', value: '' }],
  requiredTerms: '',
  excludedTerms: '',
  documentId: '',
});

const indexSearchTooltips = {
  clear: 'Reset all index search fields, results, timing, and raw response data.',
  viewJson: 'Open the raw AssistantHub/Verbex search response for the current result set.',
  index: 'Verbex index to search. Document ingestion writes extracted text to the default index unless an ingestion rule selects another index.',
  query: 'TF-IDF text query sent to Verbex. Example: quarterly revenue margin. Use * to match all records, then narrow results with labels, tags, or terms.',
  labels: 'Only include records carrying every listed label. Enter one label per row, such as finance and 2026.',
  tags: 'Only include records matching every key/value tag pair. Example: key department, value accounting.',
  requiredTerms: 'Comma-separated terms that must appear in each matching document. Example: invoice, payment, due.',
  excludedTerms: 'Comma-separated terms that must not appear in matching documents. Example: draft, obsolete, confidential.',
  documentId: 'Client-side filter for returned Verbex document IDs, AssistantHub document IDs, or source identifiers.',
  minScore: 'Client-side score floor. Leave blank to keep every result returned by Verbex.',
  maxResults: 'Maximum number of results requested from Verbex before client-side filtering.',
  useAndLogic: 'Require every query term to match instead of accepting records that match any query term.',
  search: 'Run the search against the selected Verbex index.',
};

const integerFormatter = new Intl.NumberFormat();

const formatInteger = (value) => {
  if (value == null || value === '') return '';
  const numeric = Number(value);
  return Number.isFinite(numeric) ? integerFormatter.format(numeric) : String(value);
};

const getIndexRecordLength = (result) => {
  const record = result?.Document || result?.Record || result;
  return getDocumentLength(record) ?? getContentLength(record) ?? getDocumentLength(result) ?? getContentLength(result);
};

const getIndexTermMatches = (result) => {
  const record = result?.Document || result?.Record || result;
  return getTermMatchCount(result) ?? getTermMatchCount(record);
};

const formatDateTime = (value) => {
  if (!value) return '';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
};

const getAssistantHubDocumentId = (record) => {
  const metadata = getCustomMetadata(record);
  return metadata.AssistantHubDocumentId || metadata.DocumentId || '';
};

const renderMatchedTermsSummary = (terms, fallback = '') => {
  if (!terms || terms.length < 1) return fallback;
  const visibleTerms = terms.slice(0, 3);
  const hiddenCount = terms.length - visibleTerms.length;
  return (
    <div className="artifact-chip-list artifact-chip-list-compact" title={terms.join(', ')}>
      {visibleTerms.map((term) => <span key={term} className="artifact-chip">{term}</span>)}
      {hiddenCount > 0 && <span className="artifact-chip artifact-more-chip">+{hiddenCount}</span>}
    </div>
  );
};

function IndexSearchView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const location = useLocation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const requestedIndex = searchParams.get('indexId') || location.state?.indexId || '';
  const [indices, setIndices] = useState([]);
  const [selectedIndex, setSelectedIndex] = useState('');
  const [filters, setFilters] = useState(() => createDefaultFilters());
  const [results, setResults] = useState([]);
  const [responseJson, setResponseJson] = useState(null);
  const [detailTarget, setDetailTarget] = useState(null);
  const [showJson, setShowJson] = useState(false);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [elapsedMs, setElapsedMs] = useState(null);
  const [error, setError] = useState('');
  const [alert, setAlert] = useState(null);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getIndices({ maxResults: 1000 });
        const items = unwrapObjects(result);
        setIndices(items);
        setSelectedIndex((current) => {
          if (requestedIndex && items.some((item) => getIndexId(item) === requestedIndex)) return requestedIndex;
          if (!current && items.length === 1) return getIndexId(items[0]);
          return current;
        });
      } catch (err) {
        setAlert({ title: 'Error', message: err.message || 'Failed to load indices' });
      }
    })();
  }, [serverUrl, credential, requestedIndex]);

  const filteredResults = useMemo(() => {
    const minScore = filters.minScore === '' ? null : Number(filters.minScore);
    const documentNeedle = filters.documentId.trim().toLowerCase();
    return results.filter((result) => {
      const score = getScore(result);
      if (minScore != null && Number.isFinite(minScore) && Number(score) < minScore) return false;
      if (documentNeedle) {
        const record = result.Document || result.Record || result;
        const metadata = getCustomMetadata(record);
        const idText = [
          result.DocumentId,
          getRecordId(record),
          metadata.AssistantHubDocumentId,
        ].filter(Boolean).join(' ').toLowerCase();
        if (!idText.includes(documentNeedle)) return false;
      }
      return true;
    });
  }, [results, filters.minScore, filters.documentId]);

  const runSearch = async (e) => {
    e.preventDefault();
    if (!selectedIndex) return;

    setLoading(true);
    setError('');
    setSearched(true);
    const started = performance.now();
    try {
      const request = {
        Query: filters.query.trim() || '*',
        MaxResults: Number(filters.maxResults),
        UseAndLogic: filters.useAndLogic,
        IncludeMatchedTerms: true,
        IncludeTermDetails: true,
        IncludeDocumentTermStats: true,
      };
      const labels = labelRowsToList(filters.labels);
      const tags = tagRowsToObject(filters.tags);
      const requiredTerms = parseListInput(filters.requiredTerms);
      const excludedTerms = parseListInput(filters.excludedTerms);
      if (labels.length > 0) request.Labels = labels;
      if (Object.keys(tags).length > 0) request.Tags = tags;
      if (requiredTerms.length > 0) request.RequiredTerms = requiredTerms;
      if (excludedTerms.length > 0) request.ExcludedTerms = excludedTerms;

      const result = await api.searchIndex(selectedIndex, request);
      setElapsedMs(Math.round(performance.now() - started));
      setResponseJson(result);
      setResults(unwrapSearchResults(result));
    } catch (err) {
      setElapsedMs(Math.round(performance.now() - started));
      setError(err.message || 'Search failed');
      setResults([]);
      setResponseJson(null);
    } finally {
      setLoading(false);
    }
  };

  const clearSearch = () => {
    setFilters(createDefaultFilters());
    setResults([]);
    setResponseJson(null);
    setElapsedMs(null);
    setError('');
    setSearched(false);
  };

  const openDocumentInIngestion = (documentId) => {
    if (!documentId) return;
    setDetailTarget(null);
    navigate(`/documents?documentId=${encodeURIComponent(documentId)}`);
  };

  const resultTiming = responseJson?.ElapsedMs ?? responseJson?.Data?.ElapsedMs ?? responseJson?.DurationMs ?? elapsedMs;

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Index Search</h1>
          <p className="content-subtitle">Search Verbex inverted indices with text and TF-IDF scoring.</p>
        </div>
        <div className="artifact-header-actions">
          <button className="btn btn-secondary" onClick={clearSearch} title={indexSearchTooltips.clear}>Clear</button>
          <button className="btn btn-secondary" onClick={() => setShowJson(true)} disabled={!responseJson} title={indexSearchTooltips.viewJson}>View JSON</button>
        </div>
      </div>

      <form className="data-table-container artifact-form" onSubmit={runSearch}>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.index}>Index</Tooltip></label>
            <select title={indexSearchTooltips.index} value={selectedIndex} onChange={(e) => setSelectedIndex(e.target.value)}>
              <option value="">Select an index...</option>
              {indices.map((index) => <option key={getIndexId(index)} value={getIndexId(index)}>{index.Name || getIndexId(index)}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.query}>Query</Tooltip></label>
            <input title={indexSearchTooltips.query} value={filters.query} onChange={(e) => setFilters({ ...filters, query: e.target.value })} placeholder="Search terms or *" />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.labels}>Labels</Tooltip></label>
            <LabelConstraintInput
              value={filters.labels}
              onChange={(labels) => setFilters({ ...filters, labels })}
              inputTitle={indexSearchTooltips.labels}
              addTitle="Add another required label constraint."
              deleteTitle="Delete this label constraint."
            />
          </div>
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.tags}>Tags</Tooltip></label>
            <TagConstraintInput
              value={filters.tags}
              onChange={(tags) => setFilters({ ...filters, tags })}
              keyTitle="Required tag key. Example: department."
              valueTitle="Required tag value. Example: accounting."
              addTitle="Add another required tag constraint."
              deleteTitle="Delete this tag constraint."
            />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.requiredTerms}>Required Terms</Tooltip></label>
            <input title={indexSearchTooltips.requiredTerms} value={filters.requiredTerms} onChange={(e) => setFilters({ ...filters, requiredTerms: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.excludedTerms}>Excluded Terms</Tooltip></label>
            <input title={indexSearchTooltips.excludedTerms} value={filters.excludedTerms} onChange={(e) => setFilters({ ...filters, excludedTerms: e.target.value })} />
          </div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.documentId}>Document ID</Tooltip></label>
            <input title={indexSearchTooltips.documentId} value={filters.documentId} onChange={(e) => setFilters({ ...filters, documentId: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.minScore}>Minimum Score</Tooltip></label>
            <input title={indexSearchTooltips.minScore} type="number" step="0.0001" value={filters.minScore} onChange={(e) => setFilters({ ...filters, minScore: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={indexSearchTooltips.maxResults}>Max Results</Tooltip></label>
            <select title={indexSearchTooltips.maxResults} value={filters.maxResults} onChange={(e) => setFilters({ ...filters, maxResults: e.target.value })}>
              {[10, 25, 50, 100, 250].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <label className="form-toggle artifact-toggle" title={indexSearchTooltips.useAndLogic}>
            <input title={indexSearchTooltips.useAndLogic} type="checkbox" checked={filters.useAndLogic} onChange={(e) => setFilters({ ...filters, useAndLogic: e.target.checked })} />
            <Tooltip text={indexSearchTooltips.useAndLogic}><span>Require all terms</span></Tooltip>
          </label>
        </div>
        <div className="artifact-form-actions">
          <button className="btn btn-primary" type="submit" disabled={!selectedIndex || loading} title={indexSearchTooltips.search}>{loading ? 'Searching...' : 'Search'}</button>
        </div>
      </form>

      <div className="artifact-result-summary">
        <span>{filteredResults.length} result{filteredResults.length === 1 ? '' : 's'}</span>
        {resultTiming !== '' && resultTiming != null && <span>{formatDuration(resultTiming)}</span>}
      </div>

      <div className="data-table-container artifact-search-results-container">
        {loading ? (
          <div className="loading"><div className="spinner" /></div>
        ) : error ? (
          <div className="empty-state error-state"><p>{error}</p></div>
        ) : !searched ? (
          <div className="empty-state"><p>No query submitted.</p></div>
        ) : filteredResults.length === 0 ? (
          <div className="empty-state"><p>No search results.</p></div>
        ) : (
          <table className="data-table artifact-index-search-table">
            <thead>
              <tr>
                <th title="Verbex record identifier.">Record</th>
                <th title="Verbex TF-IDF relevance score.">Score</th>
                <th title="Query terms matched by this document.">Matched Terms</th>
                <th title="Indexed document length in characters.">Length</th>
                <th title="Number of unique indexed terms in the whole document.">Unique Terms</th>
                <th title="Total indexed term occurrences in the whole document.">Term Occurrences</th>
                <th title="Total occurrences of matched query terms.">Term Matches</th>
                <th title="Content type carried through Verbex custom metadata.">Content Type</th>
              </tr>
            </thead>
            <tbody>
              {filteredResults.map((result, idx) => {
                const record = result.Document || result.Record || result;
                const recordId = result.DocumentId || getRecordId(record);
                const matchedTerms = getMatchedTerms(result);
                return (
                  <tr key={recordId || idx} onClick={() => setDetailTarget(result)} style={{ cursor: 'pointer' }}>
                    <td><CopyableId id={recordId || ''} /></td>
                    <td><ScoreBar score={getScore(result)} /></td>
                    <td>{renderMatchedTermsSummary(matchedTerms, result.MatchedTermCount ?? '')}</td>
                    <td className="artifact-number-cell">{formatInteger(getIndexRecordLength(result))}</td>
                    <td className="artifact-number-cell">{formatInteger(getTotalTerms(result))}</td>
                    <td className="artifact-number-cell">{formatInteger(getTotalTermOccurrences(result))}</td>
                    <td className="artifact-number-cell">{formatInteger(getIndexTermMatches(result))}</td>
                    <td>{getContentType(record) || getContentType(result)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {detailTarget && (
        <Modal title="Index Search Result" onClose={() => setDetailTarget(null)} extraWide className="index-search-result-modal">
          {(() => {
            const record = detailTarget.Document || detailTarget.Record || detailTarget;
            const metadata = getCustomMetadata(record);
            const termDetails = getTermDetails(detailTarget);
            const matchedTerms = getMatchedTerms(detailTarget);
            const assistantHubDocumentId = getAssistantHubDocumentId(record);
            const recordId = detailTarget.DocumentId || getRecordId(record);
            const uniqueTerms = getTotalTerms(detailTarget);
            const termOccurrences = getTotalTermOccurrences(detailTarget);
            const termMatches = getIndexTermMatches(detailTarget);
            const contentType = getContentType(record) || getContentType(detailTarget);
            const indexedDate = getIndexedDate(record) || getIndexedDate(detailTarget);
            const indexingRuntime = getIndexingRuntimeMs(record) ?? getIndexingRuntimeMs(detailTarget);
            const contentSha256 = getContentSha256(record) || getContentSha256(detailTarget);
            const metadataJson = formatJson(metadata);
            const rawResultJson = formatJson(detailTarget);
            return (
              <>
                <div className="artifact-modal-field-rows">
                  <div className="artifact-modal-field-row three">
                    <div className="artifact-field">
                      <div className="artifact-field-label">Record ID</div>
                      <div className="artifact-field-value"><CopyableId id={recordId} /></div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Document ID</div>
                      <div className="artifact-field-value artifact-linked-id">
                        {assistantHubDocumentId ? (
                          <>
                            <CopyableId id={assistantHubDocumentId} />
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() => openDocumentInIngestion(assistantHubDocumentId)}
                              title="Open this document in Ingestion > Documents"
                            >
                              View in Documents
                            </button>
                          </>
                        ) : ''}
                      </div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Score</div>
                      <div className="artifact-field-value"><ScoreBadge score={getScore(detailTarget)} /></div>
                    </div>
                  </div>
                  <div className="artifact-modal-field-row three">
                    <div className="artifact-field">
                      <div className="artifact-field-label">Matched Term Count</div>
                      <div className="artifact-field-value">{formatInteger(detailTarget.MatchedTermCount)}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Total Term Matches</div>
                      <div className="artifact-field-value">{formatInteger(termMatches)}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Unique Terms</div>
                      <div className="artifact-field-value">{formatInteger(uniqueTerms)}</div>
                    </div>
                  </div>
                  <div className="artifact-modal-field-row three">
                    <div className="artifact-field">
                      <div className="artifact-field-label">Term Occurrences</div>
                      <div className="artifact-field-value">{formatInteger(termOccurrences)}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Document Length</div>
                      <div className="artifact-field-value">{formatInteger(getIndexRecordLength(detailTarget))}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Content Type</div>
                      <div className="artifact-field-value">{contentType}</div>
                    </div>
                  </div>
                  <div className="artifact-modal-field-row three">
                    <div className="artifact-field">
                      <div className="artifact-field-label">Indexed</div>
                      <div className="artifact-field-value">{formatDateTime(indexedDate)}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Indexing Runtime</div>
                      <div className="artifact-field-value">{formatDuration(indexingRuntime)}</div>
                    </div>
                    <div className="artifact-field">
                      <div className="artifact-field-label">Content SHA-256</div>
                      <div className="artifact-field-value">{contentSha256 ? <CopyableId id={contentSha256} /> : ''}</div>
                    </div>
                  </div>
                </div>
                <div className="artifact-detail-section">
                  <h4>Matched Terms</h4>
                  {matchedTerms.length > 0 ? <BadgeList values={matchedTerms} /> : <span className="text-muted">{detailTarget.MatchedTermCount ?? ''}</span>}
                </div>
                {termDetails.length > 0 && (
                  <div className="artifact-detail-section">
                    <h4>Term Details</h4>
                    <table className="data-table">
                      <thead><tr><th>Term</th><th>Score</th><th>Frequency</th></tr></thead>
                      <tbody>
                        {termDetails.map((term, index) => (
                          <tr key={term.Term || term.Text || index}>
                            <td>{term.Term || term.Text || term.Value || ''}</td>
                            <td><ScoreBadge score={term.Score ?? term.Weight ?? ''} /></td>
                            <td>{formatInteger(term.Frequency ?? term.Count ?? '')}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
                <div className="artifact-detail-section">
                  <h4>Labels</h4>
                  <BadgeList values={getLabels(record)} empty={<span className="text-muted">No labels</span>} />
                </div>
                <div className="artifact-detail-section">
                  <h4>Tags</h4>
                  {Object.keys(getTags(record)).length > 0 ? <TagBadges tags={getTags(record)} /> : <span className="text-muted">No tags</span>}
                </div>
                <div className="artifact-detail-section">
                  <div className="artifact-detail-section-header">
                    <h4>Custom Metadata</h4>
                    <CopyButton text={metadataJson} title="Copy custom metadata JSON" />
                  </div>
                  <pre className="json-view compact">{metadataJson}</pre>
                </div>
                <div className="artifact-detail-section">
                  <div className="artifact-detail-section-header">
                    <h4>Raw Result</h4>
                    <CopyButton text={rawResultJson} title="Copy raw result JSON" />
                  </div>
                  <pre className="json-view compact">{rawResultJson}</pre>
                </div>
              </>
            );
          })()}
        </Modal>
      )}

      {showJson && responseJson && <JsonViewModal title="Index Search JSON" data={responseJson} onClose={() => setShowJson(false)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default IndexSearchView;
