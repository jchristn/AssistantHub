import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import ChatView from './views/ChatView';

function App() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/chat/:assistantId" element={<ChatView />} />
      <Route path="/login" element={isAuthenticated ? <Navigate to="/" /> : <Login />} />
      <Route path="/*" element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" />} />
    </Routes>
  );
}

export default App;
