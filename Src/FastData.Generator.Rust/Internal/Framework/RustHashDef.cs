using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName) =>
        $$"""
              {{(dataType == DataType.String ? "#[inline]" : "#[inline(always)]")}}
              {{(dataType == DataType.String ? "unsafe " : "")}}fn get_hash(value: {{typeName}}) -> u64 {
          {{GetHash(dataType)}}
              }
          """;

    private static string GetHash(DataType dataType)
    {
        if (dataType == DataType.String)
        {
            return """
                           let mut hash: u64 = 352654597;

                           let mut ptr = value.as_ptr();
                           let mut len = value.len();

                            while len > 0 {
                                   hash = (((hash << 5) | (hash >> 27)).wrapping_add(hash)) ^ (ptr.read() as u64);
                                   ptr = ptr.add(1);
                                   len -= 1;
                            }

                           hash.wrapping_mul(1566083941).wrapping_add(352654597)
                   """;
        }

        if (dataType.IsIdentityHash() || dataType == DataType.Boolean)
            return "        value as u64";

        if (dataType == DataType.Single)
        {
            return """
                           let mut bits = value.to_bits();

                           if ((bits.wrapping_sub(1)) & !0x8000_0000) >= 0x7F80_0000 {
                               bits &= 0x7F80_0000;
                           }
                           bits as u64
                   """;
        }

        if (dataType == DataType.Double)
        {
            return """
                           let mut bits = value.to_bits();

                           if ((bits.wrapping_sub(1)) & !0x8000_0000_0000_0000) >= 0x7FF0_0000_0000_0000 {
                               bits &= 0x7FF0_0000_0000_0000;
                           }
                           bits
                   """;
        }

        throw new InvalidOperationException("Unsupported data type: " + dataType);
    }
}