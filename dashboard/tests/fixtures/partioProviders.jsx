import React from 'react';
import { createRoot } from 'react-dom/client';
import Inference from '../../src/components/modals/InferenceEndpointFormModal';
import Embedding from '../../src/components/modals/EmbeddingEndpointFormModal';
import '../../src/App.css';
const params = new URLSearchParams(location.search);
const Component = params.get('kind') === 'embedding' ? Embedding : Inference;
const editing = params.has('edit');
const source = params.get('format') === 'Bedrock'
  ? { Id: 'cep_test', ApiFormat: 'Bedrock', Model: 'test-model', Active: false, ProviderSettings: { Bedrock: { Region: 'us-east-1', CredentialSource: 'Static', AccessKeyId: 'dummy-access', SecretAccessKey: '****cret', SessionToken: '****oken' } } }
  : { Id: 'ep_test', ApiFormat: 'Ollama', Model: 'test-model', Endpoint: 'http://localhost:11434', Active: false };
window.saved = []; window.reveals = 0;
createRoot(document.getElementById('root')).render(<Component {...{ [editing ? 'endpoint' : 'initialData']: source }} onClose={() => {}}
  onSave={async data => { window.saved.push(data); }}
  onRevealEndpoint={async () => { window.reveals++; return { ...source, ProviderSettings: { Bedrock: { ...source.ProviderSettings?.Bedrock, SecretAccessKey: 'dummy-secret', SessionToken: 'dummy-token' } } }; }} />);
