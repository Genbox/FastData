namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface ILanguageDef
{
    bool UseUTF16Encoding { get; }
    IList<ITypeDef> TypeDefinitions { get; }
    string ArraySizeType { get; }
}