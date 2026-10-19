import React from 'react';
import Tooltip from '../Tooltip';
import PasswordInput from '../PasswordInput';
import { getEndpointProviderFields, resolveProviderEndpoint } from '../../utils/endpointDefaults';

export default function EndpointConnectionFields({ form, onChange }) {
  const fields = getEndpointProviderFields(form.ApiFormat);
  return (
    <>
      <div className="form-group">
        <label><Tooltip text={fields.endpointHelp}>Endpoint</Tooltip></label>
        <input
          type="text"
          value={resolveProviderEndpoint(form.ApiFormat, form.Endpoint)}
          onChange={e => onChange('Endpoint', e.target.value)}
          readOnly={!!fields.fixedEndpoint}
          required
        />
        <small>{fields.endpointHelp}</small>
      </div>
      <div className="form-group">
        <label><Tooltip text={fields.credentialHelp}>{fields.credentialLabel} {fields.credentialRequired ? '(required)' : '(optional)'}</Tooltip></label>
        <PasswordInput
          value={form.ApiKey}
          onChange={e => onChange('ApiKey', e.target.value)}
          placeholder={fields.credentialRequired ? 'Required' : 'Optional'}
          required={fields.credentialRequired}
          aria-label={fields.credentialLabel}
        />
        <small>{fields.credentialHelp}</small>
      </div>
    </>
  );
}
