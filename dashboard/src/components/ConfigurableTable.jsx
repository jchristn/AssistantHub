import React, { useCallback, useId, useLayoutEffect, useRef, useState } from 'react';
import { normalizeHiddenColumns, readHiddenColumns, storeHiddenColumns } from '../utils/columnPreferences';
import './ConfigurableTable.css';

const NO_REQUIRED_COLUMNS = [];

// Discover the rendered headers so custom cells, conditional columns, and Markdown
// tables use the same control without changing their sorting or row interactions.
function discoverColumns(table, requiredColumns) {
  const header = table.tHead?.rows[0] || Array.from(table.rows).find(row => row.querySelector('th'));
  if (!header) return [];
  const seen = new Map();
  let identityFound = false;
  return Array.from(header.cells).map((cell, index) => {
    const selection = !!cell.querySelector('input[type="checkbox"]');
    const text = cell.textContent.trim().replace(/\s+/g, ' ');
    const actions = cell.classList.contains('actions-cell') || /^(actions?|options)$/i.test(text) || (!text && !selection);
    const label = selection ? 'Select rows' : text || 'Actions';
    const base = cell.dataset.columnKey || (selection ? '__selection' : actions ? '__actions' : label);
    const occurrence = seen.get(base) || 0;
    seen.set(base, occurrence + 1);
    const key = occurrence ? `${base}:${occurrence}` : base;
    const identity = !selection && !actions && !identityFound;
    if (identity) identityFound = true;
    return {
      key, label, index,
      required: identity || selection || actions || cell.dataset.columnRequired === 'true'
        || requiredColumns.includes(key) || requiredColumns.includes(index)
    };
  });
}

export default function ConfigurableTable({ tableId, requiredColumns = NO_REQUIRED_COLUMNS, onColumnVisibilityChange, children, ...props }) {
  const tableRef = useRef(null);
  const generatedId = useId();
  const controlsRef = useRef(null);
  const originalSpans = useRef(new WeakMap());
  const [columns, setColumns] = useState([]);
  const [hidden, setHidden] = useState(() => readHiddenColumns(tableId));
  const schemaRef = useRef('');
  const visibilityRef = useRef('');
  const callbackRef = useRef(onColumnVisibilityChange);
  callbackRef.current = onColumnVisibilityChange;
  const hiddenRef = useRef(hidden);
  const requiredRef = useRef(requiredColumns);
  hiddenRef.current = hidden;
  requiredRef.current = requiredColumns;

  const applyColumns = useCallback(() => {
    const table = tableRef.current;
    if (!table) return;
    const schema = discoverColumns(table, requiredRef.current);
    const signature = JSON.stringify(schema);
    if (schemaRef.current !== signature) {
      schemaRef.current = signature;
      setColumns(schema);
    }
    const hiddenKeys = new Set(normalizeHiddenColumns(schema, hiddenRef.current));
    const visibleCount = schema.length - hiddenKeys.size;
    const visibility = JSON.stringify([...hiddenKeys]);
    if (visibilityRef.current !== visibility) {
      visibilityRef.current = visibility;
      callbackRef.current?.([...hiddenKeys]);
    }
    for (const row of Array.from(table.rows)) {
      let index = 0;
      for (const cell of Array.from(row.cells)) {
        const span = originalSpans.current.get(cell);
        const width = span && cell.colSpan === span.applied ? span.original : cell.colSpan;
        const covered = schema.slice(index, index + width);
        const visible = covered.filter(column => !hiddenKeys.has(column.key)).length;
        // Spanning empty states must follow the visible header width.
        if (width > 1 && schema.length) {
          const nextSpan = Math.max(1, width === schema.length ? visibleCount : visible);
          originalSpans.current.set(cell, { original: width, applied: nextSpan });
          cell.colSpan = nextSpan;
        }
        cell.toggleAttribute('data-column-hidden', !!covered.length && visible === 0);
        index += width;
      }
    }
  }, []);

  useLayoutEffect(() => {
    setHidden(readHiddenColumns(tableId));
  }, [tableId]);

  useLayoutEffect(() => {
    applyColumns();
    const observer = new MutationObserver(applyColumns);
    observer.observe(tableRef.current, { childList: true, subtree: true, characterData: true });
    return () => observer.disconnect();
  }, [applyColumns, children, hidden, requiredColumns]);

  useLayoutEffect(() => {
    const close = event => {
      const details = controlsRef.current;
      if (!details?.open) return;
      if (event.type === 'keydown') {
        if (event.key !== 'Escape') return;
        event.stopPropagation();
        details.open = false;
        details.querySelector('summary')?.focus();
      } else if (!details.contains(event.target)) {
        details.open = false;
      }
    };
    document.addEventListener('click', close);
    document.addEventListener('keydown', close, true);
    return () => {
      document.removeEventListener('click', close);
      document.removeEventListener('keydown', close, true);
    };
  }, []);

  const update = next => {
    const normalized = normalizeHiddenColumns(columns, next);
    setHidden(normalized);
    storeHiddenColumns(tableId, normalized);
  };
  const hiddenKeys = new Set(normalizeHiddenColumns(columns, hidden));

  return (
    <div className="configurable-table" data-table-id={tableId}>
      <div className="column-controls">
        <details ref={controlsRef}>
          <summary aria-controls={props.id || generatedId}>Columns <span>({columns.length - hiddenKeys.size}/{columns.length})</span></summary>
          <fieldset className="column-options" aria-label="Visible columns">
            <legend>Visible columns</legend>
            {columns.map(column => (
              <label key={column.key}>
                <input type="checkbox" checked={!hiddenKeys.has(column.key)} disabled={column.required}
                  onChange={event => update(event.target.checked ? hidden.filter(key => key !== column.key) : [...hidden, column.key])} />
                <span>{column.label}</span>
                {column.required && <small>Required</small>}
              </label>
            ))}
            <button type="button" className="btn btn-secondary" onClick={() => update([])}>Show all columns</button>
          </fieldset>
        </details>
      </div>
      <table {...props} id={props.id || generatedId} ref={tableRef}>{children}</table>
    </div>
  );
}
