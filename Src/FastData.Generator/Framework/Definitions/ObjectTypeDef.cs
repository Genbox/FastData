using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class ObjectTypeDef(Func<ValueSpec, TypeMap, string> printDeclaration, Func<TypeMap, ValueSpec, string> print) : ITypeDef<object>
{
    public DataType DataType => DataType.Object;
    public string Name=> throw new NotSupportedException("not supported");
    public Func<object, string> PrintObj => throw new NotSupportedException("not supported");
    public Func<object, string> Print => throw new NotSupportedException("not supported");

    public Func<TypeMap, ValueSpec, string> PrintValues { get; } = print;

    public string GetDeclaration(ValueSpec spec, TypeMap map) => printDeclaration(spec, map);
}