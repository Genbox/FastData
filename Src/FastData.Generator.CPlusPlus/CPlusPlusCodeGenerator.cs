using Genbox.FastData.Generator.CPlusPlus.Internal;
using Genbox.FastData.Generator.Template;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusCodeGenerator(CPlusPlusCodeGeneratorConfig cppCfg) : TemplatedCodeGenerator(new CPlusPlusLanguageDef(), GeneratorEncoding.Utf8Bytes)
{
    protected override string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables)
    {
        if (genCfg is NumericGeneratorConfig numCfg && typeof(TKey) == typeof(char))
        {
            char maxValue = (char)numCfg.Constants.MaxValue;
            if (maxValue > 127)
                throw new InvalidOperationException("C++ generator does not support chars outside ASCII. Please use a different data type or reduce the max value to 127 or lower.");
        }

        string templatePath = Path.Combine(TemplateDir, genCfg.StructureName + ".tt");
        string templateSource = File.ReadAllText(templatePath);

        variables["CPlusPlusConfig"] = cppCfg;
        return manager.Render(templatePath, templateSource, variables);
    }
}