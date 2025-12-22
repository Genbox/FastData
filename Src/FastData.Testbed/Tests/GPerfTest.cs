using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Genbox.FastData.Testbed.Tests;

/// <summary>This code enables verification of FastData against GPerf. When Trace logging is enabled and a special version of GPerf is run on the same files, it should give the same console text</summary>
internal static class GPerfTest
{
    private static readonly Random _random = new Random(42);

    public static void ProduceOutputs(string path)
    {
        LoggerSinkConfiguration baseConf = new LoggerConfiguration()
                                           .MinimumLevel.Verbose()
                                           .WriteTo;

        foreach (string file in Directory.GetFiles(path))
        {
            if (!file.EndsWith(".txt", StringComparison.Ordinal))
                continue;

            FileStreamOptions options = new FileStreamOptions();
            options.Access = FileAccess.Write;
            options.Mode = FileMode.Create;

            string logFile = Path.Combine(path, "fastdata", Path.GetFileNameWithoutExtension(file) + ".output");

            Logger serilog = baseConf.File(logFile, formatProvider: CultureInfo.InvariantCulture).CreateLogger();
            using SerilogLoggerFactory factory = new SerilogLoggerFactory(serilog);

            string[] data = File.ReadAllLines(file);
            StringProperties props = KeyAnalyzer.GetStringProperties(data, false);

            GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, new GPerfAnalyzerConfig(), new Simulator(data.Length, GeneratorEncoding.UTF16), factory.CreateLogger<GPerfAnalyzer>());
            Candidate cand = analyzer.GetCandidates(data).First();
            StringHashFunc func = cand.StringHash.GetExpression().Compile();

            HashData hashData = HashData.Create(data, 1, x =>
            {
                byte[] bytes = Encoding.ASCII.GetBytes(x);
                return func(bytes, bytes.Length);
            });
            HashTablePerfectStructure<string, byte> structure = new HashTablePerfectStructure<string, byte>(hashData, KeyType.String);
            structure.Create(data, null);
        }
    }

    public static void MakeTestFiles(string path)
    {
        for (int i = 2; i <= 255; i++) //number of items
        {
            string filename = i + ".txt";

            string[] elements = new string[i];

            for (int k = 0; k < i; k++)
            {
                elements[k] = TestHelper.GenerateRandomString(_random, _random.Next(1, 10));
            }

            using FileStream fs = File.OpenWrite(Path.Combine(path, filename));
            using StreamWriter tw = new StreamWriter(fs);
            tw.NewLine = "\n";

            foreach (string s in elements)
            {
                tw.WriteLine(s);
            }
        }
    }
}