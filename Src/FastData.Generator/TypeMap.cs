using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Generator;

/// <summary>Takes in type definitions and can then be used as a lookup table afterward.</summary>
public sealed class TypeMap : ITypeMap
{
    private readonly GeneratorEncoding _encoding;
    private readonly ITypeDef?[] _index = new ITypeDef?[19];

    /// <summary>Initializes a new instance of the <see cref="TypeMap" /> class.</summary>
    /// <param name="typeSpecs">The type definitions supported by a target language.</param>
    /// <param name="encoding">The string encoding model used by the generator.</param>
    public TypeMap(IList<ITypeDef> typeSpecs, GeneratorEncoding encoding)
    {
        _encoding = encoding;
        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeDef spec = typeSpecs[i];
            byte idx = (byte)spec.KeyType;

            // Fail early if a language registers two definitions for the same CLR type code.
            if (_index[idx] != null)
                throw new InvalidOperationException($"Duplicate type spec found for '{spec.KeyType}'");

            _index[idx] = spec;
        }
    }

    /// <summary>Gets the target-language literal for a value based on its runtime type.</summary>
    /// <param name="value">The value, or <see langword="null" />.</param>
    /// <returns>The target-language value literal.</returns>
    public string GetValueLiteral(object? value)
    {
        if (value == null)
        {
            ITypeDef? definition = _index[(int)TypeCode.Empty];

            if (definition == null)
                throw new InvalidOperationException("No null type definition was registered.");

            return definition.PrintObj(this, null!);
        }

        return Get(value.GetType()).PrintObj(this, value);
    }

    /// <summary>Gets the target-language declaration for an object type.</summary>
    /// <param name="type">The object type.</param>
    /// <returns>The target-language object declaration.</returns>
    public string GetObjectDeclaration(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "The object type cannot be null.");

        ValidateSupportedType(type);

        if (Type.GetTypeCode(type) != TypeCode.Object)
            throw new ArgumentException("The type must be an object type.", nameof(type));

        ITypeDef? definition = _index[(int)TypeCode.Object];

        if (definition == null)
            throw new InvalidOperationException("No object type definition was registered.");

        if (definition is not IObjectTypeDef objectDefinition)
            throw new InvalidOperationException("The registered object type definition does not support object declarations.");

        return objectDefinition.PrintDeclaration(this, type);
    }

    /// <summary>Gets the target-language type name for a CLR type.</summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The target-language type name.</returns>
    public string GetTypeName(Type type)
    {
        ITypeDef res = Get(type);

        if (res is IObjectTypeDef)
            return type.Name;

        return res.Name;
    }

    /// <summary>Gets the string length for the encoding model used by this type map.</summary>
    /// <param name="value">The string value.</param>
    /// <returns>The length in the units used by the target-language string type.</returns>
    public int GetStringLength(string value) => StringHelper.GetLengthFunc(_encoding)(value);

    /// <summary>Gets the type definition for a CLR type.</summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    /// <returns>The type definition for <typeparamref name="T" />.</returns>
    public ITypeDef<T> Get<T>() => (ITypeDef<T>)Get(typeof(T));

    /// <summary>Gets the type definition for a CLR type.</summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The type definition for <paramref name="type" />.</returns>
    public ITypeDef Get(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "The CLR type cannot be null.");

        ValidateSupportedType(type);

        ITypeDef? res = _index[(int)Type.GetTypeCode(type)];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + type.Name);

        if (res is DynamicStringTypeDef dyn)
            res = dyn.Get(_encoding).StringTypeDef;

        return res;
    }

    private static void ValidateSupportedType(Type type)
    {
        if (type.IsEnum)
            throw new NotSupportedException("Enum types are not supported by the type map.");

        if (type == typeof(nint) || type == typeof(nuint))
            throw new NotSupportedException("Native integer types are not supported by the type map.");
    }
}