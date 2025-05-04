namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate()
        => $$"""
             public:
                 [[nodiscard]]
                 {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
                 {
             {{cfg.GetEarlyExits(genCfg)}}

                     if ({{FormatList(ctx.Data, static x => $"value == {ToValueLabel(x)}", " || ")}})
                         return true;

                     return false;
                 }
             """;
}