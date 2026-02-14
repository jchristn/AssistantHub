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
  const [theme, setTheme] = useState(() => localStorage.getItem('ah_theme') || 'light');

  const isAuthenticated = !!(user && credential);
  const isAdmin = user?.IsAdmin || false;

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
      return { success: true };
    }
    return { success: false, error: data.ErrorMessage || 'Authentication failed' };
  }, []);

  const logout = useCallback(() => {
    setUser(null);
    setCredential(null);
    localStorage.removeItem('ah_user');
    localStorage.removeItem('ah_credential');
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme(prev => prev === 'light' ? 'dark' : 'light');
  }, []);

  return (
    <AuthContext.Provider value={{
      serverUrl, setServerUrl, user, credential,
      isAuthenticated, isAdmin, theme,
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
