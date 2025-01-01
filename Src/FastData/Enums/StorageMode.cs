namespace Genbox.FastData.Enums;

public enum StorageMode : byte
{
    Auto = 0,
    Array,
    BinarySearch,
    EytzingerSearch,
    Switch,
    SwitchHashCode,
    MinimalPerfectHash,
    HashSet,
    UniqueKeyLength,
    UniqueKeyLengthSwitch,
    KeyLength,
    SingleValue,
    Conditional
}