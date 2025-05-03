namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate()
        => $$"""
             public:
                 {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
                 {
             {{cfg.GetEarlyExits(genCfg)}}

                     if ({{FormatList(ctx.Data, Render, " || ")}})
                         return true;

                     return false;
                 }
             """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("value == " + ToValueLabel(obj));
}