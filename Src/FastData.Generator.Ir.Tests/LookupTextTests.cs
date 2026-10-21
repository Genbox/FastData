using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupTextTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8, 3, 0xC3)]
    [InlineData(IrTextEncoding.Utf16, 2, 0xE9)]
    public void TextHelpersReadEncodedUnits(IrTextEncoding encoding, int length, int firstUnit)
    {
        IrLiteral Unit(int unit) => new IrText(encoding).Unit((uint)unit);
        IrLiteral Run(IrType type, Func<FastDataProgramBuilder, IrExpression> expression) => LookupTestPrograms.Evaluate(IrType.String, type, expression, encoding, LookupText.CreateAsciiFold);

        IrType unitType = Unit(0).Type;

        Assert.Equal(Constant(length), Run(IrType.I32, x => x.Text.Length(Constant("éA"))));
        Assert.Equal(Unit(firstUnit), Run(unitType, x => x.Text.UnitAt(Constant("éA"), Constant(0))));
        Assert.Equal(Constant(true), Run(IrType.Bool, x => x.Text.EqualsAt(Constant("éA"), Constant(length - 1), Constant("a"), x.Keys.FoldFunctionId)));
    }

    [Theory]
    [InlineData(IrTextEncoding.Utf8, false)]
    [InlineData(IrTextEncoding.Utf16, false)]
    [InlineData(IrTextEncoding.Utf8, true)]
    [InlineData(IrTextEncoding.Utf16, true)]
    public void SuffixHelpersMatchEmptyShortAndFoldedFragments(IrTextEncoding encoding, bool folded)
    {
        IrLiteral EndsWith(string value, string fragment) => LookupTestPrograms.Evaluate(IrType.String, IrType.Bool, x => x.Text.EndsWith(Constant(value), Constant(fragment), x.Keys.FoldFunctionId), encoding, folded ? LookupText.CreateAsciiFold : null);

        Assert.Equal(Constant(true), EndsWith("café", "é"));
        Assert.Equal(Constant(true), EndsWith("", ""));
        Assert.Equal(Constant(false), EndsWith("a", "ba"));
        Assert.Equal(Constant(false), EndsWith("abc", "ab"));
        Assert.Equal(Constant(folded), EndsWith("xxAB", "ab"));
    }
}