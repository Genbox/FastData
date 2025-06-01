using Genbox.FastData.InternalShared.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

public sealed class TestTheoryData : TheoryData<ITestData>
{
    public TestTheoryData()
    {
        foreach (ITestData data in TestVectorHelper.GetTestData())
        {
            Add(data);
        }
    }
}