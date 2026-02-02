using Genbox.FastData.InternalShared.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

public sealed class ValueTestVectorTheoryData : TheoryData<ITestVector>
{
    public ValueTestVectorTheoryData()
    {
        foreach (ITestVector data in TestVectorHelper.GetKeyValueTestVectors())
        {
            Add(data);
        }
    }
}