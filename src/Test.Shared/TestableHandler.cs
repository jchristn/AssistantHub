namespace Test.Shared
{
    using System;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using SyslogLogging;

    public class TestableHandler : HandlerBase
    {
        public TestableHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, null, null, retrieval, inference, null)
        {
        }

        public new bool ValidateTenantAccess(AuthContext auth, string tenantId) => base.ValidateTenantAccess(auth, tenantId);
        public new bool EnforceTenantOwnership(AuthContext auth, string recordTenantId) => base.EnforceTenantOwnership(auth, recordTenantId);
    }
}
