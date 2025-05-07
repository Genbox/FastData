using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestDataClass : TheoryData<ITestData>
{
    public TestDataClass()
    {
        foreach (ITestData data in TestVectorHelper.GetTestData())
        {
            Add(data);
        }
    }
}