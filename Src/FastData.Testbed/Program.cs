using System.Text;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Testbed;

internal static class Program
{
    // private static readonly object[] Data =  ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
    private static readonly Random _random = new Random(42);

    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string _outPath = @"C:\Temp\gperf-tests\";

    private static string GetString(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = _alphabet[_random.Next(_alphabet.Length)];

        return new string(chars);
    }

    private static void Main()
    {
        // MakeTestFiles();
        ProduceOutputs();
    }

    private static void ProduceOutputs()
    {
        foreach (string file in Directory.GetFiles(_outPath))
        {
            if (!file.EndsWith(".txt", StringComparison.Ordinal))
                continue;

            Console.Error.WriteLine(file);

            // if (!file.EndsWith("477.10.txt"))
                // continue;

            var options = new FileStreamOptions();
            options.Access = FileAccess.Write;
            options.Mode = FileMode.Create;

            using TextWriter tw = new StreamWriter(Path.Combine(_outPath, "fastdata", Path.GetFileNameWithoutExtension(file) + ".output"), Encoding.Latin1, options);
            tw.NewLine = "\n";

            Console.SetOut(tw);

            object[] data = File.ReadAllLines(file).Cast<object>().ToArray();

            StringProperties props = DataAnalyzer.GetStringProperties(data);
            PerfectHashGPerfStructure code = new PerfectHashGPerfStructure();
            try
            {
                code.TryCreate(data, KnownDataType.String, new DataProperties { StringProps = props }, new FastDataConfig("name", data), out _);
            }
            catch
            {
                // Console.WriteLine("Error");
            }
        }
    }

    private static void MakeTestFiles()
    {
        for (int i = 2; i <= 255; i++) //number of items
        {
            for (int j = 1; j <= 5; j++) //length of items
            {
                string filename = i + "." + j + ".txt";

                string[] elements = new string[i];

                for (int k = 0; k < i; k++)
                    elements[k] = GetString(j);

                using FileStream fs = File.OpenWrite(Path.Combine(_outPath, filename));
                using StreamWriter tw = new StreamWriter(fs);
                tw.NewLine = "\n";

                foreach (string s in elements)
                    tw.WriteLine(s);
            }
        }
    }
}