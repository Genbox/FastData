using System.Diagnostics;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal;

internal static class StringTransform
{
    internal static string[] SubStringKeys(ReadOnlySpan<string> keys, StringKeyProperties props)
    {
        int prefix = props.DeltaData.Prefix.Length;
        int suffix = props.DeltaData.Suffix.Length;

        Debug.Assert(prefix > 0 || suffix > 0, "Don't call this method if there is nothing to trim");

        string[] modified = new string[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            modified[i] = key.Substring(prefix, key.Length - prefix - suffix);
        }

        return modified;
    }
}