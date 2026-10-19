import test, { after } from 'node:test';
import assert from 'node:assert/strict';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';
import { fileURLToPath } from 'node:url';

const server = await createServer({ root: fileURLToPath(new URL('..', import.meta.url)), server: { middlewareMode: true }, appType: 'custom' });
after(() => server.close());
const modals = await Promise.all(['Inference', 'Embedding'].map(async kind => [kind, (await server.ssrLoadModule(`/src/components/modals/${kind}EndpointFormModal.jsx`)).default]));
for (const [kind, Modal] of modals) {
  for (const editing of [false, true]) {
    for (const [format, label, required, fixed] of [
      ['Ollama', 'Bearer Token', false, false],
      ['OllamaCloud', 'Bearer Token', true, true],
      ['OpenAI', 'API Key', false, false],
      ['xAI', 'Bearer Token', true, false],
      ['Gemini', 'API Key', true, false]
    ]) {
      test(`${kind} ${editing ? 'edit' : 'create'} ${format} renders provider-specific connection fields`, () => {
        const source = { ApiFormat: format, Model: 'test-model', Endpoint: 'https://custom.example', ApiKey: '' };
        const html = renderToStaticMarkup(React.createElement(Modal, {
          [editing ? 'endpoint' : 'initialData']: source, onSave() {}, onClose() {}
        }));
        assert.ok(html.includes(`${label} (${required ? 'required' : 'optional'})`));
        assert.equal(/readonly=""/i.test(html), fixed);
        assert.equal(/class="btn btn-primary" disabled=""/.test(html), required);
        assert.ok(html.includes(fixed ? 'value="https://ollama.com"' : 'value="https://custom.example"'));
        if (format === 'Gemini') assert.ok(html.includes('x-goog-api-key'));
        if (format === 'OllamaCloud') assert.ok(!html.includes('>API Key'));
      });
    }
  }
}
