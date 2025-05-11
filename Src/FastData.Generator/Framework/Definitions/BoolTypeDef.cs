using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework;

public class BoolTypeDef<T>(string name) : ITypeDef<T> where T : notnull
{
    public DataType DataType => DataType.Boolean;
    public string Name { get; } = name;
    public Func<T, string> Print => x => (bool)(object)x ? "true" : "false";
}