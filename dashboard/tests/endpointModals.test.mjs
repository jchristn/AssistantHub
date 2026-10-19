import test, { after } from 'node:test';
import assert from 'node:assert/strict';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';
import { fileURLToPath } from 'node:url';

const server = await createServer({ root: fileURLToPath(new URL('..', import.meta.url)), server: { middlewareMode: true }, appType: 'custom' });
after(() => server.close());
const modals = await Promise.all(['Inference', 'Embedding'].map(async kind => [kind, (await server.ssrLoadModule(`/src/components/modals/${kind}EndpointFormModal.jsx`)).default]));
const { applyProviderChange, PROVIDERS, getProvider, defaultProviderSettings, authFromEndpoint, providerSettingsToForm, buildConnectionPayload, validateConnection, applyAuthChange, mergeRevealedSecrets, usesConnectivityProbe } = await import('../src/utils/providers.js');
for (const [kind, Modal] of modals) {
  const endpointKind = kind === 'Inference' ? 'completion' : 'embedding';
  for (const editing of [false, true]) {
    for (const provider of PROVIDERS.filter(p => p.kinds.includes(endpointKind))) {
      test(`${kind} ${editing ? 'edit' : 'create'} ${provider.value} renders its contract`, () => {
        const source = { ApiFormat: provider.value, Model: 'test-model', Endpoint: 'https://custom.example', ApiKey: editing ? '****test' : '', ProviderSettings: {} };
        const html = renderToStaticMarkup(React.createElement(Modal, {
          [editing ? 'endpoint' : 'initialData']: source, onSave() {}, onClose() {}, onRevealEndpoint() {}
        }));
        assert.ok(html.includes(provider.label));
        assert.ok(html.includes(provider.model.label));
        if (provider.endpoint.mode === 'fixed') assert.ok(html.includes('provider-readonly'));
        assert.equal(html.includes('>Health Check URL<'), !usesConnectivityProbe(provider.value));
        if (provider.value === 'OllamaCloud') { assert.ok(html.includes('Bearer Token')); assert.ok(html.includes('https://ollama.com')); }
        if (provider.value === 'Bedrock') { assert.ok(html.includes('Secret Access Key')); assert.ok(!html.includes('>API Key')); }
        if (provider.value === 'VertexAI') assert.ok(html.includes('Service Account Key JSON'));
      });
    }
  }
}
for (const mode of ['None', 'Bearer', 'Header', 'QueryParameter', 'Basic']) {
  test(`OpenAI selectable auth ${mode} produces the matching Partio contract`, () => {
    const form = { ApiFormat: 'OpenAI', Model: 'test', Endpoint: 'https://example.test', ApiKey: 'secret', ProviderSettings: null,
      Auth: { Mode: mode, HeaderName: 'X-Key', QueryParameterName: 'token', Username: 'user' } };
    const payload = buildConnectionPayload(form);
    assert.equal(payload.ProviderSettings.Auth.Mode, mode);
    assert.equal(payload.ApiKey, mode === 'None' ? null : 'secret');
    assert.deepEqual(validateConnection(form, 'completion'), {});
  });
}
test('masked nested credentials survive normal editing and explicit reveal', () => {
  const form = { ApiFormat: 'Bedrock', Model: 'model', ProviderSettings: { Region: 'us-east-1', SecretAccessKey: '****test', SessionToken: '****oken' } };
  assert.equal(buildConnectionPayload(form).ProviderSettings.Bedrock.SecretAccessKey, '****test');
  const revealed = mergeRevealedSecrets(form, { ProviderSettings: { Bedrock: { SecretAccessKey: 'secret-test', SessionToken: 'token', Region: 'changed' } } });
  assert.equal(revealed.ProviderSettings.SecretAccessKey, 'secret-test');
  assert.equal(revealed.ProviderSettings.Region, 'us-east-1');
});
test('picker excludes unsupported endpoint kinds', () => {
  assert.equal(getProvider('Anthropic').kinds.includes('embedding'), false);
  assert.equal(getProvider('TEI').kinds.includes('completion'), false);
  assert.equal(getProvider('VoyageAI').kinds.includes('completion'), false);
});

test('changing providers clears credentials from the previous provider', () => {
  const changed = applyProviderChange({ ApiFormat: 'OpenAI', ApiKey: '****test', Model: 'model', Endpoint: 'https://api.openai.com' }, 'Anthropic', 'completion', false);
  assert.equal(changed.ApiKey, '');
});
