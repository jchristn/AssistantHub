import React, { useState, useRef, useCallback } from 'react';
import { createPortal } from 'react-dom';

function Tooltip({ text, children }) {
  const [visible, setVisible] = useState(false);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const wrapperRef = useRef(null);

  const show = useCallback(() => {
    if (!wrapperRef.current) return;
    const rect = wrapperRef.current.getBoundingClientRect();
    setPosition({
      top: rect.top - 6,
      left: rect.left + rect.width / 2,
    });
    setVisible(true);
  }, []);

  const hide = useCallback(() => setVisible(false), []);

  if (!text) return children;

  return (
    <span
      className="tooltip-wrapper"
      ref={wrapperRef}
      onMouseEnter={show}
      onMouseLeave={hide}
    >
      {children}
      {visible && createPortal(
        <span className="tooltip-popup" style={{ top: position.top, left: position.left }}>
          {text}
        </span>,
        document.body
      )}
    </span>
  );
}

export default Tooltip;
