using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Shared;

namespace Genbox.FastData.Generator.CSharp.Tests;

/// <summary>
/// These tests are designed to ensure that every supported data structure can be generated with different types
/// and that they are working as expected.
/// </summary>
[SuppressMessage("Usage", "xUnit1016:MemberData must reference a public member")]
public class DataStructureTests
{
    [Fact]
    public void OutputIsUniq()
    {
        FastDataConfig config = new FastDataConfig("MyData", ["item", "item"]);
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate(config, new CSharpCodeGenerator(new CSharpGeneratorConfig())));
    }

    [Theory, MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(DataStructure ds, object[] data, Func<CSharpGeneratorConfig, string>? configure)
    {
        //We need to make a defensive clone to avoid the generator from manipulating with the data
        FastDataConfig config = new FastDataConfig("MyData", (object[])data.Clone());

        CSharpGeneratorConfig config2 = new CSharpGeneratorConfig();
        config2.ClassType = ClassType.Instance;
        string? extra = configure?.Invoke(config2);

        string source = FastDataGenerator.Generate(ds, config, new CSharpCodeGenerator(config2));

        Assert.NotEmpty(source);

        KnownDataType dataType = config.GetDataType();

        File.WriteAllText($@"..\..\..\Generated\DataStructures\{ds}{extra}-{dataType}.output", source);

        if (dataType == KnownDataType.String)
        {
            IFastSet<string> set = CodeGenerator.CreateFastSet<string>(source, false);
            Assert.True(set.Contains((string)data[0]));
            Assert.False(set.Contains("dontexist"));
        }
        else if (dataType == KnownDataType.Int32)
        {
            IFastSet<int> set = CodeGenerator.CreateFastSet<int>(source, false);
            Assert.True(set.Contains((int)data[0]));
            Assert.False(set.Contains(100));
        }
    }

    internal static TheoryData<DataStructure, object[], Func<CSharpGeneratorConfig, string>?> GetDataStructures()
    {
        TheoryData<DataStructure, object[], Func<CSharpGeneratorConfig, string>?> data = new TheoryData<DataStructure, object[], Func<CSharpGeneratorConfig, string>?>();

        object[] single1 = ["item0"];
        object[] normal1 = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];

        //We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        object[] uniqueLength1 = ["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"];

        data.Add(DataStructure.SingleValue, single1, null);
        data.Add(DataStructure.Array, normal1, null);
        data.Add(DataStructure.BinarySearch, normal1, null);
        data.Add(DataStructure.EytzingerSearch, normal1, null);
        data.Add(DataStructure.Conditional, normal1, c =>
        {
            c.ConditionalBranchType = BranchType.If;
            return "If";
        });
        data.Add(DataStructure.Conditional, normal1, c =>
        {
            c.ConditionalBranchType = BranchType.Switch;
            return "Switch";
        });
        data.Add(DataStructure.MinimalPerfectHash, normal1, null);
        data.Add(DataStructure.HashSetChain, normal1, null);
        data.Add(DataStructure.HashSetLinear, normal1, null);
        data.Add(DataStructure.KeyLength, normal1, null);
        data.Add(DataStructure.KeyLength, uniqueLength1, c =>
        {
            c.KeyLengthUniqBranchType = BranchType.If;
            return "UniqIf";
        });
        data.Add(DataStructure.KeyLength, uniqueLength1, c =>
        {
            c.KeyLengthUniqBranchType = BranchType.Switch;
            return "UniqSwitch";
        });

        object[] normal2 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        object[] single2 = [42];

        // Not supported with integers:
        // - UniqueKeyLength
        // - UniqueKeyLengthSwitch
        // - KeyLength

        data.Add(DataStructure.SingleValue, single2, null);
        data.Add(DataStructure.Array, normal2, null);
        data.Add(DataStructure.BinarySearch, normal2, null);
        data.Add(DataStructure.EytzingerSearch, normal2, null);
        data.Add(DataStructure.Conditional, normal2, c =>
        {
            c.ConditionalBranchType = BranchType.If;
            return "If";
        });
        data.Add(DataStructure.Conditional, normal2, c =>
        {
            c.ConditionalBranchType = BranchType.Switch;
            return "Switch";
        });
        data.Add(DataStructure.MinimalPerfectHash, normal2, null);
        data.Add(DataStructure.HashSetChain, normal2, null);
        data.Add(DataStructure.HashSetLinear, normal2, null);

        return data;
    }
}