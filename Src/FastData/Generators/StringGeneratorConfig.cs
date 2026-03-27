using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Extensions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for string code generators in the FastData library.</summary>
public sealed class StringGeneratorConfig : GeneratorConfigBase
{
    internal StringGeneratorConfig(Type structureType, uint itemCount, uint minLen, uint maxLen, bool ignoreCase, CharacterClass classes, GeneratorEncoding encoding, AnnotatedExpr[] earlyExits, string trimPrefix, string trimSuffix, bool typeReductionEnabled, StringHashInfo? hashInfo, StringFunction stringFunctions) : base(structureType.GetFriendlyName(), earlyExits, itemCount, typeReductionEnabled)
    {
        // We reduce the dependencies in generators by only providing a subset of StringKeyProperties
        Constants = new StringConstants
        {
            MinStringLength = minLen,
            MaxStringLength = maxLen,
            CharacterClasses = classes
        };

        Encoding = encoding;
        IgnoreCase = ignoreCase;

        // We use an empty string instead of null to simplify calculations later in the pipeline
        TrimPrefix = trimPrefix;
        TrimSuffix = trimSuffix;

        HashInfo = hashInfo;
        StringFunctions = stringFunctions;
    }

    public StringHashInfo? HashInfo { get; }

    public StringConstants Constants { get; }

    /// <summary>A set of functions used in hashing/comparing string in generated expressions</summary>
    public StringFunction StringFunctions { get; }

    /// <summary>Gets the encoding used for string keys.</summary>
    public GeneratorEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether string keys should be treated as case-insensitive.</summary>
    public bool IgnoreCase { get; }

    public string TrimPrefix { get; }
    public string TrimSuffix { get; }

    public int TotalTrimLength => TrimPrefix.Length + TrimSuffix.Length;
}