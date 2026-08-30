using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed class KeyValueVectors : TheoryData<object>
{
    public KeyValueVectors()
    {
        foreach (ITestVector vector in TestVectorHelper.GetKeyValueTestVectors())
            Add(vector);
    }
}