using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Lowering;

internal sealed class LoweringContext<TKey, TValue>
{
    public LoweringContext(GeneratorConfigBase config, IrBackendCapabilities capabilities)
    {
        Config = config;
        IrType keyType = IrClrConverter.FromClrType(typeof(TKey));
        bool ignoreCase = config is StringGeneratorConfig { IgnoreCase: true };
        Builder = new FastDataProgramBuilder(keyType, capabilities.TextEncoding, ignoreCase ? LookupText.CreateAsciiFold : null, capabilities.SupportedBuiltins);
        Expressions = new FrontendExpressions(config, Builder, typeof(TKey));
        Enumeration = new LookupEnumerationBuilder(Builder);
        Storage = new IntegerStorageBuilder(Builder.Program, config.TypeReductionEnabled);
    }

    public FastDataProgramBuilder Builder { get; }
    public FrontendExpressions Expressions { get; }
    public LookupEnumerationBuilder Enumeration { get; }
    public IntegerStorageBuilder Storage { get; }
    public GeneratorConfigBase Config { get; }
    public IrType KeyType => Builder.KeyParameterId.Type;
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

        IrDataId id = Builder.Program.AddData("values", IrClrConverter.FromValue(GetArray(values), encoding: Builder.Program.Text.Encoding));
        Builder.Values = new LookupValues(id);

        if (exposeStorage)
            ValueStorage = id;
    }

    public IrIndexExpression ValueAt(IrExpression position) => Index(Read(Builder.Values!.DataId), position);

    public IrType ValueType() => IrClrConverter.FromClrType(typeof(TValue));

    public IrFunctionCallExpression MultiplyHigh(IrExpression a, IrExpression b) => Builder.Builtins.Call(IrBuiltinOperation.MultiplyHigh, a, b);

    public IrDataId ArrayLookup(TKey[] keys, ReadOnlyMemory<TValue> values, bool sparse = false)
    {
        IrDataId data = AddKeys(keys, !sparse);
        AddValues(values, !sparse);

        IrExpression KeyAt(IrExpression position) => Index(Read(data), position);

        // Padding may repeat a real key; only its canonical lookup slot belongs to enumeration.
        IrExpression Occupied(IrExpression position)
        {
            IrExpression key = KeyAt(position);
            IrExpression canonical = Equal(Call(Builder.FindFunctionId, key), position);
            return KeyType == IrType.String ? AndAlso(NotEqual(key, new IrNullLiteral(IrType.String)), canonical) : canonical;
        }

        Enumeration.AddCursor(keys.Length, KeyAt, Builder.Values == null ? null : ValueAt, sparse ? Occupied : null);

        return data;
    }

    public Func<IrExpression, IrExpression, IrExpression> SparseHashEntries(KeyValuePair<TKey, ulong>[] entries, bool storeHashCode, ReadOnlyMemory<TValue> values)
    {
        TKey[] keys = new TKey[entries.Length];
        ulong[]? hashes = storeHashCode ? new ulong[entries.Length] : null;

        for (int i = 0; i < entries.Length; i++)
        {
            keys[i] = entries[i].Key;
            if (hashes != null)
                hashes[i] = entries[i].Value;
        }

        return HashEntries(keys, hashes, values, true);
    }

    public Func<IrExpression, IrExpression, IrExpression> HashEntries(TKey[] keys, ulong[]? hashes, ReadOnlyMemory<TValue> values, bool sparse = false)
    {
        IrDataId data = ArrayLookup(keys, values, sparse);
        IrDataId? stored = hashes == null ? null : Storage.Unsigned("hashes", hashes);
        return (index, hash) =>
        {
            IrExpression equal = Builder.Keys.Equal(Index(Read(data), index), Key);
            return stored is {} h ? AndAlso(Equal(Unsigned(Index(Read(h), index)), hash), equal) : equal;
        };
    }
}