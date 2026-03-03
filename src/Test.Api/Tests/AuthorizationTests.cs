namespace Test.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Handlers;
    using SyslogLogging;
    using Test.Common;
    using WatsonWebserver.Core;

    /// <summary>
    /// Exposes protected HandlerBase methods for testing.
    /// </summary>
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

    public static class AuthorizationTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("AuthorizationTests");

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

            await runner.RunTestAsync("Auth.ValidateTenantAccess: null auth returns false", async ct =>
            {
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(null, "ten_abc"), "should be false");
            }, token);

            await runner.RunTestAsync("Auth.ValidateTenantAccess: global admin can access any tenant", async ct =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_abc"), "global admin should access any tenant");
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(admin, "ten_xyz"), "global admin should access any tenant");
            }, token);

            await runner.RunTestAsync("Auth.ValidateTenantAccess: tenant admin can access own tenant", async ct =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(tenantAdmin, "ten_abc"), "should access own tenant");
            }, token);

            await runner.RunTestAsync("Auth.ValidateTenantAccess: tenant admin cannot access other tenant", async ct =>
            {
                AuthContext tenantAdmin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = true, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(tenantAdmin, "ten_xyz"), "should NOT access other tenant");
            }, token);

            await runner.RunTestAsync("Auth.ValidateTenantAccess: regular user can access own tenant", async ct =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.ValidateTenantAccess(regularUser, "ten_abc"), "should access own tenant");
            }, token);

            await runner.RunTestAsync("Auth.ValidateTenantAccess: regular user cannot access other tenant", async ct =>
            {
                AuthContext regularUser = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, IsTenantAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.ValidateTenantAccess(regularUser, "ten_xyz"), "should NOT access other tenant");
            }, token);

            // --- EnforceTenantOwnership tests ---

            await runner.RunTestAsync("Auth.EnforceTenantOwnership: null auth returns false", async ct =>
            {
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(null, "ten_abc"), "should be false");
            }, token);

            await runner.RunTestAsync("Auth.EnforceTenantOwnership: global admin bypasses ownership check", async ct =>
            {
                AuthContext admin = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = true, TenantId = null };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_abc"), "global admin bypasses");
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(admin, "ten_xyz"), "global admin bypasses");
            }, token);

            await runner.RunTestAsync("Auth.EnforceTenantOwnership: matching tenant passes", async ct =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(true, handler.EnforceTenantOwnership(user, "ten_abc"), "matching tenant should pass");
            }, token);

            await runner.RunTestAsync("Auth.EnforceTenantOwnership: mismatched tenant fails", async ct =>
            {
                AuthContext user = new AuthContext { IsAuthenticated = true, IsGlobalAdmin = false, TenantId = "ten_abc" };
                AssertHelper.AreEqual(false, handler.EnforceTenantOwnership(user, "ten_xyz"), "mismatched tenant should fail");
            }, token);

            // --- AuthContext model tests ---

            await runner.RunTestAsync("AuthContext: defaults are not authenticated", async ct =>
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
            }, token);

            await runner.RunTestAsync("AuthContext: global admin auth context properties", async ct =>
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
            }, token);

            await runner.RunTestAsync("AuthContext: regular user auth context properties", async ct =>
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
            }, token);
        }
    }
}
