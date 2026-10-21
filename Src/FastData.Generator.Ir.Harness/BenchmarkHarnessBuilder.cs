using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Harness.HarnessBuilder;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Harness;

/// <summary>Builds the benchmark program: two query loops, a sampler that times both, and a driver that runs the samples.</summary>
internal static class BenchmarkHarnessBuilder
{
    private const string Queries = "queries";
    private const string Baseline = "Baseline";
    private const string Lookup = "Lookup";
    private const string Sample = "Sample";

    public static HarnessProgram Build(IrArrayLiteralBase queries)
    {
        HarnessProgram program = new HarnessProgram();
        program.Data.Add(new PreparedData(Queries, queries));
        program.Functions.Add(BuildKernel(queries.Count, queries.ElementType, false));
        program.Functions.Add(BuildKernel(queries.Count, queries.ElementType, true));
        program.Functions.Add(BuildSample());
        program.Functions.Add(BuildDriver());
        return program;
    }

    // Times the baseline loop (query load only) and then the lookup loop, and reports both when requested.
    private static PreparedFunction BuildSample()
    {
        HarnessFunction sample = new HarnessFunction(Sample, IrType.I64);
        IrVariableId invocations = sample.Parameter("invocations", IrType.I64);
        IrVariableId report = sample.Parameter("report", IrType.Bool);
        IrVariableId start = sample.Local("start", IrType.I64);
        IrVariableId count = sample.Local("count", IrType.I64);
        IrVariableId overhead = sample.Local("overhead", IrType.F64);
        IrVariableId elapsed = sample.Local("elapsed", IrType.F64);
        IrVariableId ignored = sample.Local("ignored", IrType.I64);

        return sample.Build(Block(Assign(start, Call(HarnessOperation.Clock)),
            Assign(count, new HarnessFunctionCall(Baseline, IrType.I64, [Read(invocations)])),
            Assign(overhead, Call(HarnessOperation.Elapsed, Read(start))),
            Assign(ignored, Call(HarnessOperation.Consume, Read(count))),
            Assign(start, Call(HarnessOperation.Clock)),
            Assign(count, new HarnessFunctionCall(Lookup, IrType.I64, [Read(invocations)])),
            Assign(elapsed, Call(HarnessOperation.Elapsed, Read(start))),
            Assign(ignored, Call(HarnessOperation.Consume, Read(count))),
            If(Read(report), Assign(ignored, Call(HarnessOperation.Report, Read(overhead), Read(elapsed), Read(count)))),
            Return(Read(count))));
    }

    // Runs unreported warmup samples, then reported samples.
    private static PreparedFunction BuildDriver()
    {
        HarnessFunction run = new HarnessFunction("Run", IrType.I32);
        IrVariableId invocations = run.Parameter("invocations", IrType.I64);
        IrVariableId warmups = run.Parameter("warmups", IrType.I64);
        IrVariableId samples = run.Parameter("samples", IrType.I64);
        IrVariableId i = run.Local("i", IrType.I64);
        IrVariableId result = run.Local("result", IrType.I64);

        return run.Build(Block(Assign(i, I64(0)),
            While(Less(Read(i), Read(warmups)), RunSample(false), Assign(i, Add(Read(i), I64(1)))),
            Assign(i, I64(0)),
            While(Less(Read(i), Read(samples)), RunSample(true), Assign(i, Add(Read(i), I64(1)))),
            Return(I32(0))));

        IrStatement RunSample(bool report) => Assign(result, new HarnessFunctionCall(Sample, IrType.I64, [Read(invocations), Bool(report)]));
    }

    // Cycles through the queries n times. The lookup kernel counts hits; the baseline only loads each query, measuring the loop overhead.
    private static PreparedFunction BuildKernel(int queryCount, IrType keyType, bool lookup)
    {
        HarnessFunction function = new HarnessFunction(lookup ? Lookup : Baseline, IrType.I64);
        IrVariableId n = function.Parameter("n", IrType.I64);
        IrVariableId i = function.Local("i", IrType.I64);
        IrVariableId index = function.Local("index", IrType.I32);
        IrVariableId count = function.Local("count", IrType.I64);
        IrVariableId key = function.Local("key", keyType);

        List<IrStatement> loop = [Assign(key, Call(HarnessOperation.Opaque, Index(new HarnessData(Queries, IrType.ArrayOf(keyType)), Read(index))))];
        if (lookup)
            loop.Add(If(Call(HarnessOperation.Contains, Read(key)), Assign(count, Add(Read(count), I64(1)))));
        loop.Add(Increment(index));
        loop.Add(If(Equal(Read(index), I32(queryCount)), Assign(index, I32(0))));
        loop.Add(Assign(i, Add(Read(i), I64(1))));

        return function.Build(Block(Assign(i, I64(0)),
            Assign(index, I32(0)),
            Assign(count, I64(0)),
            new IrLoopStatement(Less(Read(i), Read(n)), new IrBlock(loop)),
            Return(Read(count))));
    }
}