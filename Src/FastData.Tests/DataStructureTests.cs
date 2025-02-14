using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Optimization;
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
        Assert.Throws<InvalidOperationException>(() => CodeGenerator.DynamicCreateSet<FastDataGenerator>(["item", "item"], StorageMode.Linear, false));
    }

    [Theory, MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(string type, KnownDataType kt, DataStructure ds, object[] data)
    {
        FastDataSpec spec = new FastDataSpec();
        spec.Data = (object[])data.Clone(); //We need to make a defensive clone to avoid the generator from manipulating with the data
        spec.DataTypeName = type;
        spec.KnownDataType = kt;

        StringBuilder sb = new StringBuilder();
        Generator.Generate(ds, sb, spec);

        string source = sb.ToString();
        Assert.NotEmpty(source);

        File.WriteAllText($@"..\..\..\Generated\DataStructures\{ds}-{type}.output", source);

        if (kt == KnownDataType.String)
        {
            Func<string, bool> func = CodeGenerator.GetDelegate<Func<string, bool>>(source, true);
            Assert.True(func((string)data[0]));
            Assert.False(func("dontexist"));
        }
        else if (kt == KnownDataType.Int32)
        {
            Func<int, bool> func = CodeGenerator.GetDelegate<Func<int, bool>>(source, true);
            Assert.True(func((int)data[0]));
            Assert.False(func(100));
        }
    }

    internal static TheoryData<string, KnownDataType, DataStructure, object[]> GetDataStructures()
    {
        TheoryData<string, KnownDataType, DataStructure, object[]> data = new TheoryData<string, KnownDataType, DataStructure, object[]>();

        object[] normal1 = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
        object[] uniqueLength1 = ["a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"];
        object[] single1 = ["item0"];

        data.Add("string", KnownDataType.String, DataStructure.Array, normal1);
        data.Add("string", KnownDataType.String, DataStructure.BinarySearch, normal1);
        data.Add("string", KnownDataType.String, DataStructure.EytzingerSearch, normal1);
        data.Add("string", KnownDataType.String, DataStructure.Switch, normal1);
        data.Add("string", KnownDataType.String, DataStructure.SwitchHashCode, normal1);
        data.Add("string", KnownDataType.String, DataStructure.MinimalPerfectHash, normal1);
        data.Add("string", KnownDataType.String, DataStructure.HashSetChain, normal1);
        data.Add("string", KnownDataType.String, DataStructure.HashSetLinear, normal1);
        data.Add("string", KnownDataType.String, DataStructure.UniqueKeyLength, uniqueLength1);
        data.Add("string", KnownDataType.String, DataStructure.UniqueKeyLengthSwitch, uniqueLength1);
        data.Add("string", KnownDataType.String, DataStructure.KeyLength, normal1);
        data.Add("string", KnownDataType.String, DataStructure.SingleValue, single1);
        data.Add("string", KnownDataType.String, DataStructure.Conditional, normal1);

        object[] normal2 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        object[] single2 = [42];

        // Not supported with integers:
        // - UniqueKeyLength
        // - UniqueKeyLengthSwitch
        // - KeyLength

        data.Add("int", KnownDataType.Int32, DataStructure.Array, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.BinarySearch, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.EytzingerSearch, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.Switch, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.SwitchHashCode, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.MinimalPerfectHash, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.HashSetChain, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.HashSetLinear, normal2);
        data.Add("int", KnownDataType.Int32, DataStructure.SingleValue, single2);
        data.Add("int", KnownDataType.Int32, DataStructure.Conditional, normal2);

        return data;
    }
}