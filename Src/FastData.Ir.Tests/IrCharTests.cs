using System.Globalization;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrCharTests
{
    [Fact]
    public void CharacterTypesAreDistinctPerKindAndNotNumeric()
    {
        Assert.NotEqual(IrType.Char(IrCharacterKind.Ascii), IrType.Char(IrCharacterKind.Utf16CodeUnit));
        Assert.False(IrType.Char(IrCharacterKind.Utf16CodeUnit).IsNumeric);
        Assert.Equal(new IrCharLiteral(IrCharacterKind.Utf16CodeUnit, 0), IrFactory.Default(IrType.Char(IrCharacterKind.Utf16CodeUnit)));
    }

    [Theory]
    [InlineData(IrCharacterKind.Ascii, 0x41u)]
    [InlineData(IrCharacterKind.Utf16CodeUnit, 0xDC00u)]
    public void CharactersConvertToAndFromTheirUnitType(IrCharacterKind characterKind, uint value)
    {
        IrType charType = IrType.Char(characterKind);
        IrType unitType = IrType.UnitOf(characterKind);
        IrCharLiteral character = new IrCharLiteral(characterKind, value);
        IrLiteral unit = Evaluate(unitType, Convert(unitType, character));

        Assert.Equal(VectorLiteral(unitType, value.ToString(CultureInfo.InvariantCulture)), unit);
        Assert.Equal(character, Evaluate(charType, Convert(charType, unit)));
    }

    [Theory]
    [InlineData(IrCharacterKind.Utf16CodeUnit, 0x61u, 0x62u, true)]
    [InlineData(IrCharacterKind.Ascii, 0x7Au, 0x41u, false)]
    public void CharactersOrderByUnitValue(IrCharacterKind characterKind, uint left, uint right, bool less)
    {
        IrExpression comparison = Less(new IrCharLiteral(characterKind, left), new IrCharLiteral(characterKind, right));
        Assert.Equal(Constant(less), Evaluate(IrType.Bool, comparison));
    }

    [Fact]
    public void PackedAsciiArraysPreserveCharacterValues()
    {
        IrLiteral ascii = IrClrConverter.FromValue(new byte[] { 0x61 }, IrType.ArrayOf(IrType.Char(IrCharacterKind.Ascii)));
        Assert.Equal(new IrCharLiteral(IrCharacterKind.Ascii, 0x61), Evaluate(IrType.Char(IrCharacterKind.Ascii), Index(ascii, Constant(0))));
    }
}