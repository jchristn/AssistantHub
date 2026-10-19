import test from 'node:test';
import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import { TABLE_COLUMN_DEFAULTS } from '../src/utils/tableColumnDefaults.js';
const root = new URL('../src/', import.meta.url);
async function files(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => entry.isDirectory() ? files(new URL(entry.name + '/', directory)) : entry.name.endsWith('.jsx') ? [new URL(entry.name, directory)] : []))).flat();
}
test('every dashboard table uses the shared column control with a stable preference ID', async () => {
  const ids = new Set();
  for (const file of await files(root)) {
    if (file.pathname.endsWith('/ConfigurableTable.jsx')) continue;
    const source = await readFile(file, 'utf8');
    assert.ok(!/<table(?:\s|>)/.test(source), `Uncontrolled table in ${file.pathname}`);
    for (const match of source.matchAll(/<(?:ConfigurableTable|DataTable)\s+([^>]+)/g)) {
      assert.ok(/tableId=/.test(match[1]), `Missing table ID in ${file.pathname}`);
      const id = match[1].match(/tableId="([^"]+)"/)?.[1];
      if (id) {
        assert.ok(!ids.has(id), `Duplicate table preference ID: ${id}`);
        ids.add(id);
        assert.ok(Object.hasOwn(TABLE_COLUMN_DEFAULTS, id), `Missing reviewed default columns: ${id}`);
      }
    }
  }
  assert.ok(ids.size >= 35, 'Coverage should include list views, dialogs, analytics, search, and Markdown');
});

test('all paginated tables attach column controls to the pagination toolbar', async () => {
  let count = 0;
  for (const file of await files(root)) {
    const source = await readFile(file, 'utf8');
    for (const match of source.matchAll(/<Pagination\s+([^>]+)>/g)) {
      assert.ok(/columnControlsRef=/.test(match[1]), `Pagination missing Columns slot in ${file.pathname}`);
      assert.ok(/<ConfigurableTable[^>]+controlsTarget=/.test(source), `Table missing toolbar target in ${file.pathname}`);
      count++;
    }
  }
  assert.equal(count, 2, 'shared list tables and the custom records view are both covered');
});
