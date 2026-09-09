import React from 'react';
import Tabs from '../../components/Tabs';
import RequestHistoryView from '../RequestHistoryView';
import IngestionPerformanceView from '../IngestionPerformanceView';

function AnalyticsHub() {
  const tabs = [
    { key: 'requests', label: 'API Requests', render: () => <RequestHistoryView /> },
    { key: 'ingestion', label: 'Ingestion', render: () => <IngestionPerformanceView /> },
  ];
  return <Tabs tabs={tabs} defaultTabKey="requests" ariaLabel="Analytics" />;
}

export default AnalyticsHub;
