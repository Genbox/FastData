using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;
using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class TestDataClass : TheoryData<StructureType, object[]>
{
    public TestDataClass()
    {
        foreach ((StructureType type, object[] data) in TestVectorHelper.GetTestData())
            Add(type, data);
    }
}