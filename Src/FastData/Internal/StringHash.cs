using System.Linq.Expressions;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal;

internal static class StringHash
{
    internal static StringHashFunc GetHashFunc(ReadOnlySpan<string> keys, StringKeyProperties props, GeneratorEncoding encoding, ILoggerFactory factory, StringAnalyzerConfig? config)
    {
        if (config == null)
            return DefaultStringHash.GetInstance(encoding).GetExpression().Compile();

        Candidate candidate = HashBenchmark.GetBestHash(keys, props, config, factory, encoding, true);
        Expression<StringHashFunc> expression = candidate.StringHash.GetExpression();

        // hashDetails.StringHash = new StringHashDetails(expression, candidate.StringHash.Functions, candidate.StringHash.State);

        return expression.Compile();
    }
}