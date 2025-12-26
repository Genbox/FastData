using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class HarnessBoolTheoryData : TheoryData<ITestHarness, bool>
{
    public HarnessBoolTheoryData()
    {
        foreach (ITestHarness harness in TestHarness.All)
        {
            Add(harness, false);
            Add(harness, true);
        }
    }
}