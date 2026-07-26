namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Provides target-language names and literals for types.</summary>
public interface ITypeMap
{
    /// <summary>Gets the target-language literal for a value based on its runtime type.</summary>
    /// <param name="value">The value, or <see langword="null" />.</param>
    /// <returns>The target-language value literal.</returns>
    string GetValueLiteral(object? value);

    /// <summary>Gets the target-language declaration for an object type.</summary>
    /// <param name="type">The object type.</param>
    /// <returns>The target-language object declaration.</returns>
    string GetObjectDeclaration(Type type);

    /// <summary>Gets the target-language type name for a type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The target-language type name.</returns>
    string GetTypeName(Type type);
}