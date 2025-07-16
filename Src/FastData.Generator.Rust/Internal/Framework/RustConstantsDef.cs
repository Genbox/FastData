using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustConstantsDef : IConstantsDef
{
    public string Comment => "//!";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    pub const MIN_KEY_LENGTH: {type} = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    pub const MAX_KEY_LENGTH: {type} = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    pub const MIN_KEY: {type} = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    pub const MAX_KEY: {type} = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    pub const ITEM_COUNT: {type} = {value};";
}