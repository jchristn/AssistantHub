namespace Test.Xunit
{
    using Test.Automated;
    using global::Xunit;

    [Collection("Integration")]
    public class IntegrationTests
    {
        private readonly IntegrationFixture _Fixture;

        public IntegrationTests(IntegrationFixture fixture)
        {
            _Fixture = fixture ?? throw new System.ArgumentNullException(nameof(fixture));
        }

        public static IEnumerable<object[]> TestCases
        {
            get { return IntegrationData.Cases; }
        }

        [Theory]
        [MemberData(nameof(TestCases), DisableDiscoveryEnumeration = true)]
        public void IntegrationCasePasses(string testName)
        {
            bool found = _Fixture.Results.TryGetValue(testName, out AutomatedTestResult result);
            Assert.True(found, $"Result not found for '{testName}'.");
            Assert.True(result.Passed, result.ErrorMessage ?? $"Test failed for '{testName}'.");
        }
    }
}
