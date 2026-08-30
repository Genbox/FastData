using System.Linq.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Tests;

public sealed class StringExpressionInfoTests
{
    private static readonly int[] _unrelatedValues = [1];

    [Fact]
    public void StringHashInfoRejectsNullLambda()
    {
        Expression<StringHashFunc> expression = null!;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new StringHashInfo(expression, null));

        Assert.Equal("expression", exception.ParamName);
    }

    [Fact]
    public void StringHashInfoAcceptsMatchingCapturedData()
    {
        int[] values = [1, 2];
        CapturedHashData source = new CapturedHashData(values);
        Expression<StringHashFunc> expression = CreateCapturedHashExpression(source);
        AdditionalData data = new AdditionalData(nameof(CapturedHashData.Values), typeof(int), values);

        StringHashInfo info = new StringHashInfo(expression, [data]);

        Assert.Same(expression, info.Expression);
        Assert.Same(data, Assert.Single(info.AdditionalData!));
    }

    [Fact]
    public void StringHashInfoRejectsCapturedDataWithoutMetadata()
    {
        CapturedHashData source = new CapturedHashData([1]);
        Expression<StringHashFunc> expression = CreateCapturedHashExpression(source);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new StringHashInfo(expression, null));

        Assert.Contains("has no matching", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StringHashInfoRejectsInconsistentOrDuplicateMetadata()
    {
        CapturedHashData source = new CapturedHashData([1]);
        Expression<StringHashFunc> expression = CreateCapturedHashExpression(source);
        AdditionalData inconsistent = new AdditionalData(nameof(CapturedHashData.Values), typeof(long), _unrelatedValues);
        AdditionalData first = new AdditionalData(nameof(CapturedHashData.Values), typeof(int), source.Values);
        AdditionalData second = new AdditionalData(nameof(CapturedHashData.Values), typeof(int), source.Values);

        ArgumentException inconsistentException = Assert.Throws<ArgumentException>(() => new StringHashInfo(expression, [inconsistent]));
        ArgumentException duplicateException = Assert.Throws<ArgumentException>(() => new StringHashInfo(expression, [first, second]));

        Assert.Contains("matching its element type", inconsistentException.Message, StringComparison.Ordinal);
        Assert.Contains("is duplicated", duplicateException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StringHashInfoRejectsMetadataForDifferentArrayInstance()
    {
        CapturedHashData source = new CapturedHashData([1]);
        Expression<StringHashFunc> expression = CreateCapturedHashExpression(source);
        AdditionalData data = new AdditionalData(nameof(CapturedHashData.Values), typeof(int), _unrelatedValues);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new StringHashInfo(expression, [data]));

        Assert.Contains("same array instance", exception.Message, StringComparison.Ordinal);
    }

    private static Expression<StringHashFunc> CreateCapturedHashExpression(CapturedHashData source)
    {
        ParameterExpression data = Expression.Parameter(typeof(byte[]), "data");
        ParameterExpression length = Expression.Parameter(typeof(int), "length");
        MemberExpression values = Expression.Property(Expression.Constant(source), nameof(CapturedHashData.Values));
        UnaryExpression result = Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(0)), typeof(ulong));
        return Expression.Lambda<StringHashFunc>(result, data, length);
    }

    private sealed class CapturedHashData(int[] values)
    {
        public int[] Values { get; } = values;
    }
}