import React from 'react';
import Tabs from '../../components/Tabs';
import RequestHistoryView from '../RequestHistoryView';
import IngestionPerformanceView from '../IngestionPerformanceView';
import AssistantAnalyticsView from '../AssistantAnalyticsView';

function AnalyticsHub() {
  const tabs = [
    { key: 'requests', label: 'API Requests', render: () => <RequestHistoryView /> },
    { key: 'ingestion', label: 'Ingestion', render: () => <IngestionPerformanceView /> },
    { key: 'assistants', label: 'Assistants', render: () => <AssistantAnalyticsView /> },
  ];
  return <Tabs tabs={tabs} defaultTabKey="requests" ariaLabel="Analytics" />;
}

export default AnalyticsHub;
