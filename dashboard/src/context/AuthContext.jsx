import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [serverUrl, setServerUrl] = useState(() => localStorage.getItem('ah_serverUrl') || '');
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('ah_user');
    return stored ? JSON.parse(stored) : null;
  });
  const [credential, setCredential] = useState(() => {
    const stored = localStorage.getItem('ah_credential');
    return stored ? JSON.parse(stored) : null;
  });
  const [tenant, setTenant] = useState(() => {
    const stored = localStorage.getItem('ah_tenant');
    return stored ? JSON.parse(stored) : null;
  });
  const [theme, setTheme] = useState(() => localStorage.getItem('ah_theme') || 'light');

  const [globalAdminFlag, setGlobalAdminFlag] = useState(() => {
    const stored = localStorage.getItem('ah_globalAdmin');
    return stored === 'true';
  });

  const isAuthenticated = !!(user && credential);
  const isAdmin = user?.IsAdmin || globalAdminFlag || false;
  const isGlobalAdmin = user?.IsAdmin || globalAdminFlag || false;
  const isTenantAdmin = user?.IsTenantAdmin || false;
  const tenantId = user?.TenantId || tenant?.Id || null;
  const tenantName = tenant?.Name || null;

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('ah_theme', theme);
  }, [theme]);

  useEffect(() => {
    if (serverUrl) localStorage.setItem('ah_serverUrl', serverUrl);
  }, [serverUrl]);

  useEffect(() => {
    if (user) localStorage.setItem('ah_user', JSON.stringify(user));
    else localStorage.removeItem('ah_user');
  }, [user]);

  useEffect(() => {
    if (credential) localStorage.setItem('ah_credential', JSON.stringify(credential));
    else localStorage.removeItem('ah_credential');
  }, [credential]);

  useEffect(() => {
    if (tenant) localStorage.setItem('ah_tenant', JSON.stringify(tenant));
    else localStorage.removeItem('ah_tenant');
  }, [tenant]);

  // On login, fetch whoami to get full auth context including tenant info
  const fetchWhoAmI = useCallback(async (url, token) => {
    try {
      const response = await fetch(`${url}/v1.0/whoami`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        if (data.TenantId) {
          // Fetch tenant details
          try {
            const tenantResp = await fetch(`${url}/v1.0/tenants/${data.TenantId}`, {
              headers: { 'Authorization': `Bearer ${token}` }
            });
            if (tenantResp.ok) {
              const tenantData = await tenantResp.json();
              setTenant(tenantData);
            }
          } catch { /* ignore */ }
        }
      }
    } catch { /* ignore */ }
  }, []);

  const login = useCallback(async (url, authBody) => {
    const response = await fetch(`${url}/v1.0/authenticate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(authBody)
    });
    const data = await response.json();
    if (data.Success) {
      setServerUrl(url);
      setUser(data.User);
      setCredential(data.Credential);
      if (data.IsGlobalAdmin) {
        setGlobalAdminFlag(true);
        localStorage.setItem('ah_globalAdmin', 'true');
      }
      // Fetch whoami for tenant context
      if (data.Credential?.BearerToken) {
        fetchWhoAmI(url, data.Credential.BearerToken);
      }
      return { success: true };
    }
    return { success: false, error: data.ErrorMessage || 'Authentication failed' };
  }, [fetchWhoAmI]);

  const logout = useCallback(() => {
    setUser(null);
    setCredential(null);
    setTenant(null);
    setGlobalAdminFlag(false);
    localStorage.removeItem('ah_user');
    localStorage.removeItem('ah_credential');
    localStorage.removeItem('ah_tenant');
    localStorage.removeItem('ah_globalAdmin');
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme(prev => prev === 'light' ? 'dark' : 'light');
  }, []);

  return (
    <AuthContext.Provider value={{
      serverUrl, setServerUrl, user, credential, tenant,
      isAuthenticated, isAdmin, isGlobalAdmin, isTenantAdmin,
      tenantId, tenantName, theme,
      login, logout, toggleTheme
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
