import React, { useState, useRef, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';

function ActionMenu({ items }) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const triggerRef = useRef(null);
  const menuRef = useRef(null);

  const handleOpen = () => {
    if (triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      setPosition({
        top: rect.bottom + 4,
        left: rect.right - 160,
        triggerTop: rect.top,
        triggerBottom: rect.bottom,
      });
    }
    setOpen(true);
  };

  const handleClose = useCallback(() => setOpen(false), []);

  useEffect(() => {
    if (!open || !menuRef.current) return;
    const menuRect = menuRef.current.getBoundingClientRect();
    if (menuRect.bottom > window.innerHeight) {
      setPosition((prev) => ({
        ...prev,
        top: prev.triggerTop - menuRect.height - 4,
      }));
    }
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const handleClickOutside = (e) => {
      if (triggerRef.current && !triggerRef.current.contains(e.target) &&
          menuRef.current && !menuRef.current.contains(e.target)) {
        handleClose();
      }
    };
    const handleScroll = () => handleClose();
    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('scroll', handleScroll, true);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('scroll', handleScroll, true);
    };
  }, [open, handleClose]);

  return (
    <>
      <button ref={triggerRef} className="action-menu-trigger" onClick={handleOpen} title="Actions">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><circle cx="8" cy="3" r="1.5"/><circle cx="8" cy="8" r="1.5"/><circle cx="8" cy="13" r="1.5"/></svg>
      </button>
      {open && createPortal(
        <div ref={menuRef} className="action-menu" style={{ top: position.top, left: position.left }}>
          {items.map((item, i) => (
            <button key={i} className={`action-menu-item ${item.danger ? 'danger' : ''}`} onClick={() => { handleClose(); item.onClick(); }}>
              {item.label}
            </button>
          ))}
        </div>,
        document.body
      )}
    </>
  );
}

export default ActionMenu;
