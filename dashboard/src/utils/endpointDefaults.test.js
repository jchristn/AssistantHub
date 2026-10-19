import test from 'node:test';
import assert from 'node:assert/strict';
import { API_FORMAT_OPTIONS, getApiFormatDefaults, getDefaultHealthCheckUrl, getDefaultModel, getDefaultToolCallingApiFormat, getHealthCheckUrlForEndpointChange } from './endpointDefaults.js';

for (const [format, endpoint, path] of [['OllamaCloud', 'https://ollama.com', '/api/tags'], ['xAI', 'https://api.x.ai', '/v1/models']]) {
  test(`${format} defaults configure authenticated cloud health checks`, () => {
    assert.ok(API_FORMAT_OPTIONS.includes(format));
    const defaults = getApiFormatDefaults(format);
    assert.equal(defaults.Endpoint, endpoint);
    assert.equal(defaults.HealthCheckUrl, endpoint + path);
    assert.equal(defaults.HealthCheckUseAuth, true);
    assert.equal(getDefaultModel(format, 'embedding'), '');
    assert.equal(getHealthCheckUrlForEndpointChange(endpoint + path, endpoint, 'https://custom.example', format), 'https://custom.example' + path);
  });
}
test('xAI version suffix is not duplicated', () => {
  assert.equal(getDefaultHealthCheckUrl('https://api.x.ai/v1/', 'xAI'), 'https://api.x.ai/v1/models');
});

test('cloud tool formats follow their native wire protocols', () => {
  assert.equal(getDefaultToolCallingApiFormat('OllamaCloud'), 'OllamaChat');
  assert.equal(getDefaultToolCallingApiFormat('xAI'), 'OpenAIChatCompletions');
});
