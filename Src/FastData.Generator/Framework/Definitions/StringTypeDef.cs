using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class StringTypeDef(string name) : ITypeDef<string>
{
    public DataType DataType => DataType.String;
    public string Name { get; } = name;
    public Func<string, string> Print => x => $"\"{x}\"";
}