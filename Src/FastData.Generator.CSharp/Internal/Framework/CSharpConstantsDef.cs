using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpConstantsDef : IConstantsDef
{
    public string Comment => "//";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"public const {type} MinLength = {value}";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"public const {type} MaxLength = {value}";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"public const {type} MinValue = {value}";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"public const {type} MaxValue = {value}";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"public const {type} ItemCount = {value}";
}