using Genbox.FastData.Config.Limits;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Config;

public class StructureConfig
{
    private readonly Dictionary<Type, List<ILimit>> _limits = new Dictionary<Type, List<ILimit>>();
    private readonly HashSet<Type> _disabled = new HashSet<Type>();

    public static StructureConfig Default
    {
        get
        {
            StructureConfig cfg = new StructureConfig();
            cfg.AppendLimit(typeof(BitSetStructure<,>), new ValueDensityMinMaxLimit(0, 0.5f));

            // Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
            cfg.AppendLimit(typeof(ConditionalStructure<,>), new ItemCountMinMaxLimit(0, 400));
            cfg.AppendLimit(typeof(KeyLengthStructure<,>), new ValueDensityMinMaxLimit(0.75f, 1));
            cfg.AppendLimit(typeof(RrrBitVectorStructure<,>), new ItemCountMinMaxLimit(512, uint.MaxValue));
            cfg.AppendLimit(typeof(RrrBitVectorStructure<,>), new ValueDensityMinMaxLimit(0, 0.015625f)); // 1 / 64
            cfg.AppendLimit(typeof(EliasFanoStructure<,>), new ItemCountMinMaxLimit(256, uint.MaxValue));
            cfg.AppendLimit(typeof(EliasFanoStructure<,>), new ValueDensityMinMaxLimit(0, 0.083333f)); // 1 / 12

            return cfg;
        }
    }

    public void AppendLimit(Type type, ILimit limit)
    {
        if (!_limits.TryGetValue(type, out List<ILimit> list))
            _limits[type] = list = new List<ILimit>();

        list.Add(limit);
    }

    public void Disable(Type structureType) => _disabled.Add(structureType);

    public bool CheckDensityLimits(Type type, float density)
    {
        foreach (ValueDensityMinMaxLimit limit in GetLimitsOfType<ValueDensityMinMaxLimit>(type))
            if (!limit.IsWithinLimit(density))
                return false;

        return true;
    }

    public bool CheckItemCountLimits(Type type, uint itemCount)
    {
        foreach (ItemCountMinMaxLimit limit in GetLimitsOfType<ItemCountMinMaxLimit>(type))
            if (!limit.IsWithinLimit(itemCount))
                return false;

        return true;
    }

    internal bool IsEnabled(Type structureType) => !_disabled.Contains(structureType);

    private IEnumerable<T> GetLimitsOfType<T>(Type type)
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