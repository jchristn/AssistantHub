namespace Test.Xunit
{
    using global::Xunit;

    [CollectionDefinition("Model", DisableParallelization = true)]
    public class ModelCollectionDefinition : ICollectionFixture<ModelFixture>
    {
    }
}
