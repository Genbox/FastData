using System.Globalization;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config) => config.DataType switch
    {
        DataType.String => "string",
        DataType.Boolean => "bool",
        DataType.SByte => "sbyte",
        DataType.Byte => "byte",
        DataType.Char => "char",
        DataType.Int16 => "short",
        DataType.UInt16 => "ushort",
        DataType.Int32 => "int",
        DataType.UInt32 => "uint",
        DataType.Int64 => "long",
        DataType.UInt64 => "ulong",
        DataType.Single => "float",
        DataType.Double => "double",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetEqualFunction(this GeneratorConfig config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Equals(value, {variable})";

        return $"value.Equals({variable})";
    }

    internal static string GetCompareFunction(this GeneratorConfig config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Compare({variable}, value)";

        return $"{variable}.CompareTo(value)";
    }

    internal static string GetHashSource(this GeneratorConfig config, bool seeded) =>
        $$"""
              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              public static uint Hash({{config.GetTypeName()}} value{{(seeded ? ", uint seed" : "")}})
              {
          {{config.HashSpec switch
          {
              DefaultHashSpec => GetDJBHash(config.DataType, seeded),
              GeneticHashSpec ghs => GetGeneticHash(ghs, seeded),
              BruteForceHashSpec bfs => GetBruteForceHash(bfs, seeded),
              _ => throw new NotSupportedException(config.HashSpec.GetType().Name + " is not supported")
          }}}
              }
          """;

    private static string GetBruteForceHash(BruteForceHashSpec spec, bool seeded)
    {
        return spec.HashFunction switch
        {
            HashFunction.DJB2Hash => GetDJBHash(DataType.String, seeded),
            HashFunction.XxHash => $$"""
                                             ulong hash1 = {{(seeded ? "seed" : "0")}}; + (ulong)length;

                                             ref ulong ptr64 = ref Unsafe.As<char, ulong>(ref ptr);
                                             while (length >= 4)
                                             {
                                                 ulong acc = 0;
                                                 acc += ptr64 * 0xC2B2AE3D27D4EB4FUL;
                                                 acc = (acc << 31) | (acc >> (64 - 31));
                                                 acc *= 0x9E3779B185EBCA87UL;
                                                 hash1 ^= acc;
                                                 hash1 = (((hash1 << 27) | (hash1 >> (64 - 27))) * 0x9E3779B185EBCA87UL) + 0x85EBCA77C2B2AE63UL;
                                                 ptr64 = ref Unsafe.Add(ref ptr64, 1);
                                                 length -= 4;
                                             }

                                             ref ushort ptr16 = ref Unsafe.As<ulong, ushort>(ref ptr64);
                                             while (length-- > 0)
                                             {
                                                 hash1 ^= ptr16 * 0x27D4EB2F165667C5UL;
                                                 hash1 = ((hash1 << 11) | (hash1 >> (64 - 11))) * 0x9E3779B185EBCA87UL;
                                                 ptr16 = ref Unsafe.Add(ref ptr16, 1);
                                             }

                                             hash1 ^= hash1 >> 33;
                                             hash1 *= 0xC2B2AE3D27D4EB4FUL;
                                             hash1 ^= hash1 >> 29;
                                             hash1 *= 0x165667B19E3779F9UL;
                                             hash1 ^= hash1 >> 32;
                                             return unchecked((uint)hash1);
                                     """,
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.HashFunction, "Invalid hash function")
        };
    }

    private static string GetGeneticHash(GeneticHashSpec spec, bool seeded) =>
        $$"""
                  uint acc = {{(seeded ? "seed" : "0")}};

                  for (int i = 0; i < str.Length; i++)
                      acc = mixer(acc, str[i]);

                  return avalanche(acc);

                  static uint mixer(uint acc)
                  {
                      {{ExpressionConverter.Instance.GetCode(spec.GetMixer())}}
                      return acc;
                  }

                  static uint avalanche(uint acc)
                  {
                      {{ExpressionConverter.Instance.GetCode(spec.GetAvalanche())}}
                      return acc;
                  }
          """;

    private static string GetDJBHash(DataType dataType, bool seeded)
    {
        if (dataType == DataType.String)
        {
            return $$"""
                             uint hash1 = {{(seeded ? "seed" : "(5381 << 16) + 5381")}};
                             uint hash2 = {{(seeded ? "seed" : "(5381 << 16) + 5381")}};
                             int length = value.Length;
                             ReadOnlySpan<char> span = value.AsSpan();
                             ref char ptr = ref MemoryMarshal.GetReference(span);
                             ref uint ptr32 = ref Unsafe.As<char, uint>(ref ptr);

                             while (length >= 4)
                             {
                                 hash1 = (((hash1 << 5) | (hash1 >> (32 - 5))) + hash1) ^ ptr32;
                                 hash2 = (((hash2 << 5) | (hash2 >> (32 - 5))) + hash2) ^ Unsafe.Add(ref ptr32, 1);

                                 ptr32 = ref Unsafe.Add(ref ptr32, 2);
                                 length -= 4;
                             }

                             ref char ptrChar = ref Unsafe.As<uint, char>(ref ptr32);
                             while (length-- > 0)
                             {
                                 hash2 = (((hash2 << 5) | (hash2 >> (32 - 5))) + hash2) ^ ptrChar;
                                 ptrChar = ref Unsafe.Add(ref ptrChar, 1);
                             }

                             return hash1 + (hash2 * 1566083941);
                     """;
        }

        //For these types, we can use identity hashing
        return dataType switch
        {
            DataType.Char
                or DataType.SByte
                or DataType.Byte
                or DataType.Int16
                or DataType.UInt16
                or DataType.Int32
                or DataType.UInt32 => seeded ? "        return unchecked((uint)(value ^ seed));" : "        return unchecked((uint)value);",
            _ => seeded ? "        return unchecked((uint)(value.GetHashCode() ^ seed));" : "        return unchecked((uint)(value.GetHashCode()));"
        };
    }

    //TODO: Needed for analysis-based hashes
    private static string GetSlice(StringSegment segment)
    {
        if (segment.Alignment == Alignment.Left)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length == -1)
                return $"str.AsSpan({segment.Offset.ToString(NumberFormatInfo.InvariantInfo)})";

            return $"str.AsSpan({segment.Offset}, {segment.Length})";
        }

        if (segment.Alignment == Alignment.Right)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length == -1)
                return $"str.AsSpan(0, str.Length - {segment.Offset} - {segment.Length})";

            return $"str.AsSpan(str.Length - {segment.Offset} - {segment.Length}, {segment.Length})";
        }

        throw new InvalidOperationException("Invalid alignment: " + segment.Alignment);
    }
}