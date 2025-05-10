namespace Genbox.FastData.Generator.Framework.Interfaces.Specs;

public interface ILanguageSpec
{
    bool UseUTF16Encoding { get; }
    string CommentChar { get; }
    IList<ITypeSpec> Primitives { get; }
    string ArraySizeType { get; }
}