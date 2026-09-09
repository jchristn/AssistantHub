import React from 'react';
import ScopedHub from './ScopedHub';
import AssistantsView from '../AssistantsView';
import AssistantSettingsView from '../AssistantSettingsView';
import FeedbackView from '../FeedbackView';
import HistoryView from '../HistoryView';
import AssistantAnalyticsView from '../AssistantAnalyticsView';
import EvaluationView from '../EvaluationView';

const loadOptions = async (api) => {
  const result = await api.getAssistants({ maxResults: 1000 });
  const items = (result && result.Objects) ? result.Objects : (Array.isArray(result) ? result : []);
  return items.map((a) => ({ value: a.Id, label: a.Name || a.Id }));
};

function AssistantsHub({ onOpenChatDrawer }) {
  const tabs = [
    { key: 'assistants', label: 'Assistants', render: () => <AssistantsView /> },
    { key: 'settings', label: 'Settings', render: (scopeId) => <AssistantSettingsView embedded scopeAssistantId={scopeId} onOpenChatDrawer={onOpenChatDrawer} /> },
    { key: 'feedback', label: 'Feedback', render: (scopeId) => <FeedbackView embedded scopeAssistantId={scopeId} /> },
    { key: 'history', label: 'History', render: (scopeId) => <HistoryView embedded scopeAssistantId={scopeId} /> },
    { key: 'analytics', label: 'Analytics', render: (scopeId) => <AssistantAnalyticsView embedded scopeAssistantId={scopeId} /> },
    { key: 'evaluation', label: 'Evaluation', render: (scopeId) => <EvaluationView embedded scopeAssistantId={scopeId} /> },
  ];
  return (
    <ScopedHub
      scopeParam="assistantId"
      scopeLabel="Assistant"
      scopePlaceholder="All assistants"
      loadOptions={loadOptions}
      tabs={tabs}
      defaultTabKey="assistants"
      ariaLabel="Assistants"
    />
  );
}

export default AssistantsHub;
