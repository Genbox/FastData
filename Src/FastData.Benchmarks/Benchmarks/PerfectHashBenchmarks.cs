using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Order;
using Genbox.FastData.Benchmarks.Code;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class PerfectHashBenchmarks
{
    private static readonly string[] _words =
    [
        "Area", "Army", "Baby", "Back", "Ball", "Band", "Bank", "Base", "Bill", "Body", "Book", "Call", "Card",
        "Care", "Case", "Cash", "City", "Club", "Cost", "Date", "Deal", "Door", "Duty", "East", "Edge", "Face",
        "Fact", "Farm", "Fear", "File", "Film", "Fire", "Firm", "Fish", "Food", "Foot", "Form", "Fund", "Game",
        "Girl", "Goal", "Gold", "Hair", "Half", "Hall", "Hand", "Head", "Help", "Hill", "Home", "Hope", "Hour",
        "Idea", "Jack", "John", "Kind", "King", "Lack", "Lady", "Land", "Life", "Line", "List", "Look", "Lord",
        "Loss", "Love", "Mark", "Mary", "Mind", "Miss", "Move", "Name", "Need", "News", "Note", "Page", "Pain",
        "Pair", "Park", "Part", "Past", "Path", "Paul", "Plan", "Play", "Post", "Race", "Rain", "Rate", "Rest",
        "Rise", "Risk", "Road", "Rock", "Role", "Room", "Rule", "Sale", "Seat", "Shop", "Show", "Side", "Sign",
        "Site", "Size", "Skin", "Sort", "Star", "Step", "Task", "Team", "Term", "Test", "Text", "Time", "Tour",
        "Town", "Tree", "Turn", "Type", "Unit", "User", "View", "Wall", "Week", "West", "Wife", "Will", "Wind",
        "Wine", "Wood", "Word", "Work", "Year"
    ];
    private uint[] _data = null!;

    [Params(1)]
    public uint MaxCandidates { get; set; }

    [Params(8)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = TestHelper.GetIntegers(_words.OrderBy(_ => Random.Shared.Next()).Take(Length));
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstruct(MixSpec spec)
    {
        return PerfectHashHelper.Generate(_data, (obj, seed) => spec.Function(obj) ^ seed, MaxCandidates, uint.MaxValue, _data.Length * 2).ToArray();
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstructMinimal(MixSpec spec)
    {
        return PerfectHashHelper.Generate(_data, (obj, seed) => spec.Function(obj) ^ seed, MaxCandidates).ToArray();
    }

    public static IEnumerable<MixSpec> GetFunctions()
    {
        yield return new MixSpec(nameof(Mixers.Murmur_32), static obj => Mixers.Murmur_32((uint)obj));
        yield return new MixSpec(nameof(Mixers.XXH2_32), static obj => Mixers.Murmur_32((uint)obj));
    }
}