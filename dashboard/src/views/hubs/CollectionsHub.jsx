import React from 'react';
import ScopedHub from './ScopedHub';
import CollectionsView from '../CollectionsView';
import RecordsView from '../RecordsView';
import CollectionSearchView from '../CollectionSearchView';

const loadOptions = async (api) => {
  const result = await api.getCollections({ maxResults: 1000 });
  const items = (result && result.Objects) ? result.Objects : (Array.isArray(result) ? result : []);
  return items.map((c) => ({ value: c.GUID || c.Id, label: c.Name || c.GUID || c.Id }));
};

function CollectionsHub() {
  const tabs = [
    { key: 'collections', label: 'Collections', render: () => <CollectionsView /> },
    { key: 'records', label: 'Records', render: (scopeId) => <RecordsView embedded scopeCollectionId={scopeId} /> },
    { key: 'search', label: 'Search', render: (scopeId) => <CollectionSearchView embedded scopeCollectionId={scopeId} /> },
  ];
  return (
    <ScopedHub
      scopeParam="collectionId"
      scopeLabel="Collection"
      scopePlaceholder="Select a collection"
      loadOptions={loadOptions}
      tabs={tabs}
      defaultTabKey="collections"
      ariaLabel="Collections"
    />
  );
}

export default CollectionsHub;
