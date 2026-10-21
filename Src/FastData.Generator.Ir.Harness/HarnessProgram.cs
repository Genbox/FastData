using Genbox.FastData.Generator.Ir.Prepared;

namespace Genbox.FastData.Generator.Ir.Harness;

internal sealed class HarnessProgram
{
    public List<PreparedData> Data { get; } = [];
    public List<PreparedFunction> Functions { get; } = [];
}