using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed class ValueVectors : TheoryData<ITestVector>
{
    public ValueVectors()
    {
        foreach (ITestVector vector in TestVectorHelper.GetValueTestVectors())
            Add(vector);
    }
}