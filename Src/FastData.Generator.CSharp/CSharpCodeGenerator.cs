using System.Diagnostics.CodeAnalysis;

using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Template;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.CSharp;

/// <summary>Generates C# source code from FastData structure contexts.</summary>
[SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "The netstandard2.0 target does not provide ArgumentNullException.ThrowIfNull.")]
public sealed class CSharpCodeGenerator(CSharpCodeGeneratorConfig csCfg) : TemplatedCodeGenerator(new CSharpLanguageDef(), GeneratorEncoding.Utf16CodeUnits)
{
    /// <inheritdoc />
    protected override string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables)
    {
        if (genCfg == null)
            throw new ArgumentNullException(nameof(genCfg));
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (genCfg is StringGeneratorConfig { IgnoreCase: true, StructureType: StructureType.Conditional } && csCfg.ConditionalBranchType == BranchType.Switch)
            throw new InvalidOperationException("C# switch generation does not support IgnoreCase. Use BranchType.If when IgnoreCase is enabled.");

        ValidateIdentifier(csCfg.ClassName, nameof(csCfg.ClassName));

        if (csCfg.Namespace != null)
            ValidateIdentifier(csCfg.Namespace, nameof(csCfg.Namespace));

        string templatePath = Path.Combine(TemplateDir, genCfg.StructureType + ".tt");
        string templateSource = File.ReadAllText(templatePath);

        variables["CSharpConfig"] = csCfg;
        return manager.Render(templatePath, templateSource, variables);
    }
}