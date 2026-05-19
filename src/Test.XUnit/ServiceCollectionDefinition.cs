namespace Test.XUnit
{
    using Xunit;

    [CollectionDefinition("Service", DisableParallelization = true)]
    public class ServiceCollectionDefinition : ICollectionFixture<ServiceFixture>
    {
    }
}
