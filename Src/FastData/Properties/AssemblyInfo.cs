using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException.ThrowIf", Justification = "FastData targets netstandard2.0, where ArgumentNullException.ThrowIfNull is unavailable; explicit guards keep one implementation across targets.")]
[assembly: InternalsVisibleTo("FastData.Testbed")]
[assembly: InternalsVisibleTo("FastData.InternalShared")]
[assembly: InternalsVisibleTo("FastData.Generator")]
[assembly: InternalsVisibleTo("FastData.TestHarness.Runner")]