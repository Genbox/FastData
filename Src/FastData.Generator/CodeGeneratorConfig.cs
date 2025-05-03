using JetBrains.Annotations;

namespace Genbox.FastData.Generator;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public abstract class CodeGeneratorConfig(bool useUTF16Encoding)
{
    public bool UseUTF16Encoding { get; } = useUTF16Encoding;
}