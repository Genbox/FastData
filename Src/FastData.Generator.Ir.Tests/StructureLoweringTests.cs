using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Tests;

public class StructureLoweringTests
{
    [Theory]
    [InlineData(StructureType.Array)]
    [InlineData(StructureType.BinarySearch)]
    [InlineData(StructureType.BinarySearchInterpolation)]
    [InlineData(StructureType.BitSet)]
    [InlineData(StructureType.Conditional)]
    [InlineData(StructureType.HashTable)]
    [InlineData(StructureType.HashTableCompact)]
    [InlineData(StructureType.HashTablePerfect)]
    [InlineData(StructureType.Hyble)]
    [InlineData(StructureType.ConstMap)]
    [InlineData(StructureType.Pgm)]
    [InlineData(StructureType.SingleValue)]
    public void NumericStructuresPreserveValuesAndEnumeration(StructureType structure)
    {
        int[] keys = structure == StructureType.SingleValue ? [2] : [1, 2, 3, 8];
        string[] values = keys.Select(key => $"value{key}").ToArray();
        PreparingGenerator generator = new PreparingGenerator();
        FastDataGenerator.GenerateKeyed(keys, values, new NumericDataConfig { StructureTypeOverride = structure, EarlyExitConfig = new EarlyExitConfig { Disabled = true } }, generator);
        FastDataProgram lookup = generator.Lookup.Lookup;
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        for (int i = 0; i < keys.Length; i++)
        {
            int slot = ((IrI32Literal)interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(keys[i]))).Value;
            Assert.True(slot >= 0);
            Assert.Equal(new IrStringLiteral(values[i]), StoredValue(lookup, slot));
        }

        Assert.Equal(new IrI32Literal(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(0)));
        Assert.Equal(new IrI32Literal(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(4)));

        LookupEnumeration enumeration = lookup.Contract.Enumeration!;
        IrLiteral[] actualKeys = enumeration.EnumerateKeys(interpreter).ToArray();
        Assert.Equal(keys.Order(), actualKeys.Cast<IrI32Literal>().Select(key => key.Value).Order());
        Assert.Equal(actualKeys.Cast<IrI32Literal>().Select(key => new IrStringLiteral($"value{key.Value}")), enumeration.EnumerateValues(interpreter));
    }

    [Theory]
    [InlineData(StructureType.Range)]
    [InlineData(StructureType.EliasFano)]
    [InlineData(StructureType.RrrBitVector)]
    public void MembershipStructuresFindTheirKeys(StructureType structure)
    {
        int[] keys = [-7, -6, -5, 1, 8, 9, 30];
        PreparingGenerator generator = new PreparingGenerator(builtins: IrBuiltins.Required);
        FastDataGenerator.Generate(keys, new NumericDataConfig { StructureTypeOverride = structure, EarlyExitConfig = new EarlyExitConfig { Disabled = true } }, generator);
        FastDataProgram lookup = generator.Lookup.Lookup;
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        foreach (int key in keys)
            Assert.True(((IrI32Literal)interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(key))).Value >= 0);
        foreach (int key in new[] { -8, -4, 0, 7, 10, 31 })
            Assert.Equal(new IrI32Literal(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(key)));
    }

    [Fact]
    public void BloomFilterFindsEveryStoredKey()
    {
        int[] keys = [-7, -6, -5, 1, 8, 9, 30];
        PreparingGenerator generator = new PreparingGenerator(builtins: IrBuiltins.Required);
        FastDataGenerator.Generate(keys, new NumericDataConfig { StructureTypeOverride = StructureType.BloomFilter, EarlyExitConfig = new EarlyExitConfig { Disabled = true }, AllowApproximation = true }, generator);
        FastDataProgram lookup = generator.Lookup.Lookup;
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        foreach (int key in keys)
            Assert.True(((IrI32Literal)interpreter.Invoke(lookup.Contract.FindFunctionId, new IrI32Literal(key))).Value >= 0);
    }

    [Fact]
    public void RangesEnumerateEveryKeyInOrder()
    {
        int[] keys = [-7, -6, -5, 1, 8, 9, 30];
        PreparingGenerator generator = new PreparingGenerator(builtins: IrBuiltins.Required);
        FastDataGenerator.Generate(keys, new NumericDataConfig { StructureTypeOverride = StructureType.Range, EarlyExitConfig = new EarlyExitConfig { Disabled = true } }, generator);
        FastDataProgram lookup = generator.Lookup.Lookup;
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        Assert.Equal(keys.Select(key => new IrI32Literal(key)), lookup.Contract.Enumeration!.EnumerateKeys(interpreter));
    }

    [Theory]
    [InlineData(GeneratorEncoding.Utf8Bytes, StructureType.BinarySearch)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits, StructureType.BinarySearch)]
    [InlineData(GeneratorEncoding.Utf8Bytes, StructureType.HashTable)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits, StructureType.HashTable)]
    [InlineData(GeneratorEncoding.Utf8Bytes, StructureType.KeyLength)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits, StructureType.KeyLength)]
    public void FoldedStringStructuresPreserveValues(GeneratorEncoding encoding, StructureType structure)
    {
        string[] keys = structure == StructureType.KeyLength ? ["A", "bC", "_DE", "fGhI"] : ["A", "Z", "[", "_", "Bc", "HELLO"];
        int[] values = Enumerable.Range(0, keys.Length).ToArray();
        PreparingGenerator generator = new PreparingGenerator(encoding, IrBuiltins.Required);
        FastDataGenerator.GenerateKeyed(keys, values, new StringDataConfig { StructureTypeOverride = structure, EarlyExitConfig = new EarlyExitConfig { Disabled = true }, IgnoreCase = true }, generator);
        FastDataProgram lookup = generator.Lookup.Lookup;
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        for (int i = 0; i < keys.Length; i++)
        {
            int slot = ((IrI32Literal)interpreter.Invoke(lookup.Contract.FindFunctionId, new IrStringLiteral(keys[i].ToLowerInvariant()))).Value;
            Assert.True(slot >= 0);
            Assert.Equal(new IrI32Literal(i), StoredValue(lookup, slot));
        }

        Assert.Equal(new IrI32Literal(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, new IrStringLiteral("B")));
    }

    private static IrLiteral StoredValue(FastDataProgram lookup, int slot)
    {
        LookupValues storage = lookup.Contract.Values!;
        IrArrayLiteralBase array = (IrArrayLiteralBase)lookup.Program.Resolve(storage.DataId).Value;
        IrLiteral value = array.ElementAt(slot);
        return storage.FieldId is {} field ? ((IrRecordLiteral)value).Fields[field.Value] : value;
    }
}