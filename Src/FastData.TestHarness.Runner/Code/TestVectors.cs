using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class TestVectors : HarnessVectorTheoryData
{
    public TestVectors()
    {
        AddVectors(TestVectorHelper.GetTestVectors().ToArray());
    }
}