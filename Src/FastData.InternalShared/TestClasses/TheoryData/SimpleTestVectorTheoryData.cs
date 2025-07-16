using Genbox.FastData.InternalShared.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

public sealed class SimpleTestVectorTheoryData : TheoryData<ITestVector>
{
    public SimpleTestVectorTheoryData()
    {
        foreach (ITestVector data in TestVectorHelper.GetSimpleTestVectors())
        {
            Add(data);
        }
    }
}