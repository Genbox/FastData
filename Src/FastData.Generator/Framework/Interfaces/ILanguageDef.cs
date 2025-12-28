namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface ILanguageDef
{
    IList<ITypeDef> TypeDefinitions { get; }
    string ArraySizeType { get; }
}