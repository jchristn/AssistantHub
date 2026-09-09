import React from 'react';

/**
 * Single source of truth for the left-nav. `Sidebar` renders from this, and it also
 * documents which routes each top-level item owns (via `matchers`) so the nav group and
 * item stay highlighted while the user is on a tab route within the corresponding hub.
 *
 * gate values: 'none' | 'admin' (global admin) | 'adminOrTenant' | 'globalAdmin'
 * `matchers` are route path prefixes owned by the item (used for active highlighting).
 */

const icon = {
  documents: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>,
  crawlers: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/></svg>,
  assistants: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a3 3 0 0 0-3 3v4a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" y1="19" x2="12" y2="23"/><line x1="8" y1="23" x2="16" y2="23"/></svg>,
  requestHistory: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M3 3v18h18"/><path d="M7 14l3-3 3 2 4-5"/></svg>,
  apiExplorer: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10 20.5l-6-8 6-8"/><path d="M14 3.5l6 8-6 8"/><line x1="12" y1="2" x2="12" y2="22"/></svg>,
  buckets: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="21 8 21 21 3 21 3 8"/><rect x="1" y="3" width="22" height="5"/><line x1="10" y1="12" x2="14" y2="12"/></svg>,
  collections: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>,
  indices: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 6h16"/><path d="M4 12h16"/><path d="M4 18h16"/><circle cx="8" cy="6" r="2"/><circle cx="16" cy="12" r="2"/><circle cx="10" cy="18" r="2"/></svg>,
  endpoints: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/></svg>,
  ingestionRules: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>,
  authentication: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>,
  settings: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>,
};

export const navSections = [
  {
    key: 'ingestion',
    label: 'Ingestion',
    gate: 'none',
    tourTarget: 'documents',
    items: [
      { path: '/documents', label: 'Documents', gate: 'none', icon: icon.documents, matchers: ['/documents'] },
      { path: '/crawlers', label: 'Crawlers', gate: 'none', icon: icon.crawlers, matchers: ['/crawlers'] },
    ],
  },
  {
    key: 'chat',
    label: 'Chat',
    gate: 'none',
    tourTarget: 'assistants',
    items: [
      {
        path: '/assistants', label: 'Assistants', gate: 'none', icon: icon.assistants,
        matchers: ['/assistants', '/assistant-settings', '/feedback', '/history', '/assistant-analytics', '/evaluation'],
      },
    ],
  },
  {
    key: 'monitoring',
    label: 'Monitoring',
    gate: 'adminOrTenant',
    items: [
      { path: '/request-history', label: 'Request History', gate: 'adminOrTenant', icon: icon.requestHistory, matchers: ['/request-history'] },
      { path: '/api-explorer', label: 'API Explorer', gate: 'adminOrTenant', icon: icon.apiExplorer, matchers: ['/api-explorer'] },
    ],
  },
  {
    key: 'artifacts',
    label: 'Artifacts',
    gate: 'adminOrTenant',
    tourTarget: 'artifacts',
    items: [
      { path: '/buckets', label: 'Buckets', gate: 'admin', icon: icon.buckets, matchers: ['/buckets', '/objects'] },
      { path: '/collections', label: 'Collections', gate: 'admin', icon: icon.collections, matchers: ['/collections', '/records'] },
      { path: '/indices', label: 'Indices', gate: 'admin', icon: icon.indices, matchers: ['/indices'] },
    ],
  },
  {
    key: 'configuration',
    label: 'Configuration',
    gate: 'adminOrTenant',
    tourTarget: 'configuration',
    items: [
      { path: '/endpoints', label: 'Endpoints', gate: 'admin', icon: icon.endpoints, matchers: ['/endpoints', '/models'] },
      { path: '/ingestion-rules', label: 'Ingestion Rules', gate: 'adminOrTenant', icon: icon.ingestionRules, matchers: ['/ingestion-rules'] },
      { path: '/authentication', label: 'Authentication', gate: 'adminOrTenant', icon: icon.authentication, matchers: ['/authentication', '/tenants', '/users', '/credentials'] },
      { path: '/configuration', label: 'Settings', gate: 'globalAdmin', icon: icon.settings, matchers: ['/configuration'] },
    ],
  },
];

/** Evaluate a gate string against the current auth flags. */
export function gateVisible(gate, auth) {
  const { isAdmin, isGlobalAdmin, isTenantAdmin } = auth;
  switch (gate) {
    case 'globalAdmin': return !!isGlobalAdmin;
    case 'admin': return !!isAdmin;
    case 'adminOrTenant': return !!(isGlobalAdmin || isTenantAdmin);
    case 'none':
    default: return true;
  }
}

/** True when the current path belongs to the given nav item (exact or matcher prefix). */
export function itemIsActive(item, pathname) {
  const matchers = item.matchers && item.matchers.length ? item.matchers : [item.path];
  return matchers.some((m) => pathname === m || pathname.startsWith(`${m}/`));
}
