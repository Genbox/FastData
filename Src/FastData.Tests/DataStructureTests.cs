using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Tests;

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
    internal void GenerateDataStructure(DataStructure ds, object[] data)
    {
        //We need to make a defensive clone to avoid the generator from manipulating with the data
        FastDataConfig config = new FastDataConfig("MyData", (object[])data.Clone());

        string source = FastDataGenerator.Generate(ds, config, new CSharpCodeGenerator(new CSharpGeneratorConfig()
        {
            ClassType = ClassType.Instance
        }));

        Assert.NotEmpty(source);

        KnownDataType dataType = config.GetDataType();

        File.WriteAllText($@"..\..\..\Generated\DataStructures\{ds}-{dataType}.output", source);

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

    internal static TheoryData<DataStructure, object[]> GetDataStructures()
    {
        TheoryData<DataStructure, object[]> data = new TheoryData<DataStructure, object[]>();

        object[] normal1 = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
        object[] uniqueLength1 = ["a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"];
        object[] single1 = ["item0"];

        data.Add(DataStructure.Array, normal1);
        data.Add(DataStructure.BinarySearch, normal1);
        data.Add(DataStructure.EytzingerSearch, normal1);
        data.Add(DataStructure.Switch, normal1);
        data.Add(DataStructure.MinimalPerfectHash, normal1);
        data.Add(DataStructure.HashSetChain, normal1);
        data.Add(DataStructure.HashSetLinear, normal1);
        data.Add(DataStructure.UniqueKeyLength, uniqueLength1);
        data.Add(DataStructure.UniqueKeyLengthSwitch, uniqueLength1);
        data.Add(DataStructure.KeyLength, normal1);
        data.Add(DataStructure.SingleValue, single1);
        data.Add(DataStructure.Conditional, normal1);

        object[] normal2 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        object[] single2 = [42];

        // Not supported with integers:
        // - UniqueKeyLength
        // - UniqueKeyLengthSwitch
        // - KeyLength

        data.Add(DataStructure.Array, normal2);
        data.Add(DataStructure.BinarySearch, normal2);
        data.Add(DataStructure.EytzingerSearch, normal2);
        data.Add(DataStructure.Switch, normal2);
        data.Add(DataStructure.MinimalPerfectHash, normal2);
        data.Add(DataStructure.HashSetChain, normal2);
        data.Add(DataStructure.HashSetLinear, normal2);
        data.Add(DataStructure.SingleValue, single2);
        data.Add(DataStructure.Conditional, normal2);

        return data;
    }
}