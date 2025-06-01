using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustLanguageDef : ILanguageDef
{
    public bool UseUTF16Encoding => false;
    public string ArraySizeType => "usize";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new IntegerTypeDef<char>("char", char.MinValue, char.MaxValue, "char::MIN", "char::MAX", x => $"'{x.ToString(NumberFormatInfo.InvariantInfo)}'"),
        new IntegerTypeDef<sbyte>("i8", sbyte.MinValue, sbyte.MaxValue, "i8::MIN", "i8::MAX"),
        new IntegerTypeDef<byte>("u8", byte.MinValue, byte.MaxValue, "u8::MIN", "u8::MAX"),
        new IntegerTypeDef<short>("i16", short.MinValue, short.MaxValue, "i16::MIN", "i16::MAX"),
        new IntegerTypeDef<ushort>("u16", ushort.MinValue, ushort.MaxValue, "u16::MIN", "u16::MAX"),
        new IntegerTypeDef<int>("i32", int.MinValue, int.MaxValue, "i32::MIN", "i32::MAX"),
        new IntegerTypeDef<uint>("u32", uint.MinValue, uint.MaxValue, "u32::MIN", "u32::MAX"),
        new IntegerTypeDef<long>("i64", long.MinValue, long.MaxValue, "i64::MIN", "i64::MAX"),
        new IntegerTypeDef<ulong>("u64", ulong.MinValue, ulong.MaxValue, "u64::MIN", "u64::MAX"),
        new IntegerTypeDef<float>("f32", float.MinValue, float.MaxValue, "f32::MIN", "f32::MAX", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new IntegerTypeDef<double>("f64", double.MinValue, double.MaxValue, "f64::MIN", "f64::MAX", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeDef("&str"),
        new BoolTypeDef("bool")
    };
}