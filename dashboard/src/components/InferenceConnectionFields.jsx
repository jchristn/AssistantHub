import React from 'react';
import ProviderPicker from './ProviderPicker';
import ProviderConnectionFields from './ProviderConnectionFields';
import { applyProviderChange, authFromEndpoint, buildConnectionPayload, hasEndpointOverride, providerSettingsToForm } from '../utils/providers';

export function inferenceConnectionForm(settings) {
  const ApiFormat = settings.Provider || "Ollama";
  const ProviderSettings = providerSettingsToForm(ApiFormat, settings.ProviderSettings);
  return { ApiFormat, Model: settings.DefaultModel || "", Endpoint: settings.Endpoint || "", ApiKey: settings.ApiKey || "", ProviderSettings,
    Auth: authFromEndpoint({ ...settings, ApiFormat }), EndpointOverride: hasEndpointOverride(ApiFormat, settings.Endpoint, ProviderSettings) };
}

export default function InferenceConnectionFields({ settings, onChange }) {
  const form = inferenceConnectionForm(settings);
  const ApiFormat = form.ApiFormat;
  const update = next => {
    const connection = buildConnectionPayload(next);
    onChange({ ...settings, Provider: next.ApiFormat, Endpoint: connection.Endpoint, ApiKey: connection.ApiKey,
      DefaultModel: connection.Model, ProviderSettings: connection.ProviderSettings });
  };
  return <>
    <ProviderPicker kind="completion" value={ApiFormat} onChange={value => update(applyProviderChange(form, value, 'completion', false))} />
    <ProviderConnectionFields kind="completion" form={form} setForm={update} healthFieldsEdited={false} />
  </>;
}
