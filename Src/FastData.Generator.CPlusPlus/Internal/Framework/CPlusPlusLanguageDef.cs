#if NETSTANDARD2_0
using Genbox.FastData.Generator.Compat;
#endif

using System.Reflection;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusLanguageDef : ILanguageDef
{
    public GeneratorEncoding Encoding => GeneratorEncoding.UTF16;
    public string ArraySizeType => "size_t";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
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

        new ObjectTypeDef(PrintObjectDeclaration, PrintValues),

        //Support reduction from UTF16 to ASCII
        new DynamicStringTypeDef(
            new StringType(GeneratorEncoding.UTF16, "std::u32string_view", static x => $"U\"{x}\""),
            new StringType(GeneratorEncoding.ASCII, "std::string_view", static x => $"\"{x}\""))
    };

    private static string PrintValues(TypeMap map, ValueSpec spec)
    {
        //e(4286578687, -1, std::numeric_limits<float>::lowest())
        PropertyInfo[] properties = spec.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        StringBuilder sb = new StringBuilder();
        string typeName = spec.Type.Name;

        for (int i = 0; i < spec.Array.Length; i++)
        {
            sb.Append(typeName).Append('(');
            for (int j = 0; j < properties.Length; j++)
            {
                PropertyInfo prop = properties[j];
                object? val = prop.GetValue(spec.Array.GetValue(i));

                sb.Append(map.ToValueLabel(val, prop.PropertyType));
                if (j < properties.Length - 1)
                    sb.Append(", ");
            }
            sb.Append(')');

            if (i < spec.Array.Length - 1)
                sb.Append(", ");
        }

        return sb.ToString();
    }

    private static string PrintObjectDeclaration(ValueSpec spec, TypeMap typeMap)
    {
        PropertySpec[] fields = spec.GetFields(typeMap);

        return $$"""
                 struct {{spec.Type.Name}} {
                 {{RenderFields(fields)}}
                 {{RenderCtor(spec.Type.Name, fields)}}
                 };
                 """;
    }

    private static string RenderFields(PropertySpec[] fields)
    {
        // int Age;
        // std::string Name;
        StringBuilder sb = new StringBuilder();

        foreach (PropertySpec spec in fields)
            sb.AppendLine($"    const {spec.TypeName} {spec.Name};");

        return sb.ToString();
    }

    private static string RenderCtor(string name, PropertySpec[] fields)
    {
        // ValueStruct(const int32_t age, const std::string& name) : Age(age), Name(name) { }
        StringBuilder sb = new StringBuilder();
        sb.Append($"    {name}(");
        sb.AppendJoin(", ", fields.Select(x => x.TypeName + " " + x.Name.ToLowerInvariant()));
        sb.Append(") : ");
        sb.AppendJoin(", ", fields.Select(x => $"{x.Name}({x.Name.ToLowerInvariant()})"));
        sb.Append(" { }");

        return sb.ToString();
    }
}