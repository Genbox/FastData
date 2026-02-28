using System.Numerics;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for string code generators in the FastData library.</summary>
public sealed class StringGeneratorConfig : GeneratorConfigBase
{
    internal StringGeneratorConfig(Type structureType, uint itemCount, StringKeyProperties props, HashDetails hashDetails, GeneratorEncoding encoding, string trimPrefix, string trimSuffix, FastDataConfig cfg) : base(structureType, hashDetails, cfg, itemCount, GetEarlyExits(props, structureType, itemCount, cfg, encoding).ToArray())
    {
        // We reduce the dependencies in generators by only providing a subset of StringKeyProperties
        Constants = new StringConstants
        {
            MinStringLength = props.LengthData.LengthMap.Min,
            MaxStringLength = props.LengthData.LengthMap.Max,
            CharacterClasses = props.CharacterData.CharacterClasses
        };

        Encoding = encoding;
        IgnoreCase = cfg.IgnoreCase;

        // We use an empty string instead of null to simplify calculations later in the pipeline
        TrimPrefix = trimPrefix;
        TrimSuffix = trimSuffix;
    }

    public StringConstants Constants { get; }

    /// <summary>Gets the encoding used for string keys.</summary>
    public GeneratorEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether string keys should be treated as case-insensitive.</summary>
    public bool IgnoreCase { get; }

    public string TrimPrefix { get; }
    public string TrimSuffix { get; }

    public int TotalTrimLength => TrimPrefix.Length + TrimSuffix.Length;

    private static IEnumerable<IEarlyExit> GetEarlyExits(StringKeyProperties props, Type structureType, uint itemCount, FastDataConfig cfg, GeneratorEncoding enc)
    {
        //These don't support early exits
        if (structureType == typeof(SingleValueStructure<,>))
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == typeof(ConditionalStructure<,>) && itemCount <= 3)
            yield break;

        LengthData lengthData = props.LengthData;

        MapStrategy lengthStrategy = GetLengthMapStrategy(lengthData.LengthMap, cfg);
        if (lengthStrategy == MapStrategy.Equals)
            yield return new LengthEqualEarlyExit(lengthData.LengthMap.Min, lengthData.MinByteCount);
        else if (lengthStrategy == MapStrategy.Range)
            yield return new LengthRangeEarlyExit(lengthData.LengthMap.Min, lengthData.LengthMap.Max, lengthData.MinByteCount, lengthData.MaxByteCount);
        else
            yield return new LengthBitmapEarlyExit(lengthData.LengthMap.Values);

        // if (lengthData.CharDivisor > 1 || lengthData.ByteDivisor > 1)
        // yield return new LengthDivisorEarlyExit(lengthData.CharDivisor, lengthData.ByteDivisor);

        if (ShouldApplyCharMap(props.LengthData.LengthMap.Min, props.CharacterData.AllAscii, enc, cfg.IgnoreCase))
        {
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            AsciiMap lastMap = props.CharacterData.LastCharMap;
            MapStrategy firstStrategy = GetCharMapStrategy(firstMap, cfg);
            MapStrategy lastStrategy = GetCharMapStrategy(lastMap, cfg);

            if (firstStrategy == MapStrategy.Equals)
                yield return new CharEqualsEarlyExit(CharPosition.First, firstMap.Min);
            else if (lastStrategy == MapStrategy.Equals)
                yield return new CharEqualsEarlyExit(CharPosition.Last, lastMap.Min);
            else if (firstStrategy == MapStrategy.Range)
                yield return new CharRangeEarlyExit(CharPosition.First, firstMap.Min, firstMap.Max);
            else if (lastStrategy == MapStrategy.Range)
                yield return new CharRangeEarlyExit(CharPosition.Last, lastMap.Min, lastMap.Max);
            else if (firstStrategy == MapStrategy.Bitmap)
                yield return new CharBitmapEarlyExit(CharPosition.First, firstMap.Low, firstMap.High);
            else if (lastStrategy == MapStrategy.Bitmap)
                yield return new CharBitmapEarlyExit(CharPosition.Last, lastMap.Low, lastMap.High);
        }
        else if (ShouldApplyStringBitMask(props.CharacterData.StringBitMask, props.CharacterData.StringBitMaskBytes, cfg, out ulong mask))
            yield return new StringBitMaskEarlyExit(mask, props.CharacterData.StringBitMaskBytes);

        if (props.DeltaData.Prefix.Length != 0 || props.DeltaData.Suffix.Length != 0)
            yield return new StringPrefixSuffixEarlyExit(props.DeltaData.Prefix, props.DeltaData.Suffix);
    }

    private static MapStrategy GetLengthMapStrategy(LengthBitArray map, FastDataConfig cfg)
    {
        if (map.BitCount <= 1)
            return MapStrategy.Equals;

        ulong range = (ulong)map.Max - map.Min + 1;
        double density = map.BitCount / (double)range;
        return density >= cfg.LengthMapMinDensity ? MapStrategy.Range : MapStrategy.Bitmap;
    }

    private static bool ShouldApplyStringBitMask(ulong mask, int byteCount, FastDataConfig cfg, out ulong stringMask)
    {
        stringMask = mask;

        if (mask == 0 || byteCount <= 0)
            return false;

        int missingBits = BitOperations.PopCount(mask);
        int bitWidth = byteCount * 8;
        double density = missingBits / (double)bitWidth;
        return density >= cfg.StringBitMaskMinDensity;
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

    private static MapStrategy GetCharMapStrategy(AsciiMap map, FastDataConfig cfg)
    {
        if (map.BitCount == 1)
            return MapStrategy.Equals;

        return map.Density >= cfg.CharMapMinDensity ? MapStrategy.Range : MapStrategy.Bitmap;
    }

    private enum MapStrategy
    {
        Equals,
        Range,
        Bitmap
    }
}