using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpConstantsDef : IConstantsDef
{
    public string MinLengthName => "MinLength";
    public string MaxLengthName => "MaxLength";
    public string MinValueName => "MinValue";
    public string MaxValueName => "MaxValue";
    public string ItemName => "ItemCount";
    public string FieldModifier => "public const ";
    public string CommentChar => "//";
}