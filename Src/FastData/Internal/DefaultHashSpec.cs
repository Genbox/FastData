using Genbox.FastData.Abstracts;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Analysis.Analyzers;

namespace Genbox.FastData.Internal;

internal sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() {}
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public HashFunc GetHashFunction() => HashHelper.HashString;
    public EqualFunc GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);
}