import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
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
  const { isAdmin } = useAuth();

  return (
    <div className="dashboard">
      <Sidebar />
      <div className="main-content">
        <Topbar />
        <div className="content-area">
          <Routes>
            <Route path="/" element={<Navigate to="/assistants" />} />
            {isAdmin && <Route path="/users" element={<UsersView />} />}
            {isAdmin && <Route path="/credentials" element={<CredentialsView />} />}
            {isAdmin && <Route path="/buckets" element={<BucketsView />} />}
            {isAdmin && <Route path="/objects" element={<ObjectsView />} />}
            {isAdmin && <Route path="/collections" element={<CollectionsView />} />}
            {isAdmin && <Route path="/records" element={<RecordsView />} />}
            <Route path="/assistants" element={<AssistantsView />} />
            <Route path="/assistant-settings" element={<AssistantSettingsView />} />
            {isAdmin && <Route path="/endpoints/embedding" element={<EmbeddingEndpointsView />} />}
            {isAdmin && <Route path="/endpoints/inference" element={<InferenceEndpointsView />} />}
            {isAdmin && <Route path="/ingestion-rules" element={<IngestionRulesView />} />}
            <Route path="/documents" element={<DocumentsView />} />
            <Route path="/feedback" element={<FeedbackView />} />
            <Route path="/history" element={<HistoryView />} />
            <Route path="/models" element={<ModelsView />} />
            {isAdmin && <Route path="/configuration" element={<ConfigurationView />} />}
            <Route path="*" element={<Navigate to="/assistants" />} />
          </Routes>
        </div>
      </div>
    </div>
  );
}

export default Dashboard;
