using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal abstract class RustOutputWriter<T> : OutputWriter<T>
{
    protected string MethodModifier => "pub ";
    protected string MethodAttribute => "#[must_use]";
    protected string FieldModifier => string.Empty;
    protected string GetKeyTypeName(bool customType) => customType ? $"&'static {KeyTypeName}" : KeyTypeName;
    protected string GetValueTypeName(bool customType) => customType ? $"&'static {ValueTypeName}" : ValueTypeName;
}