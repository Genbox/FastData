namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for code generators</summary>
public interface ICodeGenerator
{
    /// <summary>Gets a value indicating whether the generator uses UTF-16 encoding for string inputs (used in hashing).</summary>
    bool UseUTF16Encoding { get; }

    /// <summary>Attempts to generate source code using the specified generator configuration and context.</summary>
    /// <typeparam name="T">The type of data being generated.</typeparam>
    /// <param name="genCfg">The generator configuration.</param>
    /// <param name="context">The context for code generation.</param>
    /// <param name="source">When this method returns, contains the generated source code if the operation succeeded; otherwise, null.</param>
    /// <returns>True if code generation succeeded; otherwise, false.</returns>
    bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source);
}