using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal abstract class RustOutputWriter<T> : OutputWriter<T>
{
    protected string MethodModifier => "pub ";
    protected string MethodAttribute => "#[must_use]";
    protected string FieldModifier => string.Empty;
    protected string TypeNameWithLifetime => GeneratorConfig.DataType == DataType.String ? "&'static str" : KeyTypeName;
}