using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class RrrBitVectorCode<TKey>(RrrBitVectorContext ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        string helperModifier = FieldModifier.Contains(" static ", StringComparison.Ordinal) ? "private static " : "private ";
        string mapSource = GetMapSource();

        return $$"""
                     private const ulong _rrrMinValue = {{ToValueLabel(ctx.MinValue)}};
                     private const ulong _rrrMaxValue = {{ToValueLabel(ctx.MaxValue)}};
                     private const int _rrrBlockSize = {{ctx.BlockSize.ToStringInvariant()}};
                     {{FieldModifier}}byte[] _rrrClasses = new byte[] {
                 {{FormatColumns(ctx.Classes, static x => ((int)x).ToStringInvariant())}}
                     };
                     {{FieldModifier}}uint[] _rrrOffsets = new uint[] {
                 {{FormatColumns(ctx.Offsets, ToValueLabel)}}
                     };

                               {{MethodAttribute}}
                               {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
                     {
                 {{GetMethodHeader(MethodType.Contains)}}

                         ulong mapped = {{mapSource}};

                         if (mapped < _rrrMinValue || mapped > _rrrMaxValue)
                             return false;

                         ulong normalized = mapped - _rrrMinValue;
                         int blockIndex = (int)(normalized / (ulong)_rrrBlockSize);
                         int bitInBlock = (int)(normalized % (ulong)_rrrBlockSize);
                         int classValue = _rrrClasses[blockIndex];

                         if (classValue == 0)
                             return false;

                         uint rank = _rrrOffsets[blockIndex];
                         return DecodeBit(classValue, rank, bitInBlock);
                     }

                     {{helperModifier}}bool DecodeBit(int classValue, uint rank, int targetBit)
                     {
                         int remaining = classValue;

                         for (int bit = _rrrBlockSize - 1; bit >= 0; bit--)
                         {
                             if (remaining == 0)
                                 return false;

                             int comb = Binomial(bit, remaining);
                             bool isSet;

                             if (rank >= (uint)comb)
                             {
                                 rank -= (uint)comb;
                                 remaining--;
                                 isSet = true;
                             }
                             else
                                 isSet = false;

                             if (bit == targetBit)
                                 return isSet;
                         }

                         return false;
                     }

                     {{helperModifier}}int Binomial(int n, int k)
                     {
                         if (k < 0 || k > n)
                             return 0;

                         if (k == 0 || k == n)
                             return 1;

                         if (k > n - k)
                             k = n - k;

                         int result = 1;

                         for (int i = 1; i <= k; i++)
                             result = checked(result * (n - (k - i)) / i);

                         return result;
                     }
                 """;
    }

    private string GetMapSource()
    {
        return KeyType switch
        {
            KeyType.Char => $"(ulong){LookupKeyName}",
            KeyType.Byte => $"(ulong){LookupKeyName}",
            KeyType.UInt16 => $"(ulong){LookupKeyName}",
            KeyType.UInt32 => $"(ulong){LookupKeyName}",
            KeyType.UInt64 => LookupKeyName,
            KeyType.SByte => $"(ulong)(byte)({LookupKeyName} ^ sbyte.MinValue)",
            KeyType.Int16 => $"(ulong)(ushort)({LookupKeyName} ^ short.MinValue)",
            KeyType.Int32 => $"(ulong)(uint)({LookupKeyName} ^ int.MinValue)",
            KeyType.Int64 => $"(ulong)({LookupKeyName} ^ long.MinValue)",
            _ => throw new InvalidOperationException("RRR bitvector only supports integral key types.")
        };
    }
}