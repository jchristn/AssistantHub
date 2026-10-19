const PREFIX = 'assistanthub.table.columns.';

export function normalizeHiddenColumns(columns, hidden = []) {
  const requested = new Set(Array.isArray(hidden) ? hidden.filter(key => typeof key === 'string') : []);
  return columns.filter(column => !column.required && requested.has(column.key)).map(column => column.key);
}

export function readHiddenColumns(tableId) {
  try {
    const value = JSON.parse(window.localStorage.getItem(PREFIX + tableId) || '[]');
    return Array.isArray(value) ? value.filter(key => typeof key === 'string') : [];
  } catch {
    return [];
  }
}

export function storeHiddenColumns(tableId, hidden) {
  try {
    window.localStorage.setItem(PREFIX + tableId, JSON.stringify(hidden));
  } catch {
    // Column selection still works when browser storage is unavailable.
  }
}
