namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Provides target-language names and literals for types.</summary>
public interface ITypeMap
{
    /// <summary>Gets the target-language type name for a type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The target-language type name.</returns>
    string GetTypeName(Type type);

    /// <summary>Gets the target-language literal for a null value.</summary>
    /// <returns>The target-language null literal.</returns>
    string GetNull();
}