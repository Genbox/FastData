using System.Text;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Testbed.Tests;

/// <summary>
/// This code enables verification of FastData against GPerf.
/// When DebugPrint is enabled and a special version of GPerf is run on the same files, it should give the same console text
/// </summary>
internal static class GPerfTest
{
    private static readonly Random _random = new Random(42);

    public static void ProduceOutputs(string path)
    {
        TextWriter org = Console.Out;

        foreach (string file in Directory.GetFiles(path))
        {
            if (!file.EndsWith(".txt", StringComparison.Ordinal))
                continue;

            FileStreamOptions options = new FileStreamOptions();
            options.Access = FileAccess.Write;
            options.Mode = FileMode.Create;

            using TextWriter tw = new StreamWriter(Path.Combine(path, "fastdata", Path.GetFileNameWithoutExtension(file) + ".output"), Encoding.Latin1, options);
            tw.NewLine = "\n";

            Console.SetOut(tw);

            object[] data = File.ReadAllLines(file).Cast<object>().ToArray();

            StructureConfig cfg = new StructureConfig(new DataProperties(data));
            PerfectHashGPerfStructure code = new PerfectHashGPerfStructure(cfg);

            try
            {
                code.TryCreate(data, out _);
            }
            catch
            {
                // Console.WriteLine("Error");
            }
        }

        Console.SetOut(org);
    }

    public static void MakeTestFiles(string path)
    {
        for (int i = 2; i <= 255; i++) //number of items
        {
            string filename = i + ".txt";

            string[] elements = new string[i];

            for (int k = 0; k < i; k++)
                elements[k] = TestHelper.GenerateRandomString(_random.Next(1, 10));

            using FileStream fs = File.OpenWrite(Path.Combine(path, filename));
            using StreamWriter tw = new StreamWriter(fs);
            tw.NewLine = "\n";

            foreach (string s in elements)
                tw.WriteLine(s);
        }
    }
}