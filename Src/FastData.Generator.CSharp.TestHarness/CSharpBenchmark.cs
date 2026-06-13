using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBenchmark(DockerManager dockerManager) : BenchmarkBase<CSharpBootstrap>(new CSharpBootstrap(HarnessType.Benchmark), dockerManager)
{
    protected override string Render(ITestData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        BenchmarkQuerySet querySet = data.GetQuerySet(Bootstrap.Map);

        return $"""
                using System;
                using System.Diagnostics;
                using System.Globalization;

                {data.Generate(Bootstrap.Generator)}

                {Bootstrap.Wrap($$"""
                                        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
                                        static T BlackBox<T>(T value)
                                        {
                                            return value;
                                        }

                                        var keys = new[] { {{FormatList(querySet.Keys, s => s)}} };
                                        string[] args = Environment.GetCommandLineArgs();
                                        if (args.Length != 4)
                                            return 2;

                                        long invocationCount = long.Parse(args[1], CultureInfo.InvariantCulture);
                                        int warmupCount = int.Parse(args[2], CultureInfo.InvariantCulture);
                                        int sampleCount = int.Parse(args[3], CultureInfo.InvariantCulture);

                                        double TicksToNanoseconds(long ticks)
                                        {
                                            return ticks * 1_000_000_000d / Stopwatch.Frequency;
                                        }

                                        double MeasureBaseline(long invocations)
                                        {
                                            int keyIndex = 0;

                                            long startTicks = Stopwatch.GetTimestamp();

                                            for (long i = 0; i < invocations; i++)
                                            {
                                                BlackBox(keys[keyIndex]);
                                                keyIndex++;
                                                if (keyIndex == keys.Length)
                                                    keyIndex = 0;
                                            }

                                            return TicksToNanoseconds(Stopwatch.GetTimestamp() - startTicks);
                                        }

                                        double MeasureLookup(long invocations, out long foundCount)
                                        {
                                            int keyIndex = 0;
                                            foundCount = 0;

                                            long startTicks = Stopwatch.GetTimestamp();

                                            for (long i = 0; i < invocations; i++)
                                            {
                                                foundCount += FastData.Contains(BlackBox(keys[keyIndex])) ? 1 : 0;
                                                keyIndex++;
                                                if (keyIndex == keys.Length)
                                                    keyIndex = 0;
                                            }

                                            GC.KeepAlive(foundCount);

                                            return TicksToNanoseconds(Stopwatch.GetTimestamp() - startTicks);
                                        }

                                        for (int i = 0; i < warmupCount; i++)
                                        {
                                            GC.KeepAlive(MeasureBaseline(invocationCount));
                                        }

                                        for (int i = 0; i < warmupCount; i++)
                                        {
                                            GC.KeepAlive(MeasureLookup(invocationCount, out long warmupFoundCount));
                                            GC.KeepAlive(warmupFoundCount);
                                        }

                                        for (int i = 0; i < sampleCount; i++)
                                        {
                                            double overhead = MeasureBaseline(invocationCount);
                                            Console.WriteLine("overhead " + overhead.ToString("R", CultureInfo.InvariantCulture));

                                            double elapsed = MeasureLookup(invocationCount, out long sampleFoundCount);
                                            Console.WriteLine("sample " + elapsed.ToString("R", CultureInfo.InvariantCulture) + " " + sampleFoundCount.ToString(CultureInfo.InvariantCulture));
                                        }

                                        return 0;
                                  """)}
                """;
    }
}