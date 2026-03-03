namespace Test.Services.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using Test.Common;

    public static class AuthenticationServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("AuthenticationServiceTests");

            // Setup shared test data
            MockDatabaseDriver db = new MockDatabaseDriver();
            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;

            // Create test tenant
            TenantMetadata tenant = new TenantMetadata { Name = "Test Tenant", Active = true };
            tenant = await db.Tenant.CreateAsync(tenant, token);

            // Create test user
            UserMaster user = new UserMaster
            {
                TenantId = tenant.Id,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Active = true,
                IsAdmin = false,
                IsTenantAdmin = false
            };
            user.SetPassword("testpass123");
            user = await db.User.CreateAsync(user, token);

            // Create test credential
            Credential cred = new Credential
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Name = "Test Credential",
                Active = true
            };
            cred = await db.Credential.CreateAsync(cred, token);

            // Create inactive user
            UserMaster inactiveUser = new UserMaster
            {
                TenantId = tenant.Id,
                Email = "inactive@example.com",
                Active = false
            };
            inactiveUser.SetPassword("pass");
            inactiveUser = await db.User.CreateAsync(inactiveUser, token);

            Credential inactiveCred = new Credential
            {
                TenantId = tenant.Id,
                UserId = inactiveUser.Id,
                Active = false
            };
            inactiveCred = await db.Credential.CreateAsync(inactiveCred, token);

            // Settings with admin API key
            AssistantHubSettings settings = new AssistantHubSettings();
            settings.AdminApiKeys = new List<string> { "admin-api-key-12345" };

            AuthenticationService authService = new AuthenticationService(db, logging, settings);

            // --- AuthenticateBearerAsync tests ---

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: valid bearer returns AuthContext", async ct =>
            {
                var authCtx = await authService.AuthenticateBearerAsync(cred.BearerToken, ct);
                AssertHelper.IsNotNull(authCtx, "AuthContext");
                AssertHelper.AreEqual(true, authCtx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(user.Id, authCtx.UserId, "UserId");
                AssertHelper.AreEqual(tenant.Id, authCtx.TenantId, "TenantId");
                AssertHelper.AreEqual(cred.Id, authCtx.CredentialId, "CredentialId");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: admin API key returns admin AuthContext", async ct =>
            {
                var authCtx = await authService.AuthenticateBearerAsync("admin-api-key-12345", ct);
                AssertHelper.IsNotNull(authCtx, "AuthContext");
                AssertHelper.AreEqual(true, authCtx.IsAuthenticated, "IsAuthenticated");
                AssertHelper.AreEqual(true, authCtx.IsGlobalAdmin, "IsGlobalAdmin");
                AssertHelper.IsNull(authCtx.UserId, "UserId null for admin API key");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: invalid token returns null", async ct =>
            {
                var authCtx = await authService.AuthenticateBearerAsync("invalid-token", ct);
                AssertHelper.IsNull(authCtx, "should be null");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: inactive credential returns null", async ct =>
            {
                var authCtx = await authService.AuthenticateBearerAsync(inactiveCred.BearerToken, ct);
                AssertHelper.IsNull(authCtx, "should be null for inactive credential");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: inactive user returns null", async ct =>
            {
                // Create credential for inactive user that IS active itself
                Credential activeCred = new Credential
                {
                    TenantId = tenant.Id,
                    UserId = inactiveUser.Id,
                    Active = true
                };
                activeCred = await db.Credential.CreateAsync(activeCred, ct);
                var authCtx = await authService.AuthenticateBearerAsync(activeCred.BearerToken, ct);
                AssertHelper.IsNull(authCtx, "should be null for inactive user");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateBearerAsync: inactive tenant returns null", async ct =>
            {
                // Create inactive tenant with user and credential
                TenantMetadata inactiveTenant = new TenantMetadata { Name = "Inactive Tenant", Active = false };
                inactiveTenant = await db.Tenant.CreateAsync(inactiveTenant, ct);

                UserMaster tenantUser = new UserMaster { TenantId = inactiveTenant.Id, Email = "t@t.com", Active = true };
                tenantUser.SetPassword("p");
                tenantUser = await db.User.CreateAsync(tenantUser, ct);

                Credential tenantCred = new Credential { TenantId = inactiveTenant.Id, UserId = tenantUser.Id, Active = true };
                tenantCred = await db.Credential.CreateAsync(tenantCred, ct);

                var authCtx = await authService.AuthenticateBearerAsync(tenantCred.BearerToken, ct);
                AssertHelper.IsNull(authCtx, "should be null for inactive tenant");
            }, token);

            // --- AuthenticateByEmailPasswordAsync tests ---

            await runner.RunTestAsync("AuthService.AuthenticateByEmailPassword: correct email/password", async ct =>
            {
                var result = await authService.AuthenticateByEmailPasswordAsync(tenant.Id, "test@example.com", "testpass123", ct);
                AssertHelper.IsNotNull(result, "result");
                AssertHelper.AreEqual(true, result.Success, "Success");
                AssertHelper.IsNotNull(result.User, "User");
                AssertHelper.AreEqual(tenant.Id, result.TenantId, "TenantId");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateByEmailPassword: wrong password", async ct =>
            {
                var result = await authService.AuthenticateByEmailPasswordAsync(tenant.Id, "test@example.com", "wrong", ct);
                AssertHelper.AreEqual(false, result.Success, "should fail");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateByEmailPassword: non-existent email", async ct =>
            {
                var result = await authService.AuthenticateByEmailPasswordAsync(tenant.Id, "nobody@example.com", "pass", ct);
                AssertHelper.AreEqual(false, result.Success, "should fail");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateByEmailPassword: default tenant used when not specified", async ct =>
            {
                // Create user in default tenant
                TenantMetadata defaultTenant = new TenantMetadata { Active = true };
                defaultTenant.Id = Constants.DefaultTenantId;
                defaultTenant.Name = "Default Tenant";
                await db.Tenant.CreateAsync(defaultTenant, ct);

                UserMaster defaultUser = new UserMaster
                {
                    TenantId = Constants.DefaultTenantId,
                    Email = "defaultuser@test.com",
                    Active = true
                };
                defaultUser.SetPassword("defaultpass");
                await db.User.CreateAsync(defaultUser, ct);

                var result = await authService.AuthenticateByEmailPasswordAsync(null, "defaultuser@test.com", "defaultpass", ct);
                AssertHelper.AreEqual(true, result.Success, "should succeed with default tenant");
            }, token);

            await runner.RunTestAsync("AuthService.AuthenticateByEmailPassword: password redacted in result", async ct =>
            {
                var result = await authService.AuthenticateByEmailPasswordAsync(tenant.Id, "test@example.com", "testpass123", ct);
                AssertHelper.AreEqual(true, result.Success, "Success");
                AssertHelper.IsNull(result.User.PasswordSha256, "password should be redacted");
            }, token);
        }
    }
}
