using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class StringTypeDef : ITypeDef<string>
{
    public StringTypeDef(string name, Func<string, string> print)
    {
        Name = name;

        Print = (_, x) => print(x);
        PrintObj = (_, x) => print(x.ToString() ?? string.Empty);
    }

    public TypeCode KeyType => TypeCode.String;
    public string Name { get; }
    public Func<TypeMap, object, string> PrintObj { get; }
    public Func<TypeMap, string, string> Print { get; }
}