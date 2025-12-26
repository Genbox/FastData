using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class HarnessTheoryData : TheoryData<ITestHarness>
{
    public HarnessTheoryData()
    {
        foreach (ITestHarness harness in TestHarness.All)
            Add(harness);
    }
}