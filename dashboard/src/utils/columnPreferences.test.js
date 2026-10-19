import test from 'node:test';
import assert from 'node:assert/strict';
import { getStoredPageSize, setStoredPageSize } from './pageSizePreference.js';
import { normalizeHiddenColumns, readHiddenColumns, storeHiddenColumns } from './columnPreferences.js';
const columns = [{ key: 'Id', required: true }, { key: 'Name' }, { key: 'Status' }, { key: '__actions', required: true }];
test('required columns cannot be hidden, including tampered saved settings', () => {
  assert.deepEqual(normalizeHiddenColumns(columns, ['Id', 'Name', '__actions', 'removed', 123]), ['Name']);
});
test('new columns start visible and removed columns do not affect the schema', () => {
  assert.deepEqual(normalizeHiddenColumns([...columns, { key: 'New' }], ['Status', 'Removed']), ['Status']);
});
test('missing or invalid storage leaves all columns visible', () => {
  assert.deepEqual(readHiddenColumns('missing'), []);
  assert.deepEqual(normalizeHiddenColumns(columns, { invalid: true }), []);
});
test('preferences persist independently and storage failures do not prevent using tables', () => {
  const values = new Map();
  globalThis.window = { localStorage: { getItem: key => values.get(key), setItem: (key, value) => values.set(key, value) } };
  storeHiddenColumns('one', ['Name']);
  storeHiddenColumns('two', ['Status']);
  assert.deepEqual(readHiddenColumns('one'), ['Name']);
  assert.deepEqual(readHiddenColumns('two'), ['Status']);
  window.localStorage.getItem = () => { throw new Error('Blocked storage'); };
  window.localStorage.setItem = () => { throw new Error('Quota exceeded'); };
  assert.deepEqual(readHiddenColumns('one'), []);
  assert.doesNotThrow(() => storeHiddenColumns('one', []));
  delete globalThis.window;
});

test('shared table page size also tolerates blocked storage accessors', () => {
  globalThis.window = {};
  Object.defineProperty(window, 'localStorage', { get() { throw new Error('Blocked storage'); } });
  assert.equal(getStoredPageSize(25), 25);
  assert.equal(setStoredPageSize(50), 50);
  delete globalThis.window;
});
