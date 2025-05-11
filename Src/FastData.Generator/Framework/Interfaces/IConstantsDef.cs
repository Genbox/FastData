namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IConstantsDef
{
    string Comment { get; }
    Func<string, string, string> MinLengthTemplate { get; }
    Func<string, string, string> MaxLengthTemplate { get; }
    Func<string, string, string> MinValueTemplate { get; }
    Func<string, string, string> MaxValueTemplate { get; }
    Func<string, string, string> ItemCountTemplate { get; }
}