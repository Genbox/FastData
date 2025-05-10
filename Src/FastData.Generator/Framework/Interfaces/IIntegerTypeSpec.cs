using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IIntegerTypeSpec
{
    public DataType DataType { get; }
    public string Name { get; }
    public string MinValue { get; }
    public string MaxValue { get; }
    public IntegerTypeFlags Flags { get; }
}