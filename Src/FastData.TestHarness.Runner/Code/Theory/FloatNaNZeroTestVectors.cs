using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed class FloatNaNZeroTestVectors : TheoryData<ITestVector>
{
    public FloatNaNZeroTestVectors()
    {
        foreach (ITestVector vector in TestVectorHelper.GetFloatNaNZeroTestVectors())
            Add(vector);
    }
}