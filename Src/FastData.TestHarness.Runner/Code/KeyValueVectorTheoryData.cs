using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.TestHarness.Runner.Code;

public sealed class KeyValueVectorTheoryData : TheoryData<ITestVector>
{
    public KeyValueVectorTheoryData()
    {
        foreach (ITestVector vector in TestVectorHelper.GetKeyValueTestVectors())
            Add(vector);
    }
}