using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the contract implemented by target-language code generators.</summary>
public interface ICodeGenerator
{
    /// <summary>Gets the string encoding model expected by the generated code.</summary>
    GeneratorEncoding Encoding { get; }

    /// <summary>Generates source code using the specified generator configuration and structure context.</summary>
    /// <typeparam name="TKey">The lookup key type.</typeparam>
    /// <typeparam name="TValue">The associated value type, or <see cref="byte" /> for membership-only data.</typeparam>
    /// <param name="genCfg">The generator configuration.</param>
    /// <param name="context">The context for code generation.</param>
    /// <returns>The generated source code.</returns>
    string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context);
}