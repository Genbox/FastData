using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class StringTypeDef : ITypeDef<string>
{
    public StringTypeDef(string name, Func<string, string>? print = null)
    {
        Name = name;

        if (print != null)
        {
            Print = (_, x) => print(x);
            PrintObj = (_, x) => print(x.ToString());
        }
        else
        {
            Print = static (_, x) => $"\"{x}\"";
            PrintObj = static (_, x) => $"\"{x}\"";
        }
    }

    public KeyType KeyType => KeyType.String;
    public string Name { get; }
    public Func<TypeMap, object, string> PrintObj { get; }
    public Func<TypeMap, string, string> Print { get; }
}