import React from 'react';
import ScopedHub from './ScopedHub';
import IndicesView from '../IndicesView';
import IndexRecordsView from '../IndexRecordsView';
import IndexSearchView from '../IndexSearchView';
import { getIndexId, unwrapObjects } from '../../utils/artifactSearch.jsx';

const loadOptions = async (api) => {
  const result = await api.getIndices({ maxResults: 1000 });
  const items = unwrapObjects(result);
  return items
    .map((i) => {
      const id = getIndexId(i);
      if (!id) return null;
      const name = i?.Name;
      return { value: id, label: name && name !== id ? `${name} (${id})` : id };
    })
    .filter(Boolean);
};

function IndicesHub() {
  const tabs = [
    { key: 'indices', label: 'Indices', render: () => <IndicesView /> },
    { key: 'records', label: 'Records', render: (scopeId) => <IndexRecordsView embedded scopeIndexId={scopeId} /> },
    { key: 'search', label: 'Search', render: (scopeId) => <IndexSearchView embedded scopeIndexId={scopeId} /> },
  ];
  return (
    <ScopedHub
      scopeParam="indexId"
      scopeLabel="Index"
      scopePlaceholder="Select an index"
      loadOptions={loadOptions}
      tabs={tabs}
      defaultTabKey="indices"
      ariaLabel="Indices"
    />
  );
}

export default IndicesHub;
