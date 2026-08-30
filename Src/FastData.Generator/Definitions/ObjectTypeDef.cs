using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes target-language declarations and literals for object types.</summary>
/// <param name="userPrintDeclaration">The function used to print object type declarations.</param>
/// <param name="userPrintValue">The function used to print object values.</param>
public class ObjectTypeDef(Func<TypeMap, Type, string> userPrintDeclaration, Func<TypeMap, object, string> userPrintValue) : IObjectTypeDef
{
    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.Object;

    /// <inheritdoc />
    public string Name => throw new NotSupportedException("not supported");

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj => userPrintValue;

    /// <inheritdoc />
    public Func<TypeMap, object, string> Print => userPrintValue;

    /// <inheritdoc />
    public string PrintDeclaration(TypeMap map, Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "The object type cannot be null.");

        return userPrintDeclaration(map, type);
    }
}