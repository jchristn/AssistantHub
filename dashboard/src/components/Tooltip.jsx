import React from 'react';

function Tooltip({ text, children }) {
  if (!text) return children;
  return (
    <span className="tooltip-wrapper" data-tooltip={text}>
      {children}
    </span>
  );
}

export default Tooltip;
