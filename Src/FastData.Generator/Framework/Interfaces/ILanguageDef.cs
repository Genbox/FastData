using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface ILanguageDef
{
    GeneratorEncoding Encoding { get; }
    IList<ITypeDef> TypeDefinitions { get; }
    string ArraySizeType { get; }
}