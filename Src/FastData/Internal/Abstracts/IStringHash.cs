using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    AdditionalData[]? AdditionalData { get; }
    IEnumerable<IEarlyExit> GetMandatoryExits();
    Expression<StringHashFunc> GetExpression();
}