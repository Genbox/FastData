namespace Genbox.FastData.Generator.Abstracts;

/// <summary>Describes how a type is represented in a target language.</summary>
public interface ITypeDef
{
    /// <summary>Gets the CLR type code represented by this definition.</summary>
    TypeCode KeyType { get; }

    /// <summary>Gets the target-language type name.</summary>
    string Name { get; }

    /// <summary>Gets a function that prints a CLR value as a target-language literal.</summary>
    Func<TypeMap, object, string> PrintObj { get; }
}

/// <summary>Describes how a CLR type is represented in a target language.</summary>
/// <typeparam name="T">The CLR value type represented by this definition.</typeparam>
public interface ITypeDef<in T> : ITypeDef
{
    /// <summary>Gets a function that prints a typed CLR value as a target-language literal.</summary>
    Func<TypeMap, T, string> Print { get; }
}