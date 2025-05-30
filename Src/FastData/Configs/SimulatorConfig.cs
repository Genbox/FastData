using JetBrains.Annotations;

namespace Genbox.FastData.Configs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class SimulatorConfig
{
    public double CapacityFactor { get; set; } = 1.0;
}