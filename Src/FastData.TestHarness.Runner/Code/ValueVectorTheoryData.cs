using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Xunit;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class ValueVectorTheoryData : TheoryData<ITestVector>
{
    public ValueVectorTheoryData()
    {
        foreach (ITestVector vector in TestVectorHelper.GetValueTestVectors())
            Add(vector);
    }
}