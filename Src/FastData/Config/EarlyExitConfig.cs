using Genbox.FastData.Config.Limits;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Config;

/// <summary>Controls generation, filtering, and reduction of early-exit checks.</summary>
public class EarlyExitConfig
{
    private readonly HashSet<Type> _disabled = new HashSet<Type>();
    private readonly HashSet<StructureType> _disabledForStructure = new HashSet<StructureType>();
    private readonly Dictionary<Type, List<ILimit>> _limits = new Dictionary<Type, List<ILimit>>();

    /// <summary>Gets the default early-exit configuration.</summary>
    public static EarlyExitConfig Default
    {
        get
        {
            EarlyExitConfig cfg = new EarlyExitConfig();
            cfg.MinItemCount = 3;
            cfg.MaxCandidates = 4;
            cfg.Optimize = true;
            cfg.MinRejectionRatio = 0.05f;

            // These don't support early exits
            cfg.DisableForStructure(StructureType.Range);
            cfg.DisableForStructure(StructureType.SingleValue);

            cfg.AppendLimit(typeof(ValueBitMaskEarlyExit), new ValueDensityMinMaxLimit(0, 0.25f));
            cfg.AppendLimit(typeof(ValueLowByteBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.50f));
            cfg.AppendLimit(typeof(LengthBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.45f));
            cfg.AppendLimit(typeof(UnitAtBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.45f));

            return cfg;
        }
    }

    /// <summary>Gets or sets a value indicating whether generated early exits are disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the minimum item count required before early exits are considered.</summary>
    public uint MinItemCount { get; set; }

    /// <summary>Gets or sets the maximum number of generated early-exit candidates to keep.</summary>
    public int MaxCandidates { get; set; }

    /// <summary>When enabled, early exit expressions are optimized</summary>
    public bool Optimize { get; set; }

    /// <summary>Gets or sets the minimum fraction of the observed keyspace an early exit must reject to be kept.</summary>
    public float MinRejectionRatio { get; set; }

    /// <summary>Disables a specific early-exit type.</summary>
    /// <param name="earlyExitType">The early-exit type to disable.</param>
    public void DisableEarlyExit(Type earlyExitType) => _disabled.Add(earlyExitType);

    /// <summary>Disables generated early exits for a specific structure type.</summary>
    /// <param name="structureType">The structure type.</param>
    public void DisableForStructure(StructureType structureType) => _disabledForStructure.Add(structureType);

    /// <summary>Adds a limit for a specific early-exit type.</summary>
    /// <param name="type">The early-exit type.</param>
    /// <param name="limit">The limit to apply when the early exit is considered.</param>
    public void AppendLimit(Type type, ILimit limit)
    {
        if (!_limits.TryGetValue(type, out List<ILimit>? list))
            _limits[type] = list = new List<ILimit>();

        list.Add(limit);
    }

    internal bool CheckDensityLimits(Type earlyExitType, float density)
    {
        foreach (ValueDensityMinMaxLimit limit in GetLimitsOfType<ValueDensityMinMaxLimit>(earlyExitType))
        {
            if (!limit.IsWithinLimit(density))
                return false;
        }

        return true;
    }

    internal bool IsEarlyExitEnabled(Type earlyExitType) => !_disabled.Contains(earlyExitType);

    internal bool IsEnabledForStructure(StructureType structureType) => !_disabledForStructure.Contains(structureType);

    private IEnumerable<T> GetLimitsOfType<T>(Type type) where T : ILimit
    {
        if (_limits.TryGetValue(type, out List<ILimit>? limits))
        {
            foreach (ILimit limit in limits)
            {
                if (limit is T typed)
                    yield return typed;
            }
        }
    }
}