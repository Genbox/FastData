using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestVectorClass : TheoryData<ITestVector>
{
    public TestVectorClass()
    {
        foreach (ITestVector data in TestVectorHelper.GetTestVectors())
        {
            Add(data);
        }
    }
}