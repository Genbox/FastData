using Genbox.FastData.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName, bool use64Bit)
    {
        string type = use64Bit ? "ulong" : "uint";

        return $$"""
                     [MethodImpl(MethodImplOptions.AggressiveInlining)]
                     private static {{type}} Hash({{typeName}} value)
                     {
                 {{GetHash(dataType, use64Bit, type)}}
                     }
                 """;
    }

    private static string GetHash(DataType dataType, bool use64Bit, string type)
    {
        if (dataType == DataType.String)
        {
            return $$"""
                              {{type}} hash = 352654597;

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
            return $"        return ({type})value;";

        if (dataType is DataType.Int64 or DataType.UInt64)
        {
            if (use64Bit)
                return $"        return ({type})value;";

            return $"        return ({type})value.GetHashCode();";
        }

        if (dataType == DataType.Single)
        {
            return $"""
                            uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

                            if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
                                bits &= 0x7FF0_0000;

                            return ({type})bits;
                    """;
        }

        if (dataType == DataType.Double)
        {
            return $"""
                            ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

                            if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
                                bits &= 0x7FF0_0000_0000_0000;

                            return {(use64Bit ? "bits" : "(uint)bits ^ (uint)(bits >> 32)")};
                    """;
        }

        return $"        return ({type})value.GetHashCode();";
    }
}