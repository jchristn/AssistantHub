namespace Test.XUnit
{
    using Xunit;

    [CollectionDefinition("Model", DisableParallelization = true)]
    public class ModelCollectionDefinition : ICollectionFixture<ModelFixture>
    {
    }
}
