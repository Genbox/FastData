using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public class CodeHelper(ILanguageSpec spec, TypeMap typeMap)
{
    public virtual void Comment(StringBuilder sb, string value) => sb.Append(spec.CommentChar).Append(' ').AppendLine(value);
    public virtual void Assign(StringBuilder sb, string left, string right) => sb.Append(left).Append(spec.AssignmentChar).Append(right);

    public string ToValueLabel<T>(T? value)
    {
        ITypeSpec<T> s = typeMap.Get<T>();
        return s.Print(value);
    }

    public string GetSmallestUIntType(ulong value) => value switch
    {
        <= byte.MaxValue => typeMap.Get<byte>().Name,
        <= ushort.MaxValue => typeMap.Get<ushort>().Name,
        <= uint.MaxValue => typeMap.Get<uint>().Name,
        _ => typeMap.Get<ulong>().Name
    };

    public string GetSmallestIntType(long value) => value switch
    {
        <= sbyte.MaxValue => typeMap.Get<sbyte>().Name,
        <= short.MaxValue => typeMap.Get<short>().Name,
        <= int.MaxValue => typeMap.Get<int>().Name,
        _ => typeMap.Get<long>().Name
    };
}