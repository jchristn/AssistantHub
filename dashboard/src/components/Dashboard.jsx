import React, { useState, useEffect, useCallback, lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import Tour from './Tour';
import SetupWizard from './SetupWizard';
import ChatDrawer from './ChatDrawer';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import { useUploadQueue } from '../hooks/useUploadQueue';
import UploadProgressPanel from './UploadProgressPanel';
import ConfirmModal from './ConfirmModal';
import AlertModal from './AlertModal';

// Views load when first opened, which keeps the initial bundle small.
const DocumentsView = lazy(() => import('../views/DocumentsView'));
const ApiExplorerView = lazy(() => import('../views/ApiExplorerView'));
const ConfigurationView = lazy(() => import('../views/ConfigurationView'));
const IngestionRulesView = lazy(() => import('../views/IngestionRulesView'));
const CrawlersView = lazy(() => import('../views/CrawlersView'));
const AssistantsHub = lazy(() => import('../views/hubs/AssistantsHub'));
const BucketsHub = lazy(() => import('../views/hubs/BucketsHub'));
const CollectionsHub = lazy(() => import('../views/hubs/CollectionsHub'));
const IndicesHub = lazy(() => import('../views/hubs/IndicesHub'));
const EndpointsHub = lazy(() => import('../views/hubs/EndpointsHub'));
const AuthenticationHub = lazy(() => import('../views/hubs/AuthenticationHub'));
const AnalyticsHub = lazy(() => import('../views/hubs/AnalyticsHub'));

function Dashboard() {
  const { serverUrl, credential, isAdmin, isGlobalAdmin, isTenantAdmin } = useAuth();
  const [showTour, setShowTour] = useState(false);
  const [showWizard, setShowWizard] = useState(false);
  const [drawerAssistantId, setDrawerAssistantId] = useState(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const { records, dismissRecord, cancelRecord, clearFinishedRecords } = useUploadQueue(api);
  const [cancelTarget, setCancelTarget] = useState(null);
  const [cancelling, setCancelling] = useState(false);
  const [cancelError, setCancelError] = useState(null);

  const isAdminOrTenantAdmin = isGlobalAdmin || isTenantAdmin;

  const handleConfirmCancelIngestion = useCallback(async () => {
    if (!cancelTarget) return;
    setCancelling(true);
    try {
      if (cancelTarget.serverDocId) {
        await api.deleteDocument(cancelTarget.serverDocId);
      }
      cancelRecord(cancelTarget.id);
      setCancelTarget(null);
    } catch (err) {
      setCancelError(err.message || 'Failed to cancel ingestion.');
    } finally {
      setCancelling(false);
    }
  }, [cancelTarget, api, cancelRecord]);

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
          <Suspense fallback={<div className="loading">Loading...</div>}>
          <Routes>
            <Route path="/" element={<Navigate to="/assistants" />} />

            {/* Ingestion */}
            <Route path="/documents" element={<DocumentsView />} />
            <Route path="/crawlers" element={<CrawlersView />} />

            {/* Chat */}
            <Route path="/assistants" element={<AssistantsHub onOpenChatDrawer={openChatDrawer} />} />
            <Route path="/assistant-settings" element={<Navigate to="/assistants?tab=settings" replace />} />
            <Route path="/feedback" element={<Navigate to="/assistants?tab=feedback" replace />} />
            <Route path="/history" element={<Navigate to="/assistants?tab=history" replace />} />
            <Route path="/assistant-analytics" element={<Navigate to="/analytics?tab=assistants" replace />} />
            <Route path="/evaluation" element={<Navigate to="/assistants?tab=evaluation" replace />} />

            {/* Monitoring */}
            {isAdminOrTenantAdmin && <Route path="/analytics" element={<AnalyticsHub />} />}
            {isAdminOrTenantAdmin && <Route path="/request-history" element={<Navigate to="/analytics?tab=requests" replace />} />}
            {isAdminOrTenantAdmin && <Route path="/api-explorer" element={<ApiExplorerView />} />}

            {/* Artifacts */}
            {isAdmin && <Route path="/buckets" element={<BucketsHub />} />}
            {isAdmin && <Route path="/objects" element={<Navigate to="/buckets?tab=objects" replace />} />}
            {isAdmin && <Route path="/collections" element={<CollectionsHub />} />}
            {isAdmin && <Route path="/records" element={<Navigate to="/collections?tab=records" replace />} />}
            {isAdmin && <Route path="/collections/search" element={<Navigate to="/collections?tab=search" replace />} />}
            {isAdmin && <Route path="/indices" element={<IndicesHub />} />}
            {isAdmin && <Route path="/indices/records" element={<Navigate to="/indices?tab=records" replace />} />}
            {isAdmin && <Route path="/indices/search" element={<Navigate to="/indices?tab=search" replace />} />}

            {/* Configuration */}
            {isAdmin && <Route path="/endpoints" element={<EndpointsHub />} />}
            {isAdmin && <Route path="/endpoints/embedding" element={<Navigate to="/endpoints?tab=embedding" replace />} />}
            {isAdmin && <Route path="/endpoints/inference" element={<Navigate to="/endpoints?tab=inference" replace />} />}
            <Route path="/models" element={<Navigate to="/endpoints?tab=models" replace />} />
            {isAdminOrTenantAdmin && <Route path="/ingestion-rules" element={<IngestionRulesView />} />}
            {isAdminOrTenantAdmin && <Route path="/authentication" element={<AuthenticationHub />} />}
            {isGlobalAdmin && <Route path="/tenants" element={<Navigate to="/authentication?tab=tenants" replace />} />}
            {isAdminOrTenantAdmin && <Route path="/users" element={<Navigate to="/authentication?tab=users" replace />} />}
            {isAdminOrTenantAdmin && <Route path="/credentials" element={<Navigate to="/authentication?tab=credentials" replace />} />}
            {isGlobalAdmin && <Route path="/configuration" element={<ConfigurationView />} />}

            <Route path="*" element={<Navigate to="/assistants" />} />
          </Routes>
          </Suspense>
        </div>
      </div>
      <ChatDrawer assistantId={drawerAssistantId} isOpen={drawerOpen} onClose={closeChatDrawer} />
      <UploadProgressPanel records={records} onDismiss={dismissRecord} onCancel={setCancelTarget} onClearFinished={clearFinishedRecords} />
      {cancelTarget && (
        <ConfirmModal
          title="Cancel Ingestion"
          message="This document will be deleted. Are you sure you wish to cancel ingestion?"
          confirmLabel="Cancel Ingestion"
          loadingLabel="Cancelling..."
          isLoading={cancelling}
          danger
          onConfirm={handleConfirmCancelIngestion}
          onClose={() => setCancelTarget(null)}
        />
      )}
      {cancelError && <AlertModal title="Error" message={cancelError} onClose={() => setCancelError(null)} />}
      {showTour && <Tour onComplete={handleTourComplete} />}
      {showWizard && <SetupWizard onClose={() => setShowWizard(false)} />}
    </div>
  );
}

export default Dashboard;
