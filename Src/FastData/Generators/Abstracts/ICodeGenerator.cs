using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for code generators</summary>
public interface ICodeGenerator
{
    /// <summary>Gets a value indicating whether the generator uses UTF-16 encoding for string inputs (used in hashing).</summary>
    GeneratorEncoding Encoding { get; }

    /// <summary>Attempts to generate source code using the specified generator configuration and context.</summary>
    /// <typeparam name="T">The type of data being generated.</typeparam>
    /// <param name="genCfg">The generator configuration.</param>
    /// <param name="context">The context for code generation.</param>
    /// <returns>True if code generation succeeded; otherwise, false.</returns>
    string Generate<T>(GeneratorConfig<T> genCfg, IContext<T> context);
}