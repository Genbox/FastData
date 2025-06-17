namespace Genbox.FastData.Generator.Framework;

public sealed class StateInfo(string name, string typeName, string[] values)
{
    public string Name { get; } = name;
    public string TypeName { get; } = typeName;
    public string[] Values { get; } = values;
}