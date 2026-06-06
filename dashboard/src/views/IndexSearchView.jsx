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
  getCustomMetadata,
  getIndexId,
  getLabels,
  getMatchedTerms,
  getRecordContent,
  getRecordId,
  getScore,
  getTags,
  labelRowsToList,
  getTermDetails,
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

function IndexSearchView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const location = useLocation();
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

  const resultTiming = responseJson?.ElapsedMs ?? responseJson?.Data?.ElapsedMs ?? responseJson?.DurationMs ?? elapsedMs;

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Index Search</h1>
          <p className="content-subtitle">Search Verbex inverted indices with text and TF-IDF scoring.</p>
        </div>
        <div className="artifact-header-actions">
          <button className="btn btn-secondary" onClick={clearSearch}>Clear</button>
          <button className="btn btn-secondary" onClick={() => setShowJson(true)} disabled={!responseJson}>View JSON</button>
        </div>
      </div>

      <form className="data-table-container artifact-form" onSubmit={runSearch}>
        <div className="form-row">
          <div className="form-group">
            <label>Index</label>
            <select value={selectedIndex} onChange={(e) => setSelectedIndex(e.target.value)}>
              <option value="">Select an index...</option>
              {indices.map((index) => <option key={getIndexId(index)} value={getIndexId(index)}>{index.Name || getIndexId(index)}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label>Query</label>
            <input value={filters.query} onChange={(e) => setFilters({ ...filters, query: e.target.value })} placeholder="Search terms or *" />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>Labels</label>
            <LabelConstraintInput value={filters.labels} onChange={(labels) => setFilters({ ...filters, labels })} />
          </div>
          <div className="form-group">
            <label>Tags</label>
            <TagConstraintInput value={filters.tags} onChange={(tags) => setFilters({ ...filters, tags })} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>Required Terms</label>
            <input value={filters.requiredTerms} onChange={(e) => setFilters({ ...filters, requiredTerms: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Excluded Terms</label>
            <input value={filters.excludedTerms} onChange={(e) => setFilters({ ...filters, excludedTerms: e.target.value })} />
          </div>
        </div>
        <div className="form-row artifact-form-row-compact">
          <div className="form-group">
            <label>Document ID</label>
            <input value={filters.documentId} onChange={(e) => setFilters({ ...filters, documentId: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Minimum Score</label>
            <input type="number" step="0.0001" value={filters.minScore} onChange={(e) => setFilters({ ...filters, minScore: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Max Results</label>
            <select value={filters.maxResults} onChange={(e) => setFilters({ ...filters, maxResults: e.target.value })}>
              {[10, 25, 50, 100, 250].map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <label className="form-toggle artifact-toggle">
            <input type="checkbox" checked={filters.useAndLogic} onChange={(e) => setFilters({ ...filters, useAndLogic: e.target.checked })} />
            <span>Require all terms</span>
          </label>
        </div>
        <div className="artifact-form-actions">
          <button className="btn btn-primary" type="submit" disabled={!selectedIndex || loading}>{loading ? 'Searching...' : 'Search'}</button>
        </div>
      </form>

      <div className="artifact-result-summary">
        <span>{filteredResults.length} result{filteredResults.length === 1 ? '' : 's'}</span>
        {resultTiming !== '' && resultTiming != null && <span>{formatDuration(resultTiming)}</span>}
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
                <th>Score</th>
                <th>Matched Terms</th>
                <th>Labels</th>
                <th>Tags</th>
                <th>Content</th>
              </tr>
            </thead>
            <tbody>
              {filteredResults.map((result, idx) => {
                const record = result.Document || result.Record || result;
                const recordId = result.DocumentId || getRecordId(record);
                return (
                  <tr key={recordId || idx} onClick={() => setDetailTarget(result)} style={{ cursor: 'pointer' }}>
                    <td><CopyableId id={recordId || ''} /></td>
                    <td><ScoreBar score={getScore(result)} /></td>
                    <td><BadgeList values={getMatchedTerms(result)} empty={result.MatchedTermCount ?? ''} /></td>
                    <td><BadgeList values={getLabels(record)} /></td>
                    <td><TagBadges tags={getTags(record)} /></td>
                    <td className="artifact-content-cell">{String(getRecordContent(record)).slice(0, 240)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {detailTarget && (
        <Modal title="Index Search Result" onClose={() => setDetailTarget(null)} extraWide>
          {(() => {
            const record = detailTarget.Document || detailTarget.Record || detailTarget;
            const metadata = getCustomMetadata(record);
            const termDetails = getTermDetails(detailTarget);
            return (
              <>
                <FieldGrid rows={[
                  { label: 'Record ID', value: <CopyableId id={detailTarget.DocumentId || getRecordId(record)} /> },
                  { label: 'AssistantHub Document', value: metadata.AssistantHubDocumentId ? <CopyableId id={metadata.AssistantHubDocumentId} /> : '' },
                  { label: 'Score', value: formatNumber(getScore(detailTarget)) },
                  { label: 'Matched Terms', value: <BadgeList values={getMatchedTerms(detailTarget)} /> },
                  { label: 'Labels', value: <BadgeList values={getLabels(record)} /> },
                  { label: 'Tags', value: <TagBadges tags={getTags(record)} /> },
                ]} />
                {termDetails.length > 0 && (
                  <div className="artifact-detail-section">
                    <h4>Term Details</h4>
                    <table className="data-table">
                      <thead><tr><th>Term</th><th>Score</th><th>Frequency</th></tr></thead>
                      <tbody>
                        {termDetails.map((term, index) => (
                          <tr key={term.Term || term.Text || index}>
                            <td>{term.Term || term.Text || term.Value || ''}</td>
                            <td>{term.Score ?? term.Weight ?? ''}</td>
                            <td>{term.Frequency ?? term.Count ?? ''}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
                <div className="artifact-detail-section">
                  <h4>Content</h4>
                  <pre className="artifact-content-preview">{getRecordContent(record)}</pre>
                </div>
                <div className="artifact-detail-section">
                  <h4>Raw Result</h4>
                  <pre className="json-view compact">{formatJson(detailTarget)}</pre>
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
