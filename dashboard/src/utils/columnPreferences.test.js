import test from 'node:test';
import assert from 'node:assert/strict';
import { getStoredPageSize, setStoredPageSize } from './pageSizePreference.js';
import { normalizeHiddenColumns, readHiddenColumns, storeHiddenColumns, resolveHiddenColumns, resetHiddenColumns } from './columnPreferences.js';
const columns = [{ key: 'Id', required: true }, { key: 'Name' }, { key: 'Status' }, { key: '__actions', required: true }];
test('required columns cannot be hidden, including tampered saved settings', () => {
  assert.deepEqual(normalizeHiddenColumns(columns, ['Id', 'Name', '__actions', 'removed', 123]), ['Name']);
});
test('new columns start visible and removed columns do not affect the schema', () => {
  assert.deepEqual(normalizeHiddenColumns([...columns, { key: 'New' }], ['Status', 'Removed']), ['Status']);
});
test('missing or invalid storage uses table defaults', () => {
  assert.deepEqual(readHiddenColumns('missing'), null);
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
  assert.deepEqual(readHiddenColumns('one'), null);
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

test('endpoint defaults emphasize model, format and health; saved choices take precedence', () => {
  const schema = ['Id', 'Name', 'Model', 'Endpoint', 'ApiFormat', 'Active', 'Health', 'CreatedUtc', '__actions'].map(key => ({ key, required: key === 'Id' || key === '__actions' }));
  assert.deepEqual(resolveHiddenColumns('inferenceendpoints-list-1', schema, null), ['Endpoint', 'CreatedUtc']);
  assert.deepEqual(resolveHiddenColumns('inferenceendpoints-list-1', schema, []), []);
  assert.deepEqual(resolveHiddenColumns('inferenceendpoints-list-1', schema, ['Model', 'Id']), ['Model']);
});
test('different table defaults keep operational fields while hiding optional detail', () => {
  const schema = keys => keys.map(key => ({ key }));
  assert.deepEqual(resolveHiddenColumns('credentials-list-1', schema(['Id', 'Name', 'BearerToken', 'Active']), null), ['BearerToken']);
  assert.deepEqual(resolveHiddenColumns('requesthistory-list-1', schema(['Id', 'RequestPath', 'StatusCode', 'DurationMs', 'ThreadId']), null), ['ThreadId']);
  assert.deepEqual(resolveHiddenColumns('history-1', schema(['Stage', 'Duration', 'Tokens', 'Provider Load', 'Prompt Eval']), null), ['Provider Load', 'Prompt Eval']);
  assert.deepEqual(resolveHiddenColumns('chatpanel-2', schema(['Custom', 'Author', 'Columns']), null), []);
});
test('reset removes the stored override and required columns survive a narrow default', () => {
  const storage = new Map();
  globalThis.window = { localStorage: { setItem: (key, value) => storage.set(key, value), getItem: key => storage.get(key) ?? null, removeItem: key => storage.delete(key) } };
  storeHiddenColumns('test', []);
  assert.deepEqual(readHiddenColumns('test'), []);
  resetHiddenColumns('test');
  assert.equal(readHiddenColumns('test'), null);
  assert.deepEqual(resolveHiddenColumns('test', columns, null, ['Name']), ['Status']);
  delete globalThis.window;
});
