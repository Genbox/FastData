using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpConstantsDef : IConstantsDef
{
    public string Comment => "//";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    public const {type} MinKeyLength = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    public const {type} MaxKeyLength = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    public const {type} MinKey = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    public const {type} MaxKey = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    public const {type} ItemCount = {value};";
}