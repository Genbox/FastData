using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName, HashInfo info) =>
        $$"""
              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static ulong Hash({{typeName}} value)
              {
          {{GetHash(dataType, info)}}
              }
          """;

    private static string GetHash(DataType dataType, HashInfo info)
    {
        if (dataType == DataType.String)
        {
            return """
                           ulong hash = 352654597;

                           ref char ptr = ref MemoryMarshal.GetReference(value.AsSpan());
                           int len = value.Length;

                           while (len-- > 0)
                           {
                               hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
                               ptr = ref Unsafe.Add(ref ptr, 1);
                           }

                           return 352654597 + (hash * 1566083941);
                   """;
        }

        if (dataType.IsIdentityHash())
            return "        return (ulong)value;";

        if (dataType == DataType.Single)
        {
            return info.HasZeroOrNaN
                ? """
                          uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

                          if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
                              bits &= 0x7FF0_0000;

                          return (ulong)bits;
                  """
                : "        return (ulong)Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));";
        }

        if (dataType == DataType.Double)
        {
            return info.HasZeroOrNaN
                ? """
                          ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

                          if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
                              bits &= 0x7FF0_0000_0000_0000;

                          return bits;
                  """
                : "        return Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));";
        }

        return "        return (ulong)value.GetHashCode();";
    }
}