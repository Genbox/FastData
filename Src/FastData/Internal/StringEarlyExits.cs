using System.Numerics;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal;

internal static class StringEarlyExits
{
    internal static IEnumerable<IEarlyExit> GetCandidates(Type structureType, StringKeyProperties props, StringDataConfig cfg, GeneratorEncoding enc, EarlyExitConfig config)
    {
        if (!config.IsEnabledForStructure(structureType))
            yield break;

        (uint minByteCount, uint maxByteCount, _, LengthBitArray map, _, _) = props.LengthData;

        if (config.IsEarlyExitEnabled(typeof(LengthEqualEarlyExit)) && map.BitCount == 1)
        {
            // Does not matter which length we use, as the yare all the same
            yield return new LengthEqualEarlyExit(map.Max, minByteCount);
            yield break;
        }

        float lengthDensity = (float)map.BitCount / ((map.Max - map.Min) + 1);

        if (config.IsEarlyExitEnabled(typeof(LengthRangeEarlyExit)) && config.CheckDensityLimits(typeof(LengthRangeEarlyExit), lengthDensity))
        {
            yield return new LengthRangeEarlyExit(map.Min, map.Max, minByteCount, maxByteCount); //TODO: Move byte variants into map?
            yield break;
        }

        if (config.IsEarlyExitEnabled(typeof(LengthBitmapEarlyExit)) && config.CheckDensityLimits(typeof(LengthBitmapEarlyExit), lengthDensity))
        {
            yield return new LengthBitmapEarlyExit(map.Values); //TODO: Create byte variant
            yield break;
        }

        if (ShouldApplyCharMap(props.LengthData.LengthMap.Min, props.CharacterData.AllAscii, enc, cfg.IgnoreCase))
        {
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharEqualsEarlyExit)) && firstMap.BitCount == 1)
            {
                yield return new CharEqualsEarlyExit(CharPosition.First, firstMap.Min);
            }
            else
            {
                if (config.IsEarlyExitEnabled(typeof(CharBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharRangeEarlyExit), firstMap.Density))
                    yield return new CharBitmapEarlyExit(CharPosition.First, firstMap.Low, firstMap.High);
                else if (config.IsEarlyExitEnabled(typeof(CharRangeEarlyExit)))
                    yield return new CharRangeEarlyExit(CharPosition.First, firstMap.Min, firstMap.Max);
            }

            AsciiMap lastMap = props.CharacterData.LastCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharEqualsEarlyExit)) && lastMap.BitCount == 1)
            {
                yield return new CharEqualsEarlyExit(CharPosition.Last, lastMap.Min);
            }
            else
            {
                if (config.IsEarlyExitEnabled(typeof(CharBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharRangeEarlyExit), lastMap.Density))
                    yield return new CharBitmapEarlyExit(CharPosition.Last, lastMap.Low, lastMap.High);
                else if (config.IsEarlyExitEnabled(typeof(CharRangeEarlyExit)))
                    yield return new CharRangeEarlyExit(CharPosition.Last, lastMap.Min, lastMap.Max);
            }
        }

        float bitMaskDensity = BitOperations.PopCount(props.CharacterData.StringBitMask) / (float)(props.CharacterData.StringBitMaskBytes * 8);

        if (config.IsEarlyExitEnabled(typeof(StringBitMaskEarlyExit)) && config.CheckDensityLimits(typeof(StringBitMaskEarlyExit), bitMaskDensity))
            yield return new StringBitMaskEarlyExit(props.CharacterData.StringBitMask, props.CharacterData.StringBitMaskBytes);

        if (config.IsEarlyExitEnabled(typeof(StringPrefixSuffixEarlyExit)) && (props.DeltaData.Prefix.Length != 0 || props.DeltaData.Suffix.Length != 0))
            yield return new StringPrefixSuffixEarlyExit(props.DeltaData.Prefix, props.DeltaData.Suffix);
    }

    private static bool ShouldApplyCharMap(uint minLength, bool allAscii, GeneratorEncoding encoding, bool ignoreCase)
    {
        if (minLength == 0)
            return false;

        if (!allAscii)
            return false;

        if (ignoreCase && !allAscii)
            return false;

        if (encoding is GeneratorEncoding.ASCII or GeneratorEncoding.UTF8)
            return allAscii;

        return true;
    }
}