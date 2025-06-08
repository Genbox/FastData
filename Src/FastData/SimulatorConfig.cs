using JetBrains.Annotations;

namespace Genbox.FastData;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class SimulatorConfig
{
    public double CapacityFactor { get; set; } = 1.0;
}