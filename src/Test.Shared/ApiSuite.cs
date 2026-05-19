namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using SyslogLogging;
    using Test.Shared;

    public class ApiSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            MockDatabaseDriver db = new MockDatabaseDriver();
            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;
            AssistantHubSettings settings = new AssistantHubSettings();
            AuthenticationService authService = new AuthenticationService(db, logging, settings);
            InferenceSettings infSettings = new InferenceSettings();
            InferenceService inference = new InferenceService(infSettings, logging);
            ChunkingSettings chunkSettings = new ChunkingSettings();
            RecallDbSettings recallSettings = new RecallDbSettings();
            RetrievalService retrieval = new RetrievalService(chunkSettings, recallSettings, logging);

            TestableHandler handler = new TestableHandler(db, logging, settings, authService, retrieval, inference);

            // --- ValidateTenantAccess tests ---

            await ExecuteTestAsync("Auth.ValidateTenantAccess: null auth returns false", async () =>
            {
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(null, "ten_abc"), "should be false");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: global admin can access any tenant", async () =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_abc"), "global admin should access any tenant");
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_xyz"), "global admin should access any tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: tenant admin can access own tenant", async () =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(tenantAdmin, "ten_abc"), "should access own tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: tenant admin cannot access other tenant", async () =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(tenantAdmin, "ten_xyz"), "should NOT access other tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: regular user can access own tenant", async () =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(regularUser, "ten_abc"), "should access own tenant");
            });

            await ExecuteTestAsync("Auth.ValidateTenantAccess: regular user cannot access other tenant", async () =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(regularUser, "ten_xyz"), "should NOT access other tenant");
            });

            // --- EnforceTenantOwnership tests ---

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: null auth returns false", async () =>
            {
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(null, "ten_abc"), "should be false");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: global admin bypasses ownership check", async () =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_abc"), "global admin bypasses");
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_xyz"), "global admin bypasses");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: matching tenant passes", async () =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(user, "ten_abc"), "matching tenant should pass");
            });

            await ExecuteTestAsync("Auth.EnforceTenantOwnership: mismatched tenant fails", async () =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(user, "ten_xyz"), "mismatched tenant should fail");
            });

            // --- AuthContext model tests ---

            await ExecuteTestAsync("AuthContext: defaults are not authenticated", async () =>
            {
                AuthContext ctx = new AuthContext();
                AssertHelper.AreEqual(false, ctx.IsAuthenticated, "IsAuthenticated default");
                AssertHelper.AreEqual(false, ctx.IsGlobalAdmin, "IsGlobalAdmin default");
                AssertHelper.AreEqual(false, ctx.IsTenantAdmin, "IsTenantAdmin default");
                AssertHelper.IsNull(ctx.TenantId, "TenantId default");
                AssertHelper.IsNull(ctx.UserId, "UserId default");
                AssertHelper.IsNull(ctx.CredentialId, "CredentialId default");
                AssertHelper.IsNull(ctx.Email, "Email default");
                AssertHelper.IsNull(ctx.Tenant, "Tenant default");
                AssertHelper.IsNull(ctx.User, "User default");
            });

            await ExecuteTestAsync("AuthContext: global admin auth context properties", async () =>
            {
                AuthContext ctx = new AuthContext
                {
                    IsAuthenticated = true,
                    IsGlobalAdmin = true,
                    TenantId = null,
                    UserId = null
                };
                AssertHelper.AreEqual(true, ctx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(true, ctx.IsGlobalAdmin, "IsGlobalAdmin");
                AssertHelper.IsNull(ctx.TenantId, "TenantId null for admin key");
                AssertHelper.IsNull(ctx.UserId, "UserId null for admin key");
            });

            await ExecuteTestAsync("AuthContext: regular user auth context properties", async () =>
            {
                AuthContext ctx = new AuthContext
                {
                    IsAuthenticated = true,
                    IsGlobalAdmin = false,
                    IsTenantAdmin = false,
                    TenantId = "ten_abc",
                    UserId = "usr_123",
                    CredentialId = "cred_456",
                    Email = "user@example.com"
                };
                AssertHelper.AreEqual(true, ctx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(false, ctx.IsGlobalAdmin, "IsGlobalAdmin");
                AssertHelper.AreEqual(false, ctx.IsTenantAdmin, "IsTenantAdmin");
                AssertHelper.AreEqual("ten_abc", ctx.TenantId, "TenantId");
                AssertHelper.AreEqual("usr_123", ctx.UserId, "UserId");
                AssertHelper.AreEqual("cred_456", ctx.CredentialId, "CredentialId");
                AssertHelper.AreEqual("user@example.com", ctx.Email, "Email");
            });

            return GetResults();
        }
    }
}
