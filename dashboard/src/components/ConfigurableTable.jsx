import React, { useCallback, useId, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { normalizeHiddenColumns, readHiddenColumns, storeHiddenColumns, resolveHiddenColumns, resetHiddenColumns } from '../utils/columnPreferences';
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

export default function ConfigurableTable({ tableId, requiredColumns = NO_REQUIRED_COLUMNS, onColumnVisibilityChange, controlsTarget, defaultVisibleColumns, children, ...props }) {
  const tableRef = useRef(null);
  const generatedId = useId();
  const controlsRef = useRef(null);
  const optionsRef = useRef(null);
  const [menuOpen, setMenuOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState({});
  const defaultsRef = useRef(defaultVisibleColumns);
  defaultsRef.current = defaultVisibleColumns;
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
    const hiddenKeys = new Set(resolveHiddenColumns(tableId, schema, hiddenRef.current, defaultsRef.current));
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
  }, [tableId]);

  useLayoutEffect(() => {
    schemaRef.current = "";
    visibilityRef.current = "";
    setHidden(readHiddenColumns(tableId));
  }, [tableId]);

  useLayoutEffect(() => {
    applyColumns();
    const observer = new MutationObserver(applyColumns);
    observer.observe(tableRef.current, { childList: true, subtree: true, characterData: true });
    return () => observer.disconnect();
  }, [applyColumns, children, hidden, requiredColumns, defaultVisibleColumns]);

  useLayoutEffect(() => {
    const close = event => {
      const details = controlsRef.current;
      if (!details?.open) return;
      if (event.type === 'keydown') {
        if (event.key !== 'Escape') return;
        event.stopPropagation();
        details.open = false;
        details.querySelector('summary')?.focus();
      } else if (!details.contains(event.target) && !optionsRef.current?.contains(event.target)) {
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

  useLayoutEffect(() => {
    if (!menuOpen) return;
    const position = () => {
      const bounds = controlsRef.current.getBoundingClientRect();
      if (bounds.bottom < 0 || bounds.top > window.innerHeight) { controlsRef.current.open = false; return; }
      const below = window.innerHeight - bounds.bottom;
      const width = optionsRef.current.getBoundingClientRect().width;
      setMenuPosition({
        position: 'fixed', right: Math.max(8, Math.min(window.innerWidth - bounds.right, window.innerWidth - width - 8)),
        ...(below >= Math.min(320, bounds.top) ? { top: bounds.bottom + 6 } : { bottom: window.innerHeight - bounds.top + 6 }),
        maxHeight: Math.max(100, Math.min(320, Math.max(below, bounds.top) - 16)),
      });
    };
    position();
    optionsRef.current?.querySelector('input:not(:disabled), button')?.focus({ preventScroll: true });
    window.addEventListener('resize', position);
    window.addEventListener('scroll', position, true);
    return () => { window.removeEventListener('resize', position); window.removeEventListener('scroll', position, true); };
  }, [menuOpen, controlsTarget]);

  const update = next => {
    const normalized = normalizeHiddenColumns(columns, next);
    setHidden(normalized);
    storeHiddenColumns(tableId, normalized);
  };
  const effectiveHidden = resolveHiddenColumns(tableId, columns, hidden, defaultVisibleColumns);
  const hiddenKeys = new Set(effectiveHidden);
  const optionsId = generatedId + '-columns';
  const controls = (
    <div className="column-controls" data-table-controls-for={tableId}>
      <details ref={controlsRef} onToggle={event => setMenuOpen(event.currentTarget.open)}>
        <summary aria-controls={optionsId} aria-expanded={menuOpen}>Columns <span>({columns.length - hiddenKeys.size}/{columns.length})</span></summary>
      </details>
    </div>
  );

  return (
    <div className="configurable-table" data-table-id={tableId}>
      {controlsTarget ? createPortal(controls, controlsTarget) : controls}
      {menuOpen && createPortal(
        <div role="group" ref={optionsRef} id={optionsId} className="column-options" style={menuPosition} aria-labelledby={optionsId + '-heading'} data-column-options-for={tableId}>
          <h4 id={optionsId + '-heading'} className="column-options-title">Visible columns</h4>
          {columns.map(column => (
            <label key={column.key}>
              <input type="checkbox" checked={!hiddenKeys.has(column.key)} disabled={column.required}
                onChange={event => update(event.target.checked ? effectiveHidden.filter(key => key !== column.key) : [...effectiveHidden, column.key])} />
              <span>{column.label}</span>
              {column.required && <small>Required</small>}
            </label>
          ))}
          <div className="column-options-actions">
            <button type="button" className="btn btn-secondary" onClick={() => update([])}>Show all columns</button>
            <button type="button" className="btn btn-secondary" onClick={() => { resetHiddenColumns(tableId); setHidden(null); }}>Reset to defaults</button>
          </div>
        </div>, document.body
      )}
      <table {...props} id={props.id || generatedId} ref={tableRef}>{children}</table>
    </div>
  );
}
