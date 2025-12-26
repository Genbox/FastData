using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class FloatNaNZeroTestVectors : HarnessVectorTheoryData
{
    public FloatNaNZeroTestVectors()
    {
        AddVectors(TestVectorHelper.GetFloatNaNZeroTestVectors().ToArray());
    }
}