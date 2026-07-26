namespace Genbox.FastData.Generator.Abstracts;

/// <summary>Describes a type definition that can print target-language object declarations.</summary>
public interface IObjectTypeDef : ITypeDef<object>
{
    /// <summary>Prints the target-language declaration for an object type.</summary>
    /// <param name="map">The target-language type map.</param>
    /// <param name="type">The object type.</param>
    /// <returns>The target-language object declaration.</returns>
    string PrintDeclaration(TypeMap map, Type type);
}