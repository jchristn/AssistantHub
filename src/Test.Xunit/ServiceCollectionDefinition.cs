namespace Test.Xunit
{
    using global::Xunit;

    [CollectionDefinition("Service", DisableParallelization = true)]
    public class ServiceCollectionDefinition : ICollectionFixture<ServiceFixture>
    {
    }
}
