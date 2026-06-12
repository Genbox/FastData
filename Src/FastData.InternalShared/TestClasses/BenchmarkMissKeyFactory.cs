namespace Genbox.FastData.InternalShared.TestClasses;

internal static class BenchmarkMissKeyFactory<TKey>
{
    public static TKey Create(TKey key, HashSet<TKey> keys, int offset)
    {
        object value = key!;

        if (value is string str)
            return Cast(CreateStringMiss(str, keys, offset));

        if (value is float floatValue)
            return Cast(CreateFloatMiss(floatValue, keys, offset));

        if (value is double doubleValue)
            return Cast(CreateDoubleMiss(doubleValue, keys, offset));

        if (value is int intValue)
            return Cast(CreateIntMiss(intValue, keys, offset));

        if (value is long longValue)
            return Cast(CreateLongMiss(longValue, keys, offset));

        if (value is short shortValue)
            return Cast(CreateShortMiss(shortValue, keys, offset));

        if (value is byte byteValue)
            return Cast(CreateByteMiss(byteValue, keys, offset));

        if (value is uint uintValue)
            return Cast(CreateUIntMiss(uintValue, keys, offset));

        if (value is ulong ulongValue)
            return Cast(CreateULongMiss(ulongValue, keys, offset));

        throw new InvalidOperationException($"Benchmark miss generation does not support key type '{typeof(TKey)}'.");
    }

    private static TKey Cast<TValue>(TValue value) => (TKey)(object)value!;

    private static string CreateStringMiss(string value, HashSet<TKey> keys, int offset)
    {
        for (int i = 0; i < 24; i++)
        {
            char ch = (char)('b' + ((offset + i) % 24));
            string candidate = new string(ch, value.Length);
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        throw new InvalidOperationException("Could not create a same-length missing string key for the benchmark.");
    }

    private static float CreateFloatMiss(float value, HashSet<TKey> keys, int offset)
    {
        for (int i = 0; i < 8; i++)
        {
            float candidate = value + 0.5f + offset + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return -value - offset - 1;
    }

    private static double CreateDoubleMiss(double value, HashSet<TKey> keys, int offset)
    {
        for (int i = 0; i < 8; i++)
        {
            double candidate = value + 0.5d + offset + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return -value - offset - 1;
    }

    private static int CreateIntMiss(int value, HashSet<TKey> keys, int offset)
    {
        for (int i = 1; i <= 8 && value <= int.MaxValue - i; i++)
        {
            int candidate = value + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return value - offset - 1;
    }

    private static long CreateLongMiss(long value, HashSet<TKey> keys, int offset)
    {
        for (int i = 1; i <= 8 && value <= long.MaxValue - i; i++)
        {
            long candidate = value + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return value - offset - 1L;
    }

    private static short CreateShortMiss(short value, HashSet<TKey> keys, int offset)
    {
        for (int i = 1; i <= 8 && value <= short.MaxValue - i; i++)
        {
            short candidate = (short)(value + i);
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return (short)(value - offset - 1);
    }

    private static byte CreateByteMiss(byte value, HashSet<TKey> keys, int offset)
    {
        for (int i = 1; i <= 8 && value <= byte.MaxValue - i; i++)
        {
            byte candidate = (byte)(value + i);
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return (byte)(byte.MaxValue - offset);
    }

    private static uint CreateUIntMiss(uint value, HashSet<TKey> keys, int offset)
    {
        for (uint i = 1; i <= 8 && value <= uint.MaxValue - i; i++)
        {
            uint candidate = value + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return value > offset ? value - (uint)offset - 1U : uint.MaxValue - (uint)offset;
    }

    private static ulong CreateULongMiss(ulong value, HashSet<TKey> keys, int offset)
    {
        for (ulong i = 1; i <= 8 && value <= ulong.MaxValue - i; i++)
        {
            ulong candidate = value + i;
            if (!keys.Contains((TKey)(object)candidate))
                return candidate;
        }

        return value > (ulong)offset ? value - (ulong)offset - 1UL : ulong.MaxValue - (ulong)offset;
    }
}