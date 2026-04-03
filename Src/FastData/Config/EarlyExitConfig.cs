using Genbox.FastData.Config.Limits;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Config;

public class EarlyExitConfig
{
    private readonly HashSet<Type> _disabled = new HashSet<Type>();
    private readonly HashSet<Type> _disabledForStructure = new HashSet<Type>();
    private readonly Dictionary<Type, List<ILimit>> _limits = new Dictionary<Type, List<ILimit>>();

    public static EarlyExitConfig Default
    {
        get
        {
            EarlyExitConfig cfg = new EarlyExitConfig();
            cfg.MinItemCount = 3;
            cfg.MaxCandidates = 4;
            cfg.ReductionEnabled = true;

            // These don't support early exits
            cfg.DisableForStructure(typeof(RangeStructure<,>));
            cfg.DisableForStructure(typeof(SingleValueStructure<,>));

            cfg.AppendLimit(typeof(ValueBitMaskEarlyExit), new ValueDensityMinMaxLimit(0, 0.25f));
            cfg.AppendLimit(typeof(LengthBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.45f));
            cfg.AppendLimit(typeof(CharFirstBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.45f));
            cfg.AppendLimit(typeof(CharLastBitmapEarlyExit), new ValueDensityMinMaxLimit(0, 0.45f));

            return cfg;
        }
    }

    public bool Disabled { get; set; }
    public uint MinItemCount { get; set; }
    public int MaxCandidates { get; set; }
    public bool ReductionEnabled { get; set; }

    public void DisableEarlyExit(Type earlyExitType) => _disabled.Add(earlyExitType);
    public void DisableForStructure(Type structureType) => _disabledForStructure.Add(structureType);

    public void AppendLimit(Type type, ILimit limit)
    {
        if (!_limits.TryGetValue(type, out List<ILimit> list))
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

    internal bool IsEnabledForStructure(Type structureType) => !_disabledForStructure.Contains(structureType);

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