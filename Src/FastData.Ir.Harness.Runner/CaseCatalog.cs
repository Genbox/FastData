using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Ir.Harness.Runner;

internal static class CaseCatalog
{
    public static IEnumerable<HarnessCase> All()
    {
        foreach (ITestVector vector in TestVectorHelper.GetValueTestVectors())
            yield return Membership((dynamic)vector);

        foreach (ITestVector vector in TestVectorHelper.GetKeyValueTestVectors())
        {
            Type valueType = vector.GetType().GetGenericArguments()[1];
            if (valueType.IsPrimitive || valueType == typeof(string))
                yield return Keyed((dynamic)vector);
        }

        foreach (StructureType structure in new[] { StructureType.Array, StructureType.BinarySearch, StructureType.HashTable })
            yield return new HarnessCase<string, string>("Utf8Values_" + structure, structure, ["plain", "é", "😀", "a\0b"], ["x", "è"], ["empty", "accent", "emoji", "zero"]);
        foreach (StructureType structure in new[] { StructureType.BinarySearch, StructureType.HashTable })
            yield return new HarnessCase<string, int>("Folded_" + structure, structure, ["A", "Z", "[", "_", "aB"], ["!", "missing"], [1, 2, 3, 4, 5], true);
    }

    private static HarnessCase<TKey, byte> Membership<TKey>(TestVector<TKey> vector) => new HarnessCase<TKey, byte>("Membership_" + vector.Identifier, vector.StructureType, vector.Keys, vector.NotPresent);
    private static HarnessCase<TKey, TValue> Keyed<TKey, TValue>(TestVector<TKey, TValue> vector) => new HarnessCase<TKey, TValue>("Keyed_" + vector.Identifier + "_" + typeof(TValue).Name, vector.StructureType, vector.Keys, vector.NotPresent, vector.Values);
}