#if NETSTANDARD2_0
using Genbox.FastData.Generator.Compat;
#endif

using System.Reflection;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusLanguageDef : ILanguageDef
{
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

        new ObjectTypeDef(PrintDeclaration, PrintValue),

        //Support reduction from UTF16 to ASCII
        new DynamicStringTypeDef(
            new StringType(GeneratorEncoding.UTF32, "std::u32string_view", static x => $"U\"{x}\""),
            new StringType(GeneratorEncoding.UTF16, "std::u16string_view", static x => $"u\"{x}\""),
            new StringType(GeneratorEncoding.UTF8, "std::string_view", static x => $"u8\"{x}\""),
            new StringType(GeneratorEncoding.ASCII, "std::string_view", static x => $"\"{x}\""))
    };

    private static string PrintDeclaration(TypeMap map, Type type)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        string name = type.Name;
        return $$"""
                 struct {{name}} {
                 {{RenderFields(map, properties)}}
                 {{RenderCtor(map, name, properties)}}
                 };
                 """;
    }

    private static string PrintValue(TypeMap map, object? value)
    {
        if (value == null)
            return map.GetNull();

        Type type = value.GetType();

        if (type.IsPrimitive || type == typeof(string))
            return map.Get(type).PrintObj(map, value);

        if (type.IsArray)
            return $"{{ {string.Join(", ", ((Array)value).Cast<object>().Select(x => PrintValue(map, x)))} }}";

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return $"new {type.Name}({string.Join(", ", props.Select(p => $"{PrintValue(map, p.GetValue(value))}"))})";
    }

    private static string RenderType(TypeMap map, Type type)
    {
        if (type.IsArray)
            return $"std::vector<{RenderType(map, type.GetElementType())}>";

        if (Type.GetTypeCode(type) == TypeCode.Object)
            return type.Name + "*";

        return map.GetTypeName(type);
    }

    private static string RenderFields(TypeMap map, PropertyInfo[] properties)
    {
        // int Age;
        // std::string Name;
        StringBuilder sb = new StringBuilder();

        foreach (PropertyInfo property in properties)
        {
            if (Type.GetTypeCode(property.PropertyType) == TypeCode.Object)
                sb.AppendLine($"    const {RenderType(map, property.PropertyType)} {property.Name.ToLowerInvariant()};");
            else
                sb.AppendLine($"    {RenderType(map, property.PropertyType)} {property.Name.ToLowerInvariant()};");
        }

        return sb.ToString();
    }

    private static string RenderCtor(TypeMap map, string name, PropertyInfo[] properties)
    {
        // ValueStruct(const int32_t age, const std::string& name) : Age(age), Name(name) { }
        StringBuilder sb = new StringBuilder();
        sb.Append($"    constexpr {name}(");
        sb.AppendJoin(", ", properties.Select(x => $"const {RenderType(map, x.PropertyType)} {x.Name.ToLowerInvariant()}"));
        sb.Append(") noexcept : ");
        sb.AppendJoin(", ", properties.Select(x => $"{x.Name.ToLowerInvariant()}({x.Name.ToLowerInvariant()})"));
        sb.Append(" { }");
        return sb.ToString();
    }
}