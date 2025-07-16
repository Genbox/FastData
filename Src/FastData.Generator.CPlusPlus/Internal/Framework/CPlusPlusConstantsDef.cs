using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusConstantsDef : IConstantsDef
{
    public string Comment => "//";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    static constexpr {type} min_key_length = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    static constexpr {type} max_key_length = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    static constexpr {type} min_key = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    static constexpr {type} max_key = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    static constexpr {type} item_count = {value};";
}