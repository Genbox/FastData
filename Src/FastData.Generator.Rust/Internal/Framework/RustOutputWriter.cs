using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal abstract class RustOutputWriter<T> : OutputWriter<T>
{
    protected override string GetMethodModifier() => "pub ";

    protected string GetTypeNameWithLifetime()
    {
        if (GeneratorConfig.DataType == DataType.String)
            return "&'static str";

        return TypeName;
    }
}