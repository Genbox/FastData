namespace Genbox.FastData.Generator.Abstracts;

/// <summary>Describes a target language's type definitions.</summary>
public interface ILanguageDef
{
    /// <summary>Gets the type definitions supported by the language generator.</summary>
    IList<ITypeDef> TypeDefinitions { get; }
}