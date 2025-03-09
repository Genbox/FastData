using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, SingleValueContext ctx) : IOutputWriter
{
    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
                  return {{genCfg.GetEqualFunction("value", ToValueLabel(ctx.Data[0]))}};
              }
          """;
}