import React, { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';

/**
 * URL-synced tabbed surface. The active tab lives in the query string (default `?tab=`),
 * so bookmarks, deep links, and cross-page click-throughs can land on a specific tab.
 * Only the active tab's panel is mounted (lazy render). Other query params are preserved
 * when switching tabs, which is how entity-scoped hubs carry a selection across tabs.
 *
 * tabs: [{ key, label, render: () => ReactNode, hidden?: bool }]
 */
function Tabs({ tabs, param = 'tab', defaultTabKey, actions, ariaLabel }) {
  const [searchParams, setSearchParams] = useSearchParams();

  const visibleTabs = useMemo(() => (tabs || []).filter((t) => !t.hidden), [tabs]);

  const fallbackKey = defaultTabKey && visibleTabs.some((t) => t.key === defaultTabKey)
    ? defaultTabKey
    : visibleTabs[0]?.key;

  const requestedKey = searchParams.get(param);
  const activeKey = requestedKey && visibleTabs.some((t) => t.key === requestedKey)
    ? requestedKey
    : fallbackKey;

  const selectTab = useCallback((key) => {
    const next = new URLSearchParams(searchParams);
    next.set(param, key);
    setSearchParams(next);
  }, [param, searchParams, setSearchParams]);

  const onKeyDown = useCallback((event) => {
    if (visibleTabs.length === 0) return;
    const currentIndex = visibleTabs.findIndex((t) => t.key === activeKey);
    let nextIndex = -1;
    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') nextIndex = (currentIndex + 1) % visibleTabs.length;
    else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') nextIndex = (currentIndex - 1 + visibleTabs.length) % visibleTabs.length;
    else if (event.key === 'Home') nextIndex = 0;
    else if (event.key === 'End') nextIndex = visibleTabs.length - 1;
    if (nextIndex >= 0) {
      event.preventDefault();
      selectTab(visibleTabs[nextIndex].key);
    }
  }, [activeKey, selectTab, visibleTabs]);

  const activeTab = visibleTabs.find((t) => t.key === activeKey);

  return (
    <div className="page-tabs">
      <div className="page-tabs-bar">
        <div className="page-tabs-list" role="tablist" aria-label={ariaLabel} onKeyDown={onKeyDown}>
          {visibleTabs.map((tab) => {
            const selected = tab.key === activeKey;
            return (
              <button
                key={tab.key}
                type="button"
                role="tab"
                id={`tab-${param}-${tab.key}`}
                aria-selected={selected}
                aria-controls={`tabpanel-${param}-${tab.key}`}
                tabIndex={selected ? 0 : -1}
                className={`page-tab${selected ? ' active' : ''}`}
                onClick={() => selectTab(tab.key)}
              >
                {tab.label}
              </button>
            );
          })}
        </div>
        {actions && <div className="page-tabs-actions">{actions}</div>}
      </div>
      {activeTab && (
        <div
          role="tabpanel"
          id={`tabpanel-${param}-${activeTab.key}`}
          aria-labelledby={`tab-${param}-${activeTab.key}`}
          className="page-tab-panel"
        >
          {activeTab.render()}
        </div>
      )}
    </div>
  );
}

export default Tabs;
