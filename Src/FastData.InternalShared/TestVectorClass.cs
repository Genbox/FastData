using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestVectorClass : TheoryData<ITestData>
{
    public TestVectorClass()
    {
        foreach (ITestData data in TestVectorHelper.GetTestVectors())
        {
            Add(data);
        }
    }
}