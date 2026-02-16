using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class RrrBitVectorCode<TKey>(RrrBitVectorContext ctx) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        _ = ctx;
        bool customKey = !typeof(TKey).IsPrimitive;

        return $$"""
{{MethodAttribute}}
{{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
{{GetMethodHeader(MethodType.Contains)}}
    let _ = {{InputKeyName}};
    false
}
""";
    }
}
