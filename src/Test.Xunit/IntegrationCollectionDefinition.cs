namespace Test.Xunit
{
    using global::Xunit;

    [CollectionDefinition("Integration", DisableParallelization = true)]
    public class IntegrationCollectionDefinition : ICollectionFixture<IntegrationFixture>
    {
    }
}
