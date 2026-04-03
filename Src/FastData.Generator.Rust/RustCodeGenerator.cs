using Genbox.FastData.Generator.Rust.Internal;
using Genbox.FastData.Generator.Template;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Rust;

public sealed class RustCodeGenerator(RustCodeGeneratorConfig rustCfg) : TemplatedCodeGenerator(new RustLanguageDef(), GeneratorEncoding.Utf8Bytes)
{
    protected override string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables)
    {
        string templatePath = Path.Combine(TemplateDir, genCfg.StructureName + ".tt");
        string templateSource = File.ReadAllText(templatePath);

        variables["RustConfig"] = rustCfg;
        return manager.Render(templatePath, templateSource, variables);
    }
}