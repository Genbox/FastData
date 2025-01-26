using System.Text;

namespace Genbox.FastData.Internal.Analysis.Genetic;

internal static class Mixers
{
    internal static readonly Func<uint, StringBuilder?, uint>[] Ops =
    [
        (x, y) =>
        {
            y?.Append("Identity->");
            return x;
        },
        (x, y) =>
        {
            y?.Append("MultMix1->");
            return x * Seeds.GoodSeeds[0];
        },
        (x, y) =>
        {
            y?.Append("MultMix2->");
            return x * Seeds.GoodSeeds[1];
        },
        (x, y) =>
        {
            y?.Append("XorShiftMix1->");
            return x ^ (x >> 16);
        },
        (x, y) =>
        {
            y?.Append("XorShiftMix2->");
            return x ^ (x >> 17);
        },

        // DJB2Mix,
        // MurmurMix1,
        // MurmurMix2,
        // MurmurMix3,
    ];

    private static uint DJB2Mix(uint val, StringBuilder? y)
    {
        y?.Append("DJB2Mix->");
        return 352654597U + (val * 1566083941U);
    }

    private static uint MurmurMix1(uint val, StringBuilder? y)
    {
        y?.Append("MurmurMix1->");
        val ^= val >> 16;
        val *= 0x85ebca6b;
        return val;
    }

    private static uint MurmurMix2(uint val, StringBuilder? y)
    {
        y?.Append("MurmurMix2->");
        val ^= val >> 16;
        val *= 0x85ebca6b;
        val ^= val >> 13;
        val *= 0xc2b2ae35;
        return val;
    }

    private static uint MurmurMix3(uint val, StringBuilder? y)
    {
        y?.Append("MurmurMix3->");
        val ^= val >> 16;
        val *= 0x85ebca6b;
        val ^= val >> 13;
        val *= 0xc2b2ae35;
        val ^= val >> 16;
        return val;
    }
}