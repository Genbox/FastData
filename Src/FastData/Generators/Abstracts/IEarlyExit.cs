using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for early exit strategies used by code generators.</summary>
public interface IEarlyExit
{
    ulong KeyspaceSize { get; }
    Expression GetExpression(ParameterExpression key);
    bool IsWorseThan(IEarlyExit other);
}