namespace Test.Xunit
{
    using System.Collections.Generic;

    public static class IntegrationData
    {
        public static IEnumerable<object[]> Cases
        {
            get
            {
                IntegrationFixture fixture = new IntegrationFixture();
                List<string> names = new List<string>(fixture.Results.Keys);
                names.Sort(System.StringComparer.Ordinal);

                for (int i = 0; i < names.Count; i++)
                    yield return new object[] { names[i] };
            }
        }
    }
}
