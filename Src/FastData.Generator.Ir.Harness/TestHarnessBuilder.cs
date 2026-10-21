using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Harness.HarnessBuilder;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Harness;

internal static class TestHarnessBuilder
{
    private static IrReturnStatement Result(HarnessCheck check) => Return(I32((int)check));

    public static HarnessProgram Build(PreparedLookup lookup, IrArrayLiteralBase keys, IrArrayLiteralBase? values, IrArrayLiteralBase misses, IrArrayLiteralBase? alternateKeys = null)
    {
        HarnessProgram program = new HarnessProgram();
        program.Data.Add(new PreparedData("expectedKeys", keys));
        program.Data.Add(new PreparedData("missingKeys", misses));

        if (values != null)
            program.Data.Add(new PreparedData("expectedValues", values));

        HarnessFunction function = new HarnessFunction("Run", IrType.I32);
        IrVariableId expectedIndex = function.Local("i", IrType.I32);
        IrVariableId actualIndex = function.Local("j", IrType.I32);
        IrVariableId matches = function.Local("matches", IrType.I32);
        List<IrStatement> body = [];
        IrExpression key = Index(new HarnessData("expectedKeys", keys.Type), Read(expectedIndex));
        IrExpression? value = values == null ? null : Index(new HarnessData("expectedValues", values.Type), Read(expectedIndex));

        AddPresentChecks();
        AddAlternateChecks();
        AddMissingChecks();
        AddEnumerationChecks();
        AddKeySpanChecks();
        AddValueSpanChecks();

        body.Add(Result(HarnessCheck.Success));
        program.Functions.Add(function.Build(new IrBlock(body)));
        return program;

        void AddPresentChecks()
        {
            List<IrStatement> present = [If(Not(Call(HarnessOperation.Contains, key)), Result(HarnessCheck.MissingKey))];
            if (values != null)
                present.Add(If(Not(Call(HarnessOperation.LookupMatches, key, value!)), Result(HarnessCheck.LookupValueMismatch)));
            AddLoop(body, expectedIndex, keys.Count, present);
        }

        void AddAlternateChecks()
        {
            if (alternateKeys == null)
                return;

            program.Data.Add(new PreparedData("alternateKeys", alternateKeys));
            IrExpression alternate = Index(new HarnessData("alternateKeys", alternateKeys.Type), Read(expectedIndex));
            List<IrStatement> folded = [If(Not(Call(HarnessOperation.Contains, alternate)), Result(HarnessCheck.AlternateKeyMissing))];

            if (values != null)
                folded.Add(If(Not(Call(HarnessOperation.LookupMatches, alternate, value!)), Result(HarnessCheck.AlternateLookupValueMismatch)));

            AddLoop(body, expectedIndex, keys.Count, folded);
        }

        void AddMissingChecks()
        {
            IrExpression miss = Index(new HarnessData("missingKeys", misses.Type), Read(expectedIndex));
            List<IrStatement> absent = [If(Call(HarnessOperation.Contains, miss), Result(HarnessCheck.UnexpectedKey))];

            if (values != null)
                absent.Add(If(Not(Call(HarnessOperation.LookupMiss, miss)), Result(HarnessCheck.UnexpectedLookup)));

            AddLoop(body, expectedIndex, misses.Count, absent);
        }

        void AddEnumerationChecks()
        {
            if (lookup.Lookup.Contract.Enumeration == null)
                return;

            body.Add(If(NotEqual(Call(HarnessOperation.KeyCount), I32(keys.Count)), Result(HarnessCheck.KeyCountMismatch)));

            if (values != null)
                body.Add(If(NotEqual(Call(HarnessOperation.ValueCount), I32(keys.Count)), Result(HarnessCheck.ValueCountMismatch)));

            List<IrStatement> match = [Assign(matches, Add(Read(matches), I32(1)))];
            if (values != null)
                match.Add(If(Not(Equal(Call(HarnessOperation.ValueAt, values.ElementType, Read(actualIndex)), value!)), Result(HarnessCheck.EnumerationValueMismatch)));

            List<IrStatement> find = [Assign(matches, I32(0))];
            AddLoop(find, actualIndex, keys.Count, [new IrIfStatement(Equal(Call(HarnessOperation.KeyAt, keys.ElementType, Read(actualIndex)), key), new IrBlock(match))]);
            find.Add(If(NotEqual(Read(matches), I32(1)), Result(HarnessCheck.EnumerationKeyMismatch)));
            AddLoop(body, expectedIndex, keys.Count, find);
        }

        void AddKeySpanChecks()
        {
            if (lookup.KeyStorage == null)
                return;

            body.Add(If(NotEqual(Call(HarnessOperation.KeySpanCount), I32(keys.Count)), Result(HarnessCheck.KeySpanCountMismatch)));
            List<IrStatement> find = [Assign(matches, I32(0))];
            AddLoop(find, actualIndex, keys.Count, [If(Equal(Call(HarnessOperation.KeySpanAt, keys.ElementType, Read(actualIndex)), key), Assign(matches, Add(Read(matches), I32(1))))]);
            find.Add(If(NotEqual(Read(matches), I32(1)), Result(HarnessCheck.KeySpanContentMismatch)));
            AddLoop(body, expectedIndex, keys.Count, find);
        }

        void AddValueSpanChecks()
        {
            if (lookup.ValueStorage == null || values == null)
                return;

            body.Add(If(NotEqual(Call(HarnessOperation.ValueSpanCount), I32(values.Count)), Result(HarnessCheck.ValueSpanCountMismatch)));
            IrVariableId expectedMatches = function.Local("expectedMatches", IrType.I32);
            List<IrStatement> find = [Assign(matches, I32(0)), Assign(expectedMatches, I32(0))];

            AddLoop(find, actualIndex, values.Count,
            [
                If(Equal(Call(HarnessOperation.ValueSpanAt, values.ElementType, Read(actualIndex)), value!), Assign(matches, Add(Read(matches), I32(1)))),
                If(Equal(Index(new HarnessData("expectedValues", values.Type), Read(actualIndex)), value!), Assign(expectedMatches, Add(Read(expectedMatches), I32(1))))
            ]);

            find.Add(If(NotEqual(Read(matches), Read(expectedMatches)), Result(HarnessCheck.ValueSpanContentMismatch)));
            AddLoop(body, expectedIndex, values.Count, find);

            if (lookup.KeyStorage != null)
            {
                IrExpression spanKey = Call(HarnessOperation.KeySpanAt, keys.ElementType, Read(expectedIndex));
                IrExpression spanValue = Call(HarnessOperation.ValueSpanAt, values.ElementType, Read(expectedIndex));
                IrExpression pairMatches = Call(HarnessOperation.LookupMatches, spanKey, spanValue);
                AddLoop(body, expectedIndex, keys.Count, [If(Not(pairMatches), Result(HarnessCheck.SpanPairMismatch))]);
            }
        }
    }
}