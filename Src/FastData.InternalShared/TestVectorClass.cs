using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestVectorClass : TheoryData<StructureType, object[]>
{
    public TestVectorClass()
    {
        foreach ((StructureType type, object[] data) in TestVectorHelper.GetTestVectors())
            Add(type, data);
    }
}