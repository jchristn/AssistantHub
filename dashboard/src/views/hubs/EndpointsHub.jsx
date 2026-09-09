import React from 'react';
import Tabs from '../../components/Tabs';
import EmbeddingEndpointsView from '../EmbeddingEndpointsView';
import InferenceEndpointsView from '../InferenceEndpointsView';
import ModelsView from '../ModelsView';

function EndpointsHub() {
  const tabs = [
    { key: 'embedding', label: 'Embedding', render: () => <EmbeddingEndpointsView /> },
    { key: 'inference', label: 'Inference', render: () => <InferenceEndpointsView /> },
    { key: 'models', label: 'Models', render: () => <ModelsView /> },
  ];
  return <Tabs tabs={tabs} defaultTabKey="embedding" ariaLabel="Endpoints" />;
}

export default EndpointsHub;
