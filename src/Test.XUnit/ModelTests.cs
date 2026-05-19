namespace Test.XUnit
{
    using Test.Automated;
    using Xunit;

    [Collection("Model")]
    public class ModelTests
    {
        private readonly ModelFixture _Fixture;

        public ModelTests(ModelFixture fixture)
        {
            _Fixture = fixture ?? throw new System.ArgumentNullException(nameof(fixture));
        }

        public static IEnumerable<object[]> TestCases
        {
            get { return ModelData.Cases; }
        }

        [Theory]
        [MemberData(nameof(TestCases), DisableDiscoveryEnumeration = true)]
        public void ModelCasePasses(string testName)
        {
            bool found = _Fixture.Results.TryGetValue(testName, out AutomatedTestResult result);
            Assert.True(found, $"Result not found for '{testName}'.");
            Assert.True(result.Passed, result.ErrorMessage ?? $"Test failed for '{testName}'.");
        }
    }
}
