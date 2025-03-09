using Genbox.FastData.Abstracts;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData;

public sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() {}
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public Func<string, uint> GetFunction() => HashHelper.HashObject;

    public string GetSource() => "HashHelper.HashObject(value)";
}