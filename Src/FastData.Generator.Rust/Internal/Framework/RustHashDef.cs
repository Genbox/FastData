using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef : IHashDef
{
    public string GetStringHashSource(string typeName) =>
        """
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

    public string GetNumericHashSource(TypeCode keyType, string typeName, bool hasZeroOrNaN) => GetNumericHash(keyType, hasZeroOrNaN);

    private static string GetNumericHash(TypeCode typeCode, bool hasZeroOrNaN)
    {
        if (typeCode.UsesIdentityHash())
            return "    value as u64";

        if (typeCode == TypeCode.Single)
        {
            return hasZeroOrNaN
                ? """
                      let mut bits = value.to_bits();

                      if ((bits.wrapping_sub(1)) & !0x8000_0000) >= 0x7F80_0000 {
                          bits &= 0x7F80_0000;
                      }
                      bits as u64
                  """
                : "    value.to_bits() as u64";
        }

        if (typeCode == TypeCode.Double)
        {
            return hasZeroOrNaN
                ? """
                      let mut bits = value.to_bits();

                      if ((bits.wrapping_sub(1)) & !0x8000_0000_0000_0000) >= 0x7FF0_0000_0000_0000 {
                          bits &= 0x7FF0_0000_0000_0000;
                      }
                      bits
                  """
                : "    value.to_bits()";
        }

        throw new InvalidOperationException("Unsupported data type: " + typeCode);
    }

    public string Wrap(TypeCode keyType, string typeName, string hash)
    {
        bool isString = keyType == TypeCode.String;

        return $$"""
                 {{(isString ? "#[inline]" : "#[inline(always)]")}}
                 {{(isString ? "unsafe " : "")}}fn get_hash(value: {{(isString ? "&" : "")}}{{typeName}}) -> u64 {
                 {{hash}}
                 }
                 """;
    }
}