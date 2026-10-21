using System.Globalization;
using System.IO.Enumeration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.Ir.Harness.Runner;

/// <summary>Builds and executes generated IR correctness and benchmark programs.</summary>
public static class HarnessApplication
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

    /// <summary>Generates and runs the selected cases, returning zero when all succeed.</summary>
    public static async Task<int> RunAsync(string[] args, bool benchmark, params HarnessBackend[] backends)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(backends);

        if (args.Contains("--help", StringComparer.Ordinal))
        {
            Console.WriteLine($"--language {string.Join(',', backends.Select(x => x.Name))} --filter <glob> --list --emit-only --output <directory>");

            if (benchmark)
                Console.WriteLine("--workload hit|miss|mixed --target-ms 100 --warmups 3 --samples 10 [--invocations N]");

            return 0;
        }

        HarnessOptions options = HarnessOptions.Parse(args);
        HarnessBackend[] selected = SelectBackends(options.Languages, backends);

        HarnessCase[] cases = CaseCatalog.All().Where(x => FileSystemName.MatchesSimpleExpression(options.Filter, x.Name)).ToArray();
        if (cases.Length == 0)
            throw new ArgumentException("No cases match the filter.", nameof(args));

        if (options.List)
        {
            foreach (HarnessCase item in cases)
                Console.WriteLine(item.Name);

            return 0;
        }

        Directory.CreateDirectory(options.Output);
        Console.WriteLine("Artifacts: " + options.Output);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        int failed = 0;
        int completed = 0;
        await using DockerHarness docker = new DockerHarness();

        try
        {
            foreach (HarnessBackend backend in selected)
            {
                string language = backend.Name;
                string directory = Path.Combine(options.Output, language);
                Directory.CreateDirectory(directory);

                for (int index = 0; index < cases.Length; index++)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    HarnessCase item = cases[index];
                    string id = "case" + index;

                    try
                    {
                        await RunCaseAsync(docker, backend, item, directory, id, benchmark, options, cancellation.Token);
                        completed++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !cancellation.IsCancellationRequested)
                    {
                        failed++;
                        await Console.Error.WriteLineAsync($"FAIL {language} {item.Name}: {ex.Message}");
                        await File.WriteAllTextAsync(Path.Combine(directory, id + ".error.log"), ex.ToString(), cancellation.Token);
                    }
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }

        Console.WriteLine($"Completed: {completed}; failed: {failed}.");
        return failed == 0 ? 0 : 1;
    }

    // No --language selects every backend; otherwise names match case-insensitively, in the order given.
    private static HarnessBackend[] SelectBackends(string[] languages, HarnessBackend[] backends)
    {
        if (languages.Length == 0)
            return backends;

        return languages.Select(name => backends.FirstOrDefault(backend => string.Equals(backend.Name, name, StringComparison.OrdinalIgnoreCase))
                                        ?? throw new FormatException($"Unknown language '{name}'.")).ToArray();
    }

    private static async Task RunCaseAsync(DockerHarness docker, HarnessBackend backend, HarnessCase item, string directory, string id, bool benchmark, HarnessOptions options, CancellationToken cancellationToken)
    {
        string language = backend.Name;
        GeneratedHarness generated = item.Generate(backend, benchmark, options.Workload);
        string source = generated.Source.TrimEnd();
        await File.WriteAllTextAsync(Path.Combine(directory, id + backend.Extension), source, cancellationToken);

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        await SaveAsync(new
        {
            Case = item.Name,
            Language = language,
            backend.Image,
            benchmark,
            Workload = options.Workload.ToString().ToLowerInvariant(),
            SourceHash = hash,
            generated.QueryCount,
            generated.HitsPerCycle
        }, "manifest");

        if (options.EmitOnly)
            return;

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        await docker.BuildAsync(backend, directory, id, benchmark, timeout.Token);

        if (benchmark)
        {
            (long invocations, BenchmarkSample[] samples) = await BenchmarkMeasurement.RunAsync(docker, backend, directory, id, generated, options, timeout.Token);
            double mean = samples.Average(sample => sample.ElapsedNs / invocations);
            double overhead = samples.Average(sample => sample.OverheadNs / invocations);
            await SaveAsync(new { Case = item.Name, Language = language, Invocations = invocations, MeanNs = mean, BaselineNs = overhead, Samples = samples }, "results");
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{language} {item.Name}: {mean:F3} ns/op; baseline {overhead:F3} ns/op"));
        }
        else
        {
            // The test program's exit code is the first failed HarnessCheck.
            ProcessResult result = await docker.RunAsync(backend, directory, id, "", timeout.Token);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Harness check {(HarnessCheck)result.ExitCode} failed. {result.StandardError}");

            Console.WriteLine($"PASS {language} {item.Name}");
        }

        async Task SaveAsync<T>(T data, string suffix)
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(directory, id + "." + suffix + ".json"), json, cancellationToken);
        }
    }
}