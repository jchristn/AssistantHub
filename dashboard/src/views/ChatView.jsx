import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import ChatPanel from '../components/ChatPanel';

function ChatView() {
  const { assistantId } = useParams();
  const [theme, setTheme] = useState(() => localStorage.getItem('ah_chat_theme') || 'light');

  // Apply theme to document (standalone page owns the document theme)
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  // Listen for theme changes from ChatPanel via localStorage
  useEffect(() => {
    const handleStorage = (e) => {
      if (e.key === 'ah_chat_theme') {
        setTheme(e.newValue || 'light');
      }
    };
    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, []);

  return (
    <div className="chat-page">
      <ChatPanel assistantId={assistantId} showHeader={true} showStatusBar={true} />
    </div>
  );
}

export default ChatView;
