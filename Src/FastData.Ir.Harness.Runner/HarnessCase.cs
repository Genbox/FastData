using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Harness.Runner;

internal abstract class HarnessCase(string name, StructureType structure)
{
    public string Name { get; } = name;
    public StructureType Structure { get; } = structure;
    public abstract GeneratedHarness Generate(HarnessBackend backend, bool benchmark, BenchmarkWorkload workload);
}

internal sealed class HarnessCase<TKey, TValue>(string name, StructureType structure, TKey[] keys, TKey[] misses, TValue[]? values = null, bool ignoreCase = false) : HarnessCase(name, structure)
{
    // Bloom filters may report false positives, so misses are neither checked nor counted exactly.
    private bool Approximate => Structure == StructureType.BloomFilter;

    public override GeneratedHarness Generate(HarnessBackend backend, bool benchmark, BenchmarkWorkload workload)
    {
        TKey[] queries = SelectQueries(workload);
        int hits = workload == BenchmarkWorkload.Miss ? 0 : keys.Length;
        string source = GenerateSource(new HarnessGenerator(this, backend, benchmark, queries));
        return new GeneratedHarness(source, queries.Length, hits, !Approximate);
    }

    private TKey[] SelectQueries(BenchmarkWorkload workload) => workload switch
    {
        BenchmarkWorkload.Hit => keys,
        BenchmarkWorkload.Miss => misses.Length > 0 ? misses : throw new InvalidOperationException($"{Name} has no miss queries."),
        BenchmarkWorkload.Mixed => misses.Length > 0 ? keys.Concat(misses).ToArray() : keys,
        _ => throw new ArgumentOutOfRangeException(nameof(workload), workload, null)
    };

    // Packs harness data with the prepared lookup's text encoding.
    private string RenderHarness(HarnessBackend backend, IrSourceEmitter emitter, bool benchmark, TKey[] queries)
    {
        if (benchmark)
            return backend.RenderBenchmark(emitter, Pack(queries));

        IrArrayLiteralBase? packedValues = values == null ? null : Pack(values);
        TKey[] missing = Approximate ? [] : misses;
        IrArrayLiteralBase? alternateKeys = ignoreCase ? Pack(((string[])(object)keys).Select(key => key.ToLowerInvariant()).ToArray()) : null;
        return backend.RenderTests(emitter, Pack(keys), packedValues, Pack(missing), alternateKeys);

        IrArrayLiteralBase Pack(Array array) => (IrArrayLiteralBase)IrClrConverter.FromValue(array, encoding: emitter.Lookup.Lookup.Program.TextEncoding);
    }

    private string GenerateSource(ICodeGenerator generator)
    {
        DataConfig config = keys is string[] ? new StringDataConfig { IgnoreCase = ignoreCase } : new NumericDataConfig();
        config.StructureTypeOverride = Structure;
        config.AllowApproximation = Approximate;
        config.EarlyExitConfig.Disabled = true;

        return config switch
        {
            StringDataConfig text when values == null => FastDataGenerator.Generate((string[])(object)keys, text, generator).Source,
            StringDataConfig text => FastDataGenerator.GenerateKeyed((string[])(object)keys, values, text, generator).Source,
            NumericDataConfig numeric when values == null => FastDataGenerator.Generate(keys, numeric, generator).Source,
            NumericDataConfig numeric => FastDataGenerator.GenerateKeyed(keys, values, numeric, generator).Source,
            _ => throw new InvalidOperationException()
        };
    }

    private sealed class HarnessGenerator(HarnessCase<TKey, TValue> owner, HarnessBackend backend, bool benchmark, TKey[] queries) : ICodeGenerator
    {
        private readonly IrCodeGenerator _generator = backend.Generator();
        public GeneratorEncoding Encoding => _generator.Encoding;

        public string Generate<TInputKey, TInputValue>(GeneratorConfigBase genCfg, IContext context)
        {
            IrSourceEmitter emitter = _generator.CreateEmitter<TInputKey, TInputValue>(genCfg, context);
            return SourceWriter.JoinLines([emitter.Emit(), owner.RenderHarness(backend, emitter, benchmark, queries)]);
        }
    }
}