import React from 'react';
import Tabs from '../../components/Tabs';
import { useAuth } from '../../context/AuthContext';
import TenantsView from '../TenantsView';
import UsersView from '../UsersView';
import CredentialsView from '../CredentialsView';

function AuthenticationHub() {
  const { isGlobalAdmin } = useAuth();
  const tabs = [
    { key: 'tenants', label: 'Tenants', hidden: !isGlobalAdmin, render: () => <TenantsView /> },
    { key: 'users', label: 'Users', render: () => <UsersView /> },
    { key: 'credentials', label: 'Credentials', render: () => <CredentialsView /> },
  ];
  return <Tabs tabs={tabs} defaultTabKey={isGlobalAdmin ? 'tenants' : 'users'} ariaLabel="Authentication" />;
}

export default AuthenticationHub;
