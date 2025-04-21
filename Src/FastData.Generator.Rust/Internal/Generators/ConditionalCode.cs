namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate()
        => $$"""
                 {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
             {{cfg.GetEarlyExits(genCfg)}}

                     if {{FormatList(ctx.Data, (x, y) => Render(genCfg, x, y), " || ")}} {
                         return true;
                     }

                     false
                 }
             """;

    private static void Render(GeneratorConfig genCfg, StringBuilder sb, object obj) => sb.Append(genCfg.GetEqualFunction(ToValueLabel(obj)));
}