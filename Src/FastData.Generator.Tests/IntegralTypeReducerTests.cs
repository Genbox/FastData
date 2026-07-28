using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.Tests;

public class IntegralTypeReducerTests
{
    [Theory]
    [InlineData(sbyte.MinValue, sbyte.MaxValue, typeof(sbyte))]
    [InlineData(-129L, sbyte.MaxValue, typeof(short))]
    [InlineData(sbyte.MinValue, 128L, typeof(short))]
    [InlineData(short.MinValue, short.MaxValue, typeof(short))]
    [InlineData(short.MinValue - 1L, short.MaxValue, typeof(int))]
    [InlineData(short.MinValue, short.MaxValue + 1L, typeof(int))]
    [InlineData(int.MinValue, int.MaxValue, typeof(int))]
    [InlineData(int.MinValue - 1L, int.MaxValue, typeof(long))]
    [InlineData(int.MinValue, int.MaxValue + 1L, typeof(long))]
    [InlineData(long.MinValue, long.MaxValue, typeof(long))]
    public void GetSmallestSignedStorageTypeRespectsExactBoundaries(long minValue, long maxValue, Type expected)
    {
        Assert.Equal(expected, IntegralTypeReducer.GetSmallestSignedStorageType(minValue, maxValue));
    }

    [Theory]
    [InlineData(0UL, typeof(byte))]
    [InlineData(byte.MaxValue, typeof(byte))]
    [InlineData(byte.MaxValue + 1UL, typeof(ushort))]
    [InlineData(ushort.MaxValue, typeof(ushort))]
    [InlineData(ushort.MaxValue + 1UL, typeof(uint))]
    [InlineData(uint.MaxValue, typeof(uint))]
    [InlineData(uint.MaxValue + 1UL, typeof(ulong))]
    [InlineData(ulong.MaxValue, typeof(ulong))]
    public void GetSmallestUnsignedStorageTypeRespectsExactBoundaries(ulong maxValue, Type expected)
    {
        Assert.Equal(expected, IntegralTypeReducer.GetSmallestUnsignedStorageType(maxValue));
    }

    [Fact]
    public void StorageTypeMethodsRejectReversedIntervals()
    {
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestSignedStorageType(1, 0));
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestStorageType(typeof(ulong), 1UL, 0UL));
    }

    [Theory]
    [MemberData(nameof(GetCompositeCases))]
    public void GetSmallestStorageTypePreservesSignedness(Type sourceType, object minValue, object maxValue, Type expected)
    {
        Assert.Equal(expected, IntegralTypeReducer.GetSmallestStorageType(sourceType, minValue, maxValue));
    }

    [Fact]
    public void GetSmallestStorageTypeRequiresExactBoxedTypes()
    {
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestStorageType(typeof(int), (short)0, 1));
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestStorageType(typeof(int), 0, 1L));
    }

    [Theory]
    [InlineData(typeof(float))]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(SampleEnum))]
    [InlineData(typeof(nint))]
    public void GetSmallestStorageTypeRejectsUnsupportedSource(Type sourceType)
    {
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestStorageType(sourceType, 0, 1));
    }

    [Fact]
    public void NonNegativeSignedArrayReducesToUnsignedType()
    {
        int[] values = [0, 255];

        Assert.Equal(typeof(byte), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(int), values));
    }

    [Fact]
    public void NegativeSignedArrayPreservesSourceType()
    {
        Assert.Equal(typeof(int), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(int), new[] { 0, -1 }));
    }

    [Fact]
    public void UnsignedArraysReduceToUnsignedTypes()
    {
        Assert.Equal(typeof(byte), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(uint), new uint[] { 0, 255 }));
        Assert.Equal(typeof(ushort), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(ulong), new ulong[] { 256, 65_535 }));
        Assert.Equal(typeof(byte), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(char), new[] { (char)0, (char)255 }));
    }

    [Fact]
    public void EmptyArrayPreservesSourceType()
    {
        Assert.Equal(typeof(long), IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(long), Array.Empty<long>()));
    }

    [Fact]
    public void ArrayElementTypeMustExactlyMatchSourceType()
    {
        Assert.Throws<ArgumentException>(() => IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(int), new long[] { 0, 1 }));
    }

    [Fact]
    public void NullArrayIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => IntegralTypeReducer.GetSmallestNonNegativeStorageType(typeof(int), null!));
    }

    public static TheoryData<Type, object, object, Type> GetCompositeCases() => new TheoryData<Type, object, object, Type>
    {
        { typeof(short), (short)-128, (short)127, typeof(sbyte) },
        { typeof(int), int.MinValue, int.MaxValue, typeof(int) },
        { typeof(ushort), (ushort)0, (ushort)255, typeof(byte) },
        { typeof(uint), 256U, 65_535U, typeof(ushort) },
        { typeof(char), (char)0, (char)255, typeof(byte) }
    };

    private enum SampleEnum
    {
        Value
    }
}