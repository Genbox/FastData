using Genbox.FastData.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName) =>
        $$"""
              {{(dataType == DataType.String ? "#[inline]" : "#[inline(always)]")}}
              {{(dataType == DataType.String ? "unsafe " : "")}}fn get_hash(value: {{typeName}}) -> u32 {
          {{GetHash(dataType)}}
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
            return "        ((value as i32) ^ ((value >> 32) as i32)) as u32";

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