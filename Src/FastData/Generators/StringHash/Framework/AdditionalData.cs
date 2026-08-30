namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Describes an external array captured by a generated hash expression.</summary>
/// <param name="name">The captured member name.</param>
/// <param name="type">The array element type.</param>
/// <param name="values">The captured values.</param>
public class AdditionalData(string name, Type type, Array values)
{
    /// <summary>Gets the captured member name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the array element type.</summary>
    public Type Type { get; } = type;

    /// <summary>Gets the captured values.</summary>
    public Array Values { get; } = values;
}