import React, { useState, useEffect, useRef } from 'react';
import { ApiClient } from '../utils/api';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneLight, oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';
import MetadataFilterModal from './modals/MetadataFilterModal';
import DocumentAttachmentModal from './modals/DocumentAttachmentModal';
import CopyButton from './CopyButton';
import { WAIT_MESSAGES } from './chatWaitMessages';

function safeDecode(value) {
  try {
    return decodeURIComponent(value || '');
  } catch {
    return value || '';
  }
}

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
  const abortControllerRef = useRef(null);
  const chatOpenModelLoadRef = useRef(null);
  const autoCompactionRunRef = useRef(0);
  const lastAutoCompactionSignatureRef = useRef(null);

  function pickWaitMessage() {
    const available = WAIT_MESSAGES.filter(m => !recentWaitMessages.current.includes(m));
    const pool = available.length > 0 ? available : WAIT_MESSAGES;
    const picked = pool[Math.floor(Math.random() * pool.length)];
    recentWaitMessages.current.push(picked);
    if (recentWaitMessages.current.length > 20) recentWaitMessages.current.shift();
    return picked;
  }

  useEffect(() => {
    if (!loading) return;
    const interval = setInterval(() => {
      setWaitMessage(pickWaitMessage());
    }, 6000);
    return () => clearInterval(interval);
  }, [loading]);
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
  const [metadataFilter, setMetadataFilter] = useState(null);
  const [showFilterModal, setShowFilterModal] = useState(false);
  const [availableLabels, setAvailableLabels] = useState([]);
  const [availableTags, setAvailableTags] = useState([]);
  const [attachedDocuments, setAttachedDocuments] = useState([]);
  const [localAttachments, setLocalAttachments] = useState([]);
  const [showDocumentAttachmentModal, setShowDocumentAttachmentModal] = useState(false);
  const localFileInputRef = useRef(null);

  // Use prop theme if provided, otherwise internal
  const theme = themeProp || internalTheme;
  const managesOwnTheme = !themeProp;

  const toggleTheme = () => {
    if (!managesOwnTheme) return;
    const newTheme = internalTheme === 'light' ? 'dark' : 'light';
    setInternalTheme(newTheme);
    localStorage.setItem('ah_chat_theme', newTheme);
  };

  // When ChatPanel manages its own theme, apply it to the document
  useEffect(() => {
    if (managesOwnTheme) {
      document.documentElement.setAttribute('data-theme', theme);
    }
  }, [managesOwnTheme, theme]);

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
    autoCompactionRunRef.current += 1;
    lastAutoCompactionSignatureRef.current = null;
    setAssistant(null);
    setThreadId(localStorage.getItem(`ah_thread_${assistantId}`) || null);
    setMetadataFilter(null);
    setAttachedDocuments([]);
    setLocalAttachments([]);
    setShowDocumentAttachmentModal(false);
  }, [assistantId]);

  // Fetch available labels and tags for filter modal
  useEffect(() => {
    if (!assistantId) return;
    ApiClient.getDistinctLabels(serverUrl, assistantId).then(setAvailableLabels);
    ApiClient.getDistinctTags(serverUrl, assistantId).then(setAvailableTags);
  }, [assistantId, serverUrl]);

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

  useEffect(() => {
    if (!assistantId || !serverUrl || !assistant?.LoadModelsOnChatOpen) return;
    if (chatOpenModelLoadRef.current === assistantId) return;
    chatOpenModelLoadRef.current = assistantId;
    ApiClient.openChat(serverUrl, assistantId).catch((err) => {
      console.error('Failed to load models on chat open:', err);
    });
  }, [assistantId, serverUrl, assistant?.LoadModelsOnChatOpen]);

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

  const isConversationSummaryMessage = (message) => {
    const role = message.role || message.Role;
    const content = message.content || message.Content || '';
    return String(role).toLowerCase() === 'system' && content.startsWith('[Conversation Summary]');
  };

  const normalizeCompactMessages = (compactMessages) => {
    return compactMessages.map(m => {
      const normalized = {
        role: m.role || m.Role,
        content: m.content || m.Content || ''
      };
      if (isConversationSummaryMessage(m)) {
        normalized.hidden = true;
      }
      return normalized;
    });
  };

  const appendAssistantMessageIfMissing = (targetMessages, assistantContent, userMessage, citations) => {
    const lastMessage = targetMessages.length > 0 ? targetMessages[targetMessages.length - 1] : null;
    const alreadyPresent = lastMessage
      && lastMessage.role === 'assistant'
      && lastMessage.content === assistantContent;

    if (!alreadyPresent) {
      targetMessages.push({
        role: 'assistant',
        content: assistantContent,
        userMessage,
        citations: citations || null
      });
    }
  };

  const getChatMessages = (sourceMessages) => {
    return sourceMessages
      .filter(m => !m.isError && !m.isSystem && !m.isToolProgress)
      .map(({ role, content }) => ({ role, content }));
  };

  const buildCompactionSignature = (compactionMessages) => {
    return JSON.stringify(compactionMessages.map(m => [m.role, m.content]));
  };

  const cancelAutomaticCompaction = () => {
    autoCompactionRunRef.current += 1;
    setCompacting(false);
  };

  const scheduleAutomaticCompaction = (usage, compactionMessages, assistantContent, userMessage, citations, currentThreadId) => {
    if (!usage || !usage.context_window || usage.context_window <= 0) return;

    const usageRatio = (usage.total_tokens || 0) / usage.context_window;
    if (usageRatio < 0.75 || compactionMessages.length < 3) return;

    const signature = buildCompactionSignature(compactionMessages);
    if (signature === lastAutoCompactionSignatureRef.current) return;
    lastAutoCompactionSignatureRef.current = signature;

    const runId = autoCompactionRunRef.current + 1;
    autoCompactionRunRef.current = runId;
    setCompacting(true);

    setTimeout(() => {
      void (async () => {
        try {
          const compactResult = await ApiClient.compact(serverUrl, assistantId, compactionMessages, currentThreadId);
          if (autoCompactionRunRef.current !== runId) return;

          if (compactResult.messages) {
            const compacted = normalizeCompactMessages(compactResult.messages);
            appendAssistantMessageIfMissing(compacted, assistantContent, userMessage, citations);
            compacted.push({
              role: 'system',
              content: 'Conversation automatically compacted to free up context space.',
              isSystem: true
            });
            setMessages(compacted);
          }
          if (compactResult.usage && autoCompactionRunRef.current === runId) {
            setContextUsage(compactResult.usage);
          }
        } catch (compactErr) {
          console.error('Pre-emptive compaction failed:', compactErr);
        } finally {
          if (autoCompactionRunRef.current === runId) {
            setCompacting(false);
          }
        }
      })();
    }, 0);
  };

  const generateChatTitle = async (conversationMessages) => {
    try {
      const titleMessages = [
        ...conversationMessages.filter(m => !m.isError && !m.isSystem && !m.isToolProgress && !m.hidden).map(({ role, content }) => ({ role, content })),
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
    setAttachedDocuments([]);
    setLocalAttachments([]);
    cancelAutomaticCompaction();
    lastAutoCompactionSignatureRef.current = null;
  };

  const handleCompact = async () => {
    const chatMessages = getChatMessages(messages);
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
        const compacted = normalizeCompactMessages(result.messages);
        const lastAssistant = [...messages].reverse().find(m => m.role === 'assistant' && !m.isError);
        if (lastAssistant) {
          appendAssistantMessageIfMissing(compacted, lastAssistant.content, lastAssistant.userMessage, lastAssistant.citations);
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
    const chatMessages = messages.filter(m => !m.isError && !m.isSystem && !m.isToolProgress);
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

  const readTraceField = (trace, ...names) => {
    for (const name of names) {
      if (trace && trace[name] !== undefined && trace[name] !== null && trace[name] !== '') return trace[name];
    }
    return null;
  };

  const formatToolResultCount = (value) => {
    if (value === null || value === undefined || value === '') return null;
    const numberValue = Number(value);
    if (!Number.isFinite(numberValue)) return String(value);
    return `${numberValue} result${numberValue === 1 ? '' : 's'}`;
  };

  const formatToolDuration = (value) => {
    if (value === null || value === undefined || value === '') return null;
    const numberValue = Number(value);
    if (!Number.isFinite(numberValue)) return String(value);
    if (numberValue >= 1000) return `${(numberValue / 1000).toFixed(numberValue >= 10000 ? 0 : 1)}s`;
    return `${Math.max(1, Math.round(numberValue))}ms`;
  };

  const buildToolProgressMessage = (event, statusText, runKey = 'current') => {
    if (!event) return null;

    const eventType = String(readTraceField(event, 'event_type', 'EventType', 'type') || '').toLowerCase();
    if (!eventType.startsWith('assistant.tool_')) return null;

    const label = readTraceField(event, 'display_label', 'DisplayLabel', 'tool_name', 'ToolName') || 'Assistant tool';
    const toolName = readTraceField(event, 'tool_name', 'ToolName');
    const toolCallId = readTraceField(event, 'tool_call_id', 'ToolCallId');
    const iteration = readTraceField(event, 'iteration', 'Iteration');
    const sequence = readTraceField(event, 'sequence_number', 'SequenceNumber');
    const resultCount = formatToolResultCount(readTraceField(event, 'result_count', 'ResultCount'));
    const duration = formatToolDuration(readTraceField(event, 'duration_ms', 'DurationMs'));
    const summary = readTraceField(event, 'summary', 'Summary', 'status', 'Status') || statusText;
    const truncated = !!readTraceField(event, 'truncated', 'Truncated');

    const bubbleKey = `tool-progress:${runKey}`;
    const itemKey = toolCallId
      ? `call:${toolCallId}`
      : eventType.includes('tool_iteration')
        ? `${eventType}:${iteration || 'current'}`
        : `${eventType}:${toolName || label}:${iteration || '0'}:${sequence || '0'}`;

    let state = 'running';
    let title = label;
    let detail = summary || 'The assistant is using a server-side tool.';

    if (eventType.endsWith('tool_iteration.started')) {
      title = 'Checking tools';
      detail = summary || 'The assistant is deciding whether this turn needs tool data.';
    } else if (eventType.endsWith('tool_iteration.stopped')) {
      state = 'succeeded';
      title = 'Answering from gathered evidence';
      detail = summary || 'The assistant has enough tool evidence and is preparing the answer.';
    } else if (eventType.endsWith('.started')) {
      title = `${label} started`;
      detail = `Started server-side tool call${sequence ? ` ${sequence}` : ''}${iteration ? ` during pass ${iteration}` : ''}.`;
    } else if (eventType.endsWith('.heartbeat')) {
      title = `${label} still running`;
      detail = duration ? `Still waiting after ${duration}.` : 'Still waiting for this tool to finish.';
    } else if (eventType.endsWith('.completed')) {
      state = 'succeeded';
      title = `${label} completed`;
      const parts = [];
      if (resultCount) parts.push(resultCount);
      if (duration) parts.push(duration);
      if (truncated) parts.push('output was shortened');
      detail = parts.length > 0
        ? `Completed with ${parts.join(', ')}.`
        : (summary || 'The tool finished successfully.');
    } else if (eventType.endsWith('.failed')) {
      state = 'failed';
      title = `${label} failed`;
      detail = summary || 'The tool failed; the assistant may try another source or answer from available context.';
    } else if (eventType.endsWith('.denied')) {
      state = 'denied';
      title = `${label} denied`;
      detail = summary || 'The tool request was blocked by assistant policy.';
    } else if (eventType.endsWith('.interrupted')) {
      state = 'failed';
      title = 'Tool status interrupted';
      detail = summary || 'Tool progress stopped before the final status was received.';
    }

    const meta = [];
    if (toolName && toolName !== label) meta.push(toolName);
    if (iteration) meta.push(`pass ${iteration}`);
    if (sequence) meta.push(`call ${sequence}`);
    if (resultCount) meta.push(resultCount);
    if (duration) meta.push(duration);
    if (truncated) meta.push('truncated');

    return {
      role: 'assistant',
      isToolProgress: true,
      toolProgressKey: bubbleKey,
      toolProgressState: state,
      content: `${title}: ${detail}`,
      items: [{
        key: itemKey,
        title,
        detail,
        state,
        meta
      }]
    };
  };

  const upsertToolProgressMessage = (event, statusText, runKey) => {
    const progress = buildToolProgressMessage(event, statusText, runKey);
    if (!progress) return;
    const nextItem = progress.items?.[0];
    if (!nextItem) return;

    const summarizeProgressItems = (items) => (
      items.map(item => `${item.title}: ${item.detail}`).join('\n')
    );

    const summarizeProgressState = (items) => {
      const lastProblem = [...items].reverse().find(item => item.state === 'failed' || item.state === 'denied');
      if (lastProblem) return lastProblem.state;
      return items[items.length - 1]?.state || 'running';
    };

    setMessages(prev => {
      const updated = [...prev];
      const index = updated.findIndex(msg => msg.isToolProgress && msg.toolProgressKey === progress.toolProgressKey);
      if (index >= 0) {
        const items = Array.isArray(updated[index].items) ? [...updated[index].items] : [];
        const itemIndex = items.findIndex(item => item.key === nextItem.key);
        if (itemIndex >= 0) {
          items[itemIndex] = nextItem;
        } else {
          items.push(nextItem);
        }
        const state = summarizeProgressState(items);
        updated[index] = {
          ...updated[index],
          toolProgressState: state,
          content: summarizeProgressItems(items),
          items
        };
      } else {
        updated.push({
          ...progress,
          toolProgressState: nextItem.state,
          content: summarizeProgressItems(progress.items)
        });
      }
      return updated;
    });
  };

  const handleSend = async () => {
    if (!input.trim() || loading) return;
    const userMessage = input.trim();
    cancelAutomaticCompaction();
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

    const abortController = new AbortController();
    abortControllerRef.current = abortController;

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
      const chatMessages = getChatMessages(updatedMessages);

      let streamingIndex = null;
      let compactionDetected = false;
      const toolProgressRunKey = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const onDelta = (delta) => {
        if (delta.status === 'Compacting the conversation...') {
          compactionDetected = true;
        }
        if (delta.toolEvent) {
          upsertToolProgressMessage(delta.toolEvent, delta.status, toolProgressRunKey);
        }
        if (delta.thinking) {
          setMessages(prev => {
            const updated = [...prev];
            if (streamingIndex === null) {
              streamingIndex = updated.length;
              updated.push({ role: 'assistant', content: '', thinking: delta.thinking, userMessage, isStreaming: true });
            } else {
              updated[streamingIndex] = {
                ...updated[streamingIndex],
                thinking: (updated[streamingIndex].thinking || '') + delta.thinking
              };
            }
            return updated;
          });
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

      const attachedDocumentIds = attachedDocuments.map(doc => doc.Id).filter(Boolean);
      const localAttachmentPayload = localAttachments.map(file => ({
        name: file.name,
        content_type: file.content_type,
        base64_content: file.base64_content
      }));
      const result = await ApiClient.chat(serverUrl, assistantId, chatMessages, onDelta, currentThreadId, abortController.signal, metadataFilter, attachedDocumentIds, localAttachmentPayload);
      const toolTraces = normalizeChatToolTraces(result);

      if (streamingIndex !== null) {
        const responseMessage = result.choices?.[0]?.message || {};
        setMessages(prev => {
          const updated = [...prev];
          updated[streamingIndex] = {
            ...updated[streamingIndex],
            isStreaming: false,
            content: responseMessage.content || '',
            thinking: responseMessage.thinking || responseMessage.Thinking || updated[streamingIndex].thinking || null,
            citations: result.citations || null,
            toolTraces
          };
          return updated;
        });
      } else if (result.choices && result.choices.length > 0) {
        const responseMessage = result.choices[0].message || {};
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: responseMessage.content || '',
          thinking: responseMessage.thinking || responseMessage.Thinking || null,
          userMessage: userMessage,
          citations: result.citations || null,
          toolTraces
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

      if (hadSuccess && result.choices && result.choices.length > 0) {
        const assistantContent = result.choices[0].message.content;
        const messagesForCompaction = [...chatMessages, { role: 'assistant', content: assistantContent }];
        scheduleAutomaticCompaction(result.usage, messagesForCompaction, assistantContent, userMessage, result.citations, currentThreadId);
      }
    } catch (err) {
      if (err.name === 'AbortError') {
        // Cancelled by user — mark any partial streaming message as complete
        setMessages(prev => {
          const updated = [...prev];
          const lastMsg = updated[updated.length - 1];
          if (lastMsg && lastMsg.isStreaming) {
            updated[updated.length - 1] = { ...lastMsg, isStreaming: false };
          }
          return updated;
        });
      } else {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: err.toolStreamInterrupted
            ? 'The assistant tool stream was interrupted. Please try again.'
            : 'Failed to get response. Please try again.',
          isError: true
        }]);
      }
    } finally {
      abortControllerRef.current = null;
      setLoading(false);
    }
  };

  const handleCancel = () => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
  };

  const normalizeChatToolTraces = (result) => {
    const directTraces = result?.tool_calls || result?.ToolCalls;
    if (Array.isArray(directTraces) && directTraces.length > 0) {
      return directTraces.map((trace, index) => ({
        id: readTraceField(trace, 'tool_call_id', 'ToolCallId') || `${readTraceField(trace, 'tool_name', 'ToolName') || 'tool'}-${index}`,
        toolName: readTraceField(trace, 'display_label', 'DisplayLabel', 'tool_name', 'ToolName') || 'Tool',
        status: readTraceField(trace, 'denied', 'Denied') ? 'Denied' : readTraceField(trace, 'success', 'Success') ? 'Succeeded' : 'Failed',
        summary: readTraceField(trace, 'summary', 'Summary'),
        resultCount: readTraceField(trace, 'result_count', 'ResultCount'),
        durationMs: readTraceField(trace, 'duration_ms', 'DurationMs'),
        truncated: !!readTraceField(trace, 'truncated', 'Truncated')
      }));
    }

    const events = result?.tool_events || result?.ToolEvents;
    if (!Array.isArray(events) || events.length === 0) return [];

    const terminalEvents = events.filter(event => {
      const eventType = String(readTraceField(event, 'event_type', 'EventType', 'type') || '').toLowerCase();
      return eventType.endsWith('.completed') || eventType.endsWith('.failed') || eventType.endsWith('.denied') || eventType.endsWith('.interrupted');
    });
    const sourceEvents = terminalEvents.length > 0 ? terminalEvents : events.filter(event => {
      const eventType = String(readTraceField(event, 'event_type', 'EventType', 'type') || '').toLowerCase();
      return !eventType.endsWith('.heartbeat');
    });

    return sourceEvents.map((event, index) => {
      const eventType = String(readTraceField(event, 'event_type', 'EventType', 'type') || '').toLowerCase();
      let status = 'Running';
      if (eventType.endsWith('.completed')) status = 'Succeeded';
      else if (eventType.endsWith('.failed') || eventType.endsWith('.interrupted')) status = 'Failed';
      else if (eventType.endsWith('.denied')) status = 'Denied';

      return {
        id: readTraceField(event, 'tool_call_id', 'ToolCallId') || `${readTraceField(event, 'tool_name', 'ToolName') || 'tool'}-${index}`,
        toolName: readTraceField(event, 'display_label', 'DisplayLabel', 'tool_name', 'ToolName') || 'Tool',
        status,
        summary: readTraceField(event, 'summary', 'Summary', 'status', 'Status'),
        resultCount: readTraceField(event, 'result_count', 'ResultCount'),
        durationMs: readTraceField(event, 'duration_ms', 'DurationMs'),
        truncated: !!readTraceField(event, 'truncated', 'Truncated')
      };
    });
  };

  const renderToolTraceSummary = (traces) => {
    if (!Array.isArray(traces) || traces.length === 0) return null;
    const isGenericSummary = (trace, value) => {
      if (!value) return true;
      const normalized = value.trim().replace(/\s+/g, ' ').toLowerCase();
      const label = String(trace.toolName || '').trim().replace(/\s+/g, ' ').toLowerCase();
      return normalized === `${label} completed.`
        || normalized === `${label} completed`
        || normalized === `${label} running.`
        || normalized === `${label} running`;
    };
    const formatResultCount = (value) => {
      if (value === null || value === undefined || value === '') return '-';
      const numberValue = Number(value);
      if (!Number.isFinite(numberValue)) return String(value);
      return `${numberValue} result${numberValue === 1 ? '' : 's'}`;
    };
    const formatDuration = (value) => {
      if (value === null || value === undefined || value === '') return '-';
      const numberValue = Number(value);
      if (!Number.isFinite(numberValue)) return String(value);
      if (numberValue >= 1000) return `${(numberValue / 1000).toFixed(numberValue >= 10000 ? 0 : 1)}s`;
      return `${Math.round(numberValue)}ms`;
    };
    const detailsLabel = (trace) => {
      const notes = [];
      if (trace.truncated) notes.push('Truncated');
      const summary = trace.summary || '';
      if (summary && !isGenericSummary(trace, summary)) notes.push(summary);
      return notes.join(' | ') || '-';
    };

    const failedCount = traces.filter(trace => trace.status === 'Denied' || trace.status === 'Failed').length;
    const callCountLabel = failedCount > 0
      ? `${traces.length} call${traces.length === 1 ? '' : 's'} | ${failedCount} issue${failedCount === 1 ? '' : 's'}`
      : `${traces.length} call${traces.length === 1 ? '' : 's'}`;

    return (
      <details className="chat-tool-trace" aria-label="Tool activity">
        <summary className="chat-tool-trace-summary">
          <span className="chat-tool-trace-caret" aria-hidden="true" />
          <span className="chat-tool-trace-title">Tool activity</span>
          <span className="chat-tool-trace-count">{callCountLabel}</span>
        </summary>
        <div className="chat-tool-trace-table-wrap">
          <table className="chat-tool-trace-table">
            <thead>
              <tr>
                <th>Tool</th>
                <th>Status</th>
                <th>Results</th>
                <th>Runtime</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {traces.map((trace, index) => {
                const details = detailsLabel(trace);
                return (
                  <tr key={trace.id || index}>
                    <td data-label="Tool">
                      <span className="chat-tool-trace-name" title={trace.toolName}>{trace.toolName}</span>
                    </td>
                    <td data-label="Status">
                      <span className="chat-tool-trace-details" title={trace.status || 'Unknown'}>{trace.status || 'Unknown'}</span>
                    </td>
                    <td data-label="Results">
                      <span className="chat-tool-trace-details" title={formatResultCount(trace.resultCount)}>{formatResultCount(trace.resultCount)}</span>
                    </td>
                    <td data-label="Runtime">
                      <span className="chat-tool-trace-details" title={formatDuration(trace.durationMs)}>{formatDuration(trace.durationMs)}</span>
                    </td>
                    <td data-label="Details">
                      <span className="chat-tool-trace-details" title={details}>{details}</span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </details>
    );
  };

  const getAttachedDocumentName = (doc) => doc?.Name || doc?.OriginalFilename || doc?.Id || 'Document';

  const removeAttachedDocument = (documentId) => {
    setAttachedDocuments(current => current.filter(doc => doc.Id !== documentId));
  };

  const formatAttachmentBytes = (bytes) => {
    const value = Number(bytes || 0);
    if (value < 1024) return `${value} B`;
    if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
    return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  };

  const readFileAsBase64 = (file) => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const value = String(reader.result || '');
      const comma = value.indexOf(',');
      resolve(comma >= 0 ? value.substring(comma + 1) : value);
    };
    reader.onerror = () => reject(reader.error || new Error('Failed to read file'));
    reader.readAsDataURL(file);
  });

  const handleLocalFileChange = async (event) => {
    const files = Array.from(event.target.files || []);
    event.target.value = '';
    if (files.length < 1) return;

    const maxCount = assistant?.DocumentAttachmentMaxCount || 10;
    const remainingSlots = Math.max(0, maxCount - attachedDocuments.length - localAttachments.length);
    if (remainingSlots < 1) {
      setMessages(prev => [...prev, { role: 'system', content: `Attachment limit reached (${maxCount}).`, isSystem: true }]);
      return;
    }

    const maxBytes = 10 * 1024 * 1024;
    const accepted = [];
    const rejected = [];
    for (const file of files.slice(0, remainingSlots)) {
      if (file.size > maxBytes) {
        rejected.push(`${file.name} exceeds ${formatAttachmentBytes(maxBytes)}`);
        continue;
      }

      try {
        const base64 = await readFileAsBase64(file);
        accepted.push({
          id: `${file.name}-${file.size}-${file.lastModified}-${Math.random().toString(36).slice(2)}`,
          name: file.name,
          content_type: file.type || 'application/octet-stream',
          size_bytes: file.size,
          base64_content: base64
        });
      } catch (err) {
        rejected.push(`${file.name} could not be read`);
      }
    }

    if (accepted.length > 0) {
      setLocalAttachments(current => [...current, ...accepted]);
    }
    if (files.length > remainingSlots) {
      rejected.push(`${files.length - remainingSlots} file(s) skipped because the attachment limit was reached`);
    }
    if (rejected.length > 0) {
      setMessages(prev => [...prev, { role: 'system', content: rejected.join('\n'), isSystem: true }]);
    }
  };

  const removeLocalAttachment = (attachmentId) => {
    setLocalAttachments(current => current.filter(file => file.id !== attachmentId));
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
        .filter(m => !m.isError && !m.isSystem && !m.hidden)
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
            if (msg.hidden) return null;

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

            if (msg.isToolProgress) {
              const progressItems = Array.isArray(msg.items) && msg.items.length > 0
                ? msg.items
                : [{
                    key: 'legacy',
                    title: msg.title || 'Tool activity',
                    detail: msg.detail || msg.content,
                    meta: msg.meta || [],
                    state: msg.toolProgressState
                  }];

              return (
                <div key={idx} className={`chat-message-row assistant tool-progress ${msg.toolProgressState || 'running'}`}>
                  <div className="chat-avatar assistant-avatar">
                    <img
                      src={logoSrc}
                      alt=""
                      onError={(e) => { e.target.src = '/logo-new.png'; }}
                    />
                  </div>
                  <div className="chat-message-content-wrap">
                    <div className={`chat-bubble assistant tool-progress ${msg.toolProgressState || 'running'}`} aria-live="polite">
                      <div className="chat-tool-progress-list">
                        {progressItems.map((item, itemIndex) => (
                          <div
                            key={item.key || `${item.title}-${itemIndex}`}
                            className={`chat-tool-progress-line ${item.state || 'running'}`}
                          >
                            <b className="chat-tool-progress-title">{item.title || 'Tool activity'}</b>
                            <span className="chat-tool-progress-separator">: </span>
                            <span className="chat-tool-progress-detail">{item.detail || ''}</span>
                            {Array.isArray(item.meta) && item.meta.length > 0 && (
                              <span className="chat-tool-progress-meta"> ({item.meta.join(' / ')})</span>
                            )}
                          </div>
                        ))}
                      </div>
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
                        {msg.thinking && (
                          <details className="chat-thinking">
                            <summary>Thinking</summary>
                            <div className="chat-thinking-content">{msg.thinking}</div>
                          </details>
                        )}
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
                      const key = source.url || source.document_name || source.document_id || String(source.index);
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
                        if (source.url && !acc[key].url) {
                          acc[key].url = source.url;
                        }
                      }
                      return acc;
                    }, {}));
                    return (
                      <div className="chat-citations">
                        <div className="chat-citations-label">Sources</div>
                        <div className="chat-citations-list">
                          {grouped.map((source) => {
                            const href = source.download_url || source.url;
                            const displayName = source.document_name || source.url || source.document_id || 'Source';
                            const decodedName = safeDecode(displayName);
                            const sourceTitle = source.url ? "Source URL: " + source.url : "Source document: " + decodedName;
                            const scorePercent = Number.isFinite(source.score) ? Math.round(source.score * 100) + '%' : '';
                            return href ? (
                              <a
                                key={source.indices.join(',')}
                                className="chat-citation-card chat-citation-clickable"
                                href={href}
                                target="_blank"
                                rel="noopener noreferrer"
                                title={source.excerpt}
                              >
                                <span className="chat-citation-index" title="Citation reference number">[{source.indices.join(', ')}]</span>
                                <span className="chat-citation-name" title={sourceTitle}>
                                  {decodedName}
                                </span>
                                <span className="chat-citation-score" title="Cosine similarity score — how closely this chunk matches your query by embedding distance">
                                  {scorePercent}
                                </span>
                                {source.rerank_score != null && (
                                    <span className="chat-citation-relevance" title="LLM re-ranking relevance score (0–10) — how relevant an LLM judged this chunk to your query">{source.rerank_score.toFixed(1)}/10</span>
                                )}
                              </a>
                            ) : (
                              <div key={source.indices.join(',')} className="chat-citation-card" title={source.excerpt}>
                                <span className="chat-citation-index" title="Citation reference number">[{source.indices.join(', ')}]</span>
                                <span className="chat-citation-name" title={sourceTitle}>
                                  {decodedName}
                                </span>
                                <span className="chat-citation-score" title="Cosine similarity score — how closely this chunk matches your query by embedding distance">
                                  {scorePercent}
                                </span>
                                {source.rerank_score != null && (
                                    <span className="chat-citation-relevance" title="LLM re-ranking relevance score (0–10) — how relevant an LLM judged this chunk to your query">{source.rerank_score.toFixed(1)}/10</span>
                                )}
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    );
                  })()}
                  {renderToolTraceSummary(msg.toolTraces)}
                  {msg.role === 'assistant' && !msg.isError && (
                    <div className="chat-message-actions">
                      <CopyButton
                        text={msg.content}
                        className="chat-action-btn"
                        title="Copy response"
                      />
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
              <div className="chat-message-content-wrap chat-pending-content-wrap">
                <div className="chat-bubble assistant">
                  <div className="chat-typing-indicator">
                    <span></span><span></span><span></span>
                  </div>
                </div>
                {waitMessage && (
                  <span className="chat-wait-label" title={waitMessage}>
                    {waitMessage}
                  </span>
                )}
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>
      </div>

      {/* Input Area */}
      <div className="chat-input-area">
        {(attachedDocuments.length > 0 || localAttachments.length > 0) && (
          <div className="chat-attached-docs" aria-label="Attached documents and files">
            {attachedDocuments.map(doc => (
              <span key={doc.Id} className="chat-attached-doc-chip" title={getAttachedDocumentName(doc)}>
                <span>{getAttachedDocumentName(doc)}</span>
                <button type="button" onClick={() => removeAttachedDocument(doc.Id)} title="Remove document">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                  </svg>
                </button>
              </span>
            ))}
            {localAttachments.map(file => (
              <span key={file.id} className="chat-attached-doc-chip" title={`${file.name} (${formatAttachmentBytes(file.size_bytes)})`}>
                <span>{file.name}</span>
                <button type="button" onClick={() => removeLocalAttachment(file.id)} title="Remove file">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                  </svg>
                </button>
              </span>
            ))}
            <button className="chat-attached-doc-clear" type="button" onClick={() => { setAttachedDocuments([]); setLocalAttachments([]); }} title="Clear attachments">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M3 6h18"/><path d="M8 6v14"/><path d="M16 6v14"/><path d="M10 3h4"/><path d="M5 6l1 15h12l1-15"/>
              </svg>
            </button>
          </div>
        )}
        <div className="chat-input-container">
          <textarea
            ref={textareaRef}
            className="chat-input"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Message..."
            rows={1}
          />
          {assistant?.EnableDocumentAttachments && (
            <>
              <input
                ref={localFileInputRef}
                type="file"
                multiple
                className="chat-local-file-input"
                onChange={handleLocalFileChange}
              />
              <button
                className={`chat-filter-btn${localAttachments.length > 0 ? ' active' : ''}`}
                onClick={() => localFileInputRef.current?.click()}
                title="Attach files from this device"
                type="button"
              >
                <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <path d="M14 2v6h6"/>
                  <path d="M12 18v-6"/>
                  <path d="M9 15l3-3 3 3"/>
                </svg>
              </button>
              <button
                className={`chat-filter-btn${attachedDocuments.length > 0 ? ' active' : ''}`}
                onClick={() => setShowDocumentAttachmentModal(true)}
                title="Attach collection documents"
                type="button"
              >
                <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21.44 11.05 12.25 20.24a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"/>
                </svg>
              </button>
            </>
          )}
          <button
            className={`chat-filter-btn${metadataFilter ? ' active' : ''}`}
            onClick={() => setShowFilterModal(true)}
            title="Metadata filters"
            type="button"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>
            </svg>
          </button>
          {loading ? (
            <button
              className="chat-send-btn chat-cancel-btn"
              onClick={handleCancel}
              title="Cancel generation"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <rect x="4" y="4" width="16" height="16" rx="2" fill="currentColor" stroke="none"/>
              </svg>
            </button>
          ) : (
            <button
              className="chat-send-btn"
              onClick={handleSend}
              disabled={!input.trim()}
              title="Send message"
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="22" y1="2" x2="11" y2="13"/>
                <polygon points="22 2 15 22 11 13 2 9 22 2"/>
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Status Bar */}
      {showStatusBar && (
        <div className="chat-status-bar">
          <span className="chat-status-disclaimer">AI assistants can make mistakes. Press ENTER to send, shift-ENTER for a new line.</span>
          <div className="chat-status-right">
            {(attachedDocuments.length > 0 || localAttachments.length > 0) && (
              <span className="chat-attachment-status" title="Attached collection documents constrain retrieval; local files are sent to this chat turn">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21.44 11.05 12.25 20.24a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"/>
                </svg>
                {attachedDocuments.length + localAttachments.length} attached
              </span>
            )}
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
      {showFilterModal && (
        <MetadataFilterModal
          filter={metadataFilter}
          availableLabels={availableLabels}
          availableTags={availableTags}
          onApply={(f) => { setMetadataFilter(f); setShowFilterModal(false); }}
          onClose={() => setShowFilterModal(false)}
        />
      )}
      {showDocumentAttachmentModal && (
        <DocumentAttachmentModal
          serverUrl={serverUrl}
          assistantId={assistantId}
          selectedDocuments={attachedDocuments}
          maxCount={assistant?.DocumentAttachmentMaxCount || 10}
          onApply={(docs) => { setAttachedDocuments(docs); setShowDocumentAttachmentModal(false); }}
          onClose={() => setShowDocumentAttachmentModal(false)}
        />
      )}
    </div>
  );
}

export default ChatPanel;
