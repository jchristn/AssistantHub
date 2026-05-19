namespace Test.Automated
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class AutomatedTestExecution
    {
        public static async Task<IReadOnlyList<AutomatedTestResult>> RunAllAsync()
        {
            List<AutomatedTestResult> results = new List<AutomatedTestResult>();

            ModelSuite modelSuite = new ModelSuite();
            ServiceSuite serviceSuite = new ServiceSuite();
            ApiSuite apiSuite = new ApiSuite();
            IntegrationSuite integrationSuite = new IntegrationSuite();

            results.AddRange(await modelSuite.RunAsync().ConfigureAwait(false));
            results.AddRange(await serviceSuite.RunAsync().ConfigureAwait(false));
            results.AddRange(await apiSuite.RunAsync().ConfigureAwait(false));
            results.AddRange(await integrationSuite.RunAsync().ConfigureAwait(false));

            return results.ToArray();
        }
    }
}
