using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

[SuppressMessage("Roslynator", "RCS1197:Optimize StringBuilder.Append/AppendLine call")]
internal class CSharpHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName, HashInfo info) =>
        $$"""
          {{GetState(info.StringHash?.State)}}
              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static ulong Hash({{typeName}} value)
              {
          {{GetHash(dataType, info)}}
              }
          """;

    private static string GetState(StateInfo[]? info)
    {
        if (info == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (StateInfo state in info)
        {
            sb.Append("    private static ")
              .Append(state.TypeName)
              .Append("[] ")
              .Append(state.Name)
              .Append(" = new ")
              .Append(state.TypeName)
              .Append("[] { ")
              .Append(string.Join(", ", state.Values))
              .Append(" };\n");
        }

        return sb.ToString();
    }

    private static string GetHash(DataType dataType, HashInfo info)
    {
        if (dataType == DataType.String)
        {
            return info.StringHash != null
                ? $"""
                   {info.StringHash.HashSource}
                           return hash;
                   {GetFunctions(info.StringHash.ReaderFunctions)}
                   """
                : """
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

    private static string GetFunctions(ReaderFunctions functions)
    {
        if (functions == ReaderFunctions.None)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        if (functions.HasFlag(ReaderFunctions.ReadU8))
            sb.AppendLine("        static byte ReadU8(string ptr, int offset) => (byte)Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);");
        if (functions.HasFlag(ReaderFunctions.ReadU16))
            sb.AppendLine("        static ushort ReadU16(string ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");
        if (functions.HasFlag(ReaderFunctions.ReadU32))
            sb.AppendLine("        static uint ReadU32(string ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");
        if (functions.HasFlag(ReaderFunctions.ReadU64))
            sb.AppendLine("        static ulong ReadU64(string ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));");

        return sb.ToString();
    }
}