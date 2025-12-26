using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner.Code;

public abstract class HarnessVectorTheoryData : TheoryData<ITestHarness, ITestVector>
{
    protected void AddVectors(ITestVector[] vectors)
    {
        foreach (ITestHarness harness in TestHarness.All)
        {
            foreach (ITestVector vector in vectors)
                Add(harness, vector);
        }
    }
}