using Genbox.FastData.Abstracts;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Analysis.Analyzers;

namespace Genbox.FastData.Internal;

internal sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() {}
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public HashFunc GetHashFunction() => HashHelper.HashObject;
    public EqualFunc GetEqualFunction() => (a, b) => a.Equals(b, StringComparison.Ordinal);

    public string GetSource() => "HashHelper.HashObject(value)";
}