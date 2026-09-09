import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { ApiClient } from '../../utils/api';
import Tabs from '../../components/Tabs';

/**
 * Hub layout with a persistent scope selector rendered in the tab strip's actions area.
 * The selected scope lives in a query param (e.g. ?bucket=, ?collectionId=, ?indexId=,
 * ?assistantId=) so it carries across tab switches and deep links. Each tab's render()
 * receives the current scope id.
 *
 * loadOptions(api) -> Promise<[{ value, label }]>
 * tabs: [{ key, label, render: (scopeId) => ReactNode, hidden? }]
 */
function ScopedHub({ scopeParam, scopeLabel, scopePlaceholder, loadOptions, tabs, defaultTabKey, ariaLabel }) {
  const { serverUrl, credential } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const [options, setOptions] = useState([]);
  const scopeId = searchParams.get(scopeParam) || '';

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const opts = await loadOptions(new ApiClient(serverUrl, credential?.BearerToken));
        if (!cancelled) setOptions(Array.isArray(opts) ? opts : []);
      } catch (err) {
        if (!cancelled) setOptions([]);
      }
    })();
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serverUrl, credential]);

  const setScope = (value) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(scopeParam, value);
    else next.delete(scopeParam);
    setSearchParams(next);
  };

  const scopeSelector = (
    <label className="hub-scope">
      <span className="hub-scope-label">{scopeLabel}</span>
      <select className="hub-scope-select" value={scopeId} onChange={(e) => setScope(e.target.value)}>
        <option value="">{scopePlaceholder || `All ${scopeLabel.toLowerCase()}s`}</option>
        {options.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>
    </label>
  );

  const tabsWithScope = tabs.map((t) => ({ ...t, render: () => t.render(scopeId) }));

  return <Tabs tabs={tabsWithScope} defaultTabKey={defaultTabKey} ariaLabel={ariaLabel} actions={scopeSelector} />;
}

export default ScopedHub;
