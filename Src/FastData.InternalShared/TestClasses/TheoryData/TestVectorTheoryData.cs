using Genbox.FastData.InternalShared.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

public sealed class TestVectorTheoryData : TheoryData<ITestVector>
{
    public TestVectorTheoryData()
    {
        foreach (ITestVector data in TestVectorHelper.GetTestVectors())
        {
            Add(data);
        }
    }
}