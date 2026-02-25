import React, { useState, useEffect, useCallback } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import Tour from './Tour';
import SetupWizard from './SetupWizard';
import ChatDrawer from './ChatDrawer';
import TenantsView from '../views/TenantsView';
import UsersView from '../views/UsersView';
import CredentialsView from '../views/CredentialsView';
import AssistantsView from '../views/AssistantsView';
import DocumentsView from '../views/DocumentsView';
import FeedbackView from '../views/FeedbackView';
import HistoryView from '../views/HistoryView';
import CollectionsView from '../views/CollectionsView';
import BucketsView from '../views/BucketsView';
import ObjectsView from '../views/ObjectsView';
import RecordsView from '../views/RecordsView';
import ModelsView from '../views/ModelsView';
import ConfigurationView from '../views/ConfigurationView';
import AssistantSettingsView from '../views/AssistantSettingsView';
import IngestionRulesView from '../views/IngestionRulesView';
import EmbeddingEndpointsView from '../views/EmbeddingEndpointsView';
import InferenceEndpointsView from '../views/InferenceEndpointsView';
import { useAuth } from '../context/AuthContext';

function Dashboard() {
  const { isAdmin, isGlobalAdmin, isTenantAdmin } = useAuth();
  const [showTour, setShowTour] = useState(false);
  const [showWizard, setShowWizard] = useState(false);
  const [drawerAssistantId, setDrawerAssistantId] = useState(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const isAdminOrTenantAdmin = isGlobalAdmin || isTenantAdmin;

  const openChatDrawer = useCallback((assistantId) => {
    setDrawerAssistantId(assistantId);
    setDrawerOpen(true);
  }, []);

  const closeChatDrawer = useCallback(() => {
    setDrawerOpen(false);
  }, []);

  useEffect(() => {
    if (!localStorage.getItem('ah_tourCompleted')) {
      setShowTour(true);
    }
  }, []);

  const handleTourComplete = () => {
    setShowTour(false);
    if (isAdminOrTenantAdmin && !localStorage.getItem('ah_wizardCompleted')) {
      setShowWizard(true);
    }
  };

  return (
    <div className="dashboard">
      <Sidebar
        onStartTour={() => setShowTour(true)}
        onStartWizard={() => setShowWizard(true)}
      />
      <div className="main-content">
        <Topbar />
        <div className="content-area">
          <Routes>
            <Route path="/" element={<Navigate to="/assistants" />} />
            {isGlobalAdmin && <Route path="/tenants" element={<TenantsView />} />}
            {isAdminOrTenantAdmin && <Route path="/users" element={<UsersView />} />}
            {isAdminOrTenantAdmin && <Route path="/credentials" element={<CredentialsView />} />}
            {isAdmin && <Route path="/buckets" element={<BucketsView />} />}
            {isAdmin && <Route path="/objects" element={<ObjectsView />} />}
            {isAdmin && <Route path="/collections" element={<CollectionsView />} />}
            {isAdmin && <Route path="/records" element={<RecordsView />} />}
            <Route path="/assistants" element={<AssistantsView />} />
            <Route path="/assistant-settings" element={<AssistantSettingsView onOpenChatDrawer={openChatDrawer} />} />
            {isAdmin && <Route path="/endpoints/embedding" element={<EmbeddingEndpointsView />} />}
            {isAdmin && <Route path="/endpoints/inference" element={<InferenceEndpointsView />} />}
            {isAdminOrTenantAdmin && <Route path="/ingestion-rules" element={<IngestionRulesView />} />}
            <Route path="/documents" element={<DocumentsView />} />
            <Route path="/feedback" element={<FeedbackView />} />
            <Route path="/history" element={<HistoryView />} />
            <Route path="/models" element={<ModelsView />} />
            {isGlobalAdmin && <Route path="/configuration" element={<ConfigurationView />} />}
            <Route path="*" element={<Navigate to="/assistants" />} />
          </Routes>
        </div>
      </div>
      <ChatDrawer assistantId={drawerAssistantId} isOpen={drawerOpen} onClose={closeChatDrawer} />
      {showTour && <Tour onComplete={handleTourComplete} />}
      {showWizard && <SetupWizard onClose={() => setShowWizard(false)} />}
    </div>
  );
}

export default Dashboard;
