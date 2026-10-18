using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Building;

internal sealed class IntegerStorageBuilder(IrProgramBuilder program, bool reduce)
{
    public (IrDataId Data, IrType Type) Unsigned(string name, ulong[] values, IrType fullType) => Unsigned(name, values, fullType, static x => x);
    public (IrDataId Data, IrType Type) Unsigned(string name, uint[] values, IrType fullType) => Unsigned(name, values, fullType, static x => x);
    public (IrDataId Data, IrType Type) Unsigned(string name, ushort[] values, IrType fullType) => Unsigned(name, values, fullType, static x => x);
    public (IrDataId Data, IrType Type) Unsigned(string name, int[] values, IrType fullType) => Unsigned(name, values, fullType, static x => checked((ulong)x));

    private (IrDataId Data, IrType Type) Unsigned<T>(string name, T[] values, IrType fullType, Func<T, ulong> select)
    {
        Type fullStorage = fullType.Kind switch
        {
            IrTypeKind.U32 => typeof(uint),
            IrTypeKind.U16 => typeof(ushort),
            _ => typeof(ulong)
        };
        return AddUnsigned(name, values, select, UnsignedType(values, select, fullStorage));
    }

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

    private (IrDataId Data, IrType Type) AddUnsigned<T>(string name, T[] values, Func<T, ulong> select, Type storage)
    {
        IrLiteral literal = Type.GetTypeCode(storage) switch
        {
            TypeCode.Byte => IrClrConverter.FromValue(values as byte[] ?? values.Select(x => checked((byte)select(x))).ToArray()),
            TypeCode.UInt16 => IrClrConverter.FromValue(values as ushort[] ?? values.Select(x => checked((ushort)select(x))).ToArray()),
            TypeCode.UInt32 => IrClrConverter.FromValue(values as uint[] ?? values.Select(x => checked((uint)select(x))).ToArray()),
            TypeCode.Int32 => IrClrConverter.FromValue(values as int[] ?? values.Select(x => checked((int)select(x))).ToArray()),
            _ => IrClrConverter.FromValue(values as ulong[] ?? values.Select(select).ToArray())
        };
        return (program.AddData(name, literal), IrClrConverter.FromClrType(storage));
    }

    public (IrDataId Data, IrType Type) NonNegative(string name, int[] values)
    {
        Type storage = UnsignedType(values, static x => checked((ulong)x), typeof(int));
        return AddUnsigned(name, values, static x => checked((ulong)x), storage);
    }

    public (IrDataId Data, IrType Type) Signed<T>(string name, T[] values, Func<T, int> select)
    {
        Type storage = SignedType(values, select);
        IrLiteral literal = Type.GetTypeCode(storage) switch
        {
            TypeCode.SByte => IrClrConverter.FromValue(values.Select(x => checked((sbyte)select(x))).ToArray()),
            TypeCode.Int16 => IrClrConverter.FromValue(values.Select(x => checked((short)select(x))).ToArray()),
            _ => IrClrConverter.FromValue(values.Select(select).ToArray())
        };
        return (program.AddData(name, literal), IrClrConverter.FromClrType(storage));
    }
}