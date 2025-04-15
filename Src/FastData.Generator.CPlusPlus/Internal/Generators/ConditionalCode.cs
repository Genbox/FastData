using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate()
        => $$"""
             public:
                 {{cfg.GetMethodModifier()}} bool contains(const {{genCfg.GetTypeName()}}& value)
                 {
             {{cfg.GetEarlyExits(genCfg)}}
             
                     if ({{FormatList(ctx.Data, (x, y) => Render(genCfg, x, y), " || ")}})
                         return true;
             
                     return false;
                 }
             """;

    private static void Render(GeneratorConfig genCfg, StringBuilder sb, object obj) => sb.Append(genCfg.GetEqualFunction(ToValueLabel(obj)));
}