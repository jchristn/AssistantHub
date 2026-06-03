namespace Test.Xunit
{
    using global::Xunit;

    [CollectionDefinition("Api", DisableParallelization = true)]
    public class ApiCollectionDefinition : ICollectionFixture<ApiFixture>
    {
    }
}
