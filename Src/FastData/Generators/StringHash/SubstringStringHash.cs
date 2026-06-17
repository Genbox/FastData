using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record SubstringStringHash(ArraySegment Segment, bool IgnoreCase = false, int UnitSize = 1) : IStringHash
{
    private const ulong InitialSeed = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public AdditionalData[]? AdditionalData => null;
    public IEnumerable<IEarlyExit> GetMandatoryExits() => [new LengthLessThanEarlyExit(Segment.GetRequiredLength(UnitSize))];

    public Expression<StringHashFunc> GetExpression() => Segment.Length == UnitSize
        ? ExpressionHashBuilder.Build([Segment], static (_, read) => read, static h => h, IgnoreCase)
        : ExpressionHashBuilder.Build([Segment], Mixer, static h => h, IgnoreCase, InitialSeed);

    private static BinaryExpression Mixer(Expression hash, Expression read) => ExclusiveOr(Multiply(hash, Constant(Prime)), read);
}