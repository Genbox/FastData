using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class ValueTestVectors : HarnessVectorTheoryData
{
    public ValueTestVectors()
    {
        AddVectors(TestVectorHelper.GetValueTestVectors().ToArray());
    }
}