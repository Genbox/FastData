namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IConstantsDef
{
    string MinLengthName { get; }
    string MaxLengthName { get; }
    string MinValueName { get; }
    string MaxValueName { get; }
    string ItemName { get; }
    string FieldModifier { get; }
    string CommentChar { get; }
}