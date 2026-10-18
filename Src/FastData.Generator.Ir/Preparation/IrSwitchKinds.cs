namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>The key categories a backend can dispatch with a native switch or match.</summary>
[Flags]
public enum IrSwitchKinds : byte
{
    /// <summary>Keep conditional dispatch.</summary>
    None = 0,
    /// <summary>Switch on integral keys.</summary>
    Integral = 1,
    /// <summary>Switch on character keys.</summary>
    Character = 2,
    /// <summary>Switch on string keys using ordinal equality.</summary>
    String = 4
}