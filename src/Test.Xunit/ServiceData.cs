namespace Test.Xunit
{
    using System.Collections.Generic;

    public static class ServiceData
    {
        public static IEnumerable<object[]> Cases
        {
            get
            {
                ServiceFixture fixture = new ServiceFixture();
                List<string> names = new List<string>(fixture.Results.Keys);
                names.Sort(System.StringComparer.Ordinal);

                for (int i = 0; i < names.Count; i++)
                    yield return new object[] { names[i] };
            }
        }
    }
}
