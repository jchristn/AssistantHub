import React from 'react';
import CopyButton from './CopyButton';

function CopyableId({ id }) {
  return (
    <span className="copyable-id">
      <span>{id}</span>
      <CopyButton text={id} />
    </span>
  );
}

export default CopyableId;
