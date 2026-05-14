using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;
using Genbox.FastData.Generators.Abstracts;

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

    /// <summary>Gets the target-language literal for a null value.</summary>
    /// <returns>The target-language null literal.</returns>
    public string GetNull() => _index[0].PrintObj(this, null);

    /// <summary>Gets the target-language type name for a CLR type.</summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The target-language type name.</returns>
    public string GetTypeName(Type type)
    {
        ITypeDef res = Get(type);

        if (res is ObjectTypeDef)
            return type.Name;

        return res.Name;
    }

    /// <summary>Gets the type definition for a CLR type.</summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    /// <returns>The type definition for <typeparamref name="T" />.</returns>
    public ITypeDef<T> Get<T>() => (ITypeDef<T>)Get(typeof(T));

    /// <summary>Gets the type definition for a CLR type.</summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The type definition for <paramref name="type" />.</returns>
    public ITypeDef Get(Type type)
    {
        ITypeDef? res = _index[(int)Type.GetTypeCode(type)];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + type.Name);

        if (res is DynamicStringTypeDef dyn)
            res = dyn.Get(_encoding).StringTypeDef;

        return res;
    }
}