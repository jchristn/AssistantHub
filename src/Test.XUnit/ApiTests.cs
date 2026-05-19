namespace Test.XUnit
{
    using Test.Automated;
    using Xunit;

    [Collection("Api")]
    public class ApiTests
    {
        private readonly ApiFixture _Fixture;

        public ApiTests(ApiFixture fixture)
        {
            _Fixture = fixture ?? throw new System.ArgumentNullException(nameof(fixture));
        }

        public static IEnumerable<object[]> TestCases
        {
            get { return ApiData.Cases; }
        }

        [Theory]
        [MemberData(nameof(TestCases), DisableDiscoveryEnumeration = true)]
        public void ApiCasePasses(string testName)
        {
            bool found = _Fixture.Results.TryGetValue(testName, out AutomatedTestResult result);
            Assert.True(found, $"Result not found for '{testName}'.");
            Assert.True(result.Passed, result.ErrorMessage ?? $"Test failed for '{testName}'.");
        }
    }
}
