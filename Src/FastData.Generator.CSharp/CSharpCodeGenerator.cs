using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Template;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.CSharp;

/// <summary>Generates C# source code from FastData structure contexts.</summary>
public sealed class CSharpCodeGenerator(CSharpCodeGeneratorConfig csCfg) : TemplatedCodeGenerator(new CSharpLanguageDef(), GeneratorEncoding.Utf16CodeUnits)
{
    protected override string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables)
    {
        if (genCfg is StringGeneratorConfig { IgnoreCase: true } && csCfg.ConditionalBranchType == BranchType.Switch)
            throw new InvalidOperationException("C# switch generation does not support IgnoreCase. Use BranchType.If when IgnoreCase is enabled.");

        string templatePath = Path.Combine(TemplateDir, genCfg.StructureName + ".tt");
        string templateSource = File.ReadAllText(templatePath);

        variables["CSharpConfig"] = csCfg;
        return manager.Render(templatePath, templateSource, variables);
    }
}