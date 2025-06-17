namespace Genbox.FastData.Generators.StringHash.Framework;

public class State(string name, Type type, Array values)
{
    public string Name { get; } = name;
    public Type Type { get; } = type;
    public Array Values { get; } = values;
}