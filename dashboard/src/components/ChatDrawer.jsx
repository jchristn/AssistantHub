import React, { useEffect } from 'react';
import ChatPanel from './ChatPanel';

function ChatDrawer({ assistantId, isOpen, onClose }) {
  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  return (
    <>
      {/* Backdrop */}
      <div
        className={`chat-drawer-backdrop${isOpen ? ' open' : ''}`}
        onClick={onClose}
      />
      {/* Drawer */}
      <div className={`chat-drawer${isOpen ? ' open' : ''}`}>
        {isOpen && assistantId && (
          <ChatPanel
            assistantId={assistantId}
            showHeader={true}
            showStatusBar={true}
            onClose={onClose}
          />
        )}
      </div>
    </>
  );
}

export default ChatDrawer;
