namespace Test.XUnit
{
    using System.Collections.Generic;

    public static class ModelData
    {
        public static IEnumerable<object[]> Cases
        {
            get
            {
                ModelFixture fixture = new ModelFixture();
                List<string> names = new List<string>(fixture.Results.Keys);
                names.Sort(System.StringComparer.Ordinal);

                for (int i = 0; i < names.Count; i++)
                    yield return new object[] { names[i] };
            }
        }
    }
}
