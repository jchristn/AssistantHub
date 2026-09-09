import React from 'react';
import ScopedHub from './ScopedHub';
import BucketsView from '../BucketsView';
import ObjectsView from '../ObjectsView';

const loadOptions = async (api) => {
  const result = await api.getBuckets({ maxResults: 1000 });
  const items = (result && result.Objects) ? result.Objects : (Array.isArray(result) ? result : []);
  return items.map((b) => ({ value: b.Name, label: b.Name }));
};

function BucketsHub() {
  const tabs = [
    { key: 'buckets', label: 'Buckets', render: () => <BucketsView /> },
    { key: 'objects', label: 'Objects', render: (scopeId) => <ObjectsView embedded scopeBucket={scopeId} /> },
  ];
  return (
    <ScopedHub
      scopeParam="bucket"
      scopeLabel="Bucket"
      scopePlaceholder="Select a bucket"
      loadOptions={loadOptions}
      tabs={tabs}
      defaultTabKey="buckets"
      ariaLabel="Buckets"
    />
  );
}

export default BucketsHub;
