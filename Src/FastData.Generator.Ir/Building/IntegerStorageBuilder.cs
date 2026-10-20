using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Building;

internal sealed class IntegerStorageBuilder(IrProgramBuilder program, bool reduce)
{
    public IrDataId Unsigned(string name, ulong[] values) => Unsigned(name, values, typeof(ulong), static x => x);
    public IrDataId Unsigned(string name, uint[] values) => Unsigned(name, values, typeof(uint), static x => x);
    public IrDataId Unsigned(string name, ushort[] values) => Unsigned(name, values, typeof(ushort), static x => x);
    public IrDataId Unsigned(string name, int[] values) => Unsigned(name, values, typeof(uint), static x => checked((ulong)x));

    public Type UnsignedType<T>(T[] values, Func<T, ulong> select, Type fullType)
    {
        if (!reduce)
            return fullType;

        ulong maximum = 0;
        foreach (T value in values)
            maximum = Math.Max(maximum, select(value));

        return IntegralTypeReducer.GetSmallestUnsignedStorageType(maximum);
    }

    public Type SignedType<T>(T[] values, Func<T, int> select)
    {
        if (!reduce)
            return typeof(int);

        int minimum = select(values[0]);
        int maximum = minimum;

        for (int i = 1; i < values.Length; i++)
        {
            int value = select(values[i]);
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        return IntegralTypeReducer.GetSmallestSignedStorageType(minimum, maximum);
    }

    private IrDataId Unsigned<T>(string name, T[] values, Type fullStorage, Func<T, ulong> select)
    {
        Type storage = UnsignedType(values, select, fullStorage);
        IrLiteral literal = Type.GetTypeCode(storage) switch
        {
            TypeCode.Byte => IrClrConverter.FromValue(values as byte[] ?? values.Select(x => checked((byte)select(x))).ToArray()),
            TypeCode.UInt16 => IrClrConverter.FromValue(values as ushort[] ?? values.Select(x => checked((ushort)select(x))).ToArray()),
            TypeCode.UInt32 => IrClrConverter.FromValue(values as uint[] ?? values.Select(x => checked((uint)select(x))).ToArray()),
            TypeCode.Int32 => IrClrConverter.FromValue(values as int[] ?? values.Select(x => checked((int)select(x))).ToArray()),
            _ => IrClrConverter.FromValue(values as ulong[] ?? values.Select(select).ToArray())
        };
        return program.AddData(name, literal);
    }

    public IrDataId NonNegative(string name, int[] values) => Unsigned(name, values, typeof(int), static x => checked((ulong)x));

    public IrDataId Signed<T>(string name, T[] values, Func<T, int> select)
    {
        Type storage = SignedType(values, select);
        IrLiteral literal = Type.GetTypeCode(storage) switch
        {
            TypeCode.SByte => IrClrConverter.FromValue(values.Select(x => checked((sbyte)select(x))).ToArray()),
            TypeCode.Int16 => IrClrConverter.FromValue(values.Select(x => checked((short)select(x))).ToArray()),
            _ => IrClrConverter.FromValue(values.Select(select).ToArray())
        };
        return program.AddData(name, literal);
    }
}