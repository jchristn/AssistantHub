import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import DataTable from '../components/DataTable';
import Tooltip from '../components/Tooltip';
import CopyableId from '../components/CopyableId';
import Modal from '../components/Modal';
import ConfirmModal from '../components/ConfirmModal';
import AlertModal from '../components/AlertModal';

function EvaluationView() {
  const { serverUrl, credential } = useAuth();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const [tab, setTab] = useState('facts');
  const [assistantFilter, setAssistantFilter] = useState('');
  const [assistants, setAssistants] = useState([]);
  const [refresh, setRefresh] = useState(0);

  // Facts state
  const [factModal, setFactModal] = useState(null); // { mode: 'create'|'edit', fact }
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [alert, setAlert] = useState(null);

  // Runs state
  const [runProgressModal, setRunProgressModal] = useState(null);
  const [runResultsModal, setRunResultsModal] = useState(null);
  const [resultDetailModal, setResultDetailModal] = useState(null);
  const [deleteRunTarget, setDeleteRunTarget] = useState(null);
  const [startRunModal, setStartRunModal] = useState(false);
  const [judgePromptOverride, setJudgePromptOverride] = useState('');
  const [defaultJudgePrompt, setDefaultJudgePrompt] = useState('');
  const [startingRun, setStartingRun] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getAssistants({ maxResults: 1000 });
        const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
        setAssistants(items);
        if (items.length === 1) setAssistantFilter(items[0].Id);
      } catch (err) { console.error('Failed to load assistants', err); }
    })();
  }, [serverUrl, credential]);

  useEffect(() => {
    (async () => {
      try {
        const result = await api.getDefaultJudgePrompt();
        if (result && result.Prompt) setDefaultJudgePrompt(result.Prompt);
      } catch (err) { console.error('Failed to load default judge prompt', err); }
    })();
  }, [serverUrl, credential]);

  // ───── FACTS TAB ─────
  const factsColumns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique identifier', filterable: true, render: (row) => <CopyableId id={row.Id} /> },
    { key: 'Category', label: 'Category', tooltip: 'Organizational category', filterable: true, render: (row) => row.Category || '' },
    { key: 'Question', label: 'Question', tooltip: 'Question sent to the assistant', filterable: true, render: (row) => row.Question ? (row.Question.length > 60 ? row.Question.substring(0, 60) + '...' : row.Question) : '' },
    { key: 'ExpectedFacts', label: 'Expected Facts', tooltip: 'Number of expected facts', render: (row) => {
      try { return JSON.parse(row.ExpectedFacts || '[]').length; } catch { return 0; }
    }},
    { key: 'CreatedUtc', label: 'Created', tooltip: 'When this fact was created', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
  ];

  const fetchFacts = useCallback(async (params) => {
    const queryParams = { ...params };
    if (assistantFilter) queryParams.assistantId = assistantFilter;
    return await api.getEvalFacts(queryParams);
  }, [serverUrl, credential, assistantFilter]);

  const getFactActions = (row) => [
    { label: 'Edit', onClick: () => setFactModal({ mode: 'edit', fact: row }) },
    { label: 'Delete', danger: true, onClick: () => setDeleteTarget(row) },
  ];

  const handleDeleteFact = async () => {
    try {
      await api.deleteEvalFact(deleteTarget.Id);
      setDeleteTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete fact' });
    }
  };

  const handleBulkDeleteFacts = async (ids) => {
    try {
      for (const id of ids) await api.deleteEvalFact(id);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete some facts' });
    }
  };

  // ───── RUNS TAB ─────
  const runsColumns = [
    { key: 'Id', label: 'ID', tooltip: 'Unique run identifier', render: (row) => <CopyableId id={row.Id} /> },
    { key: 'CreatedUtc', label: 'Date', tooltip: 'When the run was started', render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : '' },
    { key: 'Status', label: 'Status', tooltip: 'Current run status', render: (row) => <StatusBadge status={row.Status} /> },
    { key: 'Facts', label: 'Facts', tooltip: 'Passed / Failed / Total', render: (row) => (
      <span>
        <span style={{ color: 'var(--success)' }}>{row.FactsPassed}</span>
        {' / '}
        <span style={{ color: 'var(--danger)' }}>{row.FactsFailed}</span>
        {' / '}
        {row.TotalFacts}
      </span>
    )},
    { key: 'PassRate', label: 'Pass Rate', tooltip: 'Percentage of facts that passed', render: (row) => row.PassRate != null ? row.PassRate.toFixed(1) + '%' : '' },
    { key: 'Duration', label: 'Duration', tooltip: 'Total run duration', render: (row) => {
      if (!row.StartedUtc) return '';
      const end = row.CompletedUtc ? new Date(row.CompletedUtc) : new Date();
      const start = new Date(row.StartedUtc);
      const seconds = Math.round((end - start) / 1000);
      return seconds < 60 ? seconds + 's' : Math.floor(seconds / 60) + 'm ' + (seconds % 60) + 's';
    }},
  ];

  const fetchRuns = useCallback(async (params) => {
    const queryParams = { ...params };
    if (assistantFilter) queryParams.assistantId = assistantFilter;
    return await api.getEvalRuns(queryParams);
  }, [serverUrl, credential, assistantFilter]);

  const getRunActions = (row) => [
    { label: 'Results', onClick: () => loadRunResults(row) },
    ...(row.Status === 'Running' || row.Status === 'Pending' ? [{ label: 'Watch', onClick: () => setRunProgressModal(row) }] : []),
    { label: 'Delete', danger: true, onClick: () => setDeleteRunTarget(row) },
  ];

  const loadRunResults = async (run) => {
    try {
      const results = await api.getEvalRunResults(run.Id);
      setRunResultsModal({ run, results: Array.isArray(results) ? results : [] });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load results' });
    }
  };

  const handleDeleteRun = async () => {
    try {
      await api.deleteEvalRun(deleteRunTarget.Id);
      setDeleteRunTarget(null);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to delete run' });
    }
  };

  const handleStartRun = async () => {
    if (!assistantFilter) {
      setAlert({ title: 'Error', message: 'Please select an assistant first.' });
      return;
    }
    setStartingRun(true);
    try {
      const body = { AssistantId: assistantFilter };
      if (judgePromptOverride.trim()) body.JudgePrompt = judgePromptOverride.trim();
      const run = await api.startEvalRun(body);
      setStartRunModal(false);
      setJudgePromptOverride('');
      setRunProgressModal(run);
      setRefresh(r => r + 1);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to start run' });
    } finally {
      setStartingRun(false);
    }
  };

  const handleFilterChange = (e) => {
    setAssistantFilter(e.target.value);
    setRefresh(r => r + 1);
  };

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Evaluation</h1>
          <p className="content-subtitle">Define expected facts and run RAG evaluation against your assistants.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <label style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-secondary)' }}>
            <Tooltip text="Filter by a specific assistant">Assistant:</Tooltip>
          </label>
          <select
            value={assistantFilter}
            onChange={handleFilterChange}
            style={{
              padding: '0.5rem 0.75rem',
              border: '1px solid var(--input-border)',
              borderRadius: 'var(--radius-sm)',
              background: 'var(--input-bg)',
              color: 'var(--text-primary)',
              fontSize: '0.875rem',
              minWidth: '280px',
            }}
          >
            <option value="">All Assistants</option>
            {assistants.map(a => (
              <option key={a.Id} value={a.Id}>
                {a.Name} ({a.Id.substring(0, 8)}...)
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Sub-tabs */}
      <div style={{ display: 'flex', gap: '0', borderBottom: '1px solid var(--border-color)', marginBottom: '1rem' }}>
        <button
          onClick={() => setTab('facts')}
          style={{
            padding: '0.625rem 1.25rem',
            border: 'none',
            borderBottom: tab === 'facts' ? '2px solid var(--accent)' : '2px solid transparent',
            background: 'none',
            color: tab === 'facts' ? 'var(--accent)' : 'var(--text-secondary)',
            fontWeight: tab === 'facts' ? 600 : 400,
            fontSize: '0.875rem',
            cursor: 'pointer',
          }}
        >
          Facts
        </button>
        <button
          onClick={() => setTab('runs')}
          style={{
            padding: '0.625rem 1.25rem',
            border: 'none',
            borderBottom: tab === 'runs' ? '2px solid var(--accent)' : '2px solid transparent',
            background: 'none',
            color: tab === 'runs' ? 'var(--accent)' : 'var(--text-secondary)',
            fontWeight: tab === 'runs' ? 600 : 400,
            fontSize: '0.875rem',
            cursor: 'pointer',
          }}
        >
          Runs
        </button>
      </div>

      {/* Facts tab */}
      {tab === 'facts' && (
        <div>
          <div style={{ marginBottom: '1rem' }}>
            <button className="btn btn-primary" onClick={() => {
              if (!assistantFilter) { setAlert({ title: 'Error', message: 'Please select an assistant first.' }); return; }
              setFactModal({ mode: 'create', fact: { AssistantId: assistantFilter, Category: '', Question: '', ExpectedFacts: '[]' } });
            }}>
              Add Fact
            </button>
          </div>
          <DataTable columns={factsColumns} fetchData={fetchFacts} getRowActions={getFactActions} refreshTrigger={refresh} onBulkDelete={handleBulkDeleteFacts} />
        </div>
      )}

      {/* Runs tab */}
      {tab === 'runs' && (
        <div>
          <div style={{ marginBottom: '1rem' }}>
            <button className="btn btn-primary" onClick={() => {
              if (!assistantFilter) { setAlert({ title: 'Error', message: 'Please select an assistant first.' }); return; }
              setStartRunModal(true);
            }}>
              Start New Run
            </button>
          </div>
          <DataTable columns={runsColumns} fetchData={fetchRuns} getRowActions={getRunActions} refreshTrigger={refresh} />
        </div>
      )}

      {/* Fact Editor Modal */}
      {factModal && (
        <FactEditorModal
          mode={factModal.mode}
          fact={factModal.fact}
          api={api}
          onClose={() => setFactModal(null)}
          onSaved={() => { setFactModal(null); setRefresh(r => r + 1); }}
          onError={(msg) => setAlert({ title: 'Error', message: msg })}
        />
      )}

      {/* Start Run Modal */}
      {startRunModal && (
        <Modal title="Start Evaluation Run" onClose={() => setStartRunModal(false)} footer={
          <>
            <button className="btn btn-secondary" onClick={() => setStartRunModal(false)}>Cancel</button>
            <button className="btn btn-primary" onClick={handleStartRun} disabled={startingRun}>
              {startingRun ? 'Starting...' : 'Start Run'}
            </button>
          </>
        }>
          <p style={{ marginBottom: '1rem', color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
            This will evaluate all facts for the selected assistant. Each fact's question will be sent through the inference pipeline and judged against the expected facts.
          </p>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: 500 }}>
              <Tooltip text="Override the judge prompt for this run. Leave empty to use the assistant's configured prompt or the system default.">Judge Prompt Override (optional)</Tooltip>
            </label>
            <textarea
              value={judgePromptOverride}
              onChange={(e) => setJudgePromptOverride(e.target.value)}
              placeholder={defaultJudgePrompt}
              rows={6}
              style={{
                width: '100%',
                padding: '0.5rem 0.75rem',
                border: '1px solid var(--input-border)',
                borderRadius: 'var(--radius-sm)',
                background: 'var(--input-bg)',
                color: 'var(--text-primary)',
                fontSize: '0.8125rem',
                fontFamily: 'monospace',
                resize: 'vertical',
              }}
            />
            <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: 'var(--text-tertiary)' }}>
              Must contain {'{'} QUESTION {'}'}, {'{'} RESPONSE {'}'}, and {'{'} EXPECTED_FACT {'}'} placeholders.
            </p>
          </div>
        </Modal>
      )}

      {/* Run Progress Modal */}
      {runProgressModal && (
        <RunProgressModal
          run={runProgressModal}
          api={api}
          serverUrl={serverUrl}
          bearerToken={credential?.BearerToken}
          onClose={() => { setRunProgressModal(null); setRefresh(r => r + 1); }}
        />
      )}

      {/* Run Results Modal */}
      {runResultsModal && (
        <RunResultsModal
          run={runResultsModal.run}
          results={runResultsModal.results}
          onClose={() => setRunResultsModal(null)}
          onViewDetail={(result) => setResultDetailModal(result)}
        />
      )}

      {/* Result Detail Modal */}
      {resultDetailModal && (
        <ResultDetailModal
          result={resultDetailModal}
          onClose={() => setResultDetailModal(null)}
        />
      )}

      {/* Delete Fact Confirm */}
      {deleteTarget && <ConfirmModal title="Delete Fact" message="Are you sure you want to delete this evaluation fact? This action cannot be undone." confirmLabel="Delete" danger onConfirm={handleDeleteFact} onClose={() => setDeleteTarget(null)} />}

      {/* Delete Run Confirm */}
      {deleteRunTarget && <ConfirmModal title="Delete Run" message="Are you sure you want to delete this evaluation run and all its results? This action cannot be undone." confirmLabel="Delete" danger onConfirm={handleDeleteRun} onClose={() => setDeleteRunTarget(null)} />}

      {/* Alert */}
      {alert && <AlertModal title={alert.title} message={alert.message} onClose={() => setAlert(null)} />}
    </div>
  );
}

