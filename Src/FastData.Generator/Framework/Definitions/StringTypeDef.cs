using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class StringTypeDef : ITypeDef<string>
{
    public StringTypeDef(string name, Func<string, string>? print = null)
    {
        Name = name;
        Print = print ?? (static x => $"\"{x}\"");
    }

    public DataType DataType => DataType.String;
    public string Name { get; }
    public Func<object, string> PrintObj => x => $"\"{x}\"";
    public Func<string, string> Print { get; }
}