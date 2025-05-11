using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class StringTypeDef<T>(string name) : ITypeDef<T> where T : notnull
{
    public DataType DataType => DataType.String;
    public string Name { get; } = name;
    public Func<T, string> Print => x => $"\"{x}\"";
}