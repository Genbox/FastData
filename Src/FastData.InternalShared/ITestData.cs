using Genbox.FastData.Enums;

namespace Genbox.FastData.InternalShared;

public interface ITestData
{
    public Type Type { get; }
    public StructureType StructureType { get; }
    public object[] Items { get; }
    string Identifier { get; }
}