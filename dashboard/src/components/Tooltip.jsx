import React, { useState, useRef, useCallback } from 'react';
import { createPortal } from 'react-dom';

function Tooltip({ text, children }) {
  const [visible, setVisible] = useState(false);
  const [style, setStyle] = useState({ top: 0, left: 0 });
  const wrapperRef = useRef(null);
  const popupRef = useRef(null);

  const show = useCallback(() => {
    if (!wrapperRef.current) return;
    const rect = wrapperRef.current.getBoundingClientRect();
    setStyle({
      top: rect.top - 6,
      left: rect.left + rect.width / 2,
    });
    setVisible(true);
  }, []);

  // After rendering, clamp so the popup stays within the viewport
  const popupCallbackRef = useCallback((node) => {
    popupRef.current = node;
    if (!node) return;
    const pr = node.getBoundingClientRect();
    const pad = 8;
    let left = parseFloat(node.style.left);
    if (pr.left < pad) {
      left += pad - pr.left;
      node.style.left = left + 'px';
    } else if (pr.right > window.innerWidth - pad) {
      left -= pr.right - (window.innerWidth - pad);
      node.style.left = left + 'px';
    }
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
        <span className="tooltip-popup" ref={popupCallbackRef} style={style}>
          {text}
        </span>,
        document.body
      )}
    </span>
  );
}

export default Tooltip;
