using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

public sealed class StringHashDetails(Expression expression, ReaderFunctions functions, State[]? state)
{
    public Expression Expression { get; } = expression;
    public ReaderFunctions Functions { get; } = functions;
    public State[]? State { get; } = state;
}