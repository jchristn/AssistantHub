import test from 'node:test';
import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
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
      }
    }
  }
  assert.ok(ids.size >= 35, 'Coverage should include list views, dialogs, analytics, search, and Markdown');
});
