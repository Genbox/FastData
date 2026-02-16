using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class EliasFanoCode<TKey>(EliasFanoContext<TKey> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        bool hasLowerBits = ctx.LowerBitCount != 0;

        return $$"""
                     private const int _lowerBitCount = {{ctx.LowerBitCount.ToStringInvariant()}};
                     {{FieldModifier}}ulong[] _upperBits = new ulong[] {
                 {{FormatColumns(ctx.UpperBits, ToValueLabel)}}
                     };
                     {{(hasLowerBits ? $$"""

                                                     {{FieldModifier}}ulong[] _lowerBits = new ulong[] {
                                                 {{FormatColumns(ctx.LowerBits, ToValueLabel)}}
                                                     };

                                                     private const ulong _lowerMask = {{ToValueLabel(ctx.LowerMask)}};
                                         """
                         : string.Empty)}}

                     private const int _sampleRateShift = {{ctx.SampleRateShift.ToStringInvariant()}};
                     {{FieldModifier}}int[] _samplePositions = new int[] {
                 {{FormatColumns(ctx.SamplePositions, static (_, x) => x.ToStringInvariant())}}
                     };

                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
                     {
                 {{GetMethodHeader(MethodType.Contains)}}

                         long value = (long){{LookupKeyName}};
                         long high = value >> _lowerBitCount;

                         long position = high == 0 ? 0 : SelectZero(high - 1) + 1;
                         if (position < 0)
                             return false;

                         long rank = position - high;
                         if ((ulong)rank >= ItemCount)
                             return false;

                         int currWord = (int)(position >> 6);

                         if ((uint)currWord >= (uint)_upperBits.Length)
                             return false;

                         ulong window = _upperBits[currWord] & (ulong.MaxValue << (int)(position & 63));
                 {{(hasLowerBits ?
                     """
                                 ulong targetLow = (ulong)value & _lowerMask;
                                 long lowerBitsOffset = rank * lowerBitCount;

                                 while (true)
                                 {
                                     while (window == 0)
                                     {
                                         currWord++;
                                         if ((uint)currWord >= (uint)_upperBits.Length)
                                             return false;

                                         window = _upperBits[currWord];
                                     }

                                     int trailing = System.Numerics.BitOperations.TrailingZeroCount(window);
                                     long onePosition = ((long)currWord << 6) + trailing;
                                     long currentHigh = onePosition - rank;

                                     if (currentHigh >= high)
                                     {
                                         if (currentHigh > high)
                                             return false;

                                         int wordIndex = (int)(lowerBitsOffset >> 6);
                                         int startBit = (int)(lowerBitsOffset & 63);

                                         ulong currentLow;
                                         if (startBit + lowerBitCount <= 64)
                                             currentLow = (_lowerBits[wordIndex] >> startBit) & _lowerMask;
                                         else
                                         {
                                             ulong lower = _lowerBits[wordIndex] >> startBit;
                                             ulong upper = _lowerBits[wordIndex + 1] << (64 - startBit);
                                             currentLow = (lower | upper) & _lowerMask;
                                         }

                                         if (currentLow == targetLow)
                                             return true;

                                         if (currentLow > targetLow)
                                             return false;
                                     }

                                     window &= window - 1;
                                     rank++;

                                     if ((ulong)rank >= ItemCount)
                                         return false;

                                     lowerBitsOffset += lowerBitCount;
                                 }
                     """
                     : """
                                   while (true)
                                   {
                                       while (window == 0)
                                       {
                                           currWord++;
                                           if ((uint)currWord >= (uint)_upperBits.Length)
                                               return false;

                                           window = _upperBits[currWord];
                                       }

                                       int trailing = System.Numerics.BitOperations.TrailingZeroCount(window);
                                       long onePosition = ((long)currWord << 6) + trailing;
                                       long currentHigh = onePosition - rank;

                                       if (currentHigh >= high)
                                       {
                                           if (currentHigh > high)
                                               return false;

                                           return true;
                                       }

                                       window &= window - 1;
                                       rank++;

                                       if ((ulong)rank >= ItemCount)
                                           return false;
                                   }
                       """)}}
                     }

                     private static long SelectZero(long rank)
                     {
                         if (rank < 0)
                             return -1;

                         int sampleIndex = (int)(rank >> _sampleRateShift);
                         if ((uint)sampleIndex >= (uint)_samplePositions.Length)
                             return -1;

                         long zeroRank = (long)sampleIndex << _sampleRateShift;
                         int startPosition = _samplePositions[sampleIndex];
                         int wordIndex = startPosition >> 6;
                         int startBit = startPosition & 63;

                         for (; wordIndex < _upperBits.Length; wordIndex++)
                         {
                             int validBits = wordIndex == _upperBits.Length - 1 ? {{ctx.UpperBitLength & 63}} : 64;
                             ulong validMask = validBits == 64 ? ulong.MaxValue : (1UL << validBits) - 1;
                             ulong zeros = ~_upperBits[wordIndex] & validMask;

                             if (startBit > 0)
                             {
                                 zeros &= ~((1UL << startBit) - 1);
                                 startBit = 0;
                             }

                             int zeroCount = System.Numerics.BitOperations.PopCount(zeros);
                             if (zeroCount == 0)
                                 continue;

                             if (zeroRank + zeroCount > rank)
                             {
                                 int rankInWord = (int)(rank - zeroRank);
                                 int bitInWord = SelectBitInWord(zeros, rankInWord);
                                 return ((long)wordIndex << 6) + bitInWord;
                             }

                             zeroRank += zeroCount;
                         }

                         return -1;
                     }

                     [MethodImpl(MethodImplOptions.AggressiveInlining)]
                     private static int SelectBitInWord(ulong word, int rank)
                     {
                         if ((uint)rank >= 64)
                             return -1;

                         int remaining = rank;
                         ulong value = word;

                         while (remaining > 0)
                         {
                             if (value == 0)
                                 return -1;

                             value &= value - 1;
                             remaining--;
                         }

                         if (value == 0)
                             return -1;

                         return System.Numerics.BitOperations.TrailingZeroCount(value);
                     }
                 """;
    }
}