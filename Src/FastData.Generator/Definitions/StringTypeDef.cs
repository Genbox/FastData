using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes the target-language representation of string values.</summary>
public class StringTypeDef : ITypeDef<string>
{
    /// <summary>Initializes a new instance of the <see cref="StringTypeDef"/> class.</summary>
    /// <param name="name">The target-language string type name.</param>
    /// <param name="print">The function used to format string literals.</param>
    public StringTypeDef(string name, Func<string, string> print)
    {
        Name = name;

        Print = (_, x) => print(x);
        PrintObj = (_, x) => print(x.ToString() ?? string.Empty);
    }

    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.String;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj { get; }

    /// <inheritdoc />
    public Func<TypeMap, string, string> Print { get; }
}