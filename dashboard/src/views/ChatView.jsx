import React, { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { ApiClient } from '../utils/api';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneLight, oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';

function ChatView() {
  const { assistantId } = useParams();
  const [serverUrl] = useState(() => {
    return localStorage.getItem('ah_serverUrl') || window.location.origin;
  });
  const [assistant, setAssistant] = useState(null);
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [feedbackSent, setFeedbackSent] = useState({});
  const [showFeedbackText, setShowFeedbackText] = useState(null);
  const [feedbackText, setFeedbackText] = useState('');
  const messagesEndRef = useRef(null);
  const textareaRef = useRef(null);
  const [theme, setTheme] = useState(() => localStorage.getItem('ah_chat_theme') || 'light');

  // Apply theme
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  const toggleTheme = () => {
    const newTheme = theme === 'light' ? 'dark' : 'light';
    setTheme(newTheme);
    localStorage.setItem('ah_chat_theme', newTheme);
  };

  // Set favicon
  useEffect(() => {
    if (assistant?.FaviconUrl) {
      let link = document.querySelector("link[rel~='icon']");
      if (!link) {
        link = document.createElement('link');
        link.rel = 'icon';
        document.head.appendChild(link);
      }
      link.href = assistant.FaviconUrl;
    }
  }, [assistant?.FaviconUrl]);

  // Set document title
  useEffect(() => {
    if (assistant?.Title) {
      document.title = assistant.Title;
    } else if (assistant?.Name) {
      document.title = assistant.Name;
    }
  }, [assistant?.Title, assistant?.Name]);

  useEffect(() => {
    const fetchAssistant = async () => {
      try {
        const response = await fetch(`${serverUrl}/v1.0/assistants/${assistantId}/public`);
        if (response.ok) {
          const data = await response.json();
          setAssistant(data);
        } else {
          setError('Assistant not found');
        }
      } catch (err) {
        setError('Failed to connect to server');
      }
    };
    fetchAssistant();
  }, [serverUrl, assistantId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 200) + 'px';
    }
  }, [input]);

  const handleSend = async () => {
    if (!input.trim() || loading) return;
    const userMessage = input.trim();
    setInput('');
    setMessages(prev => [...prev, { role: 'user', content: userMessage }]);
    setLoading(true);

    try {
      const updatedMessages = [...messages, { role: 'user', content: userMessage }];
      const chatMessages = updatedMessages
        .filter(m => !m.isError)
        .map(({ role, content }) => ({ role, content }));

      // Add a placeholder assistant message for streaming
      let streamingIndex = null;
      const onDelta = (delta) => {
        if (delta.content) {
          setMessages(prev => {
            const updated = [...prev];
            if (streamingIndex === null) {
              streamingIndex = updated.length;
              updated.push({ role: 'assistant', content: delta.content, userMessage, isStreaming: true });
            } else {
              updated[streamingIndex] = {
                ...updated[streamingIndex],
                content: updated[streamingIndex].content + delta.content
              };
            }
            return updated;
          });
        }
      };

      const result = await ApiClient.chat(serverUrl, assistantId, chatMessages, onDelta);

      if (streamingIndex !== null) {
        // Streaming completed — finalize the message
        setMessages(prev => {
          const updated = [...prev];
          updated[streamingIndex] = {
            ...updated[streamingIndex],
            isStreaming: false,
            content: result.choices[0].message.content
          };
          return updated;
        });
      } else if (result.choices && result.choices.length > 0) {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: result.choices[0].message.content,
          userMessage: userMessage
        }]);
      } else if (result.Error) {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: result.Message || 'Sorry, I encountered an error.',
          isError: true
        }]);
      } else {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: 'Sorry, I encountered an error.',
          isError: true
        }]);
      }
    } catch (err) {
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: 'Failed to get response. Please try again.',
        isError: true
      }]);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleFeedback = (messageIndex, rating) => {
    setShowFeedbackText({ index: messageIndex, rating });
    setFeedbackText('');
  };

  const submitFeedback = async () => {
    if (!showFeedbackText) return;
    const { index, rating } = showFeedbackText;
    const msg = messages[index];

    try {
      // Build clean message history (exclude error messages and streaming flags)
      const messageHistory = messages
        .filter(m => !m.isError)
        .map(({ role, content }) => ({ role, content }));

      await ApiClient.submitFeedback(serverUrl, {
        AssistantId: assistantId,
        UserMessage: msg.userMessage || '',
        AssistantResponse: msg.content,
        Rating: rating,
        FeedbackText: feedbackText || null,
        MessageHistory: JSON.stringify(messageHistory)
      });
      setFeedbackSent(prev => ({ ...prev, [index]: rating }));
    } catch (err) {
      console.error('Failed to submit feedback:', err);
    }
    setShowFeedbackText(null);
    setFeedbackText('');
  };

  const [copiedBlock, setCopiedBlock] = useState(null);
  const copyCode = (code, id) => {
    navigator.clipboard.writeText(code);
    setCopiedBlock(id);
    setTimeout(() => setCopiedBlock(null), 2000);
  };

  let codeBlockCounter = useRef(0);

  const markdownComponents = {
    code({ node, inline, className, children, ...props }) {
      const match = /language-(\w+)/.exec(className || '');
      const codeString = String(children).replace(/\n$/, '');

      if (!inline && (match || codeString.includes('\n'))) {
        const blockId = `code-${codeBlockCounter.current++}`;
        const lang = match ? match[1] : 'text';
        return (
          <div className="chat-code-block">
            <div className="chat-code-header">
              <span className="chat-code-lang">{lang}</span>
              <button
                className="chat-code-copy"
                onClick={() => copyCode(codeString, blockId)}
              >
                {copiedBlock === blockId ? (
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="20 6 9 17 4 12"/></svg>
                ) : (
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>
                )}
                <span>{copiedBlock === blockId ? 'Copied' : 'Copy'}</span>
              </button>
            </div>
            <SyntaxHighlighter
              style={theme === 'dark' ? oneDark : oneLight}
              language={lang}
              PreTag="div"
              customStyle={{
                margin: 0,
                borderRadius: '0 0 8px 8px',
                fontSize: '0.8125rem',
              }}
              {...props}
            >
              {codeString}
            </SyntaxHighlighter>
          </div>
        );
      }
      return <code className="chat-inline-code" {...props}>{children}</code>;
    },
    table({ children }) {
      return (
        <div className="chat-table-wrapper">
          <table className="chat-md-table">{children}</table>
        </div>
      );
    },
    a({ href, children }) {
      return <a href={href} target="_blank" rel="noopener noreferrer" className="chat-md-link">{children}</a>;
    },
    img({ src, alt }) {
      return <img src={src} alt={alt} className="chat-md-image" />;
    },
  };

  const logoSrc = assistant?.LogoUrl || '/logo-no-text.png';
  const chatTitle = assistant?.Title || assistant?.Name || 'AssistantHub';

  if (error) {
    return (
      <div className="chat-page">
        <div className="chat-header">
          <div className="chat-header-inner">
            <img src="/logo-no-text.png" alt="AssistantHub" className="chat-header-logo" />
            <span className="chat-header-title">AssistantHub</span>
          </div>
        </div>
        <div className="chat-messages">
          <div className="chat-empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" opacity="0.4">
              <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            <p>{error}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="chat-page">
      {/* Header */}
      <div className="chat-header">
        <div className="chat-header-inner">
          <img
            src={logoSrc}
            alt={chatTitle}
            className="chat-header-logo"
            onError={(e) => { e.target.src = '/logo-no-text.png'; }}
          />
          <div className="chat-header-info">
            <div className="chat-header-title">{chatTitle}</div>
            {assistant?.Description && <div className="chat-header-desc">{assistant.Description}</div>}
          </div>
          <div className="chat-header-actions">
            <button className="chat-theme-toggle" onClick={toggleTheme} title={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}>
              {theme === 'light' ? (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
              ) : (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Messages */}
      <div className="chat-messages">
        <div className="chat-messages-inner">
          {messages.length === 0 && (
            <div className="chat-empty-state">
              <div className="chat-empty-icon">
                <img
                  src={logoSrc}
                  alt=""
                  className="chat-empty-logo"
                  onError={(e) => { e.target.src = '/logo-no-text.png'; }}
                />
              </div>
              <h2>How can I help you today?</h2>
              <p>Send a message to start chatting with {assistant?.Name || 'the assistant'}.</p>
            </div>
          )}
          {messages.map((msg, idx) => {
            codeBlockCounter.current = 0;
            return (
              <div key={idx} className={`chat-message-row ${msg.role}`}>
                {msg.role === 'assistant' && (
                  <div className="chat-avatar assistant-avatar">
                    <img
                      src={logoSrc}
                      alt=""
                      onError={(e) => { e.target.src = '/logo-no-text.png'; }}
                    />
                  </div>
                )}
                <div className="chat-message-content-wrap">
                  <div className={`chat-bubble ${msg.role}${msg.isError ? ' error' : ''}`}>
                    {msg.role === 'assistant' ? (
                      <div className="chat-markdown-content">
                        <ReactMarkdown
                          remarkPlugins={[remarkGfm]}
                          components={markdownComponents}
                        >
                          {msg.content}
                        </ReactMarkdown>
                      </div>
                    ) : (
                      <div className="chat-user-text">{msg.content}</div>
                    )}
                  </div>
                  {msg.role === 'assistant' && !msg.isError && (
                    <div className="chat-message-actions">
                      <button
                        className={`chat-action-btn ${feedbackSent[idx] === 'ThumbsUp' ? 'active' : ''}`}
                        onClick={() => handleFeedback(idx, 'ThumbsUp')}
                        disabled={!!feedbackSent[idx]}
                        title="Good response"
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill={feedbackSent[idx] === 'ThumbsUp' ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2">
                          <path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"/>
                        </svg>
                      </button>
                      <button
                        className={`chat-action-btn ${feedbackSent[idx] === 'ThumbsDown' ? 'active' : ''}`}
                        onClick={() => handleFeedback(idx, 'ThumbsDown')}
                        disabled={!!feedbackSent[idx]}
                        title="Bad response"
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill={feedbackSent[idx] === 'ThumbsDown' ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2">
                          <path d="M10 15v4a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3zm7-13h2.67A2.31 2.31 0 0 1 22 4v7a2.31 2.31 0 0 1-2.33 2H17"/>
                        </svg>
                      </button>
                    </div>
                  )}
                </div>
                {msg.role === 'user' && (
                  <div className="chat-avatar user-avatar">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                      <circle cx="12" cy="7" r="4"/>
                    </svg>
                  </div>
                )}
              </div>
            );
          })}
          {loading && !messages.some(m => m.isStreaming) && (
            <div className="chat-message-row assistant">
              <div className="chat-avatar assistant-avatar">
                <img
                  src={logoSrc}
                  alt=""
                  onError={(e) => { e.target.src = '/logo-no-text.png'; }}
                />
              </div>
              <div className="chat-message-content-wrap">
                <div className="chat-bubble assistant">
                  <div className="chat-typing-indicator">
                    <span></span><span></span><span></span>
                  </div>
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>
      </div>

      {/* Input Area */}
      <div className="chat-input-area">
        <div className="chat-input-container">
          <textarea
            ref={textareaRef}
            className="chat-input"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Message..."
            rows={1}
            disabled={loading}
          />
          <button
            className="chat-send-btn"
            onClick={handleSend}
            disabled={loading || !input.trim()}
            title="Send message"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="22" y1="2" x2="11" y2="13"/>
              <polygon points="22 2 15 22 11 13 2 9 22 2"/>
            </svg>
          </button>
        </div>
        <div className="chat-input-footer">
          <span>Press Enter to send, Shift+Enter for new line</span>
        </div>
      </div>

      {/* Feedback Modal */}
      {showFeedbackText && (
        <div className="chat-modal-overlay" onClick={() => setShowFeedbackText(null)}>
          <div className="chat-modal" onClick={(e) => e.stopPropagation()}>
            <div className="chat-modal-header">
              <h3>Provide feedback</h3>
              <button className="chat-modal-close" onClick={() => setShowFeedbackText(null)}>
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
              </button>
            </div>
            <div className="chat-modal-body">
              <p className="chat-modal-subtitle">
                {showFeedbackText.rating === 'ThumbsUp'
                  ? 'What did you like about this response?'
                  : 'What could be improved? (optional)'}
              </p>
              <textarea
                className="chat-modal-textarea"
                value={feedbackText}
                onChange={(e) => setFeedbackText(e.target.value)}
                rows={4}
                placeholder="Tell us more about this response..."
                autoFocus
              />
            </div>
            <div className="chat-modal-footer">
              <button className="chat-modal-btn secondary" onClick={() => { setShowFeedbackText(null); submitFeedback(); }}>Skip</button>
              <button className="chat-modal-btn primary" onClick={submitFeedback}>Submit feedback</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default ChatView;
