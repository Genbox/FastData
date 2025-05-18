using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PerfectHashBenchmarks
{
    private static ulong[] _hashCodes;

    [GlobalSetup]
    public void Setup()
    {
        string[] words =
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

        _hashCodes = words.Select(x => (ulong)x.GetHashCode()).ToArray();
    }

    [Benchmark]
    public uint TimeToConstruct()
    {
        return PerfectHashHelper.Generate(_hashCodes, (obj, seed) => Mixers.Murmur_64(obj) ^ seed, 1, (uint)(_hashCodes.Length * 2));
    }

    [Benchmark]
    public uint TimeToConstructMinimal()
    {
        return PerfectHashHelper.Generate(_hashCodes, (obj, seed) => Mixers.Murmur_64(obj) ^ seed, 1);
    }
}