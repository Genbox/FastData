using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Extensions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for string code generators in the FastData library.</summary>
public sealed class StringGeneratorConfig : GeneratorConfigBase
{
    internal StringGeneratorConfig(Type structureType, uint itemCount, int minLen, int maxLen, bool ignoreCase, CharacterClass classes, GeneratorEncoding encoding, AnnotatedExpr[] earlyExits, string trimPrefix, string trimSuffix, bool typeReductionEnabled, StringHashInfo? hashInfo, GeneratorFunction generatorFunctions) : base(structureType.GetFriendlyName(), earlyExits, itemCount, typeReductionEnabled)
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
        GeneratorFunctions = generatorFunctions;
    }

    /// <summary>Gets the selected string hash expression and additional data, when string hash analysis is active.</summary>
    public StringHashInfo? HashInfo { get; }

    /// <summary>Gets string metadata constants emitted with the generated structure.</summary>
    public StringConstants Constants { get; }

    /// <summary>A set of functions used in hashing/comparing string in generated expressions</summary>
    public GeneratorFunction GeneratorFunctions { get; }

    /// <summary>Gets the encoding used for string keys.</summary>
    public GeneratorEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether string keys should be treated as case-insensitive.</summary>
    public bool IgnoreCase { get; }

    /// <summary>Gets the common prefix removed from keys before structure data is built.</summary>
    public string TrimPrefix { get; }

    /// <summary>Gets the common suffix removed from keys before structure data is built.</summary>
    public string TrimSuffix { get; }

    /// <summary>Gets the total number of characters trimmed from each key.</summary>
    public int TotalTrimLength => TrimPrefix.Length + TrimSuffix.Length;
}