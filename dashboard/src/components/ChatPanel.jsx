import React, { useState, useEffect, useRef } from 'react';
import { ApiClient } from '../utils/api';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneLight, oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';

const WAIT_MESSAGES = [
  "Scanning available sources...",
  "Searching all the things...",
  "Consulting the oracle...",
  "Asking the hive mind...",
  "Rummaging through the archives...",
  "Dusting off the encyclopedia...",
  "Putting on my thinking cap...",
  "Crunching the numbers...",
  "Reading between the lines...",
  "Diving into the deep end...",
  "Connecting the dots...",
  "Spinning up the hamster wheel...",
  "Warming up the neurons...",
  "Poking the knowledge base...",
  "Doing a quick Snoopy dance...",
  "Translating from robot to human...",
  "Assembling the brain trust...",
  "Percolating some thoughts...",
  "Mining for gold nuggets...",
  "Untangling the web of knowledge...",
  "Firing up the think tank...",
  "Channeling my inner librarian...",
  "Shuffling through the card catalog...",
  "Brewing a fresh pot of answers...",
  "Loading infinite wisdom...",
  "Calibrating the answer engine...",
  "Tuning into the right frequency...",
  "Decoding the matrix...",
  "Phoning a friend...",
  "Checking under every rock...",
  "Flipping through the pages...",
  "Cross-referencing the universe...",
  "Letting the gears turn...",
  "Interrogating the database...",
  "Shaking the magic 8-ball...",
  "Feeding the thinking machine...",
  "Scanning the horizon...",
  "Chasing down a lead...",
  "Following the breadcrumbs...",
  "Piecing together the puzzle...",
  "Running the numbers...",
  "Consulting the archives...",
  "Sifting through the data...",
  "Waking up the brain cells...",
  "Charging the flux capacitor...",
  "Engaging warp drive...",
  "Booting up the answer module...",
  "Querying the mothership...",
  "Processing at light speed...",
  "Herding the relevant bits...",
  "Compiling a brilliant response...",
  "Fetching something clever...",
  "Reaching into the knowledge vault...",
  "Unraveling the mystery...",
  "Sorting through the haystack...",
  "Picking the brain trust...",
  "Doing some heavy lifting...",
  "Stretching the neural networks...",
  "Excavating the answer mine...",
  "Plucking from the wisdom tree...",
  "Negotiating with the algorithms...",
  "Winding up the clockwork...",
  "Tapping into the mainframe...",
  "Downloading enlightenment...",
  "Hunting for the good stuff...",
  "Marinating on that one...",
  "Rolling up my sleeves...",
  "Taking the scenic route through the data...",
  "Asking the rubber duck...",
  "Revving up the inference engine...",
  "Warming up the word generator...",
  "Sharpening the pencils...",
  "Composing a masterpiece...",
  "Loading the knowledge cannons...",
  "Tuning the signal...",
  "Riffling through the filing cabinet...",
  "Summoning the muse...",
  "Thinking really, really hard...",
  "Pondering the imponderables...",
  "Tickling the transistors...",
  "Unleashing the research bots...",
  "Navigating the info superhighway...",
  "Letting it simmer...",
  "Putting the pieces together...",
  "Combing through the details...",
  "Rustling up an answer...",
  "Giving it the old college try...",
  "Reviewing all the things...",
  "Trawling the knowledge ocean...",
  "Doing a deep dive...",
  "Wrangling the data gremlins...",
  "Cranking the idea generator...",
  "Running it through the think-o-matic...",
  "Chewing on that thought...",
  "Queuing up something good...",
  "Assembling the response squad...",
  "Digging through the treasure chest...",
  "Mapping the answer landscape...",
  "Triple-checking the double-checked coordinates...",
  "Thinking through that...",
  "Reviewing relevant context...",
  "Pulling in what matters...",
  "Aligning the signals...",
  "Organizing the pieces...",
  "Evaluating possibilities...",
  "Structuring a clear answer...",
  "Sorting signal from noise...",
  "Narrowing it down...",
  "Framing the response...",
  "Refining the details...",
  "Weighing the options...",
  "Checking internal consistency...",
  "Matching patterns...",
  "Looking at it from a few angles...",
  "Filtering the essentials...",
  "Drafting something helpful...",
  "Cross-checking assumptions...",
  "Calibrating the reply...",
  "Consolidating insights...",
  "Synthesizing information...",
  "Lining up the facts...",
  "Reviewing prior context...",
  "Selecting the strongest signal...",
  "Building the outline...",
  "Arranging the logic...",
  "Verifying the flow...",
  "Polishing clarity...",
  "Reducing ambiguity...",
  "Filling in the gaps...",
  "Stress-testing the reasoning...",
  "Clarifying the structure...",
  "Evaluating tradeoffs...",
  "Checking edge cases...",
  "Testing for completeness...",
  "Ensuring coherence...",
  "Connecting relevant ideas...",
  "Drafting carefully...",
  "Balancing precision and clarity...",
  "Optimizing for usefulness...",
  "Mapping dependencies...",
  "Examining implications...",
  "Tightening the explanation...",
  "Adjusting for context...",
  "Reviewing constraints...",
  "Harmonizing the components...",
  "Making it concise...",
  "Expanding where needed...",
  "Trimming the excess...",
  "Validating the approach...",
  "Rechecking assumptions...",
  "Improving readability...",
  "Strengthening the argument...",
  "Clarifying intent...",
  "Preparing a thoughtful answer...",
  "Evaluating supporting details...",
  "Checking for gaps...",
  "Aligning with your question...",
  "Confirming relevance...",
  "Focusing the response...",
  "Resolving ambiguities...",
  "Structuring the output...",
  "Weaving it together...",
  "Shaping the explanation...",
  "Refining the tone...",
  "Validating logic paths...",
  "Looking for blind spots...",
  "Checking consistency...",
  "Preparing something solid...",
  "Finalizing the structure...",
  "Drafting with care...",
  "Making it actionable...",
  "Enhancing clarity...",
  "Organizing key points...",
  "Reviewing dependencies...",
  "Checking assumptions twice...",
  "Connecting related threads...",
  "Sharpening the conclusion...",
  "Smoothing the transitions...",
  "Aligning the reasoning...",
  "Balancing depth and brevity...",
  "Re-evaluating context...",
  "Prioritizing what matters...",
  "Distilling the essentials...",
  "Stress-testing the logic...",
  "Validating the reasoning chain...",
  "Ensuring alignment...",
  "Clarifying the answer path...",
  "Drafting the response...",
  "Tightening up the logic...",
  "Reviewing implications...",
  "Clarifying tradeoffs...",
  "Checking constraints...",
  "Reviewing the signal strength...",
  "Refining the approach...",
  "Ensuring usefulness...",
  "Organizing insights...",
  "Final pass underway...",
  "Wrapping it together...",
  "Almost ready...",
  "Getting there...",
];

