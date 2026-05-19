namespace Test.XUnit
{
    using Xunit;

    [CollectionDefinition("Integration", DisableParallelization = true)]
    public class IntegrationCollectionDefinition : ICollectionFixture<IntegrationFixture>
    {
    }
}
