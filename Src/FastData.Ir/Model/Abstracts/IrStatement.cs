using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Ir.Model.Abstracts;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "This marker distinguishes statement nodes from expression nodes in generic IR APIs.")]
[SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix", Justification = "The name intentionally follows the Ir-prefixed node family.")]
public interface IrStatement;