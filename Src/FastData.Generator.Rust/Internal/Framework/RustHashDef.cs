using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef : IHashDef
{
    public string GetHashSource(KeyType keyType, string typeName, HashInfo info) =>
        $$"""
              {{(keyType == KeyType.String ? "#[inline]" : "#[inline(always)]")}}
              {{(keyType == KeyType.String ? "unsafe " : "")}}fn get_hash(value: {{(keyType == KeyType.String ? "&" : "")}}{{typeName}}) -> u64 {
          {{GetHash(keyType, info)}}
              }
          """;

    private static string GetHash(KeyType keyType, HashInfo info)
    {
        if (keyType == KeyType.String)
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

        if (keyType.IsIdentityHash())
            return "        value as u64";

        if (keyType == KeyType.Single)
        {
            return info.HasZeroOrNaN
                ? """
                          let mut bits = value.to_bits();

                          if ((bits.wrapping_sub(1)) & !0x8000_0000) >= 0x7F80_0000 {
                              bits &= 0x7F80_0000;
                          }
                          bits as u64
                  """
                : "        value.to_bits() as u64";
        }

        if (keyType == KeyType.Double)
        {
            return info.HasZeroOrNaN
                ? """
                          let mut bits = value.to_bits();

                          if ((bits.wrapping_sub(1)) & !0x8000_0000_0000_0000) >= 0x7FF0_0000_0000_0000 {
                              bits &= 0x7FF0_0000_0000_0000;
                          }
                          bits
                  """
                : "        value.to_bits()";
        }

        throw new InvalidOperationException("Unsupported data type: " + keyType);
    }
}