using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Techniques.Heuristics;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Testbed;

internal static class Program
{
    private static readonly object[] Data = ["razzmatazz", "quizzified", "quizzifies", "zymeszymic", "buzzworthy", "vajazzling", "whizzingly", "jazzercise", "quizzeries", "quizziness", "squeezebox", "whizzbangs", "bumfuzzled", "dizzyingly", "showbizzes", "zizyphuses", "bumfuzzles", "buzzphrase", "schemozzle", "blizzardly", "kolkhoznik", "puzzlingly", "shemozzled", "zigzaggery", "zugzwanged", "belshazzar", "bemuzzling", "dazzlingly", "embezzling", "morbidezza", "pavonazzos", "puzzledoms", "schizziest", "schnozzles", "schnozzola", "shemozzles", "shimozzles", "shlemozzle", "zygomorphy", "bathmizvah", "bedazzling", "blizzarded", "chiffchaff", "embezzlers", "hazardizes", "mizzenmast", "passamezzo", "pizzicatos", "podzolized", "pozzolanic", "puzzlement", "schizotypy", "scuzzballs", "shockjocks", "sizzlingly", "unhouzzled", "zanthoxyls", "zigzagging", "blackjacks", "crackajack"];

    private static void Main()
    {
        StringProperties props = DataAnalyzer.GetStringProperties(Data);

        HashSetChainStructure code = new HashSetChainStructure();
        HeuristicAnalyzer analyzer = new HeuristicAnalyzer(Data, props, new HeuristicAnalyzerConfig(), code.RunSimulation);
        analyzer.Run();
    }
}