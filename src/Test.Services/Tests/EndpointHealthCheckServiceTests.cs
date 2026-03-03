namespace Test.Services.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using Test.Common;

    public static class EndpointHealthCheckServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- EndpointHealthCheckService Tests ---");

            await runner.RunTestAsync("HealthCheck.Constructor_NullSettings_Throws", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new EndpointHealthCheckService(null, logging)),
                    "null settings should throw");
            }, token);

            await runner.RunTestAsync("HealthCheck.Constructor_NullLogging_Throws", async ct =>
            {
                AssistantHubSettings settings = new AssistantHubSettings();

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new EndpointHealthCheckService(settings, null)),
                    "null logging should throw");
            }, token);

            await runner.RunTestAsync("HealthCheck.Constructor_ValidParams_Succeeds", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);
                AssertHelper.IsNotNull(svc, "service should be created");
            }, token);

            await runner.RunTestAsync("HealthCheck.GetHealthState_NoEndpoints_ReturnsNull", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                EndpointHealthState state = svc.GetHealthState("ep_nonexistent");
                AssertHelper.IsNull(state, "non-existent endpoint should return null");
            }, token);

            await runner.RunTestAsync("HealthCheck.GetAllHealthStates_Empty", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                List<EndpointHealthState> states = svc.GetAllHealthStates();
                AssertHelper.IsNotNull(states, "states list should not be null");
                AssertHelper.AreEqual(0, states.Count, "no endpoints should be monitored");
            }, token);

            await runner.RunTestAsync("HealthCheck.GetAllHealthStates_WithTenantFilter_Empty", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                List<EndpointHealthState> states = svc.GetAllHealthStates("ten_test");
                AssertHelper.AreEqual(0, states.Count, "no endpoints for tenant filter");
            }, token);

            await runner.RunTestAsync("HealthCheck.IsHealthy_NoState_ReturnsTrue", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // When no health state exists, service assumes healthy (health check not enabled)
                bool healthy = svc.IsHealthy("ep_nonexistent");
                AssertHelper.AreEqual(true, healthy, "unknown endpoint should default to healthy");
            }, token);

            await runner.RunTestAsync("HealthCheck.OnEndpointCreated_NullJson_NoOp", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // Should not throw
                svc.OnEndpointCreated(null);
                svc.OnEndpointCreated("");

                List<EndpointHealthState> states = svc.GetAllHealthStates();
                AssertHelper.AreEqual(0, states.Count, "null/empty json should not add endpoints");
            }, token);

            await runner.RunTestAsync("HealthCheck.OnEndpointCreated_InvalidJson_NoOp", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // Invalid JSON should not throw, just log warning
                svc.OnEndpointCreated("not valid json");

                List<EndpointHealthState> states = svc.GetAllHealthStates();
                AssertHelper.AreEqual(0, states.Count, "invalid json should not add endpoints");
            }, token);

            await runner.RunTestAsync("HealthCheck.OnEndpointCreated_DisabledEndpoint_NotMonitored", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // Endpoint with health check disabled
                string json = "{\"Id\":\"ep_1\",\"Model\":\"test\",\"Active\":true,\"HealthCheckEnabled\":false,\"Endpoint\":\"http://localhost:9999\"}";
                svc.OnEndpointCreated(json);

                List<EndpointHealthState> states = svc.GetAllHealthStates();
                AssertHelper.AreEqual(0, states.Count, "disabled health check should not be monitored");
            }, token);

            await runner.RunTestAsync("HealthCheck.OnEndpointDeleted_NullId_NoOp", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // Should not throw
                svc.OnEndpointDeleted(null);
                svc.OnEndpointDeleted("");
                AssertHelper.IsTrue(true, "null/empty delete should not throw");
            }, token);

            await runner.RunTestAsync("HealthCheck.OnEndpointUpdated_NullJson_NoOp", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                EndpointHealthCheckService svc = new EndpointHealthCheckService(settings, logging);

                // Should not throw
                svc.OnEndpointUpdated(null);
                svc.OnEndpointUpdated("");
                AssertHelper.IsTrue(true, "null/empty update should not throw");
            }, token);

            await runner.RunTestAsync("HealthCheck.EndpointHealthState_Defaults", async ct =>
            {
                EndpointHealthState state = new EndpointHealthState();

                AssertHelper.AreEqual(string.Empty, state.EndpointId, "EndpointId default");
                AssertHelper.AreEqual(string.Empty, state.EndpointName, "EndpointName default");
                AssertHelper.AreEqual(string.Empty, state.TenantId, "TenantId default");
                AssertHelper.AreEqual(false, state.IsHealthy, "IsHealthy default");
                AssertHelper.AreEqual(0, state.ConsecutiveSuccesses, "ConsecutiveSuccesses default");
                AssertHelper.AreEqual(0, state.ConsecutiveFailures, "ConsecutiveFailures default");
                AssertHelper.IsNull(state.LastError, "LastError default");
                AssertHelper.IsNull(state.LastCheckUtc, "LastCheckUtc default");
                AssertHelper.IsNotNull(state.CheckHistory, "CheckHistory should not be null");
                AssertHelper.AreEqual(0, state.CheckHistory.Count, "CheckHistory should be empty");
                AssertHelper.IsNotNull(state.Lock, "Lock should not be null");
                AssertHelper.IsNotNull(state.HistoryLock, "HistoryLock should not be null");
                AssertHelper.AreEqual((long)0, state.TotalUptimeMs, "TotalUptimeMs default");
                AssertHelper.AreEqual((long)0, state.TotalDowntimeMs, "TotalDowntimeMs default");
            }, token);

            await runner.RunTestAsync("HealthCheck.HealthCheckRecord_Properties", async ct =>
            {
                HealthCheckRecord record = new HealthCheckRecord();
                record.TimestampUtc = DateTime.UtcNow;
                record.Success = true;

                AssertHelper.AreEqual(true, record.Success, "Success");
                AssertHelper.DateTimeRecent(record.TimestampUtc, "TimestampUtc");
            }, token);
        }
    }
}
