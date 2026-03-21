using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal;

internal static class StringEarlyExits
{
    internal static IEnumerable<IEarlyExit> GetCandidates(Type structureType, StringKeyProperties props, EarlyExitConfig config, bool ignoreCase)
    {
        if (config.Disabled)
            yield break;

        if (!config.IsEnabledForStructure(structureType))
            yield break;

        /*
           Length early exits are great for languages that provide constant time access to the length of a string (C#, Java, Python, Go, Rust)
           For languages that has to iterate the string (C, Swift, Haskell), it is slower than comparing the first char.
        */
        {
            //The length map is in bytes, in the encoding the generator has
            (_, _, _, LengthBitArray map) = props.LengthData;

            if (config.IsEarlyExitEnabled(typeof(LengthNotEqualEarlyExit)) && map.BitCount == 1) //1 bit means all lengths are the same
                yield return new LengthNotEqualEarlyExit(map.Min); //We can use min or max. They are the same.

            if (config.IsEarlyExitEnabled(typeof(LengthLessThanEarlyExit)) && map.Min > 0)
                yield return new LengthLessThanEarlyExit(map.Min);

            if (config.IsEarlyExitEnabled(typeof(LengthGreaterThanEarlyExit)) && map.Max < uint.MaxValue)
                yield return new LengthGreaterThanEarlyExit(map.Max);

            float density = (float)map.BitCount / ((map.Max - map.Min) + 1);
            if (config.IsEarlyExitEnabled(typeof(LengthBitmapEarlyExit)) && config.CheckDensityLimits(typeof(LengthBitmapEarlyExit), density))
                yield return new LengthBitmapEarlyExit(map.Values[0]);
        }

        /*
           Accessing the first char/byte in a string is always constant time.
         */
        {
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharFirstNotEqualEarlyExit)) && firstMap.BitCount == 1) //1 bit means all first characters are the same
                yield return new CharFirstNotEqualEarlyExit(firstMap.Min, ignoreCase); //We can use min or max. They are the same.

            if (config.IsEarlyExitEnabled(typeof(CharFirstLessThanEarlyExit)) && firstMap.Min > 0)
                yield return new CharFirstLessThanEarlyExit(firstMap.Min);

            if (config.IsEarlyExitEnabled(typeof(CharFirstGreaterThanEarlyExit)) && firstMap.Max < char.MaxValue)
                yield return new CharFirstGreaterThanEarlyExit(firstMap.Max);

            if (config.IsEarlyExitEnabled(typeof(CharFirstBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharFirstBitmapEarlyExit), firstMap.Density))
                yield return new CharFirstBitmapEarlyExit(firstMap.Low, firstMap.High, ignoreCase);

            AsciiMap lastMap = props.CharacterData.LastCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharLastNotEqualEarlyExit)) && lastMap.BitCount == 1) //1 bit means all last characters are the same
                yield return new CharLastNotEqualEarlyExit(lastMap.Min, ignoreCase); //We can use min or max. They are the same.

            if (config.IsEarlyExitEnabled(typeof(CharLastLessThanEarlyExit)) && lastMap.Min > 0)
                yield return new CharLastLessThanEarlyExit(lastMap.Min);

            if (config.IsEarlyExitEnabled(typeof(CharLastGreaterThanEarlyExit)) && lastMap.Max < char.MaxValue)
                yield return new CharLastGreaterThanEarlyExit(lastMap.Max);

            if (config.IsEarlyExitEnabled(typeof(CharLastBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharLastBitmapEarlyExit), lastMap.Density))
                yield return new CharLastBitmapEarlyExit(lastMap.Low, lastMap.High, ignoreCase);
        }

        /*
           If prefix/suffix trimming is disabled, we can use them as early exits instead.
         */
        {
            if (config.IsEarlyExitEnabled(typeof(StringPrefixEarlyExit)) && props.DeltaData.Prefix.Length != 0)
                yield return new StringPrefixEarlyExit(props.DeltaData.Prefix, ignoreCase);

            if (config.IsEarlyExitEnabled(typeof(StringSuffixEarlyExit)) && props.DeltaData.Suffix.Length != 0)
                yield return new StringSuffixEarlyExit(props.DeltaData.Suffix, ignoreCase);
        }
    }
}