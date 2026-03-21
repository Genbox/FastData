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
    internal static IEnumerable<IEarlyExit> GetCandidates(Type structureType, StringKeyProperties props, bool ignoreCase, GeneratorEncoding enc, EarlyExitConfig config)
    {
        if (config.Disabled)
            yield break;

        if (!config.IsEnabledForStructure(structureType))
            yield break;

        (_, LengthBitArray charMap, _, LengthBitArray byteMap) = props.LengthData;

        if (config.IsEarlyExitEnabled(typeof(LengthEqualEarlyExit)) && charMap.BitCount == 1)
        {
            // If lengths are all the same, we only use that.
            yield return new LengthEqualEarlyExit(charMap.Max, byteMap.Min);
            yield break;
        }

        float lengthDensity = (float)charMap.BitCount / ((charMap.Max - charMap.Min) + 1);

        if (config.IsEarlyExitEnabled(typeof(LengthRangeEarlyExit)) && config.CheckDensityLimits(typeof(LengthRangeEarlyExit), lengthDensity))
        {
            yield return new LengthRangeEarlyExit(charMap.Min, charMap.Max, byteMap.Min, byteMap.Max);
            yield break;
        }

        if (config.IsEarlyExitEnabled(typeof(LengthBitmapEarlyExit)) && config.CheckDensityLimits(typeof(LengthBitmapEarlyExit), lengthDensity))
        {
            yield return new LengthBitmapEarlyExit(charMap.Values); //TODO: Create byte variant
            yield break;
        }

        if (ShouldApplyCharMap(props.LengthData.LengthMap.Min, props.CharacterData.AllAscii, enc, ignoreCase))
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