// ───── StatusBadge ─────
function StatusBadge({ status }) {
  const colors = {
    Completed: { bg: 'var(--success-bg, rgba(34,197,94,0.1))', color: 'var(--success, #22c55e)' },
    Failed: { bg: 'var(--danger-bg, rgba(239,68,68,0.1))', color: 'var(--danger, #ef4444)' },
    Running: { bg: 'var(--warning-bg, rgba(234,179,8,0.1))', color: 'var(--warning, #eab308)' },
    Pending: { bg: 'var(--warning-bg, rgba(234,179,8,0.1))', color: 'var(--warning, #eab308)' },
  };
  const style = colors[status] || colors.Pending;
  return (
    <span style={{
      padding: '0.125rem 0.5rem',
      borderRadius: 'var(--radius-sm, 4px)',
      fontSize: '0.75rem',
      fontWeight: 600,
      background: style.bg,
      color: style.color,
    }}>
      {status}
    </span>
  );
}

// ───── Fact Editor Modal ─────
function FactEditorModal({ mode, fact, api, onClose, onSaved, onError }) {
  const [category, setCategory] = useState(fact.Category || '');
  const [question, setQuestion] = useState(fact.Question || '');
  const [expectedFacts, setExpectedFacts] = useState(() => {
    try { return JSON.parse(fact.ExpectedFacts || '[]'); } catch { return []; }
  });
  const [saving, setSaving] = useState(false);

  const addFact = () => setExpectedFacts([...expectedFacts, '']);
  const removeFact = (idx) => setExpectedFacts(expectedFacts.filter((_, i) => i !== idx));
  const updateFact = (idx, val) => { const copy = [...expectedFacts]; copy[idx] = val; setExpectedFacts(copy); };

  const handleSave = async () => {
    if (!question.trim()) { onError('Question is required.'); return; }
    setSaving(true);
    try {
      const payload = {
        AssistantId: fact.AssistantId,
        Category: category.trim() || null,
        Question: question.trim(),
        ExpectedFacts: JSON.stringify(expectedFacts.filter(f => f.trim())),
      };
      if (mode === 'create') {
        await api.createEvalFact(payload);
      } else {
        await api.updateEvalFact(fact.Id, payload);
      }
      onSaved();
    } catch (err) {
      onError(err.message || 'Failed to save fact');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={mode === 'create' ? 'Add Evaluation Fact' : 'Edit Evaluation Fact'} onClose={onClose} wide footer={
      <>
        <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </>
    }>
      <div className="form-group" style={{ marginBottom: '1rem' }}>
        <label style={{ display: 'block', marginBottom: '0.375rem', fontSize: '0.875rem', fontWeight: 500 }}>Category</label>
        <input
          type="text"
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          placeholder="e.g. Product, Pricing, Support"
          style={{
            width: '100%',
            padding: '0.5rem 0.75rem',
            border: '1px solid var(--input-border)',
            borderRadius: 'var(--radius-sm)',
            background: 'var(--input-bg)',
            color: 'var(--text-primary)',
            fontSize: '0.875rem',
          }}
        />
      </div>
      <div className="form-group" style={{ marginBottom: '1rem' }}>
        <label style={{ display: 'block', marginBottom: '0.375rem', fontSize: '0.875rem', fontWeight: 500 }}>Question</label>
        <textarea
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="What question should be sent to the assistant?"
          rows={3}
          style={{
            width: '100%',
            padding: '0.5rem 0.75rem',
            border: '1px solid var(--input-border)',
            borderRadius: 'var(--radius-sm)',
            background: 'var(--input-bg)',
            color: 'var(--text-primary)',
            fontSize: '0.875rem',
            resize: 'vertical',
          }}
        />
      </div>
      <div className="form-group">
        <label style={{ display: 'block', marginBottom: '0.375rem', fontSize: '0.875rem', fontWeight: 500 }}>Expected Facts</label>
        {expectedFacts.map((ef, idx) => (
          <div key={idx} style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.375rem' }}>
            <input
              type="text"
              value={ef}
              onChange={(e) => updateFact(idx, e.target.value)}
              placeholder={`Expected fact #${idx + 1}`}
              style={{
                flex: 1,
                padding: '0.5rem 0.75rem',
                border: '1px solid var(--input-border)',
                borderRadius: 'var(--radius-sm)',
                background: 'var(--input-bg)',
                color: 'var(--text-primary)',
                fontSize: '0.875rem',
              }}
            />
            <button className="btn btn-danger" onClick={() => removeFact(idx)} style={{ padding: '0.5rem 0.75rem', fontSize: '0.75rem' }}>
              Remove
            </button>
          </div>
        ))}
        <button className="btn btn-secondary" onClick={addFact} style={{ marginTop: '0.25rem', fontSize: '0.8125rem' }}>
          + Add Expected Fact
        </button>
      </div>
    </Modal>
  );
}

// ───── Run Progress Modal (SSE) ─────
function RunProgressModal({ run, api, serverUrl, bearerToken, onClose }) {
  const [currentRun, setCurrentRun] = useState(run);
  const [results, setResults] = useState([]);
  const eventSourceRef = useRef(null);

  useEffect(() => {
    const url = `${serverUrl}/v1.0/eval/runs/${run.Id}/stream`;
    const headers = bearerToken ? { 'Authorization': `Bearer ${bearerToken}` } : {};

    // Use fetch with streaming since EventSource doesn't support auth headers
    let cancelled = false;
    const fetchStream = async () => {
      try {
        const response = await fetch(url, { headers });
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (!cancelled) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop();

          for (const line of lines) {
            if (!line.startsWith('data: ')) continue;
            const data = line.substring(6);
            if (data === '[DONE]') { cancelled = true; break; }
            try {
              const parsed = JSON.parse(data);
              if (parsed.Run) setCurrentRun(parsed.Run);
              if (parsed.Results) setResults(parsed.Results);
            } catch {}
          }
        }
      } catch (err) {
        console.error('SSE stream error:', err);
      }
    };

    fetchStream();
    return () => { cancelled = true; };
  }, [run.Id, serverUrl, bearerToken]);

  const progressPercent = currentRun.TotalFacts > 0 ? (currentRun.FactsEvaluated / currentRun.TotalFacts) * 100 : 0;
  const passPercent = currentRun.FactsEvaluated > 0 ? (currentRun.FactsPassed / currentRun.FactsEvaluated) * 100 : 0;

  let progressColor = 'var(--success, #22c55e)';
  if (passPercent < 50) progressColor = 'var(--danger, #ef4444)';
  else if (passPercent < 80) progressColor = 'var(--warning, #eab308)';

  return (
    <Modal title="Evaluation Run Progress" onClose={onClose} wide>
      <div style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.375rem', fontSize: '0.875rem' }}>
          <span>Progress: {currentRun.FactsEvaluated} / {currentRun.TotalFacts}</span>
          <StatusBadge status={currentRun.Status} />
        </div>
        <div style={{ width: '100%', height: '8px', background: 'var(--bg-tertiary, #374151)', borderRadius: '4px', overflow: 'hidden' }}>
          <div style={{ width: progressPercent + '%', height: '100%', background: progressColor, transition: 'width 0.3s ease' }} />
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '0.375rem', fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
          <span style={{ color: 'var(--success, #22c55e)' }}>{currentRun.FactsPassed} passed</span>
          <span style={{ color: 'var(--danger, #ef4444)' }}>{currentRun.FactsFailed} failed</span>
          <span>Pass rate: {currentRun.PassRate != null ? currentRun.PassRate.toFixed(1) : '0.0'}%</span>
        </div>
      </div>

      <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
        {results.map((r, i) => (
          <div key={r.Id || i} style={{
            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
            padding: '0.5rem 0.75rem',
            borderBottom: '1px solid var(--border-color)',
            fontSize: '0.8125rem',
          }}>
            <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {r.Question ? (r.Question.length > 80 ? r.Question.substring(0, 80) + '...' : r.Question) : ''}
            </span>
            <span style={{ marginLeft: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span style={{
                padding: '0.125rem 0.375rem',
                borderRadius: '3px',
                fontSize: '0.6875rem',
                fontWeight: 700,
                background: r.OverallPass ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                color: r.OverallPass ? '#22c55e' : '#ef4444',
              }}>
                {r.OverallPass ? 'PASS' : 'FAIL'}
              </span>
              <span style={{ color: 'var(--text-tertiary)', fontSize: '0.75rem' }}>
                {(r.DurationMs / 1000).toFixed(1)}s
              </span>
            </span>
          </div>
        ))}
      </div>
    </Modal>
  );
}

// ───── Run Results Modal ─────
function RunResultsModal({ run, results, onClose, onViewDetail }) {
  return (
    <Modal title={`Run Results — ${run.Status}`} onClose={onClose} wide>
      <div style={{ display: 'flex', gap: '1.5rem', marginBottom: '1rem', fontSize: '0.875rem' }}>
        <div><strong>Total:</strong> {run.TotalFacts}</div>
        <div style={{ color: 'var(--success, #22c55e)' }}><strong>Passed:</strong> {run.FactsPassed}</div>
        <div style={{ color: 'var(--danger, #ef4444)' }}><strong>Failed:</strong> {run.FactsFailed}</div>
        <div><strong>Pass Rate:</strong> {run.PassRate != null ? run.PassRate.toFixed(1) : '0.0'}%</div>
      </div>

      <div style={{ maxHeight: '500px', overflowY: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.8125rem' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid var(--border-color)', textAlign: 'left' }}>
              <th style={{ padding: '0.5rem' }}>Question</th>
              <th style={{ padding: '0.5rem', width: '80px' }}>Result</th>
              <th style={{ padding: '0.5rem', width: '80px' }}>Duration</th>
              <th style={{ padding: '0.5rem', width: '80px' }}>Action</th>
            </tr>
          </thead>
          <tbody>
            {results.map((r) => (
              <tr key={r.Id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                <td style={{ padding: '0.5rem', maxWidth: '400px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {r.Question || ''}
                </td>
                <td style={{ padding: '0.5rem' }}>
                  <span style={{
                    padding: '0.125rem 0.375rem',
                    borderRadius: '3px',
                    fontSize: '0.6875rem',
                    fontWeight: 700,
                    background: r.OverallPass ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                    color: r.OverallPass ? '#22c55e' : '#ef4444',
                  }}>
                    {r.OverallPass ? 'PASS' : 'FAIL'}
                  </span>
                </td>
                <td style={{ padding: '0.5rem', color: 'var(--text-secondary)' }}>
                  {(r.DurationMs / 1000).toFixed(1)}s
                </td>
                <td style={{ padding: '0.5rem' }}>
                  <button className="btn btn-secondary" style={{ padding: '0.25rem 0.5rem', fontSize: '0.75rem' }} onClick={() => onViewDetail(r)}>
                    Detail
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Modal>
  );
}

// ───── Result Detail Modal ─────
function ResultDetailModal({ result, onClose }) {
  let verdicts = [];
  try { verdicts = JSON.parse(result.FactVerdicts || '[]'); } catch {}

  return (
    <Modal title="Evaluation Result Detail" onClose={onClose} wide>
      <div style={{ marginBottom: '1rem' }}>
        <div style={{ marginBottom: '0.75rem' }}>
          <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Question</label>
          <p style={{ margin: '0.25rem 0', fontSize: '0.875rem' }}>{result.Question}</p>
        </div>
        <div style={{ display: 'flex', gap: '1rem', marginBottom: '0.75rem' }}>
          <div>
            <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Overall Result</label>
            <div style={{ marginTop: '0.25rem' }}>
              <span style={{
                padding: '0.25rem 0.625rem',
                borderRadius: '4px',
                fontSize: '0.8125rem',
                fontWeight: 700,
                background: result.OverallPass ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                color: result.OverallPass ? '#22c55e' : '#ef4444',
              }}>
                {result.OverallPass ? 'PASS' : 'FAIL'}
              </span>
            </div>
          </div>
          <div>
            <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Duration</label>
            <p style={{ margin: '0.25rem 0', fontSize: '0.875rem' }}>{(result.DurationMs / 1000).toFixed(2)}s</p>
          </div>
        </div>
      </div>

      <div style={{ marginBottom: '1rem' }}>
        <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>LLM Response</label>
        <pre style={{
          margin: '0.25rem 0',
          padding: '0.75rem',
          background: 'var(--bg-tertiary, #1e293b)',
          borderRadius: 'var(--radius-sm)',
          fontSize: '0.8125rem',
          maxHeight: '200px',
          overflowY: 'auto',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
          color: 'var(--text-primary)',
        }}>
          {result.LlmResponse || '(empty)'}
        </pre>
      </div>

      <div>
        <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '0.5rem', display: 'block' }}>Fact Verdicts</label>
        {verdicts.map((v, i) => (
          <div key={i} style={{
            padding: '0.75rem',
            marginBottom: '0.5rem',
            border: '1px solid var(--border-color)',
            borderRadius: 'var(--radius-sm)',
            borderLeft: `3px solid ${v.pass ? '#22c55e' : '#ef4444'}`,
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.375rem' }}>
              <span style={{ fontSize: '0.8125rem', fontWeight: 500 }}>{v.fact}</span>
              <span style={{
                padding: '0.125rem 0.375rem',
                borderRadius: '3px',
                fontSize: '0.6875rem',
                fontWeight: 700,
                background: v.pass ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                color: v.pass ? '#22c55e' : '#ef4444',
              }}>
                {v.pass ? 'PASS' : 'FAIL'}
              </span>
            </div>
            {v.reasoning && (
              <p style={{ margin: 0, fontSize: '0.75rem', color: 'var(--text-secondary)', whiteSpace: 'pre-wrap' }}>
                {v.reasoning}
              </p>
            )}
          </div>
        ))}
      </div>
    </Modal>
  );
}

export default EvaluationView;
