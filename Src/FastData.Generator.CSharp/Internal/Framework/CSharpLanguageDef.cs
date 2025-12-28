using System.Reflection;
#if NETSTANDARD2_0
using Genbox.FastData.Generator.Compat;
#endif
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpLanguageDef : ILanguageDef
{
    public string ArraySizeType => "uint";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new NullTypeDef("null"),

        new IntegerTypeDef<char>("char", char.MinValue, char.MaxValue, "char.MinValue", "char.MaxValue", x => $"'{x.ToString(NumberFormatInfo.InvariantInfo)}'"),
        new IntegerTypeDef<sbyte>("sbyte", sbyte.MinValue, sbyte.MaxValue, "sbyte.MinValue", "sbyte.MaxValue"),
        new IntegerTypeDef<byte>("byte", byte.MinValue, byte.MaxValue, "byte.MinValue", "byte.MaxValue"),
        new IntegerTypeDef<short>("short", short.MinValue, short.MaxValue, "short.MinValue", "short.MaxValue"),
        new IntegerTypeDef<ushort>("ushort", ushort.MinValue, ushort.MaxValue, "ushort.MinValue", "ushort.MaxValue"),
        new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
        new IntegerTypeDef<uint>("uint", uint.MinValue, uint.MaxValue, "uint.MinValue", "uint.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeDef<long>("long", long.MinValue, long.MaxValue, "long.MinValue", "long.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "l"),
        new IntegerTypeDef<ulong>("ulong", ulong.MinValue, ulong.MaxValue, "ulong.MinValue", "ulong.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "ul"),
        new IntegerTypeDef<float>("float", float.MinValue, float.MaxValue, "float.MinValue", "float.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeDef<double>("double", double.MinValue, double.MaxValue, "double.MinValue", "double.MaxValue", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeDef("string"),

        new ObjectTypeDef(PrintDeclaration, PrintValue),
    };

    private static string PrintDeclaration(TypeMap map, Type type)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        string name = type.Name;
        return $$"""
                 public class {{name}}
                 {
                 {{RenderCtor(map, name, properties)}}
                 {{RenderProperties(map, properties)}}
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
            return $"{RenderType(map, type.GetElementType())}[]";

        if (Type.GetTypeCode(type) == TypeCode.Object)
            return type.Name;

        return map.GetTypeName(type);
    }

    private static string RenderProperties(TypeMap map, PropertyInfo[] properties)
    {
        // int Age { get; set; }
        // string Name { get; set; }
        StringBuilder sb = new StringBuilder();

        foreach (PropertyInfo property in properties)
            sb.AppendLine($"    {RenderType(map, property.PropertyType)} {property.Name} {{ get; set; }}");

        return sb.ToString();
    }

    private static string RenderCtor(TypeMap map, string name, PropertyInfo[] properties)
    {
        // ValueStruct(int age, string name)
        // {
        //     Age = age;
        //     Name = name;
        // }

        StringBuilder sb = new StringBuilder();
        sb.Append($"    public {name}(").AppendJoin(", ", properties.Select(x => $"{RenderType(map, x.PropertyType)} {x.Name.ToLowerInvariant()}")).AppendLine(")");
        sb.AppendLine("    {");
        sb.AppendJoin("\n", properties.Select(x => $"        {x.Name} = {x.Name.ToLowerInvariant()};")).AppendLine();
        sb.Append("    }");
        return sb.ToString();
    }
}