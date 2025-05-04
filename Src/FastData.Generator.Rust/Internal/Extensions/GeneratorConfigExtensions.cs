using Genbox.FastData.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config, bool asStatic = false) => config.DataType switch
    {
        DataType.String when asStatic => "&'static str",
        DataType.String => "&str",
        DataType.Boolean => "bool",
        DataType.SByte => "i8",
        DataType.Byte => "u8",
        DataType.Char => "char",
        DataType.Int16 => "i16",
        DataType.UInt16 => "u16",
        DataType.Int32 => "i32",
        DataType.UInt32 => "u32",
        DataType.Int64 => "i64",
        DataType.UInt64 => "u64",
        DataType.Single => "f32",
        DataType.Double => "f64",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetHashSource(this GeneratorConfig config) =>
        $$"""
              {{(config.DataType == DataType.String ? "#[inline]" : "#[inline(always)]")}}
              {{(config.DataType == DataType.String ? "unsafe " : "")}}fn get_hash(value: {{config.GetTypeName()}}) -> u32 {
          {{GetHash(config.DataType)}}
              }
          """;

    private static string GetHash(DataType type)
    {
        if (type == DataType.String)
        {
            return """
                           let mut hash: u32 = 352654597;

                           let mut ptr = value.as_ptr();
                           let mut len = value.len();

                            while len > 0 {
                                   hash = hash.rotate_left(5).wrapping_add(hash) ^ (ptr.read() as u32);
                                   ptr = ptr.add(1);
                                   len -= 1;
                            }

                           hash.wrapping_mul(1566083941).wrapping_add(352654597)
                   """;
        }

        if (type.IsIdentityHash() || type == DataType.Boolean)
            return "        value as u32";

        if (type is DataType.Int64 or DataType.UInt64)
            return "        return ((value as i32) ^ ((value >> 32) as i32)) as u32;";

        if (type == DataType.Single)
        {
            return """
                           let mut bits = value.to_bits();

                           if ((bits.wrapping_sub(1)) & !0x8000_0000) >= 0x7F80_0000 {
                               bits &= 0x7F80_0000;
                           }
                           bits
                   """;
        }

        if (type == DataType.Double)
        {
            return """
                           let mut bits = value.to_bits();

                           if ((bits.wrapping_sub(1)) & !0x8000_0000_0000_0000) >= 0x7FF0_0000_0000_0000 {
                               bits &= 0x7FF0_0000_0000_0000;
                           }
                           (bits as u32) ^ ((bits >> 32) as u32)
                   """;
        }

        throw new InvalidOperationException("Unsupported data type: " + type);
    }
}