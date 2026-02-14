import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';

function Login() {
  const { login, theme, toggleTheme } = useAuth();
  const [serverUrl, setServerUrl] = useState(() => localStorage.getItem('ah_serverUrl') || 'http://localhost:8800');
  const [authMode, setAuthMode] = useState('bearer'); // 'email' or 'bearer'
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [bearerToken, setBearerToken] = useState('');
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
          <img src={theme === 'dark' ? '/logo-white.png' : '/logo-black.png'} alt="AssistantHub" />
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
              <input type="text" value={bearerToken} onChange={(e) => setBearerToken(e.target.value)} placeholder="Enter bearer token" required />
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
