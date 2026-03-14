using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

[SuppressMessage("Roslynator", "RCS1197:Optimize StringBuilder.Append/AppendLine call")]
internal class CSharpHashDef : IHashDef, IHashExpressionDef
{
    public string GetStringHashSource(string typeName) =>
        """
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

    public string GetNumericHashSource(TypeCode keyType, string typeName, bool hasZeroOrNaN)
    {
        if (keyType.UsesIdentityHash())
            return "    return (ulong)value;";

        if (keyType == TypeCode.Single)
        {
            return hasZeroOrNaN
                ? """
                      uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

                      if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
                          bits &= 0x7FF0_0000;

                      return (ulong)bits;
                  """
                : "    return (ulong)Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));";
        }

        if (keyType == TypeCode.Double)
        {
            return hasZeroOrNaN
                ? """
                      ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

                      if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
                          bits &= 0x7FF0_0000_0000_0000;

                      return bits;
                  """
                : "    return Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));";
        }

        return "    return (ulong)value.GetHashCode();";
    }

    public string Wrap(TypeCode typeCode, string typeName, string hash) =>
        $$"""
          [MethodImpl(MethodImplOptions.AggressiveInlining)]
          private static ulong Hash({{typeName}} value)
          {
          {{hash}}
          }
          """;

    public string RenderAdditionalData(AdditionalData[] info)
    {
        StringBuilder sb = new StringBuilder();

        foreach (AdditionalData state in info)
        {
            sb.Append("    private static ")
              .Append(state.Type.Name)
              .Append("[] ")
              .Append(state.Name)
              .Append(" = new ")
              .Append(state.Type.Name)
              .Append("[] { ")
              .Append(string.Join(", ", state.Values))
              .Append(" };\n");
        }

        return sb.ToString();
    }

    public string RenderFunctions(ReaderFunctions functions)
    {
        StringBuilder sb = new StringBuilder();

        if (functions.HasFlag(ReaderFunctions.ReadU8))
            sb.AppendLine("    static byte ReadU8(string ptr, int offset) => (byte)Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);");
        if (functions.HasFlag(ReaderFunctions.ReadU16))
            sb.AppendLine("    static ushort ReadU16(string ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");
        if (functions.HasFlag(ReaderFunctions.ReadU32))
            sb.AppendLine("    static uint ReadU32(string ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");
        if (functions.HasFlag(ReaderFunctions.ReadU64))
            sb.AppendLine("    static ulong ReadU64(string ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");

        return sb.ToString();
    }
}