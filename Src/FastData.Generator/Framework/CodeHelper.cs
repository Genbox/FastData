using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public class CodeHelper(ILanguageSpec spec, TypeMap typeMap)
{
    public virtual void Comment(StringBuilder sb, string value) => sb.Append(spec.CommentChar).Append(' ').AppendLine(value);
    public virtual void Assign(StringBuilder sb, string left, string right) => sb.Append(left).Append(spec.AssignmentChar).Append(right);

    public virtual string ToValueLabel<T>(T? value)
    {
        ITypeSpec<T>? s = typeMap.Get<T>();

        if (s == null)
            return value.ToString();

        return value.ToString();
    }

    public virtual string ToValueLabel(object? value, DataType dataType)
    {
        ITypeSpec? s = typeMap.Get(dataType);

        if (s == null)
            return value.ToString();

        return value.ToString();
    }

    public string GetSmallestUIntType(ulong value) => value switch
    {
        <= byte.MaxValue => typeMap.GetRequired<byte>().Name,
        <= ushort.MaxValue => typeMap.GetRequired<ushort>().Name,
        <= uint.MaxValue => typeMap.GetRequired<uint>().Name,
        _ => typeMap.GetRequired<ulong>().Name
    };

    public string GetSmallestIntType(long value) => value switch
    {
        <= sbyte.MaxValue => typeMap.GetRequired<sbyte>().Name,
        <= short.MaxValue => typeMap.GetRequired<short>().Name,
        <= int.MaxValue => typeMap.GetRequired<int>().Name,
        _ => typeMap.GetRequired<long>().Name
    };
}