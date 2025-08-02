#if NETSTANDARD2_0
using Genbox.FastData.Generator.Compat;
#endif
using System.Collections.ObjectModel;
using System.Reflection;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustLanguageDef : ILanguageDef
{
    public GeneratorEncoding Encoding => GeneratorEncoding.UTF8;
    public string ArraySizeType => "usize";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new NullTypeDef("None"),
        new IntegerTypeDef<char>("char", char.MinValue, char.MaxValue, "char::MIN", "char::MAX", static x => $"'{x.ToString(NumberFormatInfo.InvariantInfo)}'"),
        new IntegerTypeDef<sbyte>("i8", sbyte.MinValue, sbyte.MaxValue, "i8::MIN", "i8::MAX"),
        new IntegerTypeDef<byte>("u8", byte.MinValue, byte.MaxValue, "u8::MIN", "u8::MAX"),
        new IntegerTypeDef<short>("i16", short.MinValue, short.MaxValue, "i16::MIN", "i16::MAX"),
        new IntegerTypeDef<ushort>("u16", ushort.MinValue, ushort.MaxValue, "u16::MIN", "u16::MAX"),
        new IntegerTypeDef<int>("i32", int.MinValue, int.MaxValue, "i32::MIN", "i32::MAX"),
        new IntegerTypeDef<uint>("u32", uint.MinValue, uint.MaxValue, "u32::MIN", "u32::MAX"),
        new IntegerTypeDef<long>("i64", long.MinValue, long.MaxValue, "i64::MIN", "i64::MAX"),
        new IntegerTypeDef<ulong>("u64", ulong.MinValue, ulong.MaxValue, "u64::MIN", "u64::MAX"),
        new IntegerTypeDef<float>("f32", float.MinValue, float.MaxValue, "f32::MIN", "f32::MAX", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new IntegerTypeDef<double>("f64", double.MinValue, double.MaxValue, "f64::MIN", "f64::MAX", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeDef("str"),
        new ObjectTypeDef(PrintDeclaration, PrintValue),
    };

    private static string PrintDeclaration(TypeMap map, Type type)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        string name = type.Name;
        return $$"""
                 pub struct {{name}} {
                 {{RenderFields(map, properties, true)}}
                 }

                 impl {{name}} {
                 {{RenderCtor(map, properties)}}
                 }
                 """;
    }

    private static string PrintValue(TypeMap map, object? value)
    {
        if (value == null)
            return "None";

        Type type = value.GetType();

        if (type.IsPrimitive || type == typeof(string))
            return map.Get(type).PrintObj(map, value);

        if (type.IsArray)
            throw new NotSupportedException("Arrays are not yet supported as a type");

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        string args = string.Join(", ", props.Select(p =>
        {
            object? val = p.GetValue(value);

            if (val == null)
                return "None";

            string inner = PrintValue(map, val);
            return IsPropertyNullable(p) ? $"Some({inner})" : inner;
        }));

        return $"&{type.Name}::new({args})";
    }

    private static string RenderType(TypeMap map, Type type, bool staticLife = false, bool nullable = false)
    {
        if (type.IsArray)
            throw new NotSupportedException("Arrays are not yet supported as a type");

        TypeCode typeCode = Type.GetTypeCode(type);

        string staticStr = staticLife ? "'static " : "";

        if (typeCode is TypeCode.Object)
        {
            if (nullable)
                return $"Option<&{staticStr + type.Name}>";

            return $"&{staticStr + type.Name}";
        }

        if (typeCode is TypeCode.String)
            return "&" + staticStr + map.GetTypeName(type);

        return map.GetTypeName(type);
    }

    private static bool IsPropertyNullable(PropertyInfo p)
    {
        if (!p.PropertyType.IsValueType && p.PropertyType != typeof(string))
            return true;

        CustomAttributeData? nullableAttr = p.CustomAttributes
                                             .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

        if (nullableAttr != null && nullableAttr.ConstructorArguments.Count > 0)
        {
            CustomAttributeTypedArgument arg = nullableAttr.ConstructorArguments[0];
            if (arg.ArgumentType == typeof(byte))
                return (byte)arg.Value == 2;
            if (arg.ArgumentType == typeof(byte[]))
                return ((ReadOnlyCollection<CustomAttributeTypedArgument>)arg.Value)[0].Value is byte b && b == 2;
        }

        return false;
    }

    private static string RenderFields(TypeMap map, PropertyInfo[] properties, bool staticLife = false)
    {
        StringBuilder sb = new StringBuilder();
        foreach (PropertyInfo prop in properties)
        {
            sb.AppendLine($"    pub {prop.Name.ToLowerInvariant()}: {RenderType(map, prop.PropertyType, staticLife, IsPropertyNullable(prop))},");
        }
        return sb.ToString();
    }

    private static string RenderCtor(TypeMap map, PropertyInfo[] properties)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("    pub const fn new(");
        sb.AppendJoin(", ", properties.Select(p =>
        {
            bool opt = IsPropertyNullable(p);
            return $"{p.Name.ToLowerInvariant()}: {RenderType(map, p.PropertyType, true, opt)}";
        }));
        sb.Append(") -> Self { Self { ");
        sb.AppendJoin(", ", properties.Select(p => p.Name.ToLowerInvariant()));
        sb.Append(" } }");
        return sb.ToString();
    }
}