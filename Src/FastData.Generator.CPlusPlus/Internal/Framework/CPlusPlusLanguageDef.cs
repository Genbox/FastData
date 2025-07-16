#if NETSTANDARD2_0
using Genbox.FastData.Generator.Compat;
#endif

using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Models;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusLanguageDef : ILanguageDef
{
    public GeneratorEncoding Encoding => GeneratorEncoding.UTF16;
    public string ArraySizeType => "size_t";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new NullTypeDef("nullptr"),

        new IntegerTypeDef<char>("char", char.MinValue, (char)127, "0", "127", x => ((byte)x).ToString(NumberFormatInfo.InvariantInfo)),
        new IntegerTypeDef<sbyte>("int8_t", sbyte.MinValue, sbyte.MaxValue, "std::numeric_limits<int8_t>::lowest()", "std::numeric_limits<int8_t>::max()"),
        new IntegerTypeDef<byte>("uint8_t", byte.MinValue, byte.MaxValue, "0", "std::numeric_limits<uint8_t>::max()"),
        new IntegerTypeDef<short>("int16_t", short.MinValue, short.MaxValue, "std::numeric_limits<int16_t>::lowest()", "std::numeric_limits<int16_t>::max()"),
        new IntegerTypeDef<ushort>("uint16_t", ushort.MinValue, ushort.MaxValue, "0", "std::numeric_limits<uint16_t>::max()"),
        new IntegerTypeDef<int>("int32_t", int.MinValue, int.MaxValue, "std::numeric_limits<int32_t>::lowest()", "std::numeric_limits<int32_t>::max()"),
        new IntegerTypeDef<uint>("uint32_t", uint.MinValue, uint.MaxValue, "0", "std::numeric_limits<uint32_t>::max()", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeDef<long>("int64_t", long.MinValue, long.MaxValue, "std::numeric_limits<int64_t>::lowest()", "std::numeric_limits<int64_t>::max()", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "ll"),
        new IntegerTypeDef<ulong>("uint64_t", ulong.MinValue, ulong.MaxValue, "0", "std::numeric_limits<uint64_t>::max()", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "ull"),
        new IntegerTypeDef<float>("float", float.MinValue, float.MaxValue, "std::numeric_limits<float>::lowest()", "std::numeric_limits<float>::max()", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeDef<double>("double", double.MinValue, double.MaxValue, "std::numeric_limits<double>::lowest()", "std::numeric_limits<double>::max()", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),

        new ObjectTypeDef(PrintObjectDeclarations, PrintValues),

        //Support reduction from UTF16 to ASCII
        new DynamicStringTypeDef(
            new StringType(GeneratorEncoding.UTF16, "std::u32string_view", static x => $"U\"{x}\""),
            new StringType(GeneratorEncoding.ASCII, "std::string_view", static x => $"\"{x}\""))
    };

    private static string PrintObjectDeclarations(TypeModel type)
    {
        return $$"""
                 struct {{type.Name}} {
                 {{RenderFields(type.Properties)}}
                 {{RenderCtor(type.Name, type.Properties)}}
                 };
                 """;
    }

    private static string PrintValues(ITypeReference elemType, IEnumerable<IValueModel> values)
    {
        string RenderVal(IValueModel v) => v switch
        {
            PrimitiveValue pv => pv.Value.ToString()!,
            ArrayValue av => "{ " + string.Join(", ", av.Items.Select(RenderVal)) + " }",
            ObjectValue ov => "new " + ov.TypeName + "(" + string.Join(", ", ov.Properties.Values.Select(RenderVal)) + ")",
            _ => throw new NotSupportedException("")
        };

        StringBuilder sb = new StringBuilder();
        foreach (IValueModel v in values)
            sb.AppendLine($"    {RenderVal(v)},");
        return sb.ToString();
    }

    private static string RenderType(ITypeReference tr) => tr switch
    {
        PrimitiveType p => p.Name,
        CustomType c => c.Name + "*",
        ArrayType a => $"std::vector<{RenderType(a.ElementType)}>",
        _ => throw new NotSupportedException("")
    };

    private static string RenderFields(IReadOnlyList<PropertyModel> properties)
    {
        // int Age;
        // std::string Name;
        StringBuilder sb = new StringBuilder();

        foreach (PropertyModel property in properties)
            sb.AppendLine($"    const {RenderType(property.Type)} {property.Name};");

        return sb.ToString();
    }

    private static string RenderCtor(string name, IReadOnlyList<PropertyModel> properties)
    {
        // ValueStruct(const int32_t age, const std::string& name) : Age(age), Name(name) { }
        StringBuilder sb = new StringBuilder();
        sb.Append($"    {name}(");
        sb.AppendJoin(", ", properties.Select(x => $"const {RenderType(x.Type)} {x.Name.ToLowerInvariant()}"));
        sb.Append(") : ");
        sb.AppendJoin(", ", properties.Select(x => $"{x.Name}({x.Name.ToLowerInvariant()})"));
        sb.Append(" { }");

        return sb.ToString();
    }
}