import React, { useMemo, useState, useEffect } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
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
  formatDuration,
  formatJson,
  formatNumber,
  getCollectionId,
  getCustomMetadata,
  getLabels,
  getRecordContent,
  getRecordId,
  getScore,
  getTags,
  labelRowsToList,
  parseListInput,
  tagRowsToObject,
  unwrapObjects,
  unwrapSearchResults,
} from '../utils/artifactSearch.jsx';

const createDefaultFilters = () => ({
  query: '',
  vector: '',
  searchType: 'TsRankCd',
  language: 'english',
  normalization: 32,
  maxResults: 25,
  minScore: '',
  requiredLabels: [''],
  excludedLabels: [''],
  requiredTags: [{ key: '', value: '' }],
  excludedTags: [{ key: '', value: '' }],
  requiredTerms: '',
  excludedTerms: '',
  createdAfter: '',
  createdBefore: '',
  documentId: '',
  includeNeighbors: '',
  continuationToken: '',
});

const collectionSearchTooltips = {
  clear: 'Reset all collection search fields, results, timing, and raw response data.',
  viewJson: 'Open the raw AssistantHub/RecallDB search response for the current result set.',
  collection: 'RecallDB collection to search. Ingested chunks are stored in the selected collection.',
  query: 'RecallDB full-text query. Example: quarterly revenue margin. Leave blank for vector-only or metadata-only searches.',
  searchType: 'RecallDB full-text search mode. WebSearch accepts web-style syntax; Phrase preserves word order; TsRankCd rewards dense term coverage.',
  language: 'Text search dictionary for stemming and stop words. Example: english. Use simple to avoid stemming.',
  normalization: 'PostgreSQL text ranking normalization bitmask passed to RecallDB. The default is 32.',
  maxResults: 'Maximum number of records requested from RecallDB before client-side filtering.',
  vector: 'Optional comma-separated embedding vector floats. Example: 0.012, -0.044, 0.087. Usually omitted unless testing a known vector.',
  continuationToken: 'Continuation token returned by RecallDB for the next result page. The Next Page button fills this automatically.',
  requiredLabels: 'Only include records carrying every listed label. Enter one label per row, such as finance and 2026.',
  excludedLabels: 'Exclude records carrying any listed label. Enter one label per row.',
  requiredTags: 'Only include records matching every key/value tag pair. Example: key department, value accounting.',
  excludedTags: 'Exclude records matching any listed key/value tag pair. Example: key status, value draft.',
  requiredTerms: 'Comma-separated terms that must appear in each matching record. Example: invoice, payment, due.',
  excludedTerms: 'Comma-separated terms that must not appear in matching records. Example: draft, obsolete, confidential.',
  createdAfter: 'Only include records created after this local date and time.',
  createdBefore: 'Only include records created before this local date and time.',
  documentId: 'Filter by RecallDB DocumentId, DocumentKey, record ID, or AssistantHub document metadata ID.',
  minScore: 'Client-side score floor. Leave blank to keep every result returned by RecallDB.',
  includeNeighbors: 'Number of adjacent records before and after each match to include. Use 0 or leave blank for no neighbors; valid range is 0-10.',
  search: 'Run the search against the selected RecallDB collection.',
  nextPage: 'Search again using the continuation token returned by RecallDB.',
};

