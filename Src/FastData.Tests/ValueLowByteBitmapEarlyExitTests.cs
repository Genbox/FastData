using System.Linq.Expressions;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Tests;

public class ValueLowByteBitmapEarlyExitTests
{
    private static readonly char[] SparseKeys = ['a', 'e', 'i', 'm', 'q', 'u', 'y', '}', '\u0081'];
    private static readonly int[] DenseValues = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    [Fact]
    public void RejectsMissingLowByteButKeepsCollisions()
    {
        ValueLowByteBitmapEarlyExit earlyExit = new ValueLowByteBitmapEarlyExit(0UL, 0b10UL, 0UL, 0UL);
        ParameterExpression key = Expression.Parameter(typeof(char), "key");
        Func<char, bool> compiled = Expression.Lambda<Func<char, bool>>(earlyExit.GetExpression(key), key).Compile();

        Assert.False(compiled('A'));
        Assert.False(compiled('\u0141'));
        Assert.True(compiled('B'));
    }

    [Fact]
    public void RejectsMissingLowByteForIntKeys()
    {
        ValueLowByteBitmapEarlyExit earlyExit = new ValueLowByteBitmapEarlyExit(1UL, 0UL, 0UL, 0UL);
        ParameterExpression key = Expression.Parameter(typeof(int), "key");
        Func<int, bool> compiled = Expression.Lambda<Func<int, bool>>(earlyExit.GetExpression(key), key).Compile();

        Assert.False(compiled(0));
        Assert.False(compiled(256));
        Assert.False(compiled(-256));
        Assert.True(compiled(1));
    }

    [Fact]
    public void AutoSelectionUsesConditionalWithLowByteEarlyExitForCharKeyedSparseSets()
    {
        NumericDataConfig config = new NumericDataConfig();
        CapturingGenerator generator = new CapturingGenerator();

        FastDataGenerator.GenerateKeyed(SparseKeys, DenseValues, config, generator);

        ConditionalContext<char, int> context = Assert.IsType<ConditionalContext<char, int>>(generator.Context);
        Assert.True(context.Keys.Span.SequenceEqual(SparseKeys));
        Assert.True(context.Values.Span.SequenceEqual(DenseValues));
        Assert.Equal("Conditional", generator.Config?.StructureName);
        Assert.NotEmpty(generator.Config?.EarlyExits ?? []);
    }

    private sealed class CapturingGenerator : ICodeGenerator
    {
        public IContext? Context { get; private set; }

        public GeneratorConfigBase? Config { get; private set; }
        public GeneratorEncoding Encoding => GeneratorEncoding.Utf16CodeUnits;

        public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
        {
            Config = genCfg;
            Context = context;
            return string.Empty;
        }
    }
}