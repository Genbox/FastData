using Genbox.FastData.InternalShared.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

public sealed class FloatNaNZeroTestVectorTheoryData : TheoryData<ITestVector>
{
    public FloatNaNZeroTestVectorTheoryData()
    {
        foreach (ITestVector data in TestVectorHelper.GetFloatNaNZeroTestVectors())
        {
            Add(data);
        }
    }
}