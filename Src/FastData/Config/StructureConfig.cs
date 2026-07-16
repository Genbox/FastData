using Genbox.FastData.Config.Limits;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Config;

/// <summary>Controls which structures can be selected automatically and the limits used during selection.</summary>
public class StructureConfig
{
    private readonly HashSet<StructureType> _disabled = new HashSet<StructureType>();
    private readonly Dictionary<StructureType, List<ILimit>> _limits = new Dictionary<StructureType, List<ILimit>>();

    /// <summary>Gets the default structure selection limits used by FastData.</summary>
    public static StructureConfig Default
    {
        get
        {
            StructureConfig cfg = new StructureConfig();

            // This representation scales with the observed span, so reserve automatic selection for dense data and let sparse data use compressed forms.
            cfg.AppendLimit(StructureType.BitSet, new ValueDensityMinMaxLimit(0.5f, 1));
            cfg.AppendLimit(StructureType.BitSet, new ValueMinMaxLimit<ulong>(0, int.MaxValue - 1UL));

            // Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
            cfg.AppendLimit(StructureType.Conditional, new ItemCountMinMaxLimit(0, 400));
            cfg.AppendLimit(StructureType.KeyLength, new ValueDensityMinMaxLimit(0.75f, 1));
            cfg.AppendLimit(StructureType.RrrBitVector, new ItemCountMinMaxLimit(512, uint.MaxValue));
            cfg.AppendLimit(StructureType.RrrBitVector, new ValueDensityMinMaxLimit(0, 0.015625f)); // 1 / 64

            // RRR stores one small entry per block, so cap automatic selection before sparse spans produce huge generated tables.
            cfg.AppendLimit(StructureType.RrrBitVector, new ValueMinMaxLimit<ulong>(0, (64UL * 1024UL * 1024UL) - 1UL));
            cfg.AppendLimit(StructureType.EliasFano, new ItemCountMinMaxLimit(256, uint.MaxValue));
            cfg.AppendLimit(StructureType.EliasFano, new ValueDensityMinMaxLimit(0, 0.083333f)); // 1 / 12
            cfg.AppendLimit(StructureType.Range, new ItemCountMinMaxLimit(1, 100));

            return cfg;
        }
    }

    /// <summary>Adds a selection limit for a structure type.</summary>
    /// <param name="type">The structure type.</param>
    /// <param name="limit">The limit to apply when the structure is considered.</param>
    public void AppendLimit(StructureType type, ILimit limit)
    {
        if (!_limits.TryGetValue(type, out List<ILimit> list))
            _limits[type] = list = new List<ILimit>();

        list.Add(limit);
    }

    /// <summary>Disables a structure so automatic selection will not choose it.</summary>
    /// <param name="structureType">The structure type to disable.</param>
    public void Disable(StructureType structureType) => _disabled.Add(structureType);

    /// <summary>Checks whether a density value satisfies all density limits for a structure.</summary>
    /// <param name="type">The structure type.</param>
    /// <param name="density">The density value to test.</param>
    /// <returns><see langword="true" /> when all configured density limits pass.</returns>
    public bool CheckDensityLimits(StructureType type, float density)
    {
        foreach (ValueDensityMinMaxLimit limit in GetLimitsOfType<ValueDensityMinMaxLimit>(type))
        {
            if (!limit.IsWithinLimit(density))
                return false;
        }

        return true;
    }

    /// <summary>Checks whether an item count satisfies all item-count limits for a structure.</summary>
    /// <param name="type">The structure type.</param>
    /// <param name="itemCount">The number of keys in the dataset.</param>
    /// <returns><see langword="true" /> when all configured item-count limits pass.</returns>
    public bool CheckItemCountLimits(StructureType type, uint itemCount)
    {
        foreach (ItemCountMinMaxLimit limit in GetLimitsOfType<ItemCountMinMaxLimit>(type))
        {
            if (!limit.IsWithinLimit(itemCount))
                return false;
        }

        return true;
    }

    /// <summary>Checks whether a value satisfies all value limits for a structure.</summary>
    /// <typeparam name="T">The value type to test.</typeparam>
    /// <param name="type">The structure type.</param>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when all configured value limits pass.</returns>
    public bool CheckValueLimits<T>(StructureType type, T value)
    {
        foreach (ValueMinMaxLimit<T> limit in GetLimitsOfType<ValueMinMaxLimit<T>>(type))
        {
            if (!limit.IsWithinLimit(value))
                return false;
        }

        return true;
    }

    internal bool IsEnabled(StructureType structureType, StructureCapability requiredCapabilities) => !_disabled.Contains(structureType) && StructureCapabilityHelper.Supports(structureType, requiredCapabilities);

    /// <summary>Needed to avoid mutating user's reference of the config. Internally FastData uses it to temporarily disable a structure or make other changes.</summary>
    internal StructureConfig Clone()
    {
        StructureConfig cfg = new StructureConfig();

        foreach (StructureType type in _disabled)
            cfg._disabled.Add(type);

        foreach (KeyValuePair<StructureType, List<ILimit>> pair in _limits)
            cfg._limits[pair.Key] = new List<ILimit>(pair.Value);

        return cfg;
    }

    private IEnumerable<T> GetLimitsOfType<T>(StructureType type)
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