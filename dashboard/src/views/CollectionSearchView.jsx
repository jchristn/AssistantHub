import React, { useMemo, useState, useEffect } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import CopyableId from '../components/CopyableId';
import Modal from '../components/Modal';
import JsonViewModal from '../components/modals/JsonViewModal';
import AlertModal from '../components/AlertModal';
import {
  BadgeList,
  FieldGrid,
  LabelConstraintInput,
  ScoreBar,
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
  includeNeighbors: false,
  continuationToken: '',
});

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
      IncludeNeighbors: activeFilters.includeNeighbors,
    };
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

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Collection Search</h1>
          <p className="content-subtitle">Search RecallDB collection records through AssistantHub.</p>
        </div>
        <div className="artifact-header-actions">
          <button className="btn btn-secondary" onClick={clearSearch}>Clear</button>
          <button className="btn btn-secondary" onClick={() => setShowJson(true)} disabled={!responseJson}>View JSON</button>
        </div>
      </div>

      <form className="data-table-container artifact-form" onSubmit={runSearch}>
        <div className="form-row">
          <div className="form-group">
            <label>Collection</label>
            <select value={selectedCollection} onChange={(e) => setSelectedCollection(e.target.value)}>
              <option value="">Select a collection...</option>
              {collections.map((collection) => <option key={getCollectionId(collection)} value={getCollectionId(collection)}>{collection.Name || getCollectionId(collection)}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label>Full Text Query</label>
            <input value={filters.query} onChange={(e) => setFilters({ ...filters, query: e.target.value })} />
          </div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group">
            <label>Search Type</label>
            <select value={filters.searchType} onChange={(e) => setFilters({ ...filters, searchType: e.target.value })}>
              {['TsRankCd', 'TsRank', 'Plain', 'Phrase', 'WebSearch'].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label>Language</label>
            <input value={filters.language} onChange={(e) => setFilters({ ...filters, language: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Normalization</label>
            <input type="number" value={filters.normalization} onChange={(e) => setFilters({ ...filters, normalization: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Max Results</label>
            <select value={filters.maxResults} onChange={(e) => setFilters({ ...filters, maxResults: e.target.value })}>
              {[10, 25, 50, 100, 250].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>Vector</label>
            <textarea value={filters.vector} onChange={(e) => setFilters({ ...filters, vector: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Continuation Token</label>
            <input value={filters.continuationToken} onChange={(e) => setFilters({ ...filters, continuationToken: e.target.value })} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>Required Labels</label>
            <LabelConstraintInput value={filters.requiredLabels} onChange={(requiredLabels) => setFilters({ ...filters, requiredLabels })} />
          </div>
          <div className="form-group">
            <label>Excluded Labels</label>
            <LabelConstraintInput value={filters.excludedLabels} onChange={(excludedLabels) => setFilters({ ...filters, excludedLabels })} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>Required Tags</label>
            <TagConstraintInput value={filters.requiredTags} onChange={(requiredTags) => setFilters({ ...filters, requiredTags })} />
          </div>
          <div className="form-group">
            <label>Excluded Tags</label>
            <TagConstraintInput value={filters.excludedTags} onChange={(excludedTags) => setFilters({ ...filters, excludedTags })} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group"><label>Required Terms</label><input value={filters.requiredTerms} onChange={(e) => setFilters({ ...filters, requiredTerms: e.target.value })} /></div>
          <div className="form-group"><label>Excluded Terms</label><input value={filters.excludedTerms} onChange={(e) => setFilters({ ...filters, excludedTerms: e.target.value })} /></div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group"><label>Created After</label><input type="datetime-local" value={filters.createdAfter} onChange={(e) => setFilters({ ...filters, createdAfter: e.target.value })} /></div>
          <div className="form-group"><label>Created Before</label><input type="datetime-local" value={filters.createdBefore} onChange={(e) => setFilters({ ...filters, createdBefore: e.target.value })} /></div>
          <div className="form-group"><label>Document ID</label><input value={filters.documentId} onChange={(e) => setFilters({ ...filters, documentId: e.target.value })} /></div>
          <div className="form-group"><label>Minimum Score</label><input type="number" step="0.0001" value={filters.minScore} onChange={(e) => setFilters({ ...filters, minScore: e.target.value })} /></div>
        </div>
        <label className="form-toggle artifact-toggle">
          <input type="checkbox" checked={filters.includeNeighbors} onChange={(e) => setFilters({ ...filters, includeNeighbors: e.target.checked })} />
          <span>Include neighbors</span>
        </label>
        <div className="artifact-form-actions">
          <button className="btn btn-primary" type="submit" disabled={!selectedCollection || loading}>{loading ? 'Searching...' : 'Search'}</button>
          {nextContinuationToken && (
            <button
              className="btn btn-secondary"
              type="button"
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
                <th>Record</th>
                <th>Document</th>
                <th>Score</th>
                <th>Text Score</th>
                <th>Labels</th>
                <th>Tags</th>
                <th>Content</th>
              </tr>
            </thead>
            <tbody>
              {filteredResults.map((result, idx) => (
                <tr key={result.GUID || result.Id || result.DocumentId || idx} onClick={() => setDetailTarget(result)} style={{ cursor: 'pointer' }}>
                  <td><CopyableId id={result.GUID || result.Id || ''} /></td>
                  <td>{result.DocumentId || result.DocumentKey || ''}</td>
                  <td><ScoreBar score={getScore(result)} /></td>
                  <td>{formatNumber(result.TextScore)}</td>
                  <td><BadgeList values={getLabels(result)} /></td>
                  <td><TagBadges tags={getTags(result)} /></td>
                  <td className="artifact-content-cell">{String(getRecordContent(result)).slice(0, 240)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {detailTarget && (
        <Modal title="Collection Search Result" onClose={() => setDetailTarget(null)} extraWide>
          <FieldGrid rows={[
            { label: 'Record ID', value: <CopyableId id={detailTarget.GUID || detailTarget.Id || ''} /> },
            { label: 'Document', value: detailTarget.DocumentId || detailTarget.DocumentKey },
            { label: 'Score', value: formatNumber(getScore(detailTarget)) },
            { label: 'Text Score', value: formatNumber(detailTarget.TextScore) },
            { label: 'Labels', value: <BadgeList values={getLabels(detailTarget)} /> },
            { label: 'Tags', value: <TagBadges tags={getTags(detailTarget)} /> },
            { label: 'Created', value: detailTarget.CreatedUtc ? new Date(detailTarget.CreatedUtc).toLocaleString() : '' },
          ]} />
          <div className="artifact-detail-section">
            <h4>Content</h4>
            <pre className="artifact-content-preview">{getRecordContent(detailTarget)}</pre>
          </div>
          <div className="artifact-detail-section">
            <h4>Raw Result</h4>
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
