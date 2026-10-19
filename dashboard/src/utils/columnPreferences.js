import { TABLE_COLUMN_DEFAULTS } from './tableColumnDefaults.js';
const PREFIX = 'assistanthub.table.columns.';

export function normalizeHiddenColumns(columns, hidden = []) {
  const requested = new Set(Array.isArray(hidden) ? hidden.filter(key => typeof key === 'string') : []);
  return columns.filter(column => !column.required && requested.has(column.key)).map(column => column.key);
}

export function readHiddenColumns(tableId) {
  try {
    const stored = window.localStorage.getItem(PREFIX + tableId);
    if (stored === null) return null;
    const value = JSON.parse(stored);
    return Array.isArray(value) ? value.filter(key => typeof key === 'string') : null;
  } catch {
    return null;
  }
}

export function storeHiddenColumns(tableId, hidden) {
  try {
    window.localStorage.setItem(PREFIX + tableId, JSON.stringify(hidden));
  } catch {
    // Column selection still works when browser storage is unavailable.
  }
}

export function resolveHiddenColumns(tableId, columns, saved, defaults = TABLE_COLUMN_DEFAULTS[tableId]) {
  const hidden = saved ?? (Array.isArray(defaults) ? columns.filter(column => !defaults.includes(column.key)).map(column => column.key) : []);
  return normalizeHiddenColumns(columns, hidden);
}

export function resetHiddenColumns(tableId) {
  try { window.localStorage.removeItem(PREFIX + tableId); } catch { /* Session-only defaults still work. */ }
}
