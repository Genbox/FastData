using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class ArrayTypeDef<T>(string name, DataType dataType) : ITypeDef<T> where T : notnull
{
    public DataType DataType => dataType;
    public string Name { get; } = name;
    public Func<T, string> Print => x => string.Join(", ", (T[])(object)x);
}