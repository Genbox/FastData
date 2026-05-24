using Genbox.FastData.Config.Limits;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Config;

/// <summary>Controls which structures can be selected automatically and the limits used during selection.</summary>
public class StructureConfig
{
    private readonly HashSet<Type> _disabled = new HashSet<Type>();
    private readonly Dictionary<Type, List<ILimit>> _limits = new Dictionary<Type, List<ILimit>>();

    /// <summary>Gets the default structure selection limits used by FastData.</summary>
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
            cfg.AppendLimit(typeof(RangeStructure<,>), new ItemCountMinMaxLimit(1, 100));

            return cfg;
        }
    }

    /// <summary>Adds a selection limit for a structure type.</summary>
    /// <param name="type">The open generic structure type, such as <c>typeof(ConditionalStructure&lt;,&gt;)</c>.</param>
    /// <param name="limit">The limit to apply when the structure is considered.</param>
    public void AppendLimit(Type type, ILimit limit)
    {
        if (!_limits.TryGetValue(type, out List<ILimit> list))
            _limits[type] = list = new List<ILimit>();

        list.Add(limit);
    }

    /// <summary>Disables a structure so automatic selection will not choose it.</summary>
    /// <param name="structureType">The open generic structure type to disable.</param>
    public void Disable(Type structureType) => _disabled.Add(structureType);

    /// <summary>Checks whether a density value satisfies all density limits for a structure.</summary>
    /// <param name="type">The open generic structure type.</param>
    /// <param name="density">The density value to test.</param>
    /// <returns><see langword="true" /> when all configured density limits pass.</returns>
    public bool CheckDensityLimits(Type type, float density)
    {
        foreach (ValueDensityMinMaxLimit limit in GetLimitsOfType<ValueDensityMinMaxLimit>(type))
        {
            if (!limit.IsWithinLimit(density))
                return false;
        }

        return true;
    }

    /// <summary>Checks whether an item count satisfies all item-count limits for a structure.</summary>
    /// <param name="type">The open generic structure type.</param>
    /// <param name="itemCount">The number of keys in the dataset.</param>
    /// <returns><see langword="true" /> when all configured item-count limits pass.</returns>
    public bool CheckItemCountLimits(Type type, uint itemCount)
    {
        foreach (ItemCountMinMaxLimit limit in GetLimitsOfType<ItemCountMinMaxLimit>(type))
        {
            if (!limit.IsWithinLimit(itemCount))
                return false;
        }

        return true;
    }

    internal bool IsEnabled(Type structureType) => !_disabled.Contains(structureType);

    /// <summary>Needed to avoid mutating user's reference of the config. Internally FastData uses it to temporarily disable a structure or make other changes.</summary>
    internal StructureConfig Clone()
    {
        StructureConfig cfg = new StructureConfig();

        foreach (Type type in _disabled)
            cfg._disabled.Add(type);

        foreach (KeyValuePair<Type, List<ILimit>> pair in _limits)
            cfg._limits[pair.Key] = new List<ILimit>(pair.Value);

        return cfg;
    }

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