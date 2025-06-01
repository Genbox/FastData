using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class BoolTypeDef(string name) : ITypeDef<bool>
{
    public DataType DataType => DataType.Boolean;
    public string Name { get; } = name;
    public Func<bool, string> Print => x => x ? "true" : "false";
}