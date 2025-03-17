using Genbox.FastData.Abstracts;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Internal;

internal sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() {}
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public Func<string, uint> GetFunction() => HashHelper.HashObject;

    public string GetSource() => "HashHelper.HashObject(value)";
}