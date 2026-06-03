namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    public class TouchstoneTests : TouchstoneNunitBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return TouchstoneSuiteCatalog.GetSuites(); }
        }

        [Test]
        public async Task SharedTouchstoneSuitesPass()
        {
            await RunAllAsync();
        }
    }
}
