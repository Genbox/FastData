using Genbox.FastData.Enums;
using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestVectorClass : TheoryData<StructureType, object[]>
{
    public TestVectorClass()
    {
        foreach ((StructureType type, object[] data) in TestVectorHelper.GetTestVectors())
        {
            Add(type, data);
        }
    }
}