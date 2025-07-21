using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        return $$"""
                     {{FieldModifier}}{{KeyTypeName}}[] _entries = new {{KeyTypeName}}[] {
                 {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                     };

                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                     {
                 {{EarlyExits}}

                         return {{GetEqualFunction("key", $"_entries[key.Length - {ctx.MinLength.ToStringInvariant()}]")}};
                     }
                 """;

    }
}