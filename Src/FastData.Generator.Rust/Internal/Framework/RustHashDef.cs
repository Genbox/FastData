using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustHashDef(TypeMap map) : IHashDef, IHashExpressionDef
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

    public string RenderAdditionalData(AdditionalData[] info)
    {
        StringBuilder sb = new StringBuilder();

        foreach (AdditionalData state in info)
        {
            string typeName = map.GetTypeName(state.Type);
            string length = state.Values.Length.ToString(CultureInfo.InvariantCulture);
            string values = string.Join(", ", state.Values.Cast<object>().Select(x => map.ToValueLabel(x, state.Type)));

            sb.Append("    const ")
              .Append(state.Name)
              .Append(": [")
              .Append(typeName)
              .Append("; ")
              .Append(length)
              .Append("] = [")
              .Append(values)
              .Append("];")
              .Append('\n');
        }

        return sb.ToString();
    }

    public string RenderFunctions(ReaderFunctions functions)
    {
        StringBuilder sb = new StringBuilder();

        if (functions.HasFlag(ReaderFunctions.ReadU8))
        {
            sb.AppendLine("    #[inline(always)]");
            sb.AppendLine("    fn read_u8(value: &str, offset: usize) -> u8 {");
            sb.AppendLine("        unsafe { *value.as_bytes().get_unchecked(offset) }");
            sb.AppendLine("    }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU16))
        {
            sb.AppendLine("    #[inline(always)]");
            sb.AppendLine("    fn read_u16(value: &str, offset: usize) -> u16 {");
            sb.AppendLine("        unsafe { ptr::read_unaligned(value.as_bytes().as_ptr().add(offset) as *const u16) }");
            sb.AppendLine("    }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU32))
        {
            sb.AppendLine("    #[inline(always)]");
            sb.AppendLine("    fn read_u32(value: &str, offset: usize) -> u32 {");
            sb.AppendLine("        unsafe { ptr::read_unaligned(value.as_bytes().as_ptr().add(offset) as *const u32) }");
            sb.AppendLine("    }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU64))
        {
            sb.AppendLine("    #[inline(always)]");
            sb.AppendLine("    fn read_u64(value: &str, offset: usize) -> u64 {");
            sb.AppendLine("        unsafe { ptr::read_unaligned(value.as_bytes().as_ptr().add(offset) as *const u64) }");
            sb.AppendLine("    }");
        }

        return sb.ToString();
    }
}
