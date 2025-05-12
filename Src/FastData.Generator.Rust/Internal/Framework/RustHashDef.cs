using Genbox.FastData.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName, bool use64Bit)
    {
        string type = use64Bit ? "u64" : "u32";

        return $$"""
                     {{(dataType == DataType.String ? "#[inline]" : "#[inline(always)]")}}
                     {{(dataType == DataType.String ? "unsafe " : "")}}fn get_hash(value: {{typeName}}) -> {{type}} {
                 {{GetHash(dataType, use64Bit, type)}}
                     }
                 """;
    }

    private static string GetHash(DataType dataType, bool use64Bit, string type)
    {
        if (dataType == DataType.String)
        {
            return $$"""
                             let mut hash: {{type}} = 352654597;

                             let mut ptr = value.as_ptr();
                             let mut len = value.len();

                              while len > 0 {
                                     hash = (((hash << 5) | (hash >> 27)).wrapping_add(hash)) ^ (ptr.read() as {{type}});
                                     ptr = ptr.add(1);
                                     len -= 1;
                              }

                             hash.wrapping_mul(1566083941).wrapping_add(352654597)
                     """;
        }

        if (dataType.IsIdentityHash() || dataType == DataType.Boolean)
            return $"        value as {type}";

        if (dataType is DataType.Int64 or DataType.UInt64)
            return $"        {(use64Bit ? "value" : "((value as u32) ^ ((value >> 32) as u32))")} as {type}";

        if (dataType == DataType.Single)
        {
            return $$"""
                             let mut bits = value.to_bits();

                             if ((bits.wrapping_sub(1)) & !0x8000_0000) >= 0x7F80_0000 {
                                 bits &= 0x7F80_0000;
                             }
                             bits as {{type}}
                     """;
        }

        if (dataType == DataType.Double)
        {
            return $$"""
                             let mut bits = value.to_bits();

                             if ((bits.wrapping_sub(1)) & !0x8000_0000_0000_0000) >= 0x7FF0_0000_0000_0000 {
                                 bits &= 0x7FF0_0000_0000_0000;
                             }
                             {{(use64Bit ? "bits" : "(bits as u32) ^ ((bits >> 32) as u32)")}}
                     """;
        }

        throw new InvalidOperationException("Unsupported data type: " + dataType);
    }
}