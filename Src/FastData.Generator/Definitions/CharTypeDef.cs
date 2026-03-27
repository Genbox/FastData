using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class CharTypeDef : ITypeDef<char>
{
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

    public TypeCode KeyType => TypeCode.Char;
    public string Name { get; }
    public Func<TypeMap, object, string> PrintObj { get; }
    public Func<TypeMap, char, string> Print { get; }
}