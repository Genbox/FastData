namespace Genbox.FastData.Internal.Structures;

internal class Step
{
    // The characters whose values are being determined in this step.
    public int ChangingCount;
    public uint[] Changing;

    // Exclusive upper bound for the _asso_values[c] of this step. A power of 2.
    public int AssoValueMax;

    // The characters whose values will be determined after this step.
    public bool[] Undetermined;

    // The keyword set partition after this step.
    public EquivalenceClass Partition;

    // The expected number of iterations in this step.
    public double ExpectedLower;
    public double ExpectedUpper;

    public Step? _next;
};