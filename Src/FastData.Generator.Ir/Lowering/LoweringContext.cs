using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.Lowering;

internal sealed class LoweringContext<TKey, TValue>
{
    public LoweringContext(GeneratorConfigBase config, IrBackendCapabilities capabilities)
    {
        Config = config;
        KeyType = IrClrConverter.FromClrType(typeof(TKey));
        bool ignoreCase = config is StringGeneratorConfig { IgnoreCase: true };
        Builder = new FastDataProgramBuilder(KeyType, capabilities.TextEncoding, ignoreCase ? LookupText.CreateAsciiFold : null, capabilities.SupportedBuiltins);
        Expressions = new FrontendExpressions(config, Builder, typeof(TKey), KeyType);
        Enumeration = new LookupEnumerationBuilder<TValue>(Builder, KeyType);
        Storage = new IntegerStorageBuilder(Builder.Program, config.TypeReductionEnabled);
    }

    public FastDataProgramBuilder Builder { get; }
    public FrontendExpressions Expressions { get; }
    public LookupEnumerationBuilder<TValue> Enumeration { get; }
    public IntegerStorageBuilder Storage { get; }
    public GeneratorConfigBase Config { get; }
    public IrType KeyType { get; }
    public IrDataId? KeyStorage { get; private set; }
    public IrDataId? ValueStorage { get; private set; }

    public IrExpression Key => Read(Builder.KeyParameterId);

    public void ExportConstants()
    {
        Builder.ExportConstant("ItemCount", I32(checked((int)Config.ItemCount)));

        if (Config is NumericGeneratorConfig numeric)
        {
            Builder.ExportConstant("MinKey", IrClrConverter.FromValue(numeric.Constants.MinValue));
            Builder.ExportConstant("MaxKey", IrClrConverter.FromValue(numeric.Constants.MaxValue));
        }

        if (Config is StringGeneratorConfig text)
        {
            Builder.ExportConstant("MinKeyLength", I32(text.Constants.MinStringLength));
            Builder.ExportConstant("MaxKeyLength", I32(text.Constants.MaxStringLength));
            Builder.ExportConstant("HasNumber", new IrBoolLiteral(text.Constants.CharacterClasses.HasFlag(CharacterClass.Number)));
            Builder.ExportConstant("HasUppercase", new IrBoolLiteral(text.Constants.CharacterClasses.HasFlag(CharacterClass.Uppercase)));
            Builder.ExportConstant("HasLowercase", new IrBoolLiteral(text.Constants.CharacterClasses.HasFlag(CharacterClass.Lowercase)));
            Builder.ExportConstant("HasSymbol", new IrBoolLiteral(text.Constants.CharacterClasses.HasFlag(CharacterClass.Symbol)));
            Builder.ExportConstant("HasWhitespace", new IrBoolLiteral(text.Constants.CharacterClasses.HasFlag(CharacterClass.Whitespace)));
        }
    }

    public IrDataId AddKeys(TKey[] keys, bool exposeStorage = true)
    {
        IrDataId id = Builder.Program.AddData("keys", IrClrConverter.FromValue(keys, encoding: Builder.Program.Text.Encoding));
        if (exposeStorage)
            KeyStorage = id;
        return id;
    }

    public void AddValues(ReadOnlyMemory<TValue> values, bool exposeStorage = true)
    {
        if (values.Length == 0)
            return;

        IrDataId id = Builder.Program.AddData("values", IrClrConverter.FromValue(GetArray(values), IrType.ArrayOf(ValueType()), Builder.Program.Text.Encoding));
        Builder.Values = new LookupValues(id);

        if (exposeStorage)
            ValueStorage = id;
    }

    public IrIndexExpression ValueAt(IrExpression position) => Index(Read(Builder.Values!.DataId), position);

    public IrType ValueType() => IrClrConverter.FromClrType(typeof(TValue));

    public ulong ToUnsigned(TKey value) => value switch
    {
        char x => x,
        sbyte x => unchecked((ulong)x),
        byte x => x,
        short x => unchecked((ulong)x),
        ushort x => x,
        int x => unchecked((ulong)x),
        uint x => x,
        long x => unchecked((ulong)x),
        ulong x => x,
        _ => throw new NotSupportedException($"'{typeof(TKey).Name}' keys are not integral.")
    };

    public IrFunctionCallExpression MultiplyHigh(IrExpression a, IrExpression b) => Builder.Builtins.Call(IrBuiltinOperation.MultiplyHigh, a, b);

    public IrDataId ArrayLookup(TKey[] keys, ReadOnlyMemory<TValue> values, bool sparse = false)
    {
        IrDataId data = AddKeys(keys, !sparse);
        AddValues(values, !sparse);

        if (sparse)
        {
            // Padding may repeat a real key; only its canonical lookup slot belongs to enumeration.
            IrExpression Occupied(IrExpression position)
            {
                IrExpression key = Index(Read(data), Int(position, IrType.I64));
                IrExpression canonical = Equal(Call(Builder.FindFunctionId, key), Int(position, IrType.I64));
                return KeyType == IrType.String ? AndAlso(NotEqual(key, new IrNullLiteral(IrType.String)), canonical) : canonical;
            }

            IrFunctionId next = Enumeration.AddNext(keys.Length - 1, Occupied);
            Enumeration.AddEnumeration(next, p => Index(Read(data), Int(p, IrType.I64)), Builder.Values == null ? null : p => ValueAt(Int(p, IrType.I64)));
        }
        else
            Enumeration.AddCursor(keys.Length, p => Index(Read(data), p), Builder.Values == null ? null : ValueAt);

        return data;
    }

    public Func<IrExpression, IrExpression, IrExpression> HashEntries(TKey[] keys, ulong[]? hashes, ReadOnlyMemory<TValue> values, bool sparse = false)
    {
        IrDataId data = ArrayLookup(keys, values, sparse);
        (IrDataId Data, IrType Type)? stored = hashes == null ? null : Storage.Unsigned("hashes", hashes, IrType.U64);
        return (index, hash) =>
        {
            if (stored is {} h)
                return AndAlso(Equal(Unsigned(Index(Read(h.Data), index), h.Type), hash), Builder.Keys.Equal(Index(Read(data), index), Key));

            return Builder.Keys.Equal(Index(Read(data), index), Key);
        };
    }
}