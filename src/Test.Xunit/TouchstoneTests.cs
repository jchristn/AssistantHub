namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using global::Xunit;

    public class TouchstoneTests : TouchstoneFactBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return TouchstoneSuiteCatalog.GetSuites(); }
        }

        [Fact]
        public async Task SharedTouchstoneSuitesPass()
        {
            await RunAllAsync();
        }
    }
}
