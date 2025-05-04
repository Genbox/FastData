namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate()
        => $$"""
                 #[must_use]
                 {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
             {{cfg.GetEarlyExits(genCfg)}}

                     if {{FormatList(ctx.Data, static x => $"value == {ToValueLabel(x)}", " || ")}} {
                         return true;
                     }

                     false
                 }
             """;
}