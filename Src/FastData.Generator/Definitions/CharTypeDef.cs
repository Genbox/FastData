using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes the target-language representation of character values.</summary>
public class CharTypeDef : ITypeDef<char>
{
    /// <summary>Initializes a new instance of the <see cref="CharTypeDef"/> class.</summary>
    /// <param name="name">The target-language character type name.</param>
    /// <param name="print">An optional function that formats character literals.</param>
    public CharTypeDef(string name, Func<char, string>? print = null)
    {
        Name = name;

        if (print != null)
        {
            Print = (_, x) => print(x);
            PrintObj = (_, x) => print((char)x);
        }
        else
        {
            Print = static (_, x) => $"'{x}'";
            PrintObj = static (_, x) => $"'{x}'";
        }
    }

    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.Char;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj { get; }

    /// <inheritdoc />
    public Func<TypeMap, char, string> Print { get; }
}