function CollectionSearchView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const requestedCollection = searchParams.get('collectionId') || location.state?.collectionId || '';
  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState('');
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
        const result = await api.getCollections({ maxResults: 1000 });
        const items = unwrapObjects(result);
        setCollections(items);
        setSelectedCollection((current) => {
          if (requestedCollection && items.some((item) => getCollectionId(item) === requestedCollection)) return requestedCollection;
          if (!current && items.length === 1) return getCollectionId(items[0]);
          return current;
        });
      } catch (err) {
        setAlert({ title: 'Error', message: err.message || 'Failed to load collections' });
      }
    })();
  }, [serverUrl, credential, requestedCollection]);

  const filteredResults = useMemo(() => {
    const minScore = filters.minScore === '' ? null : Number(filters.minScore);
    const documentNeedle = filters.documentId.trim().toLowerCase();
    return results.filter((result) => {
      const score = getScore(result);
      if (minScore != null && Number.isFinite(minScore) && Number(score) < minScore) return false;
      if (documentNeedle) {
        const metadata = getCustomMetadata(result);
        const idText = [
          result.DocumentId,
          result.DocumentKey,
          getRecordId(result),
          metadata.AssistantHubDocumentId,
        ].filter(Boolean).join(' ').toLowerCase();
        if (!idText.includes(documentNeedle)) return false;
      }
      return true;
    });
  }, [results, filters.minScore, filters.documentId]);

  const buildSearchRequest = (activeFilters = filters) => {
    const request = {
      MaxResults: Number(activeFilters.maxResults),
    };
    const includeNeighbors = parseInt(activeFilters.includeNeighbors, 10);
    if (Number.isFinite(includeNeighbors) && includeNeighbors > 0) request.IncludeNeighbors = Math.min(10, includeNeighbors);
    if (activeFilters.continuationToken.trim()) request.ContinuationToken = activeFilters.continuationToken.trim();
    if (activeFilters.query.trim()) {
      request.FullText = {
        Query: activeFilters.query.trim(),
        SearchType: activeFilters.searchType,
        Language: activeFilters.language,
        Normalization: Number(activeFilters.normalization),
      };
    }
    const vector = parseListInput(activeFilters.vector).map(Number).filter((value) => Number.isFinite(value));
    if (vector.length > 0) request.Embeddings = vector;
    const requiredLabels = labelRowsToList(activeFilters.requiredLabels);
    const excludedLabels = labelRowsToList(activeFilters.excludedLabels);
    const requiredTags = tagRowsToObject(activeFilters.requiredTags);
    const excludedTags = tagRowsToObject(activeFilters.excludedTags);
    const requiredTerms = parseListInput(activeFilters.requiredTerms);
    const excludedTerms = parseListInput(activeFilters.excludedTerms);
    if (requiredLabels.length > 0) request.RequiredLabels = requiredLabels;
    if (excludedLabels.length > 0) request.ExcludedLabels = excludedLabels;
    if (Object.keys(requiredTags).length > 0) request.RequiredTags = requiredTags;
    if (Object.keys(excludedTags).length > 0) request.ExcludedTags = excludedTags;
    if (requiredTerms.length > 0) request.RequiredTerms = requiredTerms;
    if (excludedTerms.length > 0) request.ExcludedTerms = excludedTerms;
    if (activeFilters.createdAfter) request.CreatedAfter = activeFilters.createdAfter;
    if (activeFilters.createdBefore) request.CreatedBefore = activeFilters.createdBefore;
    if (activeFilters.documentId.trim()) request.DocumentId = activeFilters.documentId.trim();
    return request;
  };

  const runSearch = async (e, overrideFilters = null) => {
    if (e?.preventDefault) e.preventDefault();
    if (!selectedCollection) return;

    const activeFilters = overrideFilters || filters;
    setLoading(true);
    setError('');
    setSearched(true);
    const started = performance.now();
    try {
      const result = await api.searchCollection(selectedCollection, buildSearchRequest(activeFilters));
      setElapsedMs(Math.round(performance.now() - started));
      setResponseJson(result);
      setResults(unwrapSearchResults(result));
    } catch (err) {
      setElapsedMs(Math.round(performance.now() - started));
      setError(err.message || 'Search failed');
      setResponseJson(null);
      setResults([]);
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

  const nextContinuationToken =
    responseJson?.ContinuationToken ||
    responseJson?.NextContinuationToken ||
    responseJson?.Data?.ContinuationToken ||
    responseJson?.Data?.NextContinuationToken ||
    '';

  const resultTiming = responseJson?.ElapsedMs ?? responseJson?.Data?.ElapsedMs ?? responseJson?.DurationMs ?? elapsedMs;
  const getCollectionDocumentId = (result) => result?.DocumentId || result?.DocumentKey || '';
  const getCollectionRecordId = (result) => result?.GUID || result?.Id || '';
  const formatDateTime = (value) => {
    if (!value) return '';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Collection Search</h1>
          <p className="content-subtitle">Search RecallDB collection records through AssistantHub.</p>
        </div>
        <div className="artifact-header-actions">
          <button className="btn btn-secondary" onClick={clearSearch} title={collectionSearchTooltips.clear}>Clear</button>
          <button className="btn btn-secondary" onClick={() => setShowJson(true)} disabled={!responseJson} title={collectionSearchTooltips.viewJson}>View JSON</button>
        </div>
      </div>

      <form className="data-table-container artifact-form" onSubmit={runSearch}>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.collection}>Collection</Tooltip></label>
            <select title={collectionSearchTooltips.collection} value={selectedCollection} onChange={(e) => setSelectedCollection(e.target.value)}>
              <option value="">Select a collection...</option>
              {collections.map((collection) => <option key={getCollectionId(collection)} value={getCollectionId(collection)}>{collection.Name || getCollectionId(collection)}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.query}>Full Text Query</Tooltip></label>
            <input title={collectionSearchTooltips.query} value={filters.query} onChange={(e) => setFilters({ ...filters, query: e.target.value })} />
          </div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.searchType}>Search Type</Tooltip></label>
            <select title={collectionSearchTooltips.searchType} value={filters.searchType} onChange={(e) => setFilters({ ...filters, searchType: e.target.value })}>
              {['TsRankCd', 'TsRank', 'Plain', 'Phrase', 'WebSearch'].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.language}>Language</Tooltip></label>
            <input title={collectionSearchTooltips.language} value={filters.language} onChange={(e) => setFilters({ ...filters, language: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.normalization}>Normalization</Tooltip></label>
            <input title={collectionSearchTooltips.normalization} type="number" value={filters.normalization} onChange={(e) => setFilters({ ...filters, normalization: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.maxResults}>Max Results</Tooltip></label>
            <select title={collectionSearchTooltips.maxResults} value={filters.maxResults} onChange={(e) => setFilters({ ...filters, maxResults: e.target.value })}>
              {[10, 25, 50, 100, 250].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.vector}>Vector</Tooltip></label>
            <textarea title={collectionSearchTooltips.vector} value={filters.vector} onChange={(e) => setFilters({ ...filters, vector: e.target.value })} />
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.continuationToken}>Continuation Token</Tooltip></label>
            <input title={collectionSearchTooltips.continuationToken} value={filters.continuationToken} onChange={(e) => setFilters({ ...filters, continuationToken: e.target.value })} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.requiredLabels}>Required Labels</Tooltip></label>
            <LabelConstraintInput
              value={filters.requiredLabels}
              onChange={(requiredLabels) => setFilters({ ...filters, requiredLabels })}
              inputTitle={collectionSearchTooltips.requiredLabels}
              addTitle="Add another required label constraint."
              deleteTitle="Delete this required label constraint."
            />
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.excludedLabels}>Excluded Labels</Tooltip></label>
            <LabelConstraintInput
              value={filters.excludedLabels}
              onChange={(excludedLabels) => setFilters({ ...filters, excludedLabels })}
              inputTitle={collectionSearchTooltips.excludedLabels}
              addTitle="Add another excluded label constraint."
              deleteTitle="Delete this excluded label constraint."
            />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.requiredTags}>Required Tags</Tooltip></label>
            <TagConstraintInput
              value={filters.requiredTags}
              onChange={(requiredTags) => setFilters({ ...filters, requiredTags })}
              keyTitle="Required tag key. Example: department."
              valueTitle="Required tag value. Example: accounting."
              addTitle="Add another required tag constraint."
              deleteTitle="Delete this required tag constraint."
            />
          </div>
          <div className="form-group">
            <label><Tooltip text={collectionSearchTooltips.excludedTags}>Excluded Tags</Tooltip></label>
            <TagConstraintInput
              value={filters.excludedTags}
              onChange={(excludedTags) => setFilters({ ...filters, excludedTags })}
              keyTitle="Excluded tag key. Example: status."
              valueTitle="Excluded tag value. Example: draft."
              addTitle="Add another excluded tag constraint."
              deleteTitle="Delete this excluded tag constraint."
            />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.requiredTerms}>Required Terms</Tooltip></label><input title={collectionSearchTooltips.requiredTerms} value={filters.requiredTerms} onChange={(e) => setFilters({ ...filters, requiredTerms: e.target.value })} /></div>
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.excludedTerms}>Excluded Terms</Tooltip></label><input title={collectionSearchTooltips.excludedTerms} value={filters.excludedTerms} onChange={(e) => setFilters({ ...filters, excludedTerms: e.target.value })} /></div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.createdAfter}>Created After</Tooltip></label><input title={collectionSearchTooltips.createdAfter} type="datetime-local" value={filters.createdAfter} onChange={(e) => setFilters({ ...filters, createdAfter: e.target.value })} /></div>
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.createdBefore}>Created Before</Tooltip></label><input title={collectionSearchTooltips.createdBefore} type="datetime-local" value={filters.createdBefore} onChange={(e) => setFilters({ ...filters, createdBefore: e.target.value })} /></div>
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.documentId}>Document ID</Tooltip></label><input title={collectionSearchTooltips.documentId} value={filters.documentId} onChange={(e) => setFilters({ ...filters, documentId: e.target.value })} /></div>
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.minScore}>Minimum Score</Tooltip></label><input title={collectionSearchTooltips.minScore} type="number" step="0.0001" value={filters.minScore} onChange={(e) => setFilters({ ...filters, minScore: e.target.value })} /></div>
          <div className="form-group"><label><Tooltip text={collectionSearchTooltips.includeNeighbors}>Include Neighbors</Tooltip></label><input title={collectionSearchTooltips.includeNeighbors} type="number" min="0" max="10" value={filters.includeNeighbors} onChange={(e) => setFilters({ ...filters, includeNeighbors: e.target.value })} placeholder="0" /></div>
        </div>
        <div className="artifact-form-actions">
          <button className="btn btn-primary" type="submit" disabled={!selectedCollection || loading} title={collectionSearchTooltips.search}>{loading ? 'Searching...' : 'Search'}</button>
          {nextContinuationToken && (
            <button
              className="btn btn-secondary"
              type="button"
              title={collectionSearchTooltips.nextPage}
              onClick={() => {
                const nextFilters = { ...filters, continuationToken: nextContinuationToken };
                setFilters(nextFilters);
                runSearch(null, nextFilters);
              }}
            >
              Next Page
            </button>
          )}
        </div>
      </form>

      <div className="artifact-result-summary">
        <span>{filteredResults.length} result{filteredResults.length === 1 ? '' : 's'}</span>
        {resultTiming !== '' && resultTiming != null && <span>{formatDuration(resultTiming)}</span>}
        {nextContinuationToken && <span>More results available</span>}
      </div>

      <div className="data-table-container">
        {loading ? (
          <div className="loading"><div className="spinner" /></div>
        ) : error ? (
          <div className="empty-state error-state"><p>{error}</p></div>
        ) : !searched ? (
          <div className="empty-state"><p>No query submitted.</p></div>
        ) : filteredResults.length === 0 ? (
          <div className="empty-state"><p>No search results.</p></div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Document ID</th>
                <th>Score</th>
                <th>Text Score</th>
                <th>Content</th>
              </tr>
            </thead>
            <tbody>
              {filteredResults.map((result, idx) => {
                const content = String(getRecordContent(result) || '');
                return (
                  <tr key={result.GUID || result.Id || result.DocumentId || idx} onClick={() => setDetailTarget(result)} style={{ cursor: 'pointer' }}>
                    <td><CopyableId id={getCollectionDocumentId(result)} /></td>
                    <td><ScoreBar score={getScore(result)} /></td>
                    <td>{formatNumber(result.TextScore)}</td>
                    <td className="artifact-content-cell artifact-content-cell-truncate" title={content}>{content}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {detailTarget && (
        <Modal title="Collection Search Result" onClose={() => setDetailTarget(null)} extraWide>
          <div className="artifact-modal-field-rows">
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Record ID</div>
                <div className="artifact-field-value"><CopyableId id={getCollectionRecordId(detailTarget)} /></div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Document ID</div>
                <div className="artifact-field-value"><CopyableId id={getCollectionDocumentId(detailTarget)} /></div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Score</div>
                <div className="artifact-field-value"><ScoreBadge score={getScore(detailTarget)} /></div>
              </div>
            </div>
            <div className="artifact-modal-field-row three">
              <div className="artifact-field">
                <div className="artifact-field-label">Labels</div>
                <div className="artifact-field-value"><BadgeList values={getLabels(detailTarget)} /></div>
              </div>
              <div className="artifact-field">
                <div className="artifact-field-label">Tags</div>
                <div className="artifact-field-value"><TagBadges tags={getTags(detailTarget)} /></div>
              </div>
              <div className="artifact-field artifact-field-empty" aria-hidden="true"></div>
            </div>
            <div className="artifact-modal-field-row one">
              <div className="artifact-field">
                <div className="artifact-field-label">Created</div>
                <div className="artifact-field-value">{formatDateTime(detailTarget.CreatedUtc)}</div>
              </div>
            </div>
          </div>
          <div className="artifact-detail-section">
            <h4>Content</h4>
            <pre className="artifact-content-preview">{getRecordContent(detailTarget)}</pre>
          </div>
          <div className="artifact-detail-section">
            <div className="artifact-detail-section-header">
              <h4>Raw Result</h4>
              <CopyButton text={formatJson(detailTarget)} title="Copy raw result JSON" />
            </div>
            <pre className="json-view compact">{formatJson(detailTarget)}</pre>
          </div>
        </Modal>
      )}

      {showJson && responseJson && <JsonViewModal title="Collection Search JSON" data={responseJson} onClose={() => setShowJson(false)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default CollectionSearchView;