function ChatPanel({ assistantId, showHeader = true, showStatusBar = true, theme: themeProp, onClose }) {
  const [serverUrl] = useState(() => {
    return localStorage.getItem('ah_serverUrl') || window.location.origin;
  });
  const [assistant, setAssistant] = useState(null);
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [waitMessage, setWaitMessage] = useState('');
  const recentWaitMessages = useRef([]);

  function pickWaitMessage() {
    const available = WAIT_MESSAGES.filter(m => !recentWaitMessages.current.includes(m));
    const pool = available.length > 0 ? available : WAIT_MESSAGES;
    const picked = pool[Math.floor(Math.random() * pool.length)];
    recentWaitMessages.current.push(picked);
    if (recentWaitMessages.current.length > 20) recentWaitMessages.current.shift();
    return picked;
  }
  const [compacting, setCompacting] = useState(false);
  const [error, setError] = useState(null);
  const [feedbackSent, setFeedbackSent] = useState({});
  const [showFeedbackText, setShowFeedbackText] = useState(null);
  const [feedbackText, setFeedbackText] = useState('');
  const [chatName, setChatName] = useState(null);
  const messagesEndRef = useRef(null);
  const textareaRef = useRef(null);
  const titleRequestedRef = useRef(false);
  const [threadId, setThreadId] = useState(() => {
    return localStorage.getItem(`ah_thread_${assistantId}`) || null;
  });
  const [internalTheme, setInternalTheme] = useState(() => localStorage.getItem('ah_chat_theme') || 'light');
  const [contextUsage, setContextUsage] = useState(null);

  // Use prop theme if provided, otherwise internal
  const theme = themeProp || internalTheme;
  const managesOwnTheme = !themeProp;

  const toggleTheme = () => {
    if (!managesOwnTheme) return;
    const newTheme = internalTheme === 'light' ? 'dark' : 'light';
    setInternalTheme(newTheme);
    localStorage.setItem('ah_chat_theme', newTheme);
  };

  // Reset state when assistantId changes
  useEffect(() => {
    setMessages([]);
    setInput('');
    setLoading(false);
    setCompacting(false);
    setError(null);
    setFeedbackSent({});
    setShowFeedbackText(null);
    setFeedbackText('');
    setChatName(null);
    setContextUsage(null);
    titleRequestedRef.current = false;
    setAssistant(null);
    setThreadId(localStorage.getItem(`ah_thread_${assistantId}`) || null);
  }, [assistantId]);

  useEffect(() => {
    if (!assistantId) return;
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

  // Persist threadId to localStorage
  useEffect(() => {
    if (!assistantId) return;
    const key = `ah_thread_${assistantId}`;
    if (threadId) {
      localStorage.setItem(key, threadId);
    } else {
      localStorage.removeItem(key);
    }
  }, [threadId, assistantId]);

  // Load chat history from server when a stored threadId exists
  useEffect(() => {
    if (!threadId || !serverUrl || !assistantId) return;
    let cancelled = false;
    const loadHistory = async () => {
      try {
        const history = await ApiClient.getThreadHistory(serverUrl, assistantId, threadId);
        if (cancelled || !history || history.length === 0) return;
        const restored = [];
        for (const entry of history) {
          if (entry.UserMessage) restored.push({ role: 'user', content: entry.UserMessage });
          if (entry.AssistantResponse) restored.push({ role: 'assistant', content: entry.AssistantResponse, userMessage: entry.UserMessage });
        }
        if (restored.length > 0) {
          setMessages(restored);
          titleRequestedRef.current = true;
          generateChatTitle(restored);
        }
      } catch (err) {
        console.error('Failed to load chat history:', err);
      }
    };
    loadHistory();
    return () => { cancelled = true; };
  }, [assistantId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Focus textarea when loading completes
  useEffect(() => {
    if (!loading && textareaRef.current) {
      textareaRef.current.focus();
    }
  }, [loading]);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 200) + 'px';
    }
  }, [input]);

  const generateChatTitle = async (conversationMessages) => {
    try {
      const titleMessages = [
        ...conversationMessages.filter(m => !m.isError).map(({ role, content }) => ({ role, content })),
        {
          role: 'user',
          content: 'Generate a short title (max 6 words) for this conversation. Reply with ONLY the title text, nothing else.'
        }
      ];
      const result = await ApiClient.generate(serverUrl, assistantId, titleMessages);
      if (result.choices && result.choices.length > 0) {
        const title = result.choices[0].message.content.trim().replace(/^["']|["']$/g, '');
        setChatName(title);
      }
    } catch (err) {
      console.error('Failed to generate chat title:', err);
    }
  };

  const handleClear = () => {
    setMessages([]);
    setThreadId(null);
    localStorage.removeItem(`ah_thread_${assistantId}`);
    setChatName(null);
    titleRequestedRef.current = false;
    setFeedbackSent({});
    setError(null);
    setContextUsage(null);
  };

  const handleCompact = async () => {
    const chatMessages = messages.filter(m => !m.isError && !m.isSystem).map(({ role, content }) => ({ role, content }));
    if (chatMessages.length < 3) {
      setMessages(prev => [...prev, { role: 'system', content: 'Not enough conversation to compact.', isSystem: true }]);
      return;
    }
    setLoading(true);
    setCompacting(true);
    setMessages(prev => [...prev, { role: 'system', content: 'Please wait while the conversation is compacted...', isSystem: true, isCompacting: true }]);
    try {
      const result = await ApiClient.compact(serverUrl, assistantId, chatMessages, threadId);
      if (result.messages) {
        const compacted = result.messages.map(m => ({ role: m.role || m.Role, content: m.content || m.Content }));
        const lastAssistant = [...messages].reverse().find(m => m.role === 'assistant' && !m.isError);
        if (lastAssistant) {
          compacted.push({ role: 'assistant', content: lastAssistant.content, userMessage: lastAssistant.userMessage, citations: lastAssistant.citations || null });
        }
        compacted.push({ role: 'system', content: 'Conversation compacted successfully.', isSystem: true });
        setMessages(compacted);
      }
      if (result.usage) {
        setContextUsage(result.usage);
      }
    } catch (err) {
      setMessages(prev => [...prev.filter(m => !m.isCompacting), { role: 'system', content: 'Failed to compact: ' + err.message, isSystem: true }]);
    } finally {
      setLoading(false);
      setCompacting(false);
    }
  };

  const handleContext = () => {
    const chatMessages = messages.filter(m => !m.isError && !m.isSystem);
    const userCount = chatMessages.filter(m => m.role === 'user').length;
    const assistantCount = chatMessages.filter(m => m.role === 'assistant').length;
    const totalChars = chatMessages.reduce((sum, m) => sum + (m.content?.length || 0), 0);
    const estimatedTokens = Math.ceil(totalChars / 4);

    let md = '### Current Context\n\n';
    md += `| Property | Value |\n|----------|-------|\n`;
    md += `| **Assistant** | ${assistant?.Name || 'Unknown'} |\n`;
    md += `| **Assistant ID** | \`${assistantId}\` |\n`;
    md += `| **Thread ID** | ${threadId ? `\`${threadId}\`` : '_None (new thread on next message)_'} |\n`;
    md += `| **Messages** | ${chatMessages.length} (${userCount} user, ${assistantCount} assistant) |\n`;
    md += `| **Est. tokens** | ~${estimatedTokens.toLocaleString()} |\n`;

    if (contextUsage) {
      if (contextUsage.prompt_tokens != null) md += `| **Prompt tokens** | ${contextUsage.prompt_tokens.toLocaleString()} |\n`;
      if (contextUsage.completion_tokens != null) md += `| **Completion tokens** | ${contextUsage.completion_tokens.toLocaleString()} |\n`;
      if (contextUsage.total_tokens != null) md += `| **Total tokens** | ${contextUsage.total_tokens.toLocaleString()} |\n`;
      if (contextUsage.context_window) md += `| **Context window** | ${contextUsage.context_window.toLocaleString()} |\n`;
      if (contextUsage.context_window && contextUsage.total_tokens != null) {
        const pct = Math.round((contextUsage.total_tokens / contextUsage.context_window) * 100);
        md += `| **Usage** | ${pct}% |\n`;
      }
    }

    md += `| **Server** | \`${serverUrl}\` |\n`;
    md += `| **Theme** | ${theme} |\n`;

    setMessages(prev => [...prev, { role: 'system', content: md, isSystem: true }]);
  };

  const handleHelp = () => {
    const helpText = `| Command | Description |\n|---------|-------------|\n| \`/clear\` | Clear all messages and reset the conversation |\n| \`/compact\` | Summarize and compact the conversation to save tokens |\n| \`/context\` | Display current context information |\n| \`/?\` or \`/help\` | Show this help message |`;
    setMessages(prev => [...prev, { role: 'system', content: helpText, isSystem: true }]);
  };

  const handleSend = async () => {
    if (!input.trim() || loading) return;
    const userMessage = input.trim();
    setInput('');

    // Handle slash commands
    if (userMessage.startsWith('/')) {
      const command = userMessage.split(/\s/)[0].toLowerCase();
      switch (command) {
        case '/clear':
          handleClear();
          return;
        case '/compact':
          handleCompact();
          return;
        case '/context':
          handleContext();
          return;
        case '/help':
        case '/?':
          handleHelp();
          return;
        default:
          setMessages(prev => [...prev, { role: 'system', content: `Unknown command \`${command}\`. Type \`/help\` to see available commands.`, isSystem: true }]);
          return;
      }
    }

    setMessages(prev => [...prev, { role: 'user', content: userMessage }]);
    setLoading(true);
    setWaitMessage(pickWaitMessage());

    try {
      // Create thread on first message
      let currentThreadId = threadId;
      if (!currentThreadId) {
        try {
          const threadResult = await ApiClient.createThread(serverUrl, assistantId);
          if (threadResult && threadResult.ThreadId) {
            currentThreadId = threadResult.ThreadId;
            setThreadId(currentThreadId);
          }
        } catch (err) {
          console.error('Failed to create thread:', err);
        }
      }

      const updatedMessages = [...messages, { role: 'user', content: userMessage }];
      const chatMessages = updatedMessages
        .filter(m => !m.isError && !m.isSystem)
        .map(({ role, content }) => ({ role, content }));

      let streamingIndex = null;
      let compactionDetected = false;
      const onDelta = (delta) => {
        if (delta.status === 'Compacting the conversation...') {
          compactionDetected = true;
        }
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

      const result = await ApiClient.chat(serverUrl, assistantId, chatMessages, onDelta, currentThreadId);

      if (streamingIndex !== null) {
        setMessages(prev => {
          const updated = [...prev];
          updated[streamingIndex] = {
            ...updated[streamingIndex],
            isStreaming: false,
            content: result.choices[0].message.content,
            citations: result.citations || null
          };
          return updated;
        });
      } else if (result.choices && result.choices.length > 0) {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: result.choices[0].message.content,
          userMessage: userMessage,
          citations: result.citations || null
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

      if (result.usage) {
        setContextUsage(result.usage);
      }

      const hadSuccess = streamingIndex !== null || (result.choices && result.choices.length > 0);
      if (hadSuccess) {
        if (!titleRequestedRef.current) {
          titleRequestedRef.current = true;
          setMessages(current => { generateChatTitle(current); return current; });
        } else if (compactionDetected) {
          setMessages(current => { generateChatTitle(current); return current; });
        }
      }

      if (hadSuccess && result.usage && result.usage.context_window > 0) {
        const usageRatio = (result.usage.total_tokens || 0) / result.usage.context_window;
        if (usageRatio >= 0.75) {
          const assistantContent = result.choices[0].message.content;
          const messagesForCompaction = [...chatMessages, { role: 'assistant', content: assistantContent }];
          if (messagesForCompaction.length >= 3) {
            try {
              const compactResult = await ApiClient.compact(serverUrl, assistantId, messagesForCompaction, currentThreadId);
              if (compactResult.messages) {
                const compacted = compactResult.messages.map(m => ({ role: m.role || m.Role, content: m.content || m.Content }));
                compacted.push({
                  role: 'assistant',
                  content: assistantContent,
                  userMessage,
                  citations: result.citations || null
                });
                compacted.push({
                  role: 'system',
                  content: 'Conversation automatically compacted to free up context space.',
                  isSystem: true
                });
                setMessages(compacted);
              }
              if (compactResult.usage) {
                setContextUsage(compactResult.usage);
              }
            } catch (compactErr) {
              console.error('Pre-emptive compaction failed:', compactErr);
            }
          }
        }
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

  const [copiedThreadId, setCopiedThreadId] = useState(false);
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

  const logoSrc = assistant?.LogoUrl || '/logo-new.png';
  const chatTitle = assistant?.Title || assistant?.Name || 'AssistantHub';

  if (!assistantId) {
    return null;
  }

  if (error) {
    return (
      <div className="chat-panel">
        {showHeader && (
          <div className="chat-header">
            <div className="chat-header-inner">
              <img src="/logo-new.png" alt="AssistantHub" className="chat-header-logo" />
              <span className="chat-header-title">AssistantHub</span>
              {onClose && (
                <div className="chat-header-actions">
                  <button className="chat-theme-toggle" onClick={onClose} title="Close">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
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
    <div className="chat-panel">
      {/* Header */}
      {showHeader && (
        <div className="chat-header">
          <div className="chat-header-inner">
            <img
              src={logoSrc}
              alt={chatTitle}
              className="chat-header-logo"
              onError={(e) => { e.target.src = '/logo-new.png'; }}
            />
            <div className="chat-header-info">
              <div className="chat-header-title">{chatName || chatTitle}</div>
              {assistant?.Description && <div className="chat-header-desc">{assistant.Description}</div>}
            </div>
            <div className="chat-header-actions">
              {managesOwnTheme && (
                <button className="chat-theme-toggle" onClick={toggleTheme} title={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}>
                  {theme === 'light' ? (
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
                  ) : (
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
                  )}
                </button>
              )}
              {onClose && (
                <button className="chat-theme-toggle" onClick={onClose} title="Close">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
              )}
            </div>
          </div>
        </div>
      )}

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
                  onError={(e) => { e.target.src = '/logo-new.png'; }}
                />
              </div>
              <h2>How can I help you today?</h2>
              <p>Send a message to start chatting with {assistant?.Name || 'the assistant'}.</p>
            </div>
          )}
          {messages.map((msg, idx) => {
            codeBlockCounter.current = 0;

            if (msg.isSystem) {
              return (
                <div key={idx} className="chat-message-row system">
                  <div className="chat-system-message">
                    <div className="chat-markdown-content">
                      <ReactMarkdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
                        {msg.content}
                      </ReactMarkdown>
                    </div>
                  </div>
                </div>
              );
            }

            return (
              <div key={idx} className={`chat-message-row ${msg.role}`}>
                {msg.role === 'assistant' && (
                  <div className="chat-avatar assistant-avatar">
                    <img
                      src={logoSrc}
                      alt=""
                      onError={(e) => { e.target.src = '/logo-new.png'; }}
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
                  {msg.citations && msg.citations.sources?.length > 0 && (() => {
                    const filtered = msg.citations.sources
                      .filter(s => !msg.citations.referenced_indices?.length || msg.citations.referenced_indices.includes(s.index));
                    const grouped = Object.values(filtered.reduce((acc, source) => {
                      const key = source.document_name;
                      if (!acc[key]) {
                        acc[key] = { ...source, indices: [source.index] };
                      } else {
                        acc[key].indices.push(source.index);
                        if (source.score > acc[key].score) {
                          acc[key].score = source.score;
                        }
                        if (source.download_url && !acc[key].download_url) {
                          acc[key].download_url = source.download_url;
                        }
                      }
                      return acc;
                    }, {}));
                    return (
                      <div className="chat-citations">
                        <div className="chat-citations-label">Sources</div>
                        <div className="chat-citations-list">
                          {grouped.map((source) => (
                            source.download_url ? (
                              <a
                                key={source.indices.join(',')}
                                className="chat-citation-card chat-citation-clickable"
                                href={source.download_url}
                                target="_blank"
                                rel="noopener noreferrer"
                                title={source.excerpt}
                              >
                                <span className="chat-citation-index">[{source.indices.join(', ')}]</span>
                                <span className="chat-citation-name">
                                  {source.document_name}
                                </span>
                                <span className="chat-citation-score">
                                  {Math.round(source.score * 100)}%
                                </span>
                              </a>
                            ) : (
                              <div key={source.indices.join(',')} className="chat-citation-card" title={source.excerpt}>
                                <span className="chat-citation-index">[{source.indices.join(', ')}]</span>
                                <span className="chat-citation-name">
                                  {source.document_name}
                                </span>
                                <span className="chat-citation-score">
                                  {Math.round(source.score * 100)}%
                                </span>
                              </div>
                            )
                          ))}
                        </div>
                      </div>
                    );
                  })()}
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
          {loading && !compacting && !messages.some(m => m.isStreaming) && (
            <div className="chat-message-row assistant">
              <div className="chat-avatar assistant-avatar">
                <img
                  src={logoSrc}
                  alt=""
                  onError={(e) => { e.target.src = '/logo-new.png'; }}
                />
              </div>
              <div className="chat-message-content-wrap">
                <div className="chat-bubble assistant">
                  <div className="chat-typing-indicator">
                    <span></span><span></span><span></span>
                  </div>
                </div>
              </div>
              {waitMessage && <span className="chat-wait-label">{waitMessage}</span>}
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
      </div>

      {/* Status Bar */}
      {showStatusBar && (
        <div className="chat-status-bar">
          <span className="chat-status-disclaimer">AI assistants can make mistakes. Press ENTER to send, shift-ENTER for a new line.</span>
          <div className="chat-status-right">
            {contextUsage && contextUsage.context_window > 0 && (
              <span className="chat-context-usage" title={`Prompt: ${contextUsage.prompt_tokens?.toLocaleString() || 0} | Completion: ${contextUsage.completion_tokens?.toLocaleString() || 0}`}>
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/></svg>
                {(contextUsage.total_tokens || 0).toLocaleString()} / {contextUsage.context_window.toLocaleString()} tokens ({Math.round(((contextUsage.total_tokens || 0) / contextUsage.context_window) * 100)}%)
              </span>
            )}
            {threadId && (
              <button
                className="chat-thread-id"
                onClick={() => {
                  navigator.clipboard.writeText(threadId);
                  setCopiedThreadId(true);
                  setTimeout(() => setCopiedThreadId(false), 2000);
                }}
                title={`Thread: ${threadId}\nClick to copy`}
              >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                <span>{copiedThreadId ? 'Copied!' : threadId.substring(0, 12) + '...'}</span>
              </button>
            )}
          </div>
        </div>
      )}

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

export default ChatPanel;
