using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.Template;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.CSharp;

public sealed class CSharpCodeGenerator(CSharpCodeGeneratorConfig csCfg) : TemplatedCodeGenerator(new CSharpLanguageDef(), GeneratorEncoding.UTF16)
{
    protected override string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables)
    {
        string templatePath = Path.Combine(TemplateDir, genCfg.StructureName + ".tt");
        string templateSource = File.ReadAllText(templatePath);

        variables["CSharpConfig"] = csCfg;
        return manager.Render(templatePath, templateSource, variables);
    }
}