import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';

function Login() {
  const { login, theme, toggleTheme } = useAuth();
  const [serverUrl, setServerUrl] = useState(() => localStorage.getItem('ah_serverUrl') || window.ASSISTANTHUB_SERVER_URL || 'http://localhost:8801');
  const [authMode, setAuthMode] = useState('bearer'); // 'email' or 'bearer'
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [tenantId, setTenantId] = useState('');
  const [bearerToken, setBearerToken] = useState('');
  const [showToken, setShowToken] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const url = serverUrl.replace(/\/+$/, '');
      let authBody;
      if (authMode === 'email') {
        authBody = { Email: email, Password: password };
        if (tenantId.trim()) authBody.TenantId = tenantId.trim();
      } else {
        authBody = { BearerToken: bearerToken };
      }
      const result = await login(url, authBody);
      if (!result.success) {
        setError(result.error || 'Authentication failed');
      }
    } catch (err) {
      setError('Connection failed. Check server URL.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <button className="theme-toggle login-theme-toggle" onClick={toggleTheme} title="Toggle theme">
        {theme === 'light' ? (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
        ) : (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
        )}
      </button>
      <div className="login-container">
        <div className="login-logo">
          <img src="/logo-new-full.png" alt="AssistantHub" />
        </div>
        <h2 className="login-title">Sign In</h2>
        {error && <div className="login-error">{error}</div>}
        <form className="login-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Server URL</label>
            <input type="text" value={serverUrl} onChange={(e) => setServerUrl(e.target.value)} placeholder="http://localhost:8800" required />
          </div>
          {authMode === 'email' ? (
            <>
              <div className="form-group">
                <label>Tenant ID <span style={{ fontWeight: 'normal', color: 'var(--text-secondary, #888)', fontSize: '0.8em' }}>(optional)</span></label>
                <input type="text" value={tenantId} onChange={(e) => setTenantId(e.target.value)} placeholder="default" />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@assistanthub.local" required />
              </div>
              <div className="form-group">
                <label>Password</label>
                <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
              </div>
            </>
          ) : (
            <div className="form-group">
              <label>Bearer Token</label>
              <div style={{ position: 'relative' }}>
                <input type={showToken ? 'text' : 'password'} value={bearerToken} onChange={(e) => setBearerToken(e.target.value)} placeholder="Enter bearer token" required style={{ paddingRight: '40px' }} />
                <button type="button" onClick={() => setShowToken(!showToken)} style={{ position: 'absolute', right: '8px', top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', padding: '4px', color: 'var(--text-secondary, #666)', display: 'flex', alignItems: 'center' }} title={showToken ? 'Hide token' : 'Show token'}>
                  {showToken ? (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/><path d="M14.12 14.12a3 3 0 1 1-4.24-4.24"/></svg>
                  ) : (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                  )}
                </button>
              </div>
            </div>
          )}
          <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
        <div className="login-toggle">
          <button onClick={() => setAuthMode(authMode === 'email' ? 'bearer' : 'email')}>
            {authMode === 'email' ? 'Use Bearer Token instead' : 'Use Email/Password instead'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default Login;
