using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusConstantsDef : IConstantsDef
{
    public string MinLengthName => "min_length";
    public string MaxLengthName => "max_length";
    public string MinValueName => "min_value";
    public string MaxValueName => "max_value";
    public string ItemName => "item_count";
    public string FieldModifier => "static constexpr ";
    public string CommentChar => "//";
}