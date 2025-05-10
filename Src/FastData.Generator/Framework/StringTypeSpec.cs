using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public class StringTypeSpec<T>(string name) : ITypeSpec<T> where T : notnull
{
    public DataType DataType => DataType.String;
    public string Name { get; } = name;
    public Func<T, string> Print => x => $"\"{x}\"";
}