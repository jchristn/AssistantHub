namespace Test.Xunit
{
    using Test.Automated;
    using global::Xunit;

    [Collection("Service")]
    public class ServiceTests
    {
        private readonly ServiceFixture _Fixture;

        public ServiceTests(ServiceFixture fixture)
        {
            _Fixture = fixture ?? throw new System.ArgumentNullException(nameof(fixture));
        }

        public static IEnumerable<object[]> TestCases
        {
            get { return ServiceData.Cases; }
        }

        [Theory]
        [MemberData(nameof(TestCases), DisableDiscoveryEnumeration = true)]
        public void ServiceCasePasses(string testName)
        {
            bool found = _Fixture.Results.TryGetValue(testName, out AutomatedTestResult result);
            Assert.True(found, $"Result not found for '{testName}'.");
            Assert.True(result.Passed, result.ErrorMessage ?? $"Test failed for '{testName}'.");
        }
    }
